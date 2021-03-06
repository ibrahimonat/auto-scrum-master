using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AutoScrumMaster.DataFlow
{
	public class OpenWorkItemsTransform
	{
		private readonly ILogger<OpenWorkItemsTransform> _logger;

		public OpenWorkItemsTransform(ILogger<OpenWorkItemsTransform> logger)
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
					             new List<string> {"Bug", "User Story"}.Contains(fields["System.WorkItemType"]
						             .Value<string>()) &&
					             // Find any open work that is not in closed state.
					             fields["System.State"].Value<string>().ToLower() != "closed" &&
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
					var currentStatus = offendingWorkItem["fields"]["System.State"].Value<string>();
					var recommendedActionText = currentStatus.ToLower() switch
					{
						"new" => "Move it to the current sprint.",
						"resolved" => "If it is verified, close it. Otherwise, get it verified.",
						"pr submitted" =>
							"Make sure the PR is reviewed, merged, and the work is verified by another engineer.",
						_ =>
							"*What is the status of it?* Can you close it or do you need to create a follow-up work item in the current sprint?"
					};

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
						"BOARD: Still open in {currentState} state. Story \"{workItemId}:{workItemTitle}\". Assigned to {userEmail} in {currentIteration}.",
						workItemId, workItemTitle, userEmail, Config.CurrentIteration.Name);

					messageBuilder.Append(
						$"{chatDisplayName}, <{workItemUrl}|{workItemTitle}> is in *{currentStatus}* state! {recommendedActionText}\n\n");
				}

				return messageBuilder.ToString();
			});
	}
}
