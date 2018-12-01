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
            if ( args.Length > 1 && args[1] == "-s" )
                proxy = new TreadmillSimulatorProxy();
            else
              proxy = new TreadmillProxy(comPort);

            ConsoleController controller = new ConsoleController(proxy);
            controller.Start();
        }
    }
}
