using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Bioreference.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Bioreference.LIS
{
    public static class SettingsExtension
    {
        public static IConfiguration BuildSettings()
        {
            string configFileDirectory =
                Environment.GetEnvironmentVariable("CONFIG_DIR")
                ?? AppContext.BaseDirectory
                ?? Directory.GetCurrentDirectory();

            configFileDirectory = Path.GetFullPath(configFileDirectory);

            var environment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Production";
            
            var runtimeRoot = AppContext.BaseDirectory
                ?? Directory.GetCurrentDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(configFileDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile(
                    Path.Combine(runtimeRoot, $"connections.{environment}.json"),
                    optional: true,
                    reloadOnChange: true
                )
                .AddEnvironmentVariables()
                .Build();


            return config;
        }



        public static IConfiguration Fetch(string connection, string app)
        {
            return Fetch(connection, app, "");
        }

        public static IConfiguration Fetch(string connection, string app, string instance)
        {
            try
            {
                var keys = FetchKeys(connection, app, instance);
                var builder = (ConfigurationBuilder)new ConfigurationBuilder().AddInMemoryCollection(keys);
                var config = builder.Build();
                return config;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to Fetch Settings.", ex);
            }
        }

        public static Dictionary<string, string?> FetchKeys(string connection, string app, string instance)
        {
            const int col_SettingKeyName = 3;
            const int col_SettingKeyValue = 5;
            const int col_SettingType = 4;
            var keys = new Dictionary<string, string?>();
            try
            {
                using (var cn = new SqlConnection()
                {
                    ConnectionString = connection
                })
                {
                    cn.Open();
                    using (var cmd = new SqlCommand()
                    {
                        Connection = cn,
                        CommandType = CommandType.StoredProcedure,
                        CommandText = "AppSettings_Fetch"
                    })
                    {
                        cmd.Parameters.AddWithValue("@SettingApp", app);
                        cmd.Parameters.AddWithValue("@SettingInstance", instance);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string key = rdr.GetString(col_SettingKeyName).Trim();
                                string value = rdr.GetString(col_SettingKeyValue).Trim();
                                string type = rdr.GetString(col_SettingType).Trim();
                                // do not use TryAdd method as we wish to update the value if the key already exists (instancing).
                                if (keys.ContainsKey(key))
                                {
                                    keys[key] = value;
                                }
                                else
                                {
                                    if(!value.Trim().Contains('|') &&  (type.Trim() == "list" || type.Trim() == "string[]"))
                                    {
                                        keys.Add(key, (value + "|"));
                                    }
                                    else
                                    {
                                        keys.Add(key, value); 
                                    }
                                }
                            }
                            rdr.Close();
                        }
                    }
                    cn.Close();
                }
                return keys;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to Fetch Settings.", ex);
            }
        }

        public static T? GetSetting<T>(this IConfiguration config, string key)
        {
            if (config == null)
                throw new Exception("Settings not initialized");
            if (config[key] == null)
                throw new Exception("Missing setting");
            return (T?)Convert.ChangeType(config[key], typeof(T));
        }

        public static T? GetSetting<T>(this IConfiguration config, string key, object defaultValue)
        {
            if (config == null)
                throw new Exception("Settings not initialized");
            if (config[key] == null)
                return (T?)defaultValue;
            return (T?)Convert.ChangeType(config[key], typeof(T));
        }

        public static int GetInt(this IConfiguration config, string key)
        {
            return GetSetting<int>(config, key, 0);
        }

        public static long GetLong(this IConfiguration config, string key)
        {
            return GetSetting<long>(config, key, 0);
        }

        public static bool GetBool(this IConfiguration config, string key)
        {
            bool? overridden = ConfigurationOverrides.GetOverride<bool?>(key);

            if (overridden.HasValue)
            {
                return overridden.Value;
            }

            return GetSetting<bool>(config, key, false);
        }

        public static string GetString(this IConfiguration config, string key)
        {
            string? overridden = ConfigurationOverrides.GetOverride<string>(key);

            if (overridden != null)
            {
                return overridden;
            }

            return GetSetting<string>(config, key) ?? string.Empty;
        }

        public static DateTime GetDateTime(this IConfiguration config, string key)
        {
            return GetSetting<DateTime>(config, key);
        }

        public static IEnumerable<string> GetList(this IConfiguration config, string key)
        {
            List<string> list = new List<string>();
            if (config == null)
                throw new Exception("Settings not initialized");

            if (config[key] != null)
            {
                string value = Convert.ChangeType(config[key], typeof(string)).ToString() ?? "";
                string[] values = value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                list.AddRange(values);
            }
            return list;
        }
    }
}
