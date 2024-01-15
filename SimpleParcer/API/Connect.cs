using SimpleParser.Helpers;
using SimpleParser.Constants;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace SimpleParser.API
{
    internal class Connect
    {
        private string _botToken;
        private MessagesHandler _messagesHandler = new();

        internal void Start()
        {
            _botToken = ReadHelper.GetFromTxt(Paths.BotToken);

            if (_botToken.Equals(string.Empty))
                return;

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
