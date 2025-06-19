using System.Text;
using Microsoft.Extensions.Configuration;
using Mindee;
using Mindee.Http;
using Mindee.Input;
using Mindee.Product.DriverLicense;
using Mindee.Product.Generated;
using Test_MindeeApi.Models;

namespace Test_MindeeApi.Service;

public class DocumentProcessingService
{
    private readonly MindeeClient _client;

    public DocumentProcessingService(IConfiguration configuration)
    {
        var apiKey = configuration["MindeeApiKey"] ?? throw new Exception("Mindee API Key missing.");
        _client = new MindeeClient(apiKey);
    }

   
    private async Task<PassportData> RecognizeDriverLicenseAsync(Stream imageStream, string fileName)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), fileName);
        using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            await imageStream.CopyToAsync(fs);

        var inputSource = new LocalInputSource(tempFile);
        var response = await _client.EnqueueAndParseAsync<DriverLicenseV1>(inputSource);
        var prediction = response.Document.Inference.Prediction;

        File.Delete(tempFile);

        return new PassportData
        {
            FirstName = prediction.FirstName?.Value,
            LastName = prediction.LastName?.Value,
            DateOfBirth = prediction.DateOfBirth?.Value,
            PlaceOfBirth = prediction.PlaceOfBirth?.Value,
            CountryCode = prediction.CountryCode?.Value,
            State = prediction.State?.Value,
            Id = prediction.Id?.Value,
            DdNumber = prediction.DdNumber?.Value,
            IssuedDate = prediction.IssuedDate?.Value,
            ExpiryDate = prediction.ExpiryDate?.Value,
            IssuingAuthority = prediction.IssuingAuthority?.Value,
            Category = prediction.Category?.Value,
            Mrz = prediction.Mrz?.Value,
        };
    }
    private PassportData MergeDriverLicenseData(PassportData front, PassportData back)
    {
        string? Choose(string? frontValue, string? backValue) =>
            !string.IsNullOrWhiteSpace(frontValue) ? frontValue : backValue;

        return new PassportData
        {
            FirstName = Choose(front.FirstName, back.FirstName),
            LastName = Choose(front.LastName, back.LastName),
            DateOfBirth = Choose(front.DateOfBirth, back.DateOfBirth),
            PlaceOfBirth = Choose(front.PlaceOfBirth, back.PlaceOfBirth),
            CountryCode = Choose(front.CountryCode, back.CountryCode),
            State = Choose(front.State, back.State),
            Id = Choose(front.Id, back.Id),
            DdNumber = Choose(front.DdNumber, back.DdNumber),
            IssuedDate = Choose(front.IssuedDate, back.IssuedDate),
            ExpiryDate = Choose(front.ExpiryDate, back.ExpiryDate),
            IssuingAuthority = Choose(front.IssuingAuthority, back.IssuingAuthority),
            Category = Choose(front.Category, back.Category),
            Mrz = Choose(front.Mrz, back.Mrz),
        };
    }

    public async Task<string> ProcessDriverLicenseFromFrontAndBackAsync(Stream frontStream, Stream backStream)
    {
        var frontData = await RecognizeDriverLicenseAsync(frontStream, "front.jpg");
        var backData = await RecognizeDriverLicenseAsync(backStream, "back.jpg");

        var merged = MergeDriverLicenseData(frontData, backData);
        SplitMrzLines(merged);
        return FormatPassportData(merged);
    }
 
    public async Task<string> ProcessTechPassportFromImageAsync(Stream imageStream)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"vin_{Guid.NewGuid():N}.jpg");
        using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            await imageStream.CopyToAsync(fs);

        var input = new LocalInputSource(tempFile);

        var endpoint = new CustomEndpoint(
            endpointName: "techpasport2",
            accountName: "DeveloperVedmedik2",
            version: "1");

        var response = await _client.EnqueueAndParseAsync<GeneratedV1>(input, endpoint);
        File.Delete(tempFile);

        var prediction = response.Document?.Inference?.Prediction;
        if (prediction == null || prediction.Fields == null)
            return "❌ Не вдалося розпізнати документ";

        prediction.Fields.TryGetValue("vehicle_identification_number", out var vinField);
        prediction.Fields.TryGetValue("manufacturer", out var manufacturerField);
        prediction.Fields.TryGetValue("data_of_first_registration", out var registrationField);
        prediction.Fields.TryGetValue("engine_type", out var engineTypeField);

        var vin = CleanValue(vinField?.ToString());
        var manufacturer = CleanValue(manufacturerField?.ToString());
        var registrationDate = CleanValue(registrationField?.ToString());
        var engineType = CleanValue(engineTypeField?.ToString());

        return FormatTechPassportData(vin, manufacturer, registrationDate, engineType);
    }
    private string FormatPassportData(PassportData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🆔 *ДАНІ ПАСПОРТА*");
        sb.AppendLine($"👤 Ім’я: {data.FirstName}");
        sb.AppendLine($"👤 Прізвище: {data.LastName}");
        sb.AppendLine($"🎂 Дата народження: {data.DateOfBirth}");
        sb.AppendLine($"🏙 Місце народження: {data.PlaceOfBirth}");
        sb.AppendLine($"🌍 Країна: {data.CountryCode} ({data.CountryCode})");
        sb.AppendLine($"🏛 Штат/регіон: {data.State}");
        sb.AppendLine($"🆔 ID документа: {data.Id}");
        sb.AppendLine($"📄 DD номер: {data.DdNumber}");
        sb.AppendLine($"📅 Дата видачі: {data.IssuedDate}");
        sb.AppendLine($"📅 Термін дії: {data.ExpiryDate}");
        sb.AppendLine($"🏢 Орган видачі: {data.IssuingAuthority}");
        sb.AppendLine($"🚘 Категорія: {data.Category}");
        sb.AppendLine("🧾 MRZ:");
        sb.AppendLine(data.Mrz1);
        sb.AppendLine(data.Mrz2);
        sb.AppendLine(data.Mrz3);

        return sb.ToString();
    }
    private string CleanValue(string? raw) => raw?.Replace(":value:", "", StringComparison.OrdinalIgnoreCase).Trim() ?? "—";

    private string FormatTechPassportData(string vin, string manufacturer, string registrationDate, string engineType)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🚗 *ДАНІ АВТОМОБІЛЯ*");
        sb.AppendLine($"🚗 VIN: {vin}");
        sb.AppendLine($"🏭 Виробник: {manufacturer}");
        sb.AppendLine($"📅 Перша реєстрація: {registrationDate}");
        sb.AppendLine($"🛠 Тип двигуна: {engineType}");
        return sb.ToString();
    }
    
    private void SplitMrzLines(PassportData data)
    {
        if (string.IsNullOrWhiteSpace(data.Mrz))
            return;

        var mrz = data.Mrz.Trim();
        int partLength = mrz.Length / 3;

        data.Mrz1 = mrz.Length >= partLength ? mrz.Substring(0, partLength) : mrz;
        data.Mrz2 = mrz.Length >= partLength * 2 ? mrz.Substring(partLength, partLength) : string.Empty;
        data.Mrz3 = mrz.Length > partLength * 2 ? mrz.Substring(partLength * 2) : string.Empty;
    }

}