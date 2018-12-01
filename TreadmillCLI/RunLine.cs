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
        internal double DifferenceMiles;

        public RunLine()
        {

        }

        public string ToDisplayLine(int consoleWidth)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{FormatTime(Duration),6}");
            sb.Append($" {DistanceMiles,6:0.000}mi");
            sb.Append($" {Distance,6:0}m ");
            sb.Append($" {FormatTime(Pace),5}");
            sb.Append($" {Speed,4:0.0}MPH");
            if (TargetPace.HasValue)
            {
                sb.Append($" {Difference.Value,5:0}m {FormatTime(TimeDifference.Value),5}");

                if (CatchupTime.HasValue)
                {
                    sb.Append($" CatchUp:{FormatTime(CatchupTime.Value):,5}");
                }
            }

            int addExtra = consoleWidth - sb.Length;
            if (addExtra > 0)
                sb.Append(new string(' ', addExtra));

            return sb.ToString();
        }

        public static string FormatTime(TimeSpan time)
        {
            bool isNeg = time.TotalHours < 0;
            if (isNeg)
                time = TimeSpan.FromSeconds(time.TotalSeconds * -1);

            StringBuilder sb = new StringBuilder();
            if (isNeg)
                sb.Append("-");

            if ( time.TotalHours >= 1.0)
                sb.Append($"{Math.Truncate(time.TotalHours):00}");

            sb.Append($"{time.Minutes:00}:{time.Seconds:00}");

            return sb.ToString();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
