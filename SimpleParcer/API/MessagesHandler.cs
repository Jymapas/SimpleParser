using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using SimpleParser.Constants;

namespace SimpleParser.API
{
    internal class MessagesHandler
    {
        private ITelegramBotClient _botClient;
        private CancellationToken _cancellationToken;

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            _botClient = botClient;
            _cancellationToken = cancellationToken;
            if ((update.Type != UpdateType.Message) || (update.Message!.Type != MessageType.Text)) return;
            var message = update?.Message;
            var messageText = message.Text.Trim().ToLower();

            if (!messageText.Equals(Commands.Announcement))
            {
                return;
            }
        }
    }
}
