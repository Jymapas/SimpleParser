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

        internal async Task Start()
        {
            _botToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? string.Empty;

            if (_botToken.Equals(string.Empty))
            {
                Console.WriteLine(ServiceLines.TgTokenError);
                return;
            }

            ITelegramBotClient bot = new TelegramBotClient(_botToken);
            using CancellationTokenSource cts = new();
            _cancellationToken = cts.Token;
            
            await SetBotCommands(bot);
            
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
            {
                try
                {
                    var now = DateTime.Now;
                    var nextRun = CalculateNextRunTime(now);

                    var delay = nextRun - now;
                    Console.WriteLine($"Следующее срабатывание: {nextRun} через {delay}");

                    // Ожидание до следующего срабатывания
                    await Task.Delay(delay);

                    // Отправка поста
                    await _postScheduler.SendPostAsync(bot, channelId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при выполнении задачи: {ex.Message}");
                }
            }
        }
        
        private DateTime CalculateNextRunTime(DateTime now)
        {
            var nextRun = now.Date.AddHours(12); // Текущее время 12:00

            switch (now.DayOfWeek)
            {
                case DayOfWeek.Monday when now.TimeOfDay < TimeSpan.FromHours(12):
                case DayOfWeek.Thursday when now.TimeOfDay < TimeSpan.FromHours(12):
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
