using SimpleParser.API;

namespace SimpleParser
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var reader = new LjPostReader();
            var post = await reader.GetAnnounce();
            Console.WriteLine(post);
        }
    }
}
