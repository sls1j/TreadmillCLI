using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TreadmillCLI
{
  class ConsoleController
  {
    private ITreadmillProxy _treadmill;
    private DateTime _startTime;
    private double _totalMeters = 0.0;
    private  double _smoothSeconds = 0.0;
    private RollingAverage _secondsAvg;
    private bool _running = false;
    private bool _haveTargetPace = false;
    private TimeSpan _targetPace = TimeSpan.FromMinutes(8);
    private List<string> _display;
    private List<RunLine> _lines;
    private AutoResetEvent _haveNewRecord;
    private bool _inError;
    private volatile bool _heartbeat = false;
    private DateTime _errorStartTime;
    private ManualResetEvent _finished;

    public ConsoleController(ITreadmillProxy treadmill)
    {
      _running = false;
      _display = new List<string>();
      _lines = new List<RunLine>();
      _finished = new ManualResetEvent(false);
      _haveNewRecord = new AutoResetEvent(false);
      _secondsAvg = new RollingAverage(3);
      _treadmill = treadmill ?? throw new ArgumentNullException(nameof(treadmill));
      _treadmill.OnOdometer = HandleOnOdometer;
      _treadmill.OnError = HandleOnError;
      _treadmill.OnPing = HandleOnPing;
      _treadmill.OnValue = HandleOnValue;

      Directory.CreateDirectory(@"c:\temp");

      try
      {
        File.Delete(@"c:\temp\tread_value.csv");
      }
      catch(Exception ex)
      {

      }
    }

    private void HandleOnValue(double time, double value)
    {
      string msg = $"{time} {value}{Environment.NewLine}";      
      File.AppendAllText( @"c:\temp\tread_value.csv", $"{time} {value}{Environment.NewLine}");
    }

    public void Start()
    {
      Console.Clear();
      ThreadPool.QueueUserWorkItem(DoWork, null);
      _finished.WaitOne();
    }

    private void HandleOnError(bool error)
    {
      if (error)
      {
        if (!_inError)
        {
          _errorStartTime = DateTime.Now;
          _inError = true;
        }
      }
      else
      {
        _inError = false;
        if (_running && _lines.Count > 0)
        {
          double minutesInError = (DateTime.Now - _errorStartTime).TotalMinutes;
          // add correction estimate to distance to compensate for monitor not working.
          lock (_lines)
          {
            if (_lines.Count > 0)
              _totalMeters += 1609 * minutesInError / _lines.Last().Pace.TotalMinutes; // in meters
          }
        }
      }
    }

    private void DoWork(object state)
    {
      while (true)
      {
        _haveNewRecord.WaitOne(250);
        DrawDisplay();

        if (Console.KeyAvailable)
        {
          switch (Console.ReadKey().KeyChar)
          {
            case ' ':
            case '.':
              if (_running)
              {
                _running = false;
                lock( _lines )
                {
                  if (_lines.Count > 0)
                  {
                    File.WriteAllText($"Run-{DateTime.Now.ToString("yyyyMMdd-hhmm")}.json", JsonConvert.SerializeObject(_lines, Formatting.Indented));
                  }
                }
              }
              else
              {
                _running = true;
                lock (_lines)
                {
                  lock (_display)
                  {
                    _display.Clear();
                    _lines.Clear();
                  }
                }
                _startTime = DateTime.Now;
                _totalMeters = 0;
                Console.Clear();
              }
              // toggle start/stop
              break;
            case 's':
              // set the pace;
              Console.WriteLine("Enter the pace: ");
              string paceStr = Console.ReadLine();
              string[] parts = paceStr.Split('.');
              _targetPace = new TimeSpan(0, int.Parse(parts[0]), int.Parse(parts[1]));
              _haveTargetPace = true;
              break;
            case 'r':
              _treadmill.Reset();
              Console.Clear();
              Console.WriteLine("Resetting");
              Thread.Sleep(500);
              Console.Clear();
              break;
            case 'q':
              _treadmill.Stop();
              _finished.Set();
              return;
          }
        }
      }
    }

    private void HandleOnPing()
    {
      _heartbeat = !_heartbeat;
    }

    private void HandleOnOdometer(double meters, double seconds)
    {
      if (!_running)
        return;

      double miles = meters / 1609.0;
      _smoothSeconds = _secondsAvg.Add(seconds);

      System.Diagnostics.Debug.WriteLine($"{meters}m {seconds}s");

      DateTime now = DateTime.Now;
      RunLine line = new RunLine();
      line.Time = now;
      line.Duration = now - _startTime;
      _totalMeters += meters;
      line.Distance = _totalMeters;
      line.DistanceMiles = line.Distance / 1609.0;
      line.Pace = TimeSpan.FromMinutes(_smoothSeconds / (60 * miles));
      line.Speed = 3600 * miles / _smoothSeconds;
      line.RawPace = TimeSpan.FromMinutes(seconds / (60 * miles));
      line.RawSpeed = 3600 * miles / seconds;

      if (_haveTargetPace)
      {
        line.TargetPace = _targetPace;
        double expectedDistanceMiles = line.Duration.TotalMinutes / line.TargetPace.Value.TotalMinutes;
        double expectedDistanceMeters = expectedDistanceMiles * 1609.0;
        line.Difference = _totalMeters - expectedDistanceMeters;
        line.DifferenceMiles = line.DistanceMiles - expectedDistanceMiles;
        line.TimeDifference = TimeSpan.FromSeconds(line.DifferenceMiles * line.Pace.TotalSeconds);
        if (line.Pace < line.TargetPace && line.Difference < 0)
        {
          double speedDiff = 1 / line.TargetPace.Value.TotalMinutes - 1 / line.Pace.TotalMinutes;
          line.CatchupTime = TimeSpan.FromMinutes(line.DifferenceMiles / speedDiff);
          Debug.WriteLine($"Catchup: {line.CatchupTime.Value}");
        }
      }
      lock (_lines)
        _lines.Add(line);

      lock (_display)
      {
        while (_display.Count > Math.Max(1, Console.WindowHeight - 5))
          _display.RemoveAt(0);
        _display.Add(line.ToDisplayLine(Console.WindowWidth));
      }
      _haveNewRecord.Set();
    }

    public void DrawDisplay()
    {
      Console.SetCursorPosition(0, 0);
      string runStatus = (_running) ? "Running" : "Stopped";
      string errorStatus = (_inError) ? "Error  " : "Working";
      string heartbeat = (_heartbeat) ? "*" : ".";
      Console.Write($"{DateTime.Now} "); // clock

      Console.Write($"{runStatus} "); // current run stats
      if (_inError)
        Console.ForegroundColor = ConsoleColor.Red;
      else
        Console.ForegroundColor = ConsoleColor.Green;
      Console.Write($"{errorStatus} ");
      Console.ForegroundColor = ConsoleColor.White;
      Console.Write(heartbeat);
      Console.WriteLine();
      string targetPace = (_haveTargetPace) ? RunLine.FormatTime(_targetPace) : "None";
      Console.WriteLine($"Target Pace: {targetPace}");

      // the history
      lock (_display)
      {
        for (int i = 0; i < _display.Count; i++)
        {
          Console.Write(_display[i]);
        }
      }
    }
  }
}
