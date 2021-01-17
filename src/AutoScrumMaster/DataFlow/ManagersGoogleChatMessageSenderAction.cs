using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster.DataFlow
{
    public class ManagersGoogleChatMessageSenderAction : ISingletonDependency
    {
        private readonly ILogger<ManagersGoogleChatMessageSenderAction> _logger;

        public ManagersGoogleChatMessageSenderAction(ILogger<ManagersGoogleChatMessageSenderAction> logger)
        {
            _logger = logger;
        }

        //todo: make it async!
        public ActionBlock<string[]> Block => new(messages =>
        {
            var httpClient = new HttpClient();
            var yesterday = DateTime.Now.Subtract(TimeSpan.FromDays(1)).Date.ToShortDateString();
            //todo: Get user info!
            var greetings = new[]
            {
                $"Hello <users/ibrahim>. Here is the report for *yesterday* ({yesterday}) progress",
            };

            var random = new Random();
            var randomGreeting = greetings[random.Next(0, greetings.Length)];

            var finalMessage = messages.All(string.IsNullOrEmpty)
                ? "The board is looking good and every thing is on track"
                : string.Join("", messages);

            var chatMessage = new
            {
                text = $"{randomGreeting}:\n\n{finalMessage}"
            };

            _logger.LogDebug("Chat message (Managers): " + chatMessage);
            // await httpClient.PostAsJsonAsync(Config.EngineeringManagerInfo.ManagerRemindersGoogleWebhookUrl,
            //     chatMessage);
        });
    }
}
