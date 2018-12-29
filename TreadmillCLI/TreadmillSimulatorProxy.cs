using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TreadmillCLI
{
  class TreadmillSimulatorProxy : ITreadmillProxy
  {
    public ErrorEvent OnError { get; set; }
    public OdometerEvent OnOdometer { get; set; }
    public PingEvent OnPing { get; set; }

    private ManualResetEvent _quit;

    public TreadmillSimulatorProxy()
    {
      _quit = new ManualResetEvent(false);
      ThreadPool.QueueUserWorkItem(o =>
      {
        while (true)
        {
          if (_quit.WaitOne(800))
          {
            return;
          }

          if (OnOdometer != null)
          {
            OnOdometer(2.864, 0.8);
          }

          if (OnPing != null)
            OnPing();
        }
      });
    }

    public void Stop()
    {
      _quit.Set();
    }

    public void Reset()
    {
      
    }
  }
}
