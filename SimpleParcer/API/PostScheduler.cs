using SimpleParser.Constants;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SimpleParser.API
{
    internal class PostScheduler
    {
        private readonly Func<DateTime, IPostReader> _postReaderFactory;

        public PostScheduler(Func<DateTime, IPostReader> postReaderFactory)
        {
            _postReaderFactory = postReaderFactory;
        }

        public async Task SendPost(ChatId channelId, MessagesSender messagesSender)
        {
            var postReader = _postReaderFactory(DateTime.Now);
            var postContent = await postReader.GetAnnounceAsync();

            if (postContent == ServiceLines.ReceivingPostError)
            {
                Console.WriteLine(postContent);
                return;
            }
            
            await messagesSender.SendAnnouncement(channelId, postContent);
        }
    }
}
