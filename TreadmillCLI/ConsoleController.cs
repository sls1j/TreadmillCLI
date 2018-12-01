using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TreadmillCLI
{
    class ConsoleController
    {
        private TreadmillProxy _treadmill;
        private DateTime _startTime;
        private double _totalMeters = 0.0;
        private bool _running = false;
        private bool _haveTargetPace = false;
        private TimeSpan _targetPace = TimeSpan.FromMinutes(8);
        private List<string> _display;
        private List<RunLine> _lines;
        private AutoResetEvent _haveNewRecord;
        private bool _inError;
        private DateTime _errorStartTime;
        private ManualResetEvent _finished;

        public ConsoleController(TreadmillProxy treadmill)
        {
            _display = new List<string>();
            _lines = new List<RunLine>();
            _finished = new ManualResetEvent(false);
            _haveNewRecord = new AutoResetEvent(false);
            _treadmill = treadmill ?? throw new ArgumentNullException(nameof(treadmill));
            _treadmill.OnOdometer = HandleOnOdometer;
            _treadmill.OnError = HandleOnError;            
        }

        public void Start()
        {
            ThreadPool.QueueUserWorkItem(DoWork, null);
            _finished.WaitOne();
        }

        private void HandleOnError(bool error)
        {
            if (error && !_inError)
            {
                _errorStartTime = DateTime.Now;
                _inError = true;
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
                            _totalMeters += minutesInError / _lines.Last().Pace.TotalMinutes * 1609.0; // in meters
                    }
                }
            }
        }

        private void DoWork(object state)
        {
            while (true)
            {
                if (_haveNewRecord.WaitOne(250))
                    DrawDisplay();
                else
                {
                    if (Console.KeyAvailable)
                    {
                        switch (Console.ReadKey().KeyChar)
                        {
                            case ' ':
                            case '.':
                                if (_running)
                                {
                                    _running = false;
                                }
                                else
                                {
                                    _running = true;
                                    _startTime = DateTime.Now;
                                    _totalMeters = 0;
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
                            case 'q':
                                _treadmill.Stop();
                                _finished.Set();
                                return;
                        }
                    }
                }
            }
        }

        private void HandleOnOdometer(double meters, double seconds)
        {
            if (!_running)
                return;

            DateTime now = DateTime.Now;
            RunLine line = new RunLine();
            line.Time = now;
            line.Duration = now - _startTime;
            line.Distance = (_totalMeters += meters);
            line.DistanceMiles = line.Distance / 1609.0;
            line.Pace = TimeSpan.FromMinutes(seconds / (60 * line.DistanceMiles));
            line.Speed = 3600 * line.DistanceMiles / seconds;
            if (_haveTargetPace)
            {
                line.TargetPace = _targetPace;
                double expectedDistanceMiles = line.Duration.TotalMinutes / line.Pace.TotalMinutes;
                double expectedDistanceMeters = expectedDistanceMiles * 1609.0;
                double diffMeters = _totalMeters - expectedDistanceMeters;
                double diffMiles = line.DistanceMiles - expectedDistanceMiles;
                double diffSeconds = diffMiles * line.Pace.TotalSeconds;
                if (line.Pace > line.TargetPace)
                {
                    line.CatchupTime = TimeSpan.FromMinutes(diffMiles * (line.Pace - line.TargetPace).Value.TotalMinutes);
                }
            }
            lock (_lines)
                _lines.Add(line);

            lock (_display)
            {
                if (_display.Count > 10)
                    _display.RemoveAt(0);
                _display.Add(line.ToDisplayLine(Console.WindowWidth));
            }
            _haveNewRecord.Set();
        }

        public void DrawDisplay()
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"{DateTime.Now}"); // clock
            Console.WriteLine($""); // current run stats
            Console.WriteLine();
            // the history
            lock (_display)
            {
                for (int i = 0; i < _display.Count; i++)
                {
                    Console.WriteLine(_display[i]);
                }
            }
        }
    }
}
