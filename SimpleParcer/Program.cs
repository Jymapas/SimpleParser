using SimpleParser.API;

namespace SimpleParser
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Connect connect = new();
            connect.Start();

            Console.ReadLine();
        }
    }
}
