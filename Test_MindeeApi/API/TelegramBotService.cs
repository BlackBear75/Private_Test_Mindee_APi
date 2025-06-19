using Telegram.Bot;

namespace Test_MindeeApi.API;

public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly UpdateHandler _updateHandler;
    private readonly ErrorHandler _errorHandler;

    public TelegramBotService(
        ITelegramBotClient botClient,
        UpdateHandler updateHandler,
        ErrorHandler errorHandler)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _errorHandler = errorHandler;
    }

    public async Task StartAsync()
    {
        _botClient.StartReceiving(
            _updateHandler.HandleAsync,
            _errorHandler.HandleAsync);
        
        await Task.Delay(-1);
    }
}