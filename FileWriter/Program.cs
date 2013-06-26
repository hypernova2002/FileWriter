using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWriter
{
    public static class Program
    {
        static void Main(string[] args)
        {
            string a = Environment.GetCommandLineArgs().First();
            Console.WriteLine("a: {0}");
            Console.ReadLine();
        }
    }
}
