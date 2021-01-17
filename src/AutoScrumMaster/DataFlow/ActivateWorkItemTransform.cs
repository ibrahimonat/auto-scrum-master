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
	public class ActivateWorkItemTransform : ISingletonDependency
	{
		private readonly ILogger<ActivateWorkItemTransform> _logger;

		public ActivateWorkItemTransform(ILogger<ActivateWorkItemTransform> logger)
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
					             fields.ContainsKey("System.AssignedTo")).ToLookup(
						wi => wi["fields"]["System.AssignedTo"]["uniqueName"].Value<string>(), t => t);

				if (!offendingWorkItems.Any())
				{
					return null;
				}

				var messageBuilder = new StringBuilder();

				foreach (var offendingWorkItem in offendingWorkItems)
				{
					// Check if there is any active work item.
					if (offendingWorkItem.Any(a =>
						a["fields"]["System.State"].Value<string>()
							.Equals("Active", StringComparison.InvariantCultureIgnoreCase)))
					{
						continue;
					}

					// Check if there is any new item to activate in the first place.
					// Don't need to send any notification when all of the work items are complete or in resolve state.
					if (!offendingWorkItem.Any(a =>
						a["fields"]["System.State"].Value<string>()
							.Equals("New", StringComparison.InvariantCultureIgnoreCase)))
					{
						continue;
					}

					var userDisplayName = offendingWorkItem.First()["fields"]?["System.CreatedBy"]?["displayName"]
						?.Value<string>();
					var userEmail = offendingWorkItem.Key;

					//todo: Get user name information from the channel!
					var chatDisplayName = !string.IsNullOrEmpty(userDisplayName)
						? userDisplayName
						: "<users/ibrahim>";

					_logger.LogInformation(
						"BOARD: Not activated any work item by {userEmail} in {currentIteration}.",
						userEmail, Config.CurrentIteration.Name);

					messageBuilder.Append(
						$"{chatDisplayName}, *activate the work item* you are working on.\n\n");
				}

				return messageBuilder.ToString();
			});
	}
}
