using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AutoScrumMaster.SettingsModels;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster.DataFlow
{
	public class IterationWorkItemsRetrieverTransform : ISingletonDependency
	{
		public TransformBlock<IterationTimeFrame, List<JObject>> Block =>
			new(async iteration =>
			{
				var retriever =
					new IterationWorkItemsRetrieverTransform();
				var workItemIds = await retriever.GetWorkItemIdsByWiql(iteration);
				return await retriever.GetWorkItems(workItemIds);
			});

		private async Task<IEnumerable<long>> GetWorkItemIdsByWiql(IterationTimeFrame iterationTimeFrame)
		{
			var iterationQueryValue = iterationTimeFrame switch
			{
				IterationTimeFrame.Current => "@CurrentIteration",
				IterationTimeFrame.Previous => "@CurrentIteration - 1",
				IterationTimeFrame.Next => "@CurrentIteration + 1",
				_ => "@CurrentIteration"
			};

			var httpResponse = await Config.AzureAuthorizedHttpClient.PostAsJsonAsync(
				$"https://tf-server.sestek.com.tr/tfs/{Config.AzureDevOpsSettings.Organization}/{Config.AzureDevOpsSettings.Project}/{Config.AzureDevOpsSettings.Team}/_apis/wit/wiql?api-version=5.0",
				new
				{
					query =
						$"Select [System.Id] From WorkItems Where [System.WorkItemType] IN ('Bug','User Story') AND " +
						$"[State] <> 'Removed' AND [System.IterationPath] = {iterationQueryValue}"
				});

			var content = await httpResponse.Content.ReadAsStringAsync();
			// todo check if the content is null or empty and return appropriate response.
			return JObject.Parse(content)!.SelectTokens("$.workItems[*].id")!.Select(a => a.Value<long>());
		}

		private async Task<List<JObject>> GetWorkItems(IEnumerable<long> workItemIds)
		{
			var result = await Config.AzureAuthorizedHttpClient.PostAsJsonAsync(
				$"https://tf-server.sestek.com.tr/tfs/{Config.AzureDevOpsSettings.Organization}/{Config.AzureDevOpsSettings.Project}/_apis/wit/workitemsbatch?api-version=5",
				new WorkItemMessage() {Ids = workItemIds.ToList()}
			);
			var content = await result.Content.ReadAsStringAsync();
			return JObject.Parse(content).SelectTokens("$.value[*]").Cast<JObject>().ToList();
		}
	}
}
