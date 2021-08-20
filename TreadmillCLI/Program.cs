using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreadmillCLI
{
  class Program
  {
    static void Main(string[] args)
    {
      int i = 0;
      ITreadmillProxy proxy = null;
      switch (args[i])
      {
        case "-c":
          string comPort = args[++i];
          proxy = new TreadmillProxy(comPort);
          break;
        case "-u":
          int udpPort = int.Parse(args[++i]);
          proxy = new TreadmillProxyUdp(udpPort);
          break;
        case "-s":
          proxy = new TreadmillSimulatorProxy();
          break;
      }

      ConsoleController controller = new ConsoleController(proxy);
      controller.Start();
    }
  }
}
