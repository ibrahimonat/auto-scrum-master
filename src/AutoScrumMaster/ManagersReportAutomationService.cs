using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AutoScrumMaster.DataFlow;
using AutoScrumMaster.SettingsModels;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster
{
    public class ManagersReportAutomationService : ISingletonDependency
    {
        private readonly Config _config;
        private readonly IterationWorkItemsRetrieverTransform _iterationWorkItemsRetrieverTransform;
        private readonly ManagersGoogleChatMessageSenderAction _managersGoogleChatMessageSenderAction;

        public ManagersReportAutomationService(Config config, IterationWorkItemsRetrieverTransform iterationWorkItemsRetrieverTransform, ManagersGoogleChatMessageSenderAction managersGoogleChatMessageSenderAction)
        {
            _config = config;
            _iterationWorkItemsRetrieverTransform = iterationWorkItemsRetrieverTransform;
            _managersGoogleChatMessageSenderAction = managersGoogleChatMessageSenderAction;
        }

        public async Task RunAsync()
        {
            await _config.SetSharedSettings();

            var iterationWorkItemsTransformBlock = _iterationWorkItemsRetrieverTransform.Block;
            var managersGoogleMessageSenderActionBlock = _managersGoogleChatMessageSenderAction.Block;

            var passedDueWorkItemsTransformBlock = PassedDueWorkItemsTransform.Block;

            var broadcastBlock = new BroadcastBlock<List<JObject>>(null);
            iterationWorkItemsTransformBlock.LinkTo(broadcastBlock);

            var batchBlock = new BatchBlock<string>(1);

            broadcastBlock.LinkTo(passedDueWorkItemsTransformBlock);
            passedDueWorkItemsTransformBlock.LinkTo(batchBlock);

            batchBlock.LinkTo(managersGoogleMessageSenderActionBlock);

            iterationWorkItemsTransformBlock.Post(IterationTimeFrame.Current);
            iterationWorkItemsTransformBlock.Complete();
            
            await managersGoogleMessageSenderActionBlock.Completion;
        }
    }
}