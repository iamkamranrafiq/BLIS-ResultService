using Bioreference.ResultService.Application.Model;
using Microsoft.Extensions.Configuration;

namespace Bioreference.ResultService.Abstractions.Application.Setting
{
    public interface ISettingService
    {
        public Task<Dictionary<string, string?>> GetResultSettings();
        public string GetConnectionString(string name);
        public Task<T> FetchSetting<T>(string connectionString, string settingKey, string instance = "") where T : class, new();
    }
}
