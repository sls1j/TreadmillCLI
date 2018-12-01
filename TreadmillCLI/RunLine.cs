using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreadmillCLI
{
    class RunLine
    {
        public DateTime Time;
        public TimeSpan Duration;
        public double Distance;
        public double DistanceMiles;
        public TimeSpan Pace;
        public double Speed;
        public TimeSpan? TargetPace;
        public double? Difference;
        public TimeSpan? TimeDifference;
        public TimeSpan? CatchupTime;
        

        public RunLine()
        {

        }

        public string ToDisplayLine(int consoleWidth)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{FormatTime(Duration):,6} {DistanceMiles:0.000,7}mi {Distance:7}m {FormatTime(Pace),5} {Speed:0.0,4}MPH");
            if (TargetPace.HasValue)
            {
                sb.Append($" {Difference:0,5}m {FormatTime(TimeDifference.Value):,5}");

                if ( CatchupTime.HasValue )
                {
                    sb.Append($" CatchUp:{FormatTime(CatchupTime.Value):,5}");
                }
            }

            int addExtra = consoleWidth - sb.Length;
            if (addExtra > 0)
                sb.Append(new string(' ', addExtra));

            return sb.ToString();
        }

        private string FormatTime(TimeSpan time)
        {
            if ( time.TotalHours > 0)
            {
                return $"{Math.Truncate(time.TotalHours)}:{time.Minutes:00}:{time.Seconds:00}";
            }
            else
            {
                return $"{time.Minutes:00}:{time.Seconds:00}";
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
