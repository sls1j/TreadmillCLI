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
            TreadmillProxy proxy = new TreadmillProxy(comPort);
            ConsoleController controller = new ConsoleController(proxy);            
        }
    }
}
