using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ACE.Engine;

namespace ACE
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (var Engine = new AceEngine())
                {
                    Engine.ExecuteOnce();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.StackTrace);
                Console.Out.Flush();
            }
        }
    }
}
