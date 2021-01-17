using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Web;
using AutoScrumMaster.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster.DataFlow
{
	public class LongCodeCompleteTransform : ISingletonDependency
	{
		private readonly ILogger<LongCodeCompleteTransform> _logger;

		public LongCodeCompleteTransform(ILogger<LongCodeCompleteTransform> logger)
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
					             new List<string>() {"Bug", "User Story"}.Contains(fields!["System.WorkItemType"]!
						             .Value<string>()) &&
					             new List<string> {"PR Submitted", "Resolved"}.Contains(fields!["System.State"]!
						             .Value<string>()) &&
					             DateTime.Parse(fields!["Microsoft.VSTS.Common.StateChangeDate"]!.Value<string>())
						             .ToLocalTime() <
					             DateTime.Now.Date.Subtract(TimeSpan.FromDays(1)) &&
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

					var workItemTitle = offendingWorkItem["fields"]!["System.Title"]!.Value<string>();
					var workItemId = offendingWorkItem["id"];
					var workItemUrl = $"{baseUrl}/{workItemId}";
					var workItemState = offendingWorkItem!["fields"]!["System.State"]!.Value<string>();
					var lastStateChange =
						DateTime.Parse(offendingWorkItem["fields"]!["Microsoft.VSTS.Common.StateChangeDate"]!
							.Value<string>()).ToLocalTime();

					var now = DateTime.Now.Date;
					var weekendCounts = DateDiffHelper.CalculateWeekendDays(lastStateChange, now);
					var idleForTimeSpan = now - lastStateChange.Date - TimeSpan.FromDays(weekendCounts);

					//todo: Get user name information from the channel!
					var chatDisplayName = !string.IsNullOrEmpty(userDisplayName)
						? userDisplayName
						: "<users/ibrahim>";

					_logger.LogInformation(
						"BOARD: Pending in incomplete state of {currentState} for {pendingForDays} days. Story \"{workItemId}:{workItemTitle}\". Assigned to {userEmail} in {currentIteration}.",
						workItemState, idleForTimeSpan.TotalDays, workItemId, workItemTitle, userEmail,
						Config.CurrentIteration.Name);

					// todo Include pr follow up message for PR Submitted state.
					messageBuilder.Append(
						$"{chatDisplayName}, *follow up* on your work of <{workItemUrl}|{workItemTitle}>. " +
						$"It is in *{workItemState}* state for *{idleForTimeSpan.TotalDays}* day(s). Don't forget to *have it verified* by a fellow engineer!\n\n");
				}

				return messageBuilder.ToString();
			});
	}
}
