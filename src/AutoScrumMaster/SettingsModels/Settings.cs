namespace AutoScrumMaster.SettingsModels
{
    public class Settings
    {
        public const string SettingsSectionName = "Settings";
        public AzureDevOpsSettings AzureDevOpsSettings { get; set; }
        
        public EngineeringManagerInfo EngineeringManagerInfo { get; set; }
    }
}