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
        private List<string> _lines;
        private AutoResetEvent _haveNewRecord;
        private DateTime _errorStartTime;
        private TimeSpan _currentPace;

        public ConsoleController(TreadmillProxy treadmill)
        {
            _haveNewRecord = new AutoResetEvent(false);
            _treadmill = treadmill ?? throw new ArgumentNullException(nameof(treadmill));
            _treadmill.OnOdometer = HandleOnOdometer;
            _treadmill.OnError = HandleOnError;
            ThreadPool.QueueUserWorkItem(DoWork, null);
        }

        private void HandleOnError(bool error)
        {
            if (error)
                _errorStartTime = DateTime.Now;
            else
            {
                if ( _running)
                {
                    double minutesInError = (DateTime.Now - _errorStartTime).TotalMinutes;
                    // add correction estimate to distance to compensate for monitor not working.
                    _totalMeters += minutesInError / _currentPace.TotalMinutes * 1609.0; // in meters
                }
            }
        }

        private void DoWork(object state)
        {
            while(true)
            {
                if (_haveNewRecord.WaitOne(250))
                    DrawDisplay();
                else
                {
                    if ( Console.KeyAvailable )
                    {
                        switch( Console.ReadKey().KeyChar )
                        {
                            case ' ':
                            case '.':
                                if ( _running )
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
                        }
                    }
                }
            }
        }

        private void HandleOnOdometer(double meters, double seconds)
        {
            if (!_running)
                return;

            _totalMeters += meters;

            TimeSpan time = DateTime.Now - _startTime;
            double totalMiles = _totalMeters / 1609.0;
            TimeSpan pace = TimeSpan.FromMinutes(seconds / (60 * totalMiles));
            double speed = 3600 * totalMiles / seconds;
            StringBuilder sb = new StringBuilder();
            sb.Append($"{time} {totalMiles:0.000}mi {_totalMeters}m ");
            if (_haveTargetPace)
            {
                double expectedDistanceMiles = time.TotalMinutes / pace.TotalMinutes;
                double expecteddistanceMeters = expectedDistanceMiles * 1609.0;
                double diffMeters = _totalMeters - expecteddistanceMeters;
                double diffMiles = totalMiles - expectedDistanceMiles;
                double diffSeconds = diffMiles * pace.TotalSeconds;
                sb.Append($"{diffSeconds:0.0} {diffMeters}m");
            }

            if (_lines.Count > 10)
                _lines.RemoveAt(0);
            _lines.Add(sb.ToString());

            DrawDisplay();
        }

        public void DrawDisplay()
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"{DateTime.Now}");
            Console.WriteLine();
            for (int i = 0; i < _lines.Count; i++)
            {
                Console.WriteLine(_lines[i]);
            }
        }
    }

    class Run
    {
        public DateTime start;
    }
}
