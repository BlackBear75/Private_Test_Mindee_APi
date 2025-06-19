using Serilog;
using Telegram.Bot;

namespace Test_MindeeApi.API;

public class ErrorHandler
{
    public Task HandleAsync(ITelegramBotClient bot, Exception ex, CancellationToken token)
    {
        Log.Error(ex, ex.Message);
        return Task.CompletedTask;
    }
}