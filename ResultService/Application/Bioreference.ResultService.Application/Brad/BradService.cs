using AutoMapper;
using Bioreference.Data.Logging;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Brad;
using Bioreference.ResultService.Application.Model;
using Common.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Brad
{
    public class BradService : IBradService
    {
        private readonly IMapper _mapper;

        public BradService(IMapper mapper)
        {
            _mapper = mapper;
        }

        private IWebClientManager objWebClient = null;
  
        public async Task<List<SpecimenTypeInfo>> GetSpecimenTypeList()
        {
            string url = "/api/SpecimenTypes";
            try
            {
                objWebClient = GetBradClient();
                string response = await Task.Run(() => objWebClient.GetResponse(url));

                var specimenTypeInfoList = string.IsNullOrWhiteSpace(response)
                    ? new List<SpecimenTypeInfo>()
                    : JsonConvert.DeserializeObject<List<SpecimenTypeInfo>>(response) ?? new List<SpecimenTypeInfo>();

                return specimenTypeInfoList;
            }
            catch (Exception ex)
            {
                return new List<SpecimenTypeInfo>(); // Return an empty list instead of throwing an exception
            }
        }


        public async Task<List<SpecimenMappedInfo>> GetMappedSpecimens(List<PendingBradSearchCriteria> SpecList)
        {
            if (SpecList == null || SpecList.Count == 0)
            {
                return new List<SpecimenMappedInfo>();
            }

            var specIdArray = SpecList.Select(item => new AccessionNbr(item.AccessionNbr).Full).ToList();
            string requestSpecimens = string.Join(",", specIdArray);
            string url = "/api/MappedSpecimen";
            string response = "";

            try
            {
                objWebClient = GetBradClient();
                response = await Task.Run(() => objWebClient.PostRequest(url, new[] { "specimenNumbers" }, new[] { requestSpecimens }));

                var specimenMappedInfoList = string.IsNullOrWhiteSpace(response)
                    ? new List<SpecimenMappedInfo>()
                    : JsonConvert.DeserializeObject<List<SpecimenMappedInfo>>(response) ?? new List<SpecimenMappedInfo>();


                if (!specimenMappedInfoList.Any())
                {
                    return new List<SpecimenMappedInfo>();
                }

                specIdArray.RemoveAll(id => specimenMappedInfoList.Any(info => id == new AccessionNbr(info.SpecimenNumber).Full));

                return specimenMappedInfoList;
            }
            catch (Exception ex)
            {
                return new List<SpecimenMappedInfo>();
            }
        }
        public async Task<List<string>> GetMappedAccessions(List<List<string>> splits, string fridgeId)
        {
            List<string> listOfFilteredAccessions = new List<string>();

            if (splits == null)
                return listOfFilteredAccessions; 

            for (int n = 0; n < splits.Count; n++)
            {
                if (splits[n] == null)
                    continue;

                string SpecimenNumbers = String.Join(",", splits[n]);
                string url = $"/api/MappedSpecimen/GetMappedSpecimens?SpecimenNumbers={SpecimenNumbers}&FridgeID={fridgeId}";
                
                objWebClient = GetBradClient();
                var response = await Task.Run(() => objWebClient.GetResponse(url));

                if (!string.IsNullOrEmpty(response))
                {
                    List<string>? mappedSpecimens = JsonConvert.DeserializeObject<List<string>>(response);

                    if (mappedSpecimens != null)
                    {
                        listOfFilteredAccessions.AddRange(TrimAccessionNbrList(mappedSpecimens));
                    }
                }
            }

            return listOfFilteredAccessions;
        }
        private List<string> TrimAccessionNbrList(List<string> responseList)
        {
            List<string> list = new List<string>();

            if (responseList.Count > 0)
            {
                foreach (string accession in responseList)
                {
                    list.Add(new AccessionNbr(accession).ToSevenDigits());
                }
            }

            return list;
        }
        public async Task<List<string>> GetWalkInFridges()
        {
            string url = "/api/SystemProperty?name=WalkInFridges";
            try
            {
                objWebClient = GetBradClient();
                string response = await Task.Run(() => objWebClient.GetResponse(url));

                var walkInFridgeIdList = string.IsNullOrWhiteSpace(response)
                    ? new List<string>()
                    : JsonConvert.DeserializeObject<List<string>>(response) ?? new List<string>();

                return walkInFridgeIdList;
            }
            catch (Exception ex)
            {
                return new List<string>(); 
            }
        }    
        public async Task<List<FridgeInfoModel>> GetFridge()
        {
            string url = "/api/Fridge";
            try
            {
                objWebClient = GetBradClient();
                string response = await Task.Run(() => objWebClient.GetResponse(url));

                var fridgeList = string.IsNullOrWhiteSpace(response)
                    ? new List<FridgeInfoModel>()
                    : JsonConvert.DeserializeObject<List<FridgeInfoModel>>(response) ?? new List<FridgeInfoModel>();

                return fridgeList;
            }
            catch (Exception ex)
            {
                return new List<FridgeInfoModel>(); 
            }
        }

      
        public async Task<List<RequestSpecimenResponseStatus>> SendToBRAD(CallToBradSearchCriteria sendToBradDTO)
        {
            var responseStatus = new List<RequestSpecimenResponseStatus>();

            if (sendToBradDTO.walkInList.Count > 0)
            {
                responseStatus.AddRange(await Task.Run(() => SendWalkInToBRAD(_mapper.Map<List<SpecimenRequestModel>>(sendToBradDTO.walkInList))));
            }

            if (sendToBradDTO.nonWalkInList.Count > 0)
            {
                responseStatus.AddRange(await Task.Run(() => SendNonWalkInToBRAD(_mapper.Map<List<SpecimenRequestModel>>(sendToBradDTO.nonWalkInList))));
            }           
            return responseStatus;
        }
        private RequestSpecimenResponseStatus CreateResponseStatusObj(string specimenNumber, string fridgeType, string status, string errorMessage, string batchNumber = "")
        {
            return new RequestSpecimenResponseStatus
            {
                SpecimenNumber = specimenNumber,
                ErrorMessage = errorMessage,
                RequestTypeFridge = fridgeType,
                Status = status,
                BatchNumber = batchNumber
            };
        }
        public async Task<List<RequestSpecimenResponseStatus>> SendWalkInToBRAD(List<SpecimenRequestModel> listRequest)
        {
            string url = "/api/B2Request/SubmitWalkinRequestMulti";
            string response = "";
            int APP_ID = 7;
            var responseStatus = new List<RequestSpecimenResponseStatus>();
            try
            {
                if (listRequest.Count > 0)
                {
                    objWebClient = GetBradClient();
                    string payload = JsonConvert.SerializeObject(listRequest);
                    response = await Task.Run(() => objWebClient.PostRequestWithJson(url, payload));
                    var obj = JsonConvert.DeserializeObject<BRADSpecimenRequestResponse>(response);

                    if (obj.SpecimenResponseDetails.Count > 0)
                    {
                        foreach (var request in listRequest)
                        {
                            foreach (var responseDetails in obj.SpecimenResponseDetails)
                            {
                                if (new AccessionNbr(request.SpecimenNumbers).Full == new AccessionNbr(responseDetails.SpecimenNumber).Full)
                                {
                                    responseStatus.Add(responseDetails.Status == BradResponseStatus.Success.ToString()
                              ? CreateResponseStatusObj(request.SpecimenNumbers, FridgeType.NonWalkIn.ToString(), BradResponseStatus.Success.ToString(), null, responseDetails.SpecimenBatchNumber)
                              : CreateResponseStatusObj(request.SpecimenNumbers, FridgeType.NonWalkIn.ToString(), BradResponseStatus.Fail.ToString(), responseDetails.ErrorMessage));

                                }
                            }
                        }                       
                    }
                    Data.Logging.LogManager.LogNewEvent(APP_ID, response, LogLevelType.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception occurred while making specimen request.");
                Data.Logging.LogManager.LogNewEvent(APP_ID, ex.ToString(), LogLevelType.Failure);    
                throw;
            }
            return responseStatus;
        }
        public async Task<List<RequestSpecimenResponseStatus>> SendNonWalkInToBRAD(List<SpecimenRequestModel> listRequest)
        {
        
            string url = "/api/B2Request/SubmitRequestMulti";
            string response = "";
            int APP_ID = 7;
            var responseStatus = new List<RequestSpecimenResponseStatus>();
            try
            {
                if (listRequest.Count > 0)
                {
                    objWebClient = GetBradClient();
                    string payload = JsonConvert.SerializeObject(listRequest);
                    response = await Task.Run(() => objWebClient.PostRequestWithJson(url, payload));
                    var obj = JsonConvert.DeserializeObject<BRADSpecimenRequestResponse>(response);

                    if (obj.SpecimenResponseDetails.Count > 0)
                    {
                        foreach (var request in listRequest)
                        {
                            foreach (var responseDetails in obj.SpecimenResponseDetails)
                            {
                                if (new AccessionNbr(request.SpecimenNumbers).Full == new AccessionNbr(responseDetails.SpecimenNumber).Full)
                                {
                                    responseStatus.Add(responseDetails.Status == BradResponseStatus.Success.ToString()
                         ? CreateResponseStatusObj(request.SpecimenNumbers, FridgeType.NonWalkIn.ToString(), BradResponseStatus.Success.ToString(), null, responseDetails.SpecimenBatchNumber)
                         : CreateResponseStatusObj(request.SpecimenNumbers, FridgeType.NonWalkIn.ToString(), BradResponseStatus.Fail.ToString(), responseDetails.ErrorMessage));
                                }
                            }
                        }
                     
                    }
                    Data.Logging.LogManager.LogNewEvent(APP_ID, response, LogLevelType.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception occurred while making specimen request.");
                Data.Logging.LogManager.LogNewEvent(APP_ID, ex.ToString(), LogLevelType.Failure);
            }
            return responseStatus;
        }

        public async Task<RequestSpecimenInfo> CheckRequestable(List<PendingBradSearchCriteria> SpecList)
        {
            RequestSpecimenInfo requestSpecimenInfo = new RequestSpecimenInfo();
            try
            {
                requestSpecimenInfo = await GetRequestSpecimenInfo(SpecList);
                if (requestSpecimenInfo == null || requestSpecimenInfo.Specimens == null || requestSpecimenInfo.Specimens.Count == 0)
                {
                    return requestSpecimenInfo;
                }
              
            }
            catch (Exception ex)
            {
                throw;
            }
            return requestSpecimenInfo;
        }

        public async Task<Dictionary<int, string>> GetRequestDepartments(List<PendingBradSearchCriteria> SpecList)
        {
            Dictionary<int, string> requestDepartmentDic = new Dictionary<int, string>();
            try
            {
                var response = await GetRequestSpecimenInfo(SpecList);
                requestDepartmentDic = response.Specimens
                                       .SelectMany(specimen => specimen.SpecimenRequestDepts)
                                       .GroupBy(reqdept => reqdept.RequestDeptID)
                                       .ToDictionary(group => group.Key, group => group.First().RequestDeptName);

            }
            catch (Exception ex)
            {
                throw;
            }
            return requestDepartmentDic;
        }

        private async Task<RequestSpecimenInfo> GetRequestSpecimenInfo(List<PendingBradSearchCriteria> SpecList)
        {
            string url = "/api/B2Request/checkRequestableMulti";
            string response = "";
            RequestSpecimenInfo requestSpecimenInfo = new RequestSpecimenInfo();
            try
            {               
                    objWebClient = GetBradClient();
                    var list = new List<object>();
                    var specimens = await Task.Run(() => GetMappedSpecimens(SpecList));
                    foreach (var specimen in specimens)
                    {
                        dynamic obj = new System.Dynamic.ExpandoObject();
                        obj.SpecimenNumber = specimen.SpecimenNumber;
                        obj.SpecimenTypeId = specimen.SpecimenTypeID;
                        list.Add(obj);
                    }
                    string payload = JsonConvert.SerializeObject(list);
                    response = await Task.Run(() => objWebClient.PostRequestWithJson(url, payload));
                    requestSpecimenInfo = JsonConvert.DeserializeObject<RequestSpecimenInfo>(response);
                    if (requestSpecimenInfo == null || requestSpecimenInfo.Specimens == null || requestSpecimenInfo.Specimens.Count == 0)
                    {
                        Console.WriteLine("No requestable specimen found in BRAD.");
                    }
                          
            }
            catch (Exception ex)
            {
                Console.WriteLine("Some error occurred while trying to fetch BRAD requestable specimens");
                throw;
            }
            return requestSpecimenInfo;
        }

        private IWebClientManager GetBradClient()
        {
            if (objWebClient == null)
            {
                BRADSettings bradSettings = new BRADSettings();
                bradSettings = GetBRADSettings(bradSettings);
                objWebClient = new BRADClientManager(bradSettings);
            }
            return objWebClient;
        }

        private BRADSettings GetBRADSettings(BRADSettings bradSettings)
        {          
                return new BRADSettings
                {
                    BRADAPIBaseURL = Bioreference.LIS.Configuration.B2Settings.GetString("BRADAPIBaseURL"),
                    BRADAPIPassword = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(Bioreference.LIS.Configuration.B2Settings.GetString("BRADAPIPassword"))),
                    BRADAPIToken_Timeout = Bioreference.LIS.Configuration.B2Settings.GetString("BRADAPIToken_Timeout"),
                    BRADAPIUsername = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(Bioreference.LIS.Configuration.B2Settings.GetString("BRADAPIUsername"))),
                    BRADAPIWindowsUsername = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(Bioreference.LIS.Configuration.B2Settings.GetString("BRADAPIWindowsUsername")))
                };     
           
        }
    }
}
