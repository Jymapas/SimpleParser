using SimpleParser.Constants;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SimpleParser.API
{
    internal class PostScheduler
    {
        private readonly Func<DateTime, IPostReader> _postReaderFactory;

        public PostScheduler(Func<DateTime, IPostReader> postReaderFactory)
        {
            _postReaderFactory = postReaderFactory;
        }

        public async Task SendPost(ITelegramBotClient bot, ChatId channelId, CancellationToken cancellationToken)
        {
            var postReader = _postReaderFactory(DateTime.Now);
            var postContent = await postReader.GetAnnounceAsync();

            if (postContent == ServiceLines.ReceivingPostError)
            {
                Console.WriteLine(postContent);
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
