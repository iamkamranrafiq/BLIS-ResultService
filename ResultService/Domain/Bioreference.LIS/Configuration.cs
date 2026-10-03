using Microsoft.Extensions.Configuration;

namespace Bioreference.LIS
{
  
    public class Configuration
    {
        public static IConfiguration AppSettings { get; set; }
        public static IConfiguration LISSettings { get; set; }
        public static IConfiguration B2Settings { get; set; }
        public static IConfiguration B24KSettings { get; set; }

        static Configuration()
        {
            AppSettings = SettingsExtension.BuildSettings();
            string instance = AppSettings.GetString("Bioreference.LIS:Instance");
            LISSettings = SettingsExtension.Fetch(ConnectionString, "BioreferenceLIS", instance);
            B2Settings = SettingsExtension.Fetch(ConnectionString, "B2", instance);
            B24KSettings = SettingsExtension.Fetch(ConnectionString, "B24K", instance);
        }

        #region Config Properties

        public static string ConnectionString
        {
            get
            {
                return AppSettings.GetConnectionString("Bioreference.LIS") ?? "";
            }
        }

        public static int RuleSetRelease
        {
            get
            {
                return AppSettings.GetInt("Bioreference.LIS:RuleSetRelease");
            }
        }

        public static int RuleSetChangeResult
        {
            get
            {
                return AppSettings.GetInt("Bioreference.LIS:RuleSetChangeResult");
            }
        }

        public static bool ToFollowEnabled
        {
            get
            {
                return AppSettings.GetBool("Bioreference.LIS:ToFollowEnabled");
            }
        }

        #endregion

    }
}