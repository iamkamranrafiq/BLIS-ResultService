using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class BRADClientManager : IWebClientManager
    {

        private string _baseurl = string.Empty;
        private BRADSettings _settings = null;
        private string WEBAPI_METHOD_PUT = "PUT";
        private string WEBAPI_METHOD_GET = "GET";
        private string WEBAPI_METHOD_POST = "POST";
        private string _token = "";
        private DateTime _lastAPICallDT = default;
        private int _tokenTimeout = 1;

        public BRADClientManager(BRADSettings settings)
        {
            _settings = settings;
            _baseurl = _settings.BRADAPIBaseURL;
            _tokenTimeout = Conversions.ToInteger(Interaction.IIf(Conversions.ToInteger(_settings.BRADAPIToken_Timeout) == 0, 20, Conversions.ToInteger(_settings.BRADAPIToken_Timeout)));
        }


        private string getToken()
        {
            string authurl = "/api/Authenticate";
            try
            {
                if (_lastAPICallDT == default || _lastAPICallDT.AddMinutes(_tokenTimeout) < DateTime.Now)
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (var client = new WebClient())
                    {
                        var values = new NameValueCollection();
                        values["UserName"] = _settings.BRADAPIUsername;
                        values["Password"] = _settings.BRADAPIPassword;
                        values["WindowsUserName"] = _settings.BRADAPIWindowsUsername;
                        object response = client.UploadValues(_baseurl + authurl, WEBAPI_METHOD_PUT, values);
                        object responseString = Encoding.Default.GetString((byte[])response);
                        _token = client.ResponseHeaders["Token"];
                    }
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return _token;

        }

        public string GetResponse(string url)
        {
            string sResponse = "";

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Headers.Add("Token", getToken());
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    sResponse = client.DownloadString(_baseurl + url);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            _lastAPICallDT = DateTime.Now;

            return sResponse;
        }

        public string PostRequest(string url, string[] paramNames, string[] paramValues)
        {
            string sResponse = "";

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    var values = new NameValueCollection();
                    for (int i = 0, loopTo = paramNames.Length - 1; i <= loopTo; i++)
                        values[paramNames[i]] = paramValues[i];

                    client.Headers.Add("Token", getToken());
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    sResponse = Encoding.Default.GetString(client.UploadValues(_baseurl + url, values));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return sResponse;
        }

        public string PostRequestWithJson(string url, string json)
        {
            string sResponse = "";

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Headers.Add("Token", getToken());
                    client.Headers.Add("Content-Type", "application/json");
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    sResponse = client.UploadString(_baseurl + url, json);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return sResponse;
        }

        public string PostRequestUri(string url)
        {
            string sResponse = "";

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    var values = new NameValueCollection();
                    client.Headers.Add("Token", getToken());
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    sResponse = Encoding.Default.GetString(client.UploadValues(_baseurl + url, values));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return sResponse;
        }
    }
}