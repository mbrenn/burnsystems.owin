using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace OwinTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Webserver starting");

            using (WebApp.Start<Startup>("http://localhost:8080"))
            {
                Console.WriteLine("Webserver started. http://localhost:8080, press key");
                Console.ReadKey();
            }
        }
    }
}
