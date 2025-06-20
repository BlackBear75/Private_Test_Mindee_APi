using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Test_MindeeApi.Service;
using Test_MindeeApi.State;

namespace Test_MindeeApi.API;

public interface IPhotoHandler
{
    Task HandleAsync(Message message, CancellationToken token);
}
public class PhotoHandler : IPhotoHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionStorage _sessions;
    private readonly DocumentProcessingService _docs;
    private readonly IOpenAiService _openAiService;

    public PhotoHandler(ITelegramBotClient bot, SessionStorage sessions, DocumentProcessingService docs, IOpenAiService openAiService)
    {
        _bot = bot;
        _sessions = sessions;
        _docs = docs;
        _openAiService = openAiService;
    }

    public async Task HandleAsync(Message message, CancellationToken token)
    {
        var chatId = message.Chat.Id;
        var session = _sessions.GetOrCreate(chatId);

        if (message.Type == MessageType.Photo)
        {
            var fileId = message.Photo.Last().FileId;
            var file = await _bot.GetFile(fileId, token);

            await using var ms = new MemoryStream();
            await _bot.DownloadFile(file.FilePath, ms, token);
            ms.Position = 0;

            switch (session.State)
            {
                case ConversationState.WaitingForPassport:
                case ConversationState.WaitingForPassportFront:
                    await HandlePassportFrontAsync(session, fileId, chatId, token);
                    break;

                case ConversationState.WaitingForPassportBack:
                    await HandlePassportBackAsync(session, ms, chatId, token);
                    break;

                case ConversationState.WaitingForVehicleDoc:
                    await HandleVehicleDocAsync(session, ms, chatId, token);
                    break;

                case ConversationState.WaitingForDataConfirmation:
                    await _bot.SendMessage(chatId, "❗ Очікується відповідь 'так' або 'ні'.", cancellationToken: token);
                    break;
            }
        }
        else if (message.Type == MessageType.Text)
        {
            var text = message.Text?.Trim().ToLowerInvariant();
            if (session.State == ConversationState.WaitingForDataConfirmation && text is not null)
            {
                await HandleConfirmationAsync(session, text, chatId, token);
            }
            else if (!string.IsNullOrEmpty(text))
            {
                var aiReply = await _openAiService.GetChatCompletionAsync(text, token);
                await _bot.SendMessage(chatId, aiReply, cancellationToken: token);
            }
            else
            {
                await _bot.SendMessage(chatId, "Очікую на фото паспорта або техпаспорта.", cancellationToken: token);
            }
        }

    }

    private async Task HandlePassportFrontAsync(UserSession session, string fileId, long chatId, CancellationToken token)
    {
        session.PassportFrontFileId = fileId;
        session.State = ConversationState.WaitingForPassportBack;
        await _bot.SendMessage(chatId, "✅ Передня сторона паспорта отримана. Надішліть задню сторону.", cancellationToken: token);
        
    }

    private async Task HandlePassportBackAsync(UserSession session, Stream backStream, long chatId, CancellationToken token)
    {
        session.PassportBackFileId = "[in-memory]";

        var front = await GetFileStream(session.PassportFrontFileId, token);
        
        await _bot.SendMessage(chatId, "📄 Документи отримано. Виконуємо розпізнавання, зачекайте кілька секунд...", cancellationToken: token);
        var result = await _docs.ProcessDriverLicenseFromFrontAndBackAsync(front, backStream);
        session.Passport = result;

        session.State = ConversationState.WaitingForDataConfirmation;
        session.CurrentConfirmationStep = ConfirmationStep.Passport;

        await _bot.SendMessage(chatId, $"👤 Розпізнано паспорт:\n{result}\nЧи правильно? (так / ні)", cancellationToken: token);
    }

    private async Task HandleVehicleDocAsync(UserSession session, Stream photoStream, long chatId, CancellationToken token)
    {
        var techPassport = await _docs.ProcessTechPassportFromImageAsync(photoStream);
        session.TechPassport = techPassport;

        session.State = ConversationState.WaitingForDataConfirmation;
        session.CurrentConfirmationStep = ConfirmationStep.VehicleDoc;

        await _bot.SendMessage(chatId, $"🚗 Розпізнано Техпаспорт:\n{techPassport}\nЧи правильно? (так / ні)", cancellationToken: token);
    }

    private async Task HandleConfirmationAsync(UserSession session, string answer, long chatId, CancellationToken token)
    {
        if (answer == "так" || answer == "yes")
        {
            if (session.CurrentConfirmationStep == ConfirmationStep.Passport)
            {
                session.State = ConversationState.WaitingForVehicleDoc;
                session.CurrentConfirmationStep = ConfirmationStep.None;
                await _bot.SendMessage(chatId, "Добре, надішліть фото техпаспорта.", cancellationToken: token);
            }
            else if (session.CurrentConfirmationStep == ConfirmationStep.VehicleDoc)
            {
                session.State = ConversationState.WaitingForPriceConfirmation;
                session.CurrentConfirmationStep = ConfirmationStep.None;
                await _bot.SendMessage(chatId, "Ціна страховки — 100 USD 💵. Згодні? (так / ні)", cancellationToken: token);
            }
        }
        else if (answer == "ні" || answer == "no")
        {
            if (session.CurrentConfirmationStep == ConfirmationStep.Passport)
            {
                session.PassportFrontFileId = null;
                session.PassportBackFileId = null;
                session.Passport = null;

                session.State = ConversationState.WaitingForPassportFront;
                session.CurrentConfirmationStep = ConfirmationStep.None;

                await _bot.SendMessage(chatId, "❗ Добре, надішліть фото передньої сторони паспорта заново.", cancellationToken: token);
            }
            else if (session.CurrentConfirmationStep == ConfirmationStep.VehicleDoc)
            {
                session.TechPassport = null;
                session.State = ConversationState.WaitingForVehicleDoc;
                session.CurrentConfirmationStep = ConfirmationStep.None;

                await _bot.SendMessage(chatId, "❗ Добре, надішліть фото техпаспорта заново.", cancellationToken: token);
            }
        }
        else
        {
            await _bot.SendMessage(chatId, "Будь ласка, відповідайте 'так' або 'ні'.", cancellationToken: token);
        }
    }

    private async Task<MemoryStream> GetFileStream(string fileId, CancellationToken token)
    {
        var stream = new MemoryStream();
        var file = await _bot.GetFile(fileId, token);
        await _bot.DownloadFile(file.FilePath, stream, token);
        stream.Position = 0;
        return stream;
    }
}
