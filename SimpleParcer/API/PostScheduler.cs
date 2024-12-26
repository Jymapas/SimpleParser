using SimpleParser.Constants;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SimpleParser.API
{
    internal class PostScheduler(IPostReader postReader, CancellationToken cancellationToken)
    {
        /// <summary>
        ///     Отправляет пост в указанный канал Telegram.
        /// </summary>
        /// <param name="bot">Экземпляр TelegramBotClient.</param>
        /// <param name="channelId">ID канала, куда отправляется сообщение.</param>
        /// <param name="date">Дата, для которой формируется пост.</param>
        public async Task SendPostAsync(ITelegramBotClient bot, ChatId channelId)
        {
            // Установить дату для получения поста
            var postContent = await postReader.GetAnnounceAsync();

            if (postContent == ServiceLines.ReceivingPostError)
            {
                Console.WriteLine("Ошибка получения поста.");
                return;
            }

            await bot.SendMessage(
                channelId,
                postContent,
                ParseMode.Html,
                linkPreviewOptions: true,
                cancellationToken: cancellationToken
            );
        }
    }
}
