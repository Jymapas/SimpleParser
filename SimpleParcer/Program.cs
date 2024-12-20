using SimpleParser.API;
using SimpleParser.Helpers;

namespace SimpleParser
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            Connect connect = new();
            await connect.Start();

            // Console.ReadLine();
            Console.WriteLine("Unexpected end of program.");
        }
    }
}
