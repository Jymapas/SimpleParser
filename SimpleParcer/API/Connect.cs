using SimpleParser.Constants;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace SimpleParser.API
{
    internal class Connect
    {
        private readonly MessagesHandler _messagesHandler = new();
        private string _botToken;
        private CancellationToken _cancellationToken;

        internal async Task Start()
        {
            _botToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? string.Empty;

            if (_botToken.Equals(string.Empty))
            {
                Console.WriteLine(ServiceLines.TgTokenError);
                return;
            }

            ITelegramBotClient bot = new TelegramBotClient(_botToken);
            using CancellationTokenSource cts = new();
            _cancellationToken = cts.Token;
            
            await SetBotCommands(bot);
            
            var receiverOptions = new ReceiverOptions();

            bot.StartReceiving(
                _messagesHandler.HandleUpdateAsync,
                _messagesHandler.HandleErrorAsync,
                receiverOptions,
                _cancellationToken
            );

            await Task.Delay(Timeout.Infinite, _cancellationToken);
        }

        private async Task SetBotCommands(ITelegramBotClient bot)
        {
            var commands = new[]
            {
                new BotCommand
                {
                    Command = Commands.Announcement,
                    Description = Commands.AnnouncementDescription,
                },
                new BotCommand
                {
                    Command = Commands.Recent,
                    Description = Commands.RecentDescription,
                },
            };

            await bot.SetMyCommands(
                commands,
                BotCommandScope.AllPrivateChats(),
                cancellationToken: _cancellationToken
            );
        }
    }
}
