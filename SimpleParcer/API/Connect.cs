using Microsoft.Extensions.Configuration;
using SimpleParser.Helpers;
using SimpleParser.Constants;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace SimpleParser.API
{
    internal class Connect
    {
        private string _botToken;
        private readonly MessagesHandler _messagesHandler = new();

        internal void Start()
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();
            
            var botConfig = config.GetSection("BotConfig").Get<BotConfig>();
            _botToken = botConfig.BotToken;

            if (_botToken.Equals(string.Empty))
            {
                Console.WriteLine(ServiceLines.TgTokenError);
                return;
            }

            ITelegramBotClient bot = new TelegramBotClient(_botToken);
            using CancellationTokenSource cts = new();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            bot.StartReceiving(
                _messagesHandler.HandleUpdateAsync,
                _messagesHandler.HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
        }
    }
}
