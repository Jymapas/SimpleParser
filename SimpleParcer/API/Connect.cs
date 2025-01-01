using Microsoft.Extensions.Configuration;
using SimpleParser.Constants;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace SimpleParser.API
{
    internal class Connect
    {
        private readonly MessagesHandler _messagesHandler = new();
        private string _botToken;
        private CancellationToken _cancellationToken;
        private PostScheduler _postScheduler;

        internal async Task Start()
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            var botConfig = config.GetSection("BotConfig").Get<BotConfig>();
            _botToken = botConfig.BotToken;
            var channelId = botConfig.ChannelId;

            if (_botToken.Equals(string.Empty))
            {
                Console.WriteLine(ServiceLines.TgTokenError);
                return;
            }

            ITelegramBotClient bot = new TelegramBotClient(_botToken);

            _postScheduler = new PostScheduler(d => new LjPostReader(d));

            using CancellationTokenSource cts = new();
            _cancellationToken = cts.Token;

            await SetBotCommands(bot);

            if (!channelId.Equals(string.Empty))
            {
                _ = StartScheduledTask(bot, channelId);
            }
            else
            {
                Console.WriteLine(ServiceLines.GetChatIdError);
            }

            var receiverOptions = new ReceiverOptions();

            bot.StartReceiving(
                _messagesHandler.HandleUpdateAsync,
                _messagesHandler.HandleErrorAsync,
                receiverOptions,
                _cancellationToken
            );

            await Task.Delay(Timeout.Infinite, _cancellationToken);
        }

        private async Task SetBotCommands(ITelegramBotClient bot)
        {
            var commands = new[]
            {
                new BotCommand
                {
                    Command = Commands.Announcement,
                    Description = Commands.AnnouncementDescription,
                },
                new BotCommand
                {
                    Command = Commands.Recent,
                    Description = Commands.RecentDescription,
                },
            };

            await bot.SetMyCommands(
                commands,
                BotCommandScope.AllPrivateChats(),
                cancellationToken: _cancellationToken
            );
        }

        private async Task StartScheduledTask(ITelegramBotClient bot, ChatId channelId)
        {
            while (true)
                try
                {
                    var now = DateTime.Now;
                    var nextRun = CalculateNextRunTime(now);

                    var delay = nextRun - now;
                    Console.WriteLine($"Следующее срабатывание: {nextRun} через {delay}");

                    // Ожидание до следующего срабатывания
                    await Task.Delay(delay);

                    // Отправка поста
                    await _postScheduler.SendPost(bot, channelId, _cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine($"Операция отменена: {e.Message}");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Ошибка при выполнении задачи: {e.Message}");
                }
        }

        private DateTime CalculateNextRunTime(DateTime now)
        {
            var postTime = new TimeSpan(12, 00, 0);
            var nextRun = now.Date + postTime;

            switch (now.DayOfWeek)
            {
                case DayOfWeek.Monday when now.TimeOfDay < postTime:
                case DayOfWeek.Thursday when now.TimeOfDay < postTime:
                    return nextRun;
                default:
                {
                    var daysToAdd = now.DayOfWeek switch
                    {
                        DayOfWeek.Monday => 3, // Следующий четверг
                        DayOfWeek.Tuesday => 2, // Следующий четверг
                        DayOfWeek.Wednesday => 1, // Следующий четверг
                        DayOfWeek.Thursday => 4, // Следующий понедельник
                        DayOfWeek.Friday => 3, // Следующий понедельник
                        DayOfWeek.Saturday => 2, // Следующий понедельник
                        DayOfWeek.Sunday => 1, // Следующий понедельник
                        _ => throw new InvalidOperationException("Неверный день недели"),
                    };
                    return nextRun.AddDays(daysToAdd);
                }
            }
        }
    }
}
