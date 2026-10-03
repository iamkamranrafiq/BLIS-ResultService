using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.COC;
using Bioreference.ResultService.Application.Model.COC;
using Bioreference.ResultService.Application.Model.Enum;


namespace Bioreference.ResultService.Application.Order
{
    public class COCService : ICOCService
    {
        private readonly IMapper _mapper;

        public COCService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<List<COCBatchReportModel>> FetchCOCReport(bool isShowClosed, int pageNo, int pageSize)
        {
            var response = await Task.Run(() => CocBatches.Fetch(isShowClosed));
            return _mapper.Map<List<COCBatchReportModel>>(response.List);
        }

        public async Task<List<COCBatchReportItemModel>> FetchCOCReportItem(int batchId)
        {

            var response = await Task.Run(() => CocBatches.Fetch(batchId));
            return _mapper.Map<List<COCBatchReportItemModel>>(response.List);
        }

        public async Task<COCAddEditResponseModel> CreateCOCSequence(COCSearchCriteria cocSearchCriteria)
        {
            CocBatchCreateResponse response = await Task.Run(() => CocBatch.CreateNew(cocSearchCriteria.Month, cocSearchCriteria.Day, cocSearchCriteria.Year
            , cocSearchCriteria.SequenceStart, cocSearchCriteria.SequenceEnd));

            return await Task.Run(() => AssembleDetails(response));
        }

        public async Task<CocBatchAccessionEditStatusModel> RemoveCOC(string accessionNumber, int batchId)
        {
            CocBatch cocBatch = await Task.Run(() => CocBatch.Fetch(batchId));
            CocBatchAccession cocBatchAccession = cocBatch.List.Where(x => x.AccessionNbr == accessionNumber).Select(x => x).First();
            return _mapper.Map<CocBatchAccessionEditStatusModel>(cocBatch.RemoveAccession(cocBatchAccession));
        }

        public async Task<CocBatchAccessionEditStatusModel> AddCOCAccession(string accessionNumber, int batchId)
        {
            CocBatch cocBatch = await Task.Run(() => CocBatch.Fetch(batchId));
            return _mapper.Map<CocBatchAccessionEditStatusModel>(cocBatch.AddAccession(accessionNumber));
        }

        public async Task<COCAddEditResponseModel> ModifyCOCSequence(COCSearchCriteria cocSearchCriteria)
        {
            CocBatchCreateResponse response = await Task.Run(() => CocBatch.ModifyBatchSetup(cocSearchCriteria.BatchId, cocSearchCriteria.Month, cocSearchCriteria.Day, cocSearchCriteria.Year
             , cocSearchCriteria.SequenceStart, cocSearchCriteria.SequenceEnd));

            return await Task.Run(() =>AssembleDetails(response));
        }

        public async Task<bool> ReleaseBatch(List<int> reports)
        {
            bool isSuccess = false;
            foreach (var r in reports)
            {
                bool bReviewerSet = false;

                var report = await Task.Run(() => Bioreference.LIS.Report.Fetch(r));

                foreach (ReportAnalytePanel p in report.AnalytePanels.List)
                {
                    if (p.Panel.IsReportable && p.TransmitStatus == transmitStatusType.Released)
                    {
                        //TODO: CLAPP 2514 set user permission after User Authentication
                        //if (GlobalModule.HasAnalyteReleasePermissions(p.PanelCode))
                        //{
                        foreach (ReportAnalyte a in p.Analytes.List)
                        {
                            if (a.Analyte.IsReportable && a.TransmitStatus == transmitStatusType.Released)
                            {
                                bReviewerSet = true;
                                a.IsCOCReviewed = true;
                                a.COCApprover = Thread.CurrentPrincipal.Identity.Name;
                            }
                        }
                        // }
                    }
                }

                foreach (ReportAnalyte a in report.Analytes.List)
                {
                    //TODO: CLAPP 2514 set user permission after User Authentication
                    //if (GlobalModule.HasAnalyteReleasePermissions(a.Code))
                    //{
                    if (a.Analyte.IsReportable && a.TransmitStatus == transmitStatusType.Released)
                    {
                        bReviewerSet = true;
                        a.IsCOCReviewed = true;
                        a.COCApprover = Thread.CurrentPrincipal.Identity.Name;
                    }
                    //}

                }

                if (bReviewerSet)
                {
                    if (report.IsValid)
                    {
                        report.Save();
                        isSuccess = true;
                    }
                    else
                    {
                        throw new Exception(report.Rules.ToString());
                    }
                }

            }
            return isSuccess;

        }

        public async Task<CocBatchAccessionEditStatusModel> ClosedReOpenBatch(bool isClosed, int batchId)
        {
            if (isClosed)
            {
                var reportItems = await FetchCOCReportItem(batchId);

                foreach (var t in reportItems)
                {
                    if (t.ResultStatus < resultStatusTypeModel.Final || t.TransmitStatus < transmitStatusTypeModel.SentToReporting || t.TransmitStatus == transmitStatusTypeModel.HeldForRerun)
                    {
                        var CocBatchAccessionEditStatusModel = new CocBatchAccessionEditStatusModel();
                        CocBatchAccessionEditStatusModel.Status = CocBatchCreateStatusModel.Failure;
                        CocBatchAccessionEditStatusModel.Message = "Not permitted to close Batch.  Not Final/Not Sent to Reporting Accessions exist.";

                        return CocBatchAccessionEditStatusModel;
                    }
                }
            }
            return _mapper.Map<CocBatchAccessionEditStatusModel>(await Task.Run(() => CocBatch.SetBatchStatus(batchId, isClosed, "")));
        }


        private async Task <COCAddEditResponseModel> AssembleDetails(CocBatchCreateResponse response)
        {
            COCAddEditResponseModel cOCAddEditResponseModel = new COCAddEditResponseModel();
            if (response.CocBatchInstance != null)
            {
                COCModel cocModel = _mapper.Map<COCModel>(response.CocBatchInstance);
                if (response.CocBatchInstance.List.Length > 0)
                {
                    cocModel.COCBatchAccessions = _mapper.Map<List<COCBatchAccessionModel>>(response.CocBatchInstance.List);
                }
                else
                {
                    CocBatch cocBatch = await Task.Run(() => CocBatch.Fetch(response.CocBatchInstance.Id));
                    cocModel.COCBatchAccessions = _mapper.Map<List<COCBatchAccessionModel>>(cocBatch.List);
                }

                cOCAddEditResponseModel.COCModel = cocModel;
            }
            cOCAddEditResponseModel.Status = (CocBatchCreateStatusModel)response.CocBatchCreateStatus;
            cOCAddEditResponseModel.Message = response.CocBatchCreateResponseMessage;

            return cOCAddEditResponseModel;
        }

        public async Task<bool> IsCocApprover(string userName)
        {
            bool isSuccess = false;
            var cocApprovers =  await Task.Run(() => COCApprovers.Fetch());
            if (cocApprovers != null && cocApprovers.ApproverList.Count>0)
            {
               var cocApprover = cocApprovers.ApproverList.Where(x => x.CocApproverLoginId == userName).Select(x=>x).FirstOrDefault();
                if (cocApprover != null)
                {
                    isSuccess = true;
                }
            }
            return isSuccess;
        }
    }
}
