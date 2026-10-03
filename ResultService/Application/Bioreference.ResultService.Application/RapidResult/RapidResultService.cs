using AutoMapper;
using Bioreference.Data.Client;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.RapidResult;
using System.Data.Common;
using System.Data;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Common.Common.RapidResult;
using Bioreference.ResultService.Abstractions.Application.Common;

namespace Bioreference.ResultService.Application.RapidResult
{
    public class RapidResultService : IRapidResultService
    {
        private readonly IMapper _mapper;
        private readonly RapidEventProcessorService _eventProcessorService;
        private readonly ICommonResultService _commonResult;
        public RapidResultService(IMapper mapper, RapidEventProcessorService eventProcessorService, ICommonResultService commonResult)
        {
            _mapper = mapper;
            _eventProcessorService = eventProcessorService;
            _commonResult = commonResult;
        }

        public async Task<RapidResultTemplateModel> RapidResultTemplateDetail(int templateId)
        {
            RapidResultTemplate rapidResultTemplates = await Task.Run(() => Bioreference.LIS.RapidResultTemplate.Fetch(templateId));
            RapidResultTemplateModel mappedResponse = _mapper.Map<RapidResultTemplateModel>(rapidResultTemplates);
            return mappedResponse;
        }

        public async Task<List<RapidResultsModel>> RapidResults(int templateId, bool isCompleted = false, int pageNo = 0, int pageSize = 0)
        {
            RapidResults rapidResults = await Task.Run(() => Bioreference.LIS.RapidResults.Fetch(isCompleted, templateId, pageNo, pageSize));
            RapidResultTemplate rapidResultTemplate = await Task.Run(() => Bioreference.LIS.RapidResultTemplate.Fetch(templateId));
            List<RapidResultsModel> rapidResultsModels = new List<RapidResultsModel>();

            
            if (rapidResults == null || rapidResults.List.Count() == 0)
            {
                RapidResultsModel templateOnlyModel = new RapidResultsModel
                {
                    Analytes = new List<AnalyteRRModel>()
                };

                foreach (RapidResultTemplateAnalyte analyte in rapidResultTemplate.List)
                {
                    templateOnlyModel.Analytes.Add(new AnalyteRRModel
                    {
                        AnalyteCode = analyte.AnalyteCode,
                        AnalyteName = analyte.AnalyteName,
                        Default = analyte.DefaultValue,
                        Value = "" 
                    });
                }

                rapidResultsModels.Add(templateOnlyModel);
                return rapidResultsModels; 
            }

            
            foreach (LIS.RapidResult item in rapidResults.List)
            {
                RapidResultsModel rapidResultsModel = new RapidResultsModel
                {
                    CreatedBy = item.CreatedBy,
                    DateCreated = item.DateCreated,
                    Id = item.Id,
                    Analytes = new List<AnalyteRRModel>()
                };

                foreach (RapidResultTemplateAnalyte analyte in rapidResultTemplate.List)
                {
                    var analyteModel = new AnalyteRRModel
                    {
                        AnalyteCode = analyte.AnalyteCode,
                        AnalyteName = analyte.AnalyteName,
                        Default = analyte.DefaultValue
                    };

                    var pendingCount = item.GetPendingResultCount(analyteModel.AnalyteCode, false);
                    if (pendingCount == -1)
                    {
                        analyteModel.Value = "";
                    }
                    else
                    {
                        analyteModel.Value = $"{pendingCount}/{item.GetResultCount(analyteModel.AnalyteCode, false)}";
                    }

                    rapidResultsModel.Analytes.Add(analyteModel);
                }

                rapidResultsModels.Add(rapidResultsModel);
            }

            return rapidResultsModels;
        }

        public async Task<CheckAccessionResponseModel> CheckAddedAccession(AccessionOrderModel request)
        {
            Bioreference.LIS.RapidResult rapidWorkSheet = null;
            AddAccessionStatusType status = AddAccessionStatusType.InvalidStatus;

            // Create or Fetch RapidResult
            if (request.templateId != null)
            {
                rapidWorkSheet = await Task.Run(() =>
                    Bioreference.LIS.RapidResult.CreateRapidResult(request.templateId.Value));
            }
            else if (request.rapidResultId != null)
            {
                rapidWorkSheet = await Task.Run(() =>
                    LIS.RapidResult.Fetch(request.rapidResultId.Value));
            }

            AccessionOrderResultModel accessionOrderResultModel;

            if (rapidWorkSheet != null)
            {
                status = rapidWorkSheet.AddOrder(request.AccessionNbr, request.IsControl);

                var rapidResultTemplates = await Task.Run(() =>
                    Bioreference.LIS.RapidResultTemplates.Fetch());

                bool requiresVerification =
                    rapidResultTemplates
                        .Find(rapidWorkSheet.RapidResultTemplateId)
                        .RequiresVerification;

               
                var finalStatus =
                    (!requiresVerification && status == AddAccessionStatusType.NoMatchingAnalytes)
                        ? AddAccessionStatusType.ExistsOnOtherWorksheet
                        : status;

                accessionOrderResultModel = new AccessionOrderResultModel
                {
                    AccessionNbr = request.AccessionNbr,
                    IsControl = request.IsControl,
                    StatusId = (int)finalStatus,
                    Status = finalStatus.ToString()
                };
            }
            else
            {
                accessionOrderResultModel = new AccessionOrderResultModel
                {
                    AccessionNbr = request.AccessionNbr,
                    IsControl = request.IsControl,
                    StatusId = (int)AddAccessionStatusType.InvalidStatus,
                    Status = AddAccessionStatusType.InvalidStatus.ToString()
                };
            }
            if (accessionOrderResultModel.StatusId == (int)LIS.AddAccessionStatusType.Success)
            {
                if (rapidWorkSheet != null)
                {
                    foreach (var rapidResultAnalyte in rapidWorkSheet.List)
                    {
                        if (rapidResultAnalyte.AccessionNbr == request.AccessionNbr)
                        {
                            if (rapidResultAnalyte.Has4kTest)
                            {
                                accessionOrderResultModel.Has4kTest = rapidResultAnalyte.Has4kTest;
                                break;
                            }
                        }
                    }
                }
            }
            var mappedResponse = _mapper.Map<RapidResultModel>(rapidWorkSheet);
            if (rapidWorkSheet != null && rapidWorkSheet.List != null)
            {
                MapAnalyteResultFlagRange(rapidWorkSheet, mappedResponse);
            }
            return new CheckAccessionResponseModel
            {
                Result = accessionOrderResultModel,
                RapidWorkSheet = mappedResponse
            };
        }

        public async Task<RapidResultModel> RapidResultAccessions(int rapidResultId)
        {
            LIS.RapidResult rapidResult = await Task.Run(() => LIS.RapidResult.Fetch(rapidResultId));
            RapidResultModel mappedResponse = _mapper.Map<RapidResultModel>(rapidResult);
            MapAnalyteResultFlagRange(rapidResult, mappedResponse);
            return mappedResponse;
        }
        public async Task<bool> DeleteRapidResult(int rapidResultId)
        {
            Bioreference.LIS.RapidResult objWsheet = Bioreference.LIS.RapidResult.Fetch(rapidResultId);
            if (objWsheet != null)
            {
                objWsheet.ClearResults();
            }
            await Task.Run(() => Bioreference.LIS.RapidResult.Delete(Convert.ToInt32(rapidResultId)));
            return true;
        }

        public async Task<RapidResultModel> EventProcessor(EventRapids events)
        {
            var results = new List<AccessionOrderResultModel>();
            Bioreference.LIS.RapidResult rapidResult = null;
            if (events.templateId != null)
            {
                rapidResult = await Task.Run(() => Bioreference.LIS.RapidResult.CreateRapidResult(events.templateId.Value));

            }
            else if (events.rapidResultId != null)
            {
                rapidResult = await Task.Run(() => LIS.RapidResult.Fetch(events.rapidResultId.Value));
            }
            if (rapidResult != null)
            {
                await Task.Run(() => _eventProcessorService.ProcessEvents(events.Events, rapidResult));
            }

            var mappedResponse = _mapper.Map<RapidResultModel>(rapidResult);
            if (rapidResult != null && rapidResult.List != null)
            {
                MapAnalyteResultFlagRange(rapidResult, mappedResponse);
            }
            return mappedResponse;
                   
        }

        private void MapAnalyteResultFlagRange(LIS.RapidResult rapidResult, RapidResultModel mappedResponse)
        {
            if (rapidResult?.List != null)
            {
                foreach (RapidResultAnalyte rapidResultAnalyte in rapidResult.List)
                {
                    var mappedRapidResultAnalyte = mappedResponse.List.First(a => a.Id == rapidResultAnalyte.Id && a.AccessionNbr == rapidResultAnalyte.AccessionNbr && a.AnalyteCode == rapidResultAnalyte.AnalyteCode);
                    RefAnalyte refAnalyte = rapidResultAnalyte.Analyte;
                    var flag = _commonResult.GetMatchingResultFlag(
                        refAnalyte.ResultFlagRanges,
                        rapidResultAnalyte.Gender,
                        rapidResultAnalyte.DOB,
                        rapidResultAnalyte.AgeNbr,
                        rapidResultAnalyte.AgeType,
                        rapidResultAnalyte.CalcDobFromDate,
                        rapidResultAnalyte.PerformingFacility);
                    mappedRapidResultAnalyte.Analyte.ResultFlagRange = _mapper.Map<ComplexResultFlagModel>(flag);
                }
            }
        }
        public async Task<RapidResultTemplates> RapidResultTemplates()
        {
            RapidResultTemplates rapidResultTemplates = await Task.Run(() => Bioreference.LIS.RapidResultTemplates.Fetch());
            return rapidResultTemplates;
        }
        public async Task<List<RapidControlModel>> RapidResultControl(int templateId)
        {
            RapidResultTemplate rapidResultTemplate = await Task.Run(() => Bioreference.LIS.RapidResultTemplate.Fetch(templateId));
            List<RapidControlModel> rapidControlModels = new List<RapidControlModel>();
            foreach (RapidResultTemplateControl templateControl in rapidResultTemplate.ControlList)
            {
                rapidControlModels.Add(new RapidControlModel
                {
                    Id = templateControl.Id,
                    Name = templateControl.Name
                });
            }
            return rapidControlModels;
        }
        public async Task<List<OutstandingRR>> OutstandingRapidResults(int templateId, int pastDays)
        {
            var da = new DataWrapper(Bioreference.LIS.Configuration.ConnectionString);
            var param = new List<DbParameter>();

            param.Add(da.CreateParameter("@TemplateId", DbType.Int32, templateId));
            param.Add(da.CreateParameter("@PastDays", DbType.Int32, pastDays));

            var dt = await Task.Run(() => da.ExecuteProcedure("lis_RapidResultsOutstandingDownload_Fetch", param.ToArray()));

            var results = new List<OutstandingRR>();

            foreach (DataRow row in dt[0].Rows)
            {
                var result = new OutstandingRR
                {
                    AccessionNbr = row[0]?.ToString(),
                    DateServiced = DateTime.TryParse(row[1]?.ToString(), out var date) ? date : default,
                    PatientName = row[2]?.ToString(),
                    AnalyteCode = row[3]?.ToString(),
                    PerformingFacility = row[4]?.ToString(),
                    ResultStatus = row[5]?.ToString()
                };

                results.Add(result);
            }

            return results;
        }

    }
}
