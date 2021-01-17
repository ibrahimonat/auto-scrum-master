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
    public class CurrentIterationAutomationService : ISingletonDependency
    {
        private readonly LastDayOfCurrentIterationGoogleChatMessageSenderAction _lastDayOfCurrentIterationGoogleChatMessageSenderAction;
        private readonly CurrentIterationGoogleChatMessageSenderAction _currentIterationGoogleChatMessageSenderAction;
        private readonly IterationWorkItemsRetrieverTransform _iterationWorkItemsRetrieverTransform;
        private readonly EstimateWorkItemsTransform _estimateWorkItemsTransform;
        private readonly DescriptiveTitlesTransform _descriptiveTitlesTransform;
        private readonly ActivateWorkItemTransform _activateWorkItemTransform;
        private readonly DescriptionTransform _descriptionTransform;
        private readonly LongCodeCompleteTransform _longCodeCompleteTransform;
        private readonly GreatWorkTransform _greatWorkTransform;
        private readonly StillActiveWorkItemsTransform _stillActiveWorkItemsTransform;
        private readonly Config _config;

        public CurrentIterationAutomationService(LastDayOfCurrentIterationGoogleChatMessageSenderAction lastDayOfCurrentIterationGoogleChatMessageSenderAction, CurrentIterationGoogleChatMessageSenderAction currentIterationGoogleChatMessageSenderAction, IterationWorkItemsRetrieverTransform iterationWorkItemsRetrieverTransform, EstimateWorkItemsTransform estimateWorkItemsTransform, DescriptiveTitlesTransform descriptiveTitlesTransform, ActivateWorkItemTransform activateWorkItemTransform, DescriptionTransform descriptionTransform, LongCodeCompleteTransform longCodeCompleteTransform, GreatWorkTransform greatWorkTransform, StillActiveWorkItemsTransform stillActiveWorkItemsTransform, Config config)
        {
            _lastDayOfCurrentIterationGoogleChatMessageSenderAction = lastDayOfCurrentIterationGoogleChatMessageSenderAction;
            _currentIterationGoogleChatMessageSenderAction = currentIterationGoogleChatMessageSenderAction;
            _iterationWorkItemsRetrieverTransform = iterationWorkItemsRetrieverTransform;
            _estimateWorkItemsTransform = estimateWorkItemsTransform;
            _descriptiveTitlesTransform = descriptiveTitlesTransform;
            _activateWorkItemTransform = activateWorkItemTransform;
            _descriptionTransform = descriptionTransform;
            _longCodeCompleteTransform = longCodeCompleteTransform;
            _greatWorkTransform = greatWorkTransform;
            _stillActiveWorkItemsTransform = stillActiveWorkItemsTransform;
            _config = config;
        }

        public async Task RunAsync()
        {
            await _config.SetSharedSettings();
            
            // No need to send anything on the first day of the sprint since it is the planning day,
            // and people most likely won't have much time to keep their work items current.
            if (Config.CurrentIteration.StartDate.Date == DateTime.Now.Date)
            {
                return;
            }

            // If in the last day of the sprint and the time of the day has past after noon
            var lastDayOfIteration = Config.CurrentIteration.FinishDate.Date == DateTime.Now.Date &&
                                     DateTime.Now.TimeOfDay > TimeSpan.FromHours(12);

            var iterationWorkItemsTransformBlock = _iterationWorkItemsRetrieverTransform.Block;
            var regularDaysMessageSenderActionBlock = _currentIterationGoogleChatMessageSenderAction.Block;
            var lastDayMessageSenderActionBlock = _lastDayOfCurrentIterationGoogleChatMessageSenderAction.Block;

            var estimateWorkItemsTransformBlock = _estimateWorkItemsTransform.Block;
            var descriptiveTitleTransformBlock = _descriptiveTitlesTransform.Block;
            var activateWorkItemTransformBlock = _activateWorkItemTransform.Block;
            var descriptionTransformBlock = _descriptionTransform.Block;
            var longCodeCompleteTransformBlock = _longCodeCompleteTransform.Block;
            var greatWorkTransformBlock = _greatWorkTransform.Block;
            var stillActiveWorkItemsTransformBlock = _stillActiveWorkItemsTransform.Block;

            var broadcastBlock = new BroadcastBlock<List<JObject>>(null);
            // Increase the limit of the batch size after adding another transform block.
            var batchBlock = new BatchBlock<string>(lastDayOfIteration ? 7 : 6);

            // On the last day of the iteration, send a different message indicating the end of the sprint.
            if (lastDayOfIteration)
            {
                broadcastBlock.LinkTo(stillActiveWorkItemsTransformBlock);
                // Adding one more to the batch block, increasing the batch size by one.
                stillActiveWorkItemsTransformBlock.LinkTo(batchBlock);
                batchBlock.LinkTo(lastDayMessageSenderActionBlock);
            }
            else
            {
                batchBlock.LinkTo(regularDaysMessageSenderActionBlock);
            }

            iterationWorkItemsTransformBlock.LinkTo(broadcastBlock);

            broadcastBlock.LinkTo(estimateWorkItemsTransformBlock);
            estimateWorkItemsTransformBlock.LinkTo(batchBlock);

            broadcastBlock.LinkTo(descriptiveTitleTransformBlock);
            descriptiveTitleTransformBlock.LinkTo(batchBlock);

            broadcastBlock.LinkTo(activateWorkItemTransformBlock);
            activateWorkItemTransformBlock.LinkTo(batchBlock);

            broadcastBlock.LinkTo(descriptionTransformBlock);
            descriptionTransformBlock.LinkTo(batchBlock);

            broadcastBlock.LinkTo(longCodeCompleteTransformBlock);
            longCodeCompleteTransformBlock.LinkTo(batchBlock);

            broadcastBlock.LinkTo(greatWorkTransformBlock);
            greatWorkTransformBlock.LinkTo(batchBlock);

            iterationWorkItemsTransformBlock.Post(IterationTimeFrame.Current);
            iterationWorkItemsTransformBlock.Complete();
            
            await regularDaysMessageSenderActionBlock.Completion;
        }
    }
}