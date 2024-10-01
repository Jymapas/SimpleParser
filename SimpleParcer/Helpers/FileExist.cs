using SimpleParser.Constants;

namespace SimpleParser.Helpers;

public class FileExist
{
    public static bool CheckExistence()
    {
        if (File.Exists(Paths.BotToken))
        {
            return true;
        }

        Console.WriteLine("File with BotToken doesn't exist!");
        return false;
    }
}