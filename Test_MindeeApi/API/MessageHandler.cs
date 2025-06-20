using Telegram.Bot;
using Telegram.Bot.Types;
using Test_MindeeApi.Service;
using Test_MindeeApi.State;

namespace Test_MindeeApi.API;


public interface IMessageHandler
{
    Task HandleAsync(Message message, CancellationToken token);
}

public class MessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly IPolicyGenerationService _policyGenerationService;
    private readonly SessionStorage _sessions;
    private readonly IOpenAiService _openAiService;
    public MessageHandler(ITelegramBotClient bot,IOpenAiService openAiService ,IPolicyGenerationService policyGenerationService, SessionStorage sessions)
    {
        _bot = bot;
        _sessions = sessions;
        _policyGenerationService = policyGenerationService; 
        _openAiService = openAiService;
    }

    public async Task HandleAsync(Message message, CancellationToken token)
    {
        var chatId = message.Chat.Id;
        var session = _sessions.GetOrCreate(chatId);
        var text = message.Text?.Trim().ToLower();

        switch (text)
        {
            case "/start":
                _sessions.Clear(chatId);
                await _bot.SendMessage(chatId, "Привіт! Надішліть фото паспорта (передню частину) 📷",  cancellationToken: token);
                break;

            case "так" when session.State == ConversationState.WaitingForPriceConfirmation:
            {
                session.State = ConversationState.Completed;

                var policyText = _policyGenerationService.GeneratePolicyText(session);
                var tempFilePath = await _policyGenerationService.SavePolicyToFileAsync(chatId, policyText);


                await using var fs = File.OpenRead(tempFilePath);
                await _bot.SendDocument(chatId, new InputFileStream(fs, "policy.txt"), "✅ Ось ваш страховий поліс", cancellationToken: token);
                await _bot.SendMessage(chatId, "✅ Готово! Якщо у вас виникнуть додаткові питання — просто напишіть. Гарного дня! 🌞", cancellationToken: token);
                break;
            }


            case "ні" when session.State == ConversationState.WaitingForPriceConfirmation:
                await _bot.SendMessage(chatId, "Вибач, але ціна фіксована 😕",  cancellationToken: token);
                break;

            default:
                if (!string.IsNullOrEmpty(text))
                {
                    var aiReply = await _openAiService.GetChatCompletionAsync(text, token);
                    await _bot.SendMessage(chatId, aiReply, cancellationToken: token);
                }
                else
                {
                    await _bot.SendMessage(chatId, "Надішліть текст або фото документа 🧾", cancellationToken: token);
                }
                break;
        }
    }

}