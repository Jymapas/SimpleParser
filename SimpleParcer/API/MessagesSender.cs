using Newtonsoft.Json;
using SimpleParser.Constants;
using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SimpleParser.API
{
    public class MessagesSender
    {
        private readonly ITelegramBotClient _botClient;
        private readonly CancellationToken _cancellationToken;

        public MessagesSender(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            _botClient = botClient;
            _cancellationToken = cancellationToken;
        }
        
        /// <summary>
        ///     Отправка сообщений ботом
        /// </summary>
        /// <param name="id">Id пользователя, чата или канала, куда будет отправлено сообщение</param>
        /// <param name="text">Текст сообщения в HTML формате</param>
        public async Task SendTextMessage(ChatId id, string text)
        {
            await _botClient.SendMessage(
                id,
                text,
                ParseMode.Html,
                linkPreviewOptions: true,
                cancellationToken: _cancellationToken
            );
        }
        
        public async Task SendAnnouncement(ChatId chatId, string announceSource, bool isPreviousRequest = false)
        {
            if (announceSource == ServiceLines.ReceivingPostError)
            {
                await SendTextMessage(chatId, ServiceLines.ReceivingPostError);
                return;
            }

            var announce = new StringBuilder();
            announce.Append(ServiceLines.TgHead);
            announce.AppendLine();

            if (isPreviousRequest)
            {
                announce.Append(ServiceLines.PostWasUpdated);
                announce.Append(DateTime.Now.ToString(Format.HeadPostDateFormat));
                announce.AppendLine();
            }

            announce.AppendLine();
            announce.AppendLine(announceSource);

            await SendTextMessage(chatId, announce.ToString());
        }
    }
}
