using System;
using System.Collections.Generic;
using RestSharp;

namespace Bioreference.LIS
{

    public class RequisitionClientManager
    {
        private RequisitionSettings _settings = null;
        private string _token = string.Empty;
        private static readonly string LoggerName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        private static readonly ILog Log = LogManager.GetLogger(LoggerName);

        public RequisitionClientManager(RequisitionSettings settings)
        {
            _settings = settings;
        }

        private void Initiailize()
        {
            if (string.IsNullOrEmpty(_token))
            {
                InitiailizeToken();
            }
        }

        public RequisitionResponse GetResponse(RequisitionRequest requestDetails)
        {
            var returnValue = new RequisitionResponse();

            try
            {
                Initiailize();
                Log.Debug(_settings.RequisitionStatusURL);

                var client = new RestClient(_settings.RequisitionStatusURL);

                AdjustAccessionNumber(requestDetails);

                string requestBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestDetails);
                Log.Debug(requestBody);

                var request = new RestRequest();
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/json");
                request.AddHeader("token", _token);
                request.AddParameter("application/json", requestBody, ParameterType.RequestBody);

                RestResponse response = client.ExecutePost(request);
                Log.DebugFormat("Response Status {0}", response.StatusCode);

                if (string.IsNullOrEmpty(response.Content))
                {
                    Log.Debug("Response content is Empty");
                    returnValue.Accessions = new List<RequisitionResponseContent>();
                }
                else
                {
                    Log.DebugFormat("Response content {0}", response.Content);
                    returnValue = Newtonsoft.Json.JsonConvert.DeserializeObject<RequisitionResponse>(response.Content);
                    if (returnValue != null)
                    {
                        AdjustAccessionNumber(returnValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            return returnValue;
        }

        private void InitiailizeToken()
        {
            RestClient client = new RestClient(_settings.RequisitionTokenURL);
            TokenResponse obj;
            var request = new RestRequest()
                          .AddHeader("cache-control", "no-cache")
                          .AddHeader("content-type", "application/x-www-form-urlencoded")
                          .AddParameter("application/x-www-form-urlencoded",
                           $"client_id={_settings.RequisitionClientId}&client_secret={_settings.RequisitionClientSecret}&grant_type={_settings.RequisitionGrantType}",
                           ParameterType.RequestBody);

            RestResponse response = client.ExecutePost(request);
            // Log.Debug(response.Content)
            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(response.Content);
            if (obj is not null)
            {
                _token = obj.access_token;
            }
            else
            {
                throw new Exception("There is some problem in making the token request.");
            }
        }

        private void AdjustAccessionNumber(RequisitionRequest request)
        {
            var accessionsList = new List<string>();
            foreach (string accessionNumber in request.Accessions)
                accessionsList.Add(new AccessionNbr(accessionNumber).Full);
            request.AddAccession(accessionsList, true);
        }

        private void AdjustAccessionNumber(RequisitionResponse response)
        {
            if (response != null)
            {
                if (response.Accessions != null && response.Accessions.Count > 0)
                {
                    foreach (RequisitionResponseContent requistionResponseContent in response.Accessions)
                        requistionResponseContent.AccessionNumber = new AccessionNbr(requistionResponseContent.AccessionNumber).ToSevenDigits();
                }
            }
        }

        public class TokenResponse
        {
            public string access_token;
            public string expires_in;
            public string token_type;
            public string scope;
        }

        public class RequisitionResponse
        {
            public List<RequisitionResponseContent> Accessions;
        }

        public class RequisitionResponseContent
        {
            public string AccessionNumber;
            public List<DateTime> ItemDate;
        }
    }
}