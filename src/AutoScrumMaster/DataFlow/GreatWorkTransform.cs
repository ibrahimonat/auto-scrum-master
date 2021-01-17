using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster.DataFlow
{
    public class GreatWorkTransform : ISingletonDependency
    {
        private readonly ILogger<GreatWorkTransform> _logger;

        public GreatWorkTransform(ILogger<GreatWorkTransform> logger)
        {
            _logger = logger;
        }
        public TransformBlock<List<JObject>, string> Block =>
            new(workItems =>
            {
                var workItemsByPersons = workItems
                    .Where(wi => wi["fields"] is JObject fields &&
                                 new List<string>() {"Bug", "User Story"}.Contains(fields["System.WorkItemType"]
                                     .Value<string>()) &&
                                 fields.ContainsKey("System.AssignedTo") &&
                                 // Excluding the engineering manager from the congratulation list to prevent self-compliments.
                                 !fields!["System.AssignedTo"]!["uniqueName"]!.Value<string>().Equals(
                                     Config.EngineeringManagerInfo.AzureDevOpsEmail,
                                     StringComparison.InvariantCultureIgnoreCase)
                    ).ToLookup(
                        wi => wi["fields"]["System.AssignedTo"]["uniqueName"].Value<string>(), t => t);

                if (!workItemsByPersons.Any())
                {
                    return null;
                }

                var messageBuilder = new StringBuilder();

                foreach (var workItemsByPerson in workItemsByPersons)
                {
                    // Check if there is any active work item.
                    if (!workItemsByPerson.All(a =>
                        a!["fields"]!["System.State"]!.Value<string>()
                            .Equals("Closed", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    // To prevent congratulating people multiple times for the same reasons, we are checking
                    // if they completed all of their work items recent enough, which is about two days ago to be safe.
                    if (!workItemsByPerson.Any(a =>
                    {
                        var closedDate =
                            DateTime.Parse(a["fields"]["Microsoft.VSTS.Common.ClosedDate"].Value<string>()).ToLocalTime().Date;
                        return closedDate > DateTime.Now.Date.Subtract(TimeSpan.FromDays(2));
                    }))
                    {
                        continue;
                    }

                    var userDisplayName = workItemsByPerson.First()["fields"]?["System.CreatedBy"]?["displayName"]
                        ?.Value<string>();
                    var userEmail = workItemsByPerson.Key;
                    
                    //todo: Get user name information from the channel!
                    var chatDisplayName = !string.IsNullOrEmpty(userDisplayName)
                        ? userDisplayName
                        : "<users/ibrahim>";

                    _logger.LogInformation(
	                    "BOARD: Closed everything in the current sprint {currentIteration}. Assigned to {userEmail}.",
	                    Config.CurrentIteration.Name, userEmail);

                    messageBuilder.Append(
                        $"{chatDisplayName}, great work 👏👏👏! You *closed* all of your work items! 🎉 \n\n");
                }

                return messageBuilder.ToString();
            });
    }
}
