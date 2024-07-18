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
            if ((update.Type != UpdateType.Message) || (update.Message!.Type != MessageType.Text))
            {
                return;
            }
            var message = update?.Message;
            var messageText = message.Text.Trim().ToLower();
            var chatId = message.Chat.Id;

            if (!messageText.Equals(Commands.Announcement))
            {
                await SendMessage(chatId, ServiceLines.UnknownCommand);
                return;
            }

            var reader = new LjPostReader();
            var announceSource = await reader.GetAnnounce();

            var announce = announceSource == ServiceLines.ReceivingPostError
                ? announceSource
                : ServiceLines.TgHead + announceSource;

            await SendMessage(chatId, announce);
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        /// <summary>
        /// Отправка сообщений ботом
        /// </summary>
        /// <param name="id">Id пользователя, чата или канала, куда будет отправлено сообщение</param>
        /// <param name="text">Текст сообщения в HTML формате</param>
        private async Task SendMessage(ChatId id, string text)
        {
            await _botClient.SendTextMessageAsync(
                chatId: id,
                text: text,
                parseMode: ParseMode.Html,
                disableWebPagePreview: true,
                cancellationToken: _cancellationToken
            );
        }
    }
}
