namespace Bioreference.LIS
{
    /// <summary>
    /// Request/async-scoped overrides for configuration values.
    /// Set from the API (or other host) at the start of a logical operation; cleared when the operation ends.
    /// Used when a value (e.g. ToFollowEnabled) is provided per-request and must override static config.
    /// </summary>
    public static class ConfigurationOverrides
    {
        private static readonly AsyncLocal<Dictionary<string, object>?> Current = new AsyncLocal<Dictionary<string, object>?>();        

        /// <summary>
        /// Set an override for a configuration key. Override is used by GetBool/GetSetting when present.
        /// </summary>
        public static void SetOverride(string key, object value)
        {
            var dict = Current.Value;
            if (dict == null)
            {
                dict = new Dictionary<string, object>();
                Current.Value = dict;
            }
            dict[key] = value;
        }

        /// <summary>
        /// Get an override for a key, if set. Returns null if no override.
        /// </summary>
        public static T? GetOverride<T>(string key)
        {
            var dict = Current.Value;
            if (dict == null || !dict.TryGetValue(key, out var value))
                return default;
            if (value is T t)
                return t;
            // Unbox value types (e.g. bool stored as object)
            if (typeof(T) == typeof(bool?) && value is bool b)
                return (T)(object)(bool?)b;
            return default;
        }

        /// <summary>
        /// Clear all overrides for the current async context (e.g. at end of request).
        /// </summary>
        public static void Clear()
        {
            Current.Value = null;
        }
    }
}
