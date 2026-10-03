using Bioreference.LIS.Helper;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Bioreference.LIS
{
    public abstract class ProxyBase
    {

        protected internal readonly string _headerClientApplicationName = "clientApplication";
        protected internal readonly string _headerUserName = "userName";
        protected internal readonly string _headerMachineName = "machineName";
        protected internal readonly string _headerApplicationId = "applicationId";
        protected internal readonly string _urlBaseKey = "Bioreference.LIS:urlBase";
        protected internal readonly string _headerToken = "Token";

        protected internal string _userName = "";
        protected internal string _machineName = "";
        protected internal string _baseAddress = "";
        protected internal string _clientApplication = "";
        protected internal int _applicationId = 0;
        protected internal bool _throwException = false;
        protected internal IToken _token;
        protected internal string _authToken;

        protected ProxyBase()
        {
            GetConfigs();
        }
        protected ProxyBase(string token, bool throwException)
        {
            _authToken = token;
            _throwException = throwException;
            GetConfigs();
        }
        protected ProxyBase(string token)
        {
            _authToken = token;
            GetConfigs();
        }

        protected ProxyBase(string userName, string machineName, IToken token)
        {
            this._token = token;
            _userName = userName;
            _machineName = machineName;
            GetConfigs();
        }

        protected ProxyBase(string userName, string machineName, IToken token, bool throwException)
        {
            _token = token;
            _userName = userName;
            _throwException = throwException;
            GetConfigs();
        }
        void GetConfigs()
        {
            var setting = Configuration.AppSettings[_urlBaseKey];
            if (setting != null)
                _baseAddress = setting.ToString();
            else
                _baseAddress = Configuration.AppSettings["Bioreference.LIS:urlBase"].ToString();

            var settingAppId = Configuration.AppSettings["applicationId"];
            if (settingAppId != null)
                _applicationId = int.Parse(settingAppId.ToString());
        }

        public string UserName
        {
            get { return _userName; }
        }

        public string BaseAddress
        {
            get { return _baseAddress; }
        }

        private string _errorMessage;
        public string ErrorMessage 
        { 
            get => _errorMessage;
            set => _errorMessage = value.NormalizeToWindows();
        }

        public System.Net.HttpStatusCode HttpCode { get; set; }

        public bool IsSuccess { get; set; }

        /// <summary>
        /// Call webapi using GET
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        protected T Get<T>(string url, int version)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(Get(url, version), typeof(T));
            }
            return JsonConvert.DeserializeObject<T>(Get(url, version));
        }

        /// <summary>
        /// Call webapi using PUT
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <typeparam name="U">Parameter Type</typeparam>
        /// <param name="url"></param>
        /// <param name="entity"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        protected T Put<T, U>(string url, U entity, int version)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(Put<U>(url, entity, version), typeof(T));
            }
            return JsonConvert.DeserializeObject<T>(Put(url, entity, version));
        }

        protected T PutFromEncoded<T, U>(string url, U entity, int version)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(PutFromEncodedContent<U>(url, entity, version), typeof(T));
            }
            return JsonConvert.DeserializeObject<T>(Put(url, entity, version));
        }

        /// <summary>
        /// Call webapi using Post
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <typeparam name="U">Parameter Type</typeparam>
        /// <param name="url"></param>
        /// <param name="entity"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        protected T Post<T, U>(string url, U entity, int version)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(Post<U>(url, entity, version), typeof(T));
            }
            return JsonConvert.DeserializeObject<T>(Post(url, entity, version));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [Obsolete("Method created to test. Use instead  T Get<T>(string url, int version)")]
        protected async Task<T> GetAsync<T>(string url, int version)
        {
            var result = await GetAsync(url, version);
            return JsonConvert.DeserializeObject<T>(result);
        }


        #region Callers

        private string Get(string url, int version)
        {
            using (var client = new HttpClient())
            {
                client.PrepareHeaders(this, url, version);

                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                HttpCode = response.StatusCode;
                IsSuccess = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    var dat = response.Content.ReadAsStringAsync().Result;
                    return dat;
                }
                ErrorMessage = response.Content.ReadAsStringAsync().Result;
                return "";
            }
        }

        /// <summary>
        /// Add object to the HTTP body
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="bodyObject"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private string Post<U>(string url, U bodyObject, int version)
        {
            using (var client = new HttpClient())
            {
                string json = JsonConvert.SerializeObject(bodyObject);
                HttpContent content = new StringContent(json);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                client.PrepareHeaders(this, url, version);

                HttpResponseMessage response = client.PostAsync(url, content).Result;
                response.EnsureSuccessStatusCode();
                HttpCode = response.StatusCode;
                IsSuccess = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    var dat = response.Content.ReadAsStringAsync().Result;
                    return dat;
                }
                ErrorMessage = response.Content.ReadAsStringAsync().Result;
                return "";
            }
        }

        /// <summary>
        /// Update server object
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="bodyObject"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private string Put<U>(string url, U bodyObject, int version)
        {
            using (var client = new HttpClient())
            {
                string json = JsonConvert.SerializeObject(bodyObject);
                HttpContent content = new StringContent(json);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                client.PrepareHeaders(this, url, version);

                HttpResponseMessage response = client.PutAsync(url, content).Result;
                response.EnsureSuccessStatusCode();
                HttpCode = response.StatusCode;
                IsSuccess = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    var dat = response.Content.ReadAsStringAsync().Result;
                    return dat;
                }
                ErrorMessage = response.Content.ReadAsStringAsync().Result;
                return "";
            }
        }
        private string PutFromEncodedContent<U>(string url, U bodyObject, int version)
        {
            using (var client = new HttpClient())
            {
                string json = JsonConvert.SerializeObject(bodyObject);
                HttpContent content = new StringContent(json);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                client.PrepareHeaders(this, url, version);

                HttpResponseMessage response = client.PutAsync(url, content).Result;
                response.EnsureSuccessStatusCode();
                HttpCode = response.StatusCode;
                IsSuccess = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    var headers = response.Headers;
                    IEnumerable<string> values;
                    if (headers.TryGetValues("Token", out values))
                        return values.First();

                }
                ErrorMessage = response.Content.ReadAsStringAsync().Result;
                return "";
            }
        }

        /// <summary>
        /// Call a webApi using Delete
        /// </summary>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        protected string Delete(string url, int version)
        {
            using (var client = new HttpClient())
            {
                client.PrepareHeaders(this, url, version);

                HttpResponseMessage response = client.DeleteAsync(url).Result;
                response.EnsureSuccessStatusCode();
                HttpCode = response.StatusCode;
                IsSuccess = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    var dat = response.Content.ReadAsStringAsync().Result;
                    return dat;
                }
                ErrorMessage = response.Content.ReadAsStringAsync().Result;
                return "";
            }
        }

        private async Task<string> GetAsync(string url, int version)
        {
            using (var client = new HttpClient())
            {
                client.PrepareHeaders(this, url, version);

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                HttpCode = response.StatusCode;
                IsSuccess = response.IsSuccessStatusCode;
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                ErrorMessage = await response.Content.ReadAsStringAsync();
                return "";
            }
        }

        #endregion
    }

    internal static class ExtensionHttp
    {
        internal static HttpClient PrepareHeaders(this HttpClient client, ProxyBase baseClass, string url, int version)
        {
            client.BaseAddress = new Uri(baseClass._baseAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add(baseClass._headerClientApplicationName, baseClass._clientApplication);
            if (baseClass._applicationId > 0)
                client.DefaultRequestHeaders.Add(baseClass._headerApplicationId, baseClass._applicationId.ToString());
            if (!String.IsNullOrEmpty(baseClass._userName))
                client.DefaultRequestHeaders.Add(baseClass._headerUserName, baseClass._userName);
            if (!String.IsNullOrEmpty(baseClass._authToken) || (baseClass._token != null && baseClass._token.ApiToken != null))
            {
                if (baseClass._token != null && baseClass._token.ApiToken != null)
                    baseClass._authToken = baseClass._token.ApiToken.ToString();

                client.DefaultRequestHeaders.Add(baseClass._headerToken, baseClass._authToken);
            }
            if (!String.IsNullOrEmpty(baseClass._machineName))
                client.DefaultRequestHeaders.Add(baseClass._headerMachineName, baseClass._machineName);


            if (version != 0)
            {
                url = url.Substring(0, 4) + "v" + version.ToString() + "/" + url.Substring(4);
                client.DefaultRequestHeaders.Add("Apiversion", version.ToString());
            }

            return client;
        }

    }
}
