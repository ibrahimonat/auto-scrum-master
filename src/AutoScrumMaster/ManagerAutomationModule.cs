using AutoScrumMaster.SettingsModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AutoScrumMaster
{

    [DependsOn(
        typeof(AbpAutofacModule)
    )]
    public class ManagerAutomationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostEnvironment = context.Services.GetSingletonInstance<IHostEnvironment>();
            
            context.Services.AddHostedService<ManagerAutomationHostedService>();
            
            context.Services.Configure<Settings>(s => configuration.GetSection(Settings.SettingsSectionName).Bind(s));
        }
    }
}
