using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster.DataFlow
{
    public class LastDayOfCurrentIterationGoogleChatMessageSenderAction : ISingletonDependency
    {
        private readonly ILogger<LastDayOfCurrentIterationGoogleChatMessageSenderAction> _logger;

        public LastDayOfCurrentIterationGoogleChatMessageSenderAction(ILogger<LastDayOfCurrentIterationGoogleChatMessageSenderAction> logger)
        {
            _logger = logger;
        }
        
        //todo: make it async
        public ActionBlock<string[]> Block => new(messages =>
        {
            var allCompleted = messages.All(string.IsNullOrWhiteSpace);

            var httpClient = new HttpClient();

            var greetings = new[]
            {
                $"Hello there team 👋, this is *the last day* of our current sprint ({Config.CurrentIteration.Name})."
            };

            var actionMessage = allCompleted
                ? "\n\nAnd, *GREAT WORK* <users/all>! 👏🎉👏🎉 *All of the work items are closed* from this sprint! " +
                  "Have a wonderful weekend and I will see you all next week!"
                : "*Unfortunately*, there are some remaining work. Please complete the actions below *before the end of the day*:\n\n";

            var random = new Random();
            var randomGreeting = greetings[random.Next(0, greetings.Length)];

            var chatMessage = new
            {
                text = $"{randomGreeting} {actionMessage}" + string.Join("", messages)
            };
            
            _logger.LogDebug("Chat message: " + chatMessage);
            //await httpClient.PostAsJsonAsync(Config.GoogleChatSettings.WebhookUrl, chatMessage);
        });
    }
}
