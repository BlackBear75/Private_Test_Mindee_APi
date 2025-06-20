using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Test_MindeeApi.API;
using Test_MindeeApi.Service;
using Test_MindeeApi.State;
using ILogger = Serilog.ILogger;


namespace Test_MindeeApi.Configuration
{
    public static class DependencyStartup
    {
        public static async Task RunAsync(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var botService = host.Services.GetRequiredService<TelegramBotService>();
            await botService.StartAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Information() 
                        .WriteTo.Console()  
                        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)  
                        .CreateLogger();

                    services.AddSingleton<ILogger>(Log.Logger); 

                    var configuration = context.Configuration;
                 

                    var botToken = configuration["TelegramBotToken"];
                    if (string.IsNullOrEmpty(botToken))
                    {
                        throw new ArgumentException("Bot token is missing in configuration.");
                    }              
                    services.AddHttpClient<IOpenAiService, OpenAiService>();

                    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
                    services.AddSingleton<TelegramBotService>();
                    services.AddSingleton<SessionStorage>();
                    services.AddSingleton<DocumentProcessingService>();
                    
                    
                    services.AddSingleton<UpdateHandler>();
                    
                    services.AddSingleton<ErrorHandler>();

                    services.AddScoped(typeof(IMessageHandler), typeof(MessageHandler));
                    
                    services.AddScoped(typeof(IPhotoHandler), typeof(PhotoHandler));
                    
                    services.AddScoped(typeof(IPolicyGenerationService), typeof(PolicyGenerationService));
                    
                })
                .UseConsoleLifetime();
    }
}
