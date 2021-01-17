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
	public class DescriptionTransform : ISingletonDependency
	{
		private readonly ILogger<DescriptionTransform> _logger;

		public DescriptionTransform(ILogger<DescriptionTransform> logger)
		{
			_logger = logger;
		}
		public TransformBlock<List<JObject>, string> Block =>
			new(workItems =>
			{
				var offendingWorkItems = workItems
					.Where(wi => wi["fields"] is JObject fields &&
					             new List<string>() {"Bug", "User Story"}.Contains(fields["System.WorkItemType"]
						             .Value<string>()) &&
					             ((fields["System.WorkItemType"].Value<string>() == "Bug" &&
					               !fields.ContainsKey("Microsoft.VSTS.TCM.ReproSteps")
					              ) ||
					              (fields["System.WorkItemType"].Value<string>() == "User Story" &&
					               !fields.ContainsKey("System.Description")
					              )
					             )).ToList();

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
					var userDisplayName = offendingWorkItem["fields"]?["System.CreatedBy"]?["displayName"]
						?.Value<string>();
					var userEmail = offendingWorkItem["fields"]?["System.CreatedBy"]?["uniqueName"]?.Value<string>();
					
					var workItemTitle = offendingWorkItem["fields"]?["System.Title"]?.Value<string>();
					var workItemId = offendingWorkItem["id"];
					var workItemUrl = $"{baseUrl}/{workItemId}";

					//todo: Get user name information from the channel!
					var chatDisplayName = !string.IsNullOrEmpty(userDisplayName)
						? userDisplayName
						: "<users/ibrahim>";

					_logger.LogInformation(
						"BOARD: Missing description for \"{workItemId}:{workItemTitle}\". Assigned to {userEmail} in {currentIteration}.",
						workItemId, workItemTitle, userEmail, Config.CurrentIteration.Name);

					messageBuilder.Append(
						$"{chatDisplayName}, add a *description* to <{workItemUrl}|{workItemTitle}>.\n\n");
				}

				return messageBuilder.ToString();
			});
	}
}
