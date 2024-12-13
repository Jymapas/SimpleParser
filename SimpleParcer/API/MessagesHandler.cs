using Newtonsoft.Json;
using SimpleParser.Constants;
using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SimpleParser.API
{
    internal class MessagesHandler
    {
        private ITelegramBotClient _botClient;
        private CancellationToken _cancellationToken;
        private IPostReader _reader;
        private bool _isPreviousRequest = false; 

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            _botClient = botClient;
            _cancellationToken = cancellationToken;
            if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
            {
                return;
            }
            var message = update?.Message;
            var messageText = message.Text.Trim();
            var chatId = message.Chat.Id;

            var messageParts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = messageParts.First();

            if (!command.Equals(Commands.Announcement, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(ServiceLines.UnknownCommand);
                await SendMessage(chatId, ServiceLines.UnknownCommand);
                return;
            }

            if (messageParts.Length > 1)
            {
                var dateArgument = messageParts[1];
                if (!DateTime.TryParseExact(
                    dateArgument,
                    Format.DateArgument,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
                {
                    await SendMessage(chatId, ServiceLines.ArgumentError);
                    return;
                }

                _isPreviousRequest = true;
                _reader = new LjPostReader(parsedDate);
            }
            else
            {
                _reader = new LjPostReader();
            }

            var announceSource = await _reader.GetAnnounceAsync();
            
            var announce = new StringBuilder();
            if (announceSource == ServiceLines.ReceivingPostError)
            {
                await SendMessage(chatId, ServiceLines.ReceivingPostError);
                return;
            }
            
            announce.Append(ServiceLines.TgHead);
            announce.AppendLine();
            
            if (_isPreviousRequest)
            {
                announce.Append(ServiceLines.PostWasUpdated);
                announce.Append(DateTime.Now.ToString("dd.MM.yyyy."));
                announce.AppendLine();
            }
            
            announce.AppendLine();
            announce.AppendLine(announceSource);

            await SendMessage(chatId, announce.ToString());
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(JsonConvert.SerializeObject(exception));
        }

        /// <summary>
        ///     Отправка сообщений ботом
        /// </summary>
        /// <param name="id">Id пользователя, чата или канала, куда будет отправлено сообщение</param>
        /// <param name="text">Текст сообщения в HTML формате</param>
        private async Task SendMessage(ChatId id, string text)
        {
            await _botClient.SendTextMessageAsync(
                id,
                text,
                parseMode: ParseMode.Html,
                disableWebPagePreview: true,
                cancellationToken: _cancellationToken
            );
        }
    }
}
