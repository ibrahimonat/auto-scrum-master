using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AutoScrumMaster.SettingsModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster
{
	public class Config : ISingletonDependency
	{
		public Config(IOptions<Settings> options)
		{
			AzureDevOpsSettings = options.Value.AzureDevOpsSettings;
		}
		public static AzureDevOpsSettings AzureDevOpsSettings { get; set; }
		public static EngineeringManagerInfo EngineeringManagerInfo { get; set; }
		public static HttpClient AzureAuthorizedHttpClient { get; set; }
		public static IterationInfo CurrentIteration { get; set; }

		public async Task SetSharedSettings()
		{
			AzureAuthorizedHttpClient = new HttpClient();
			AzureAuthorizedHttpClient.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Basic", AzureDevOpsSettings.ApiKey);

			CurrentIteration = await SetCurrentIterationSettings();
		}

		private static async Task<IterationInfo> SetCurrentIterationSettings()
		{
			var currentIterationContent = await AzureAuthorizedHttpClient.GetStringAsync(
				$"https://tf-server.sestek.com.tr/tfs/{AzureDevOpsSettings.Organization}/{AzureDevOpsSettings.Project}/{AzureDevOpsSettings.Team}/" +
				$"_apis/work/teamsettings/iterations?$timeframe=current");

			var iterationJson = JObject.Parse(currentIterationContent).SelectToken($".value[0]") as JObject;

			var iterationInfo = iterationJson!.ToObject<IterationInfo>();
			iterationInfo!.FinishDate = DateTime.Parse(iterationJson!["attributes"]!["finishDate"]!.Value<string>());
			iterationInfo!.StartDate = DateTime.Parse(iterationJson!["attributes"]!["startDate"]!.Value<string>());

			iterationInfo.TimeFrame = iterationJson!["attributes"]!["timeFrame"]!.Value<string>().ToLower() switch
			{
				"current" => IterationTimeFrame.Current,
				"past" => IterationTimeFrame.Previous,
				"future" => IterationTimeFrame.Next,
				_ => IterationTimeFrame.Current
			};

			return iterationInfo;
		}
	}
}
