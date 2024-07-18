using SimpleParser.API;

namespace SimpleParser
{
    internal class Program
    {
        private static Task Main(string[] args)
        {
            if (!Helpers.FileExist.CheckExistence())
            {
                return Task.CompletedTask;
            }
            
            Connect connect = new();
            connect.Start();

            Console.ReadLine();
            return Task.CompletedTask;
        }
    }
}
