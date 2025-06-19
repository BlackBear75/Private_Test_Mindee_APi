
using System.Text;
using Test_MindeeApi.State;

namespace Test_MindeeApi.Service;

public interface IPolicyGenerationService
{
   string GeneratePolicyText(UserSession session);
   
   Task<string> SavePolicyToFileAsync(long chatId, string policyText);
}

public class PolicyGenerationService : IPolicyGenerationService
{
    public string GeneratePolicyText(UserSession session)
    {
        var passport = session.Passport ?? "—";
        var techPassport = session.TechPassport ?? "—";

        var policyText = new StringBuilder()
            .AppendLine("📄 *СТРАХОВИЙ ПОЛІС*")
            .AppendLine($"📅 Дата оформлення: {DateTime.Now:dd.MM.yyyy}")
            .AppendLine()
            .AppendLine(passport)
            .AppendLine()
            .AppendLine(techPassport)
            .AppendLine()
            .AppendLine("💵 *Сума страховки*: 100 USD")
            .AppendLine()
            .AppendLine("📌 *Статус*: Підтверджено ✅");

        return policyText.ToString();
    }

    public async Task<string> SavePolicyToFileAsync(long chatId, string policyText)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"policy_{chatId}.txt");
        await File.WriteAllTextAsync(tempFilePath, policyText);
        return tempFilePath;
    }
}