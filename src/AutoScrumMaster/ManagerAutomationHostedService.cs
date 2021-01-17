using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace AutoScrumMaster
{
    public class ManagerAutomationHostedService : IHostedService
    {
        private readonly IAbpApplicationWithExternalServiceProvider _application;
        private readonly IServiceProvider _serviceProvider;
        private readonly HelloWorldService _helloWorldService;
        private readonly CurrentIterationAutomationService _currentIterationAutomationService;
        private ManagersReportAutomationService _managersReportAutomationService;

        public ManagerAutomationHostedService(
            IAbpApplicationWithExternalServiceProvider application,
            IServiceProvider serviceProvider,
            HelloWorldService helloWorldService, CurrentIterationAutomationService currentIterationAutomationService, ManagersReportAutomationService managersReportAutomationService)
        {
            _application = application;
            _serviceProvider = serviceProvider;
            _helloWorldService = helloWorldService;
            _currentIterationAutomationService = currentIterationAutomationService;
            _managersReportAutomationService = managersReportAutomationService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _application.Initialize(_serviceProvider);

            _helloWorldService.SayHello();
#pragma warning disable 4014
            _currentIterationAutomationService.RunAsync();
            _managersReportAutomationService.RunAsync();
#pragma warning restore 4014

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _application.Shutdown();

            return Task.CompletedTask;
        }
    }
}
