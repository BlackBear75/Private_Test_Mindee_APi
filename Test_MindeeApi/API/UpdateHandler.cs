using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Test_MindeeApi.State;

namespace Test_MindeeApi.API;

public class UpdateHandler
{
    private readonly IMessageHandler _messageHandler;
    private readonly IPhotoHandler _photoHandler;
    private readonly SessionStorage _sessions;

    public UpdateHandler(
        IMessageHandler messageHandler,
        IPhotoHandler photoHandler,
        SessionStorage sessions) 
    {
        _messageHandler = messageHandler;
        _photoHandler = photoHandler;
        _sessions = sessions;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        if (update.Type != UpdateType.Message || update.Message is not { } message)
            return;

        var chatId = message.Chat.Id;
        var session = _sessions.GetOrCreate(chatId);

        if (session.State == ConversationState.WaitingForDataConfirmation)
        {
            await _photoHandler.HandleAsync(message, token);
            return;
        }

        if (message.Photo != null)
        {
            await _photoHandler.HandleAsync(message, token);
        }
        else if (message.Text != null)
        {
            await _messageHandler.HandleAsync(message, token);
        }
    }
}