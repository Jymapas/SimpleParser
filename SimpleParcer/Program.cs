using SimpleParser.API;
using SimpleParser.Helpers;

namespace SimpleParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (FileExist.CheckExistence())
            {
                Connect connect = new();
                connect.Start();
            }

            Console.ReadLine();
        }
    }
}
