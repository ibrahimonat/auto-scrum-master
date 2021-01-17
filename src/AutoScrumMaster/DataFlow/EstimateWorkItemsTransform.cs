using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster.DataFlow
{
    public class EstimateWorkItemsTransform : ISingletonDependency
    {
        private readonly ILogger<EstimateWorkItemsTransform> _logger;

        public EstimateWorkItemsTransform(ILogger<EstimateWorkItemsTransform> logger)
        {
            _logger = logger;
        }
        public TransformBlock<List<JObject>, string> Block =>
            new(workItems =>
            {
                // Property names that has periods in them won't be parsed by Json.NET as opposed to online JSON Parser tools
                // eg. $.value[?(@.fields['Microsoft.VSTS.Scheduling.StoryPoints'] == null && @.fields['System.AssignedTo'] != null)]
                // Because of that reason, I had to use enumeration below.
                var offendingWorkItems = workItems
                    .Where(wi => wi["fields"] is JObject fields &&
                                 new List<string>() {"Bug", "User Story"}.Contains(fields["System.WorkItemType"]
                                     .Value<string>()) &&
                                 !fields.ContainsKey("Microsoft.VSTS.Scheduling.StoryPoints") &&
                                 fields.ContainsKey("System.AssignedTo")).ToList();

                if (!offendingWorkItems.Any())
                {
                    return null;
                }

                var messageBuilder = new StringBuilder();
                var baseUrl =
                    $"https://tf-server.sestek.com.tr/tfs/{HttpUtility.UrlPathEncode(Config.AzureDevOpsSettings.Organization)}/" +
                    $"{HttpUtility.UrlPathEncode(Config.AzureDevOpsSettings.Project)}/_workitems/edit";

                foreach (var offendingWorkItem in offendingWorkItems)
                {
                    var userDisplayName = offendingWorkItem["fields"]?["System.AssignedTo"]?["displayName"]
                        ?.Value<string>();
                    var userEmail = offendingWorkItem["fields"]?["System.AssignedTo"]?["uniqueName"]?.Value<string>();

                    var workItemTitle = offendingWorkItem["fields"]?["System.Title"]?.Value<string>();
                    var workItemId = offendingWorkItem["id"];
                    var workItemUrl = $"{baseUrl}/{workItemId}";

                    //todo: Get user name information from the channel!
                    var chatDisplayName = !string.IsNullOrEmpty(userDisplayName)
                        ? userDisplayName
                        : "<users/ibrahim>";

                    _logger.LogInformation(
	                    "BOARD: Missing story point for \"{workItemId}:{workItemTitle}\". Assigned to {userEmail} in {currentIteration}.",
	                    workItemId, workItemTitle, userEmail, Config.CurrentIteration.Name);

                    messageBuilder.Append(
                        $"{chatDisplayName}, *estimate* the story point of <{workItemUrl}|{workItemTitle}>.\n\n");
                }

                return messageBuilder.ToString();
            });
    }
}
