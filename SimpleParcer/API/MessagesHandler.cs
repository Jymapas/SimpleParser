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
        private bool _isPreviousRequest;
        private IPostReader _reader;
        private MessagesSender _messagesSender;

        public MessagesHandler(MessagesSender messagesSender) => _messagesSender = messagesSender;
        
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

            if (command.Equals(Commands.Announcement, StringComparison.OrdinalIgnoreCase))
            {
                await HandleAnnouncementCommand(chatId, messageParts);
            }
            else if (command.Equals(Commands.Recent, StringComparison.OrdinalIgnoreCase))
            {
                await HandleRecentCommand(chatId);
            }
            else
            {
                Console.WriteLine(ServiceLines.UnknownCommand);
                await _messagesSender.SendTextMessage(chatId, ServiceLines.UnknownCommand);
            }
        }

        private async Task HandleAnnouncementCommand(ChatId chatId, string[] messageParts)
        {
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
                    _isPreviousRequest = true;
                    await _messagesSender.SendTextMessage(chatId, ServiceLines.ArgumentError);
                    return;
                }

                _reader = new LjPostReader(parsedDate);
            }
            else
            {
                _reader = new LjPostReader();
            }

            var announceSource = await _reader.GetAnnounceAsync();
            await _messagesSender.SendAnnouncement(chatId, announceSource, _isPreviousRequest);
        }

        private async Task HandleRecentCommand(ChatId chatId)
        {
            _isPreviousRequest = true;

            var now = DateTime.Now;
            var lastPostDate = now.DayOfWeek switch
            {
                DayOfWeek.Monday => now,
                DayOfWeek.Tuesday => now.AddDays(-1),
                DayOfWeek.Wednesday => now.AddDays(-2),
                DayOfWeek.Thursday => now,
                DayOfWeek.Friday => now.AddDays(-1),
                DayOfWeek.Saturday => now.AddDays(-2),
                DayOfWeek.Sunday => now.AddDays(-3),
                _ => now,
            };

            _reader = new LjPostReader(lastPostDate);

            var announceSource = await _reader.GetAnnounceAsync();
            await _messagesSender.SendAnnouncement(chatId, announceSource);
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(JsonConvert.SerializeObject(exception));
        }
    }
}
