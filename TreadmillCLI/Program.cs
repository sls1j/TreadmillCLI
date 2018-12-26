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
      string comPort = args[0];
      ITreadmillProxy proxy = null;
      if (args.Length > 1 && args[1] == "-s")
        proxy = new TreadmillSimulatorProxy();
      else if (args.Length > 1 && args[1] == "-c")
        proxy = new TreadmillProxy(comPort);
      else
        proxy = new TreadmillProxyUdp(int.Parse(comPort));

      ConsoleController controller = new ConsoleController(proxy);
      controller.Start();
    }
  }
}
