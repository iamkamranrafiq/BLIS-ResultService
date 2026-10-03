using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace Bioreference.ResultService.Application.Setting
{
    public class SettingService : ISettingService
    {
        public IConfiguration _configuration { get; set; }
        private readonly ILogger<SettingService> _logger = null;
        public SettingService(ILogger<SettingService> logger, IConfiguration configuration)
        {           
                _logger = logger;
            _configuration = configuration;
        }

        //It's an API service method contains older version of getting settings
        public async Task<Dictionary<string, string?>> GetResultSettings()
        {
            string ConnectionString = Bioreference.LIS.Configuration.ConnectionString;
            string instance = Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.LIS:Instance");
            bool autoRefreshPendings = Bioreference.LIS.Configuration.AppSettings.GetBool("Bioreference.LIS:AutoRefreshPendings");
            int autoRefreshPendingsMinutes = Bioreference.LIS.Configuration.AppSettings.GetInt("Bioreference.LIS:AutoRefreshPendingsMinutes");
            IConfiguration B2Settings = await Task.Run(() => Bioreference.LIS.Configuration.B2Settings);

            var settingsDictionary = new Dictionary<string, string?>();
            foreach (var child in B2Settings.AsEnumerable())
            {
                settingsDictionary[child.Key] = child.Value ?? string.Empty;
            }
            
            settingsDictionary["AutoRefreshPendings"] = autoRefreshPendings.ToString();
            settingsDictionary["AutoRefreshPendingsMinutes"] = autoRefreshPendingsMinutes.ToString();

            var sortedDictionary = settingsDictionary
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return sortedDictionary;
        }

        public string GetConnectionString(string name)
        {
            var connectionString = _configuration.GetConnectionString(name);
            if (connectionString == null)
            {
                throw new InvalidOperationException($"Connection string '{name}' not found.");
            }
            return connectionString;
        }
        public async Task<T> FetchSetting<T>(string connectionString, string settingKey, string instance = "") where T : class, new()
        {
            if (string.IsNullOrEmpty(connectionString))
                return new T();

            return await Task.Run(() =>
            {
                IConfiguration config = SettingsExtension.Fetch(connectionString, settingKey, instance);

                T result = new T();
                config.Bind(result);
                
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.CanWrite && p.CanRead);

                foreach (var prop in properties)
                {                   
                    string rawValue = config[prop.Name];

                    if (string.IsNullOrWhiteSpace(rawValue))
                        continue;

                    rawValue = rawValue.Trim();
                    // If pipe-delimited and not JSON, convert to JSON array
                    if (!rawValue.StartsWith("[") && rawValue.Contains('|'))
                    {
                        var parts = rawValue
                            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => $"\"{v.Trim()}\"");
                        rawValue = $"[{string.Join(",", parts)}]";
                    }
                    // Now check if it's valid JSON (array or object)
                    if ((rawValue.StartsWith("[") && rawValue.EndsWith("]")) ||
                        (rawValue.StartsWith("{") && rawValue.EndsWith("}")))
                    {
                        try
                        {
                            var targetType = prop.PropertyType;

                            // Handle strings wrapped in quotes that are arrays
                            if (targetType == typeof(string))
                            {
                                prop.SetValue(result, rawValue);
                            }
                            else
                            {
                                var deserialized = JsonConvert.DeserializeObject(rawValue, targetType);
                                prop.SetValue(result, deserialized);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Deserialization error for property {prop.Name}: {ex.Message}");
                        }
                    }
                }

                return result;
            });
        }




    }
}
