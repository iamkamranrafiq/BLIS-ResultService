using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Lookup;
using Bioreference.ResultService.Abstractions.Application.WorkSheet;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.WorkSheet;
using System.Text;

namespace Bioreference.ResultService.Application.Report
{
    public class WorkSheetService : IWorkSheetService
    {
        private readonly IMapper _mapper;
        private readonly ILookupService _lookupService;

        public WorkSheetService(IMapper mapper, ILookupService lookupService)
        {
            _mapper = mapper;
            _lookupService = lookupService;
        }

        public async Task<List<WorkSheetReportModel>> GetWorkSheetById(int workSheetId, int pageNo, int pageSize)
        {
            List<WorkSheetReportModel> response = new List<WorkSheetReportModel>();
            var returnValue = await Task.Run(() => RackWorksheets.Fetch(workSheetId, pageNo, pageSize));
            if (returnValue != null)
            {
                response = _mapper.Map<List<WorkSheetReportModel>>(returnValue.List);
            }

            return response;
        }

        public async Task<List<RackWorkSheetModel>> GetWorkSheets(WorkSheetSearchCriteria workSheetSearchRequest)
        {
            List<RackWorkSheetModel> response = new List<RackWorkSheetModel>();
            try
            {

                RackWorksheets workSheets = await Task.Run(() => RackWorksheets.Fetch(workSheetSearchRequest.TemplateId, workSheetSearchRequest.DateFrom
                    , workSheetSearchRequest.DateTo, workSheetSearchRequest.PageNumber, workSheetSearchRequest.PageSize));
                response = _mapper.Map<List<RackWorkSheetModel>>(workSheets.List);
            }

            catch (Exception ex)
            {
                throw ex;
            }
            return response;
        }

        public async Task<bool> ReleaseCheckedWorkSheets(List<int> reportIds, int rackWorkSheetTemplateId, int workSheetId)
        {
            try
            {
                // Validate inputs
                if (reportIds == null || !reportIds.Any() || rackWorkSheetTemplateId <= 0 || workSheetId <= 0)
                {
                    return false;
                }

                RackWorksheet rackWorksheet = RackWorksheet.CreateNew();
                if (rackWorksheet == null)
                {
                    return false;
                }             
                RackWorksheetTemplate template = RackWorksheetTemplate.Fetch(rackWorkSheetTemplateId);

                try
                {
                    RackWorksheet.AuditReleaseWorksheet(workSheetId, "");

                    foreach (var id in reportIds)
                    {
                        var report = OrderManager.FetchReport(id);
                        if (report == null) continue;

                        foreach (var analyte in template.Analytes)
                        {
                            if (string.IsNullOrEmpty(analyte?.AnalyteCode)) continue;

                            try
                            {
                                var reportAnalyte = report.FindAnalyte(analyte.AnalyteCode, true);
                                if (reportAnalyte != null)
                                {
                                    reportAnalyte.MarkAsReleased();
                                }
                            }
                            catch
                            {
                                continue; // Continue with next analyte if one fails
                            }
                        }

                        if (report.IsValid)
                        {
                            report.Save();
                        }
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SaveWorksheet(WorkSheetAddUpdateModel model)
        {
            try
            {
                if (model == null || model.RackWorkSheetTemplateId <= 0)
                {
                    return false;
                }               
                RackWorksheetTemplate template = RackWorksheetTemplate.Fetch(model.RackWorkSheetTemplateId);
                if (template == null)
                {
                    return false;
                }

                var panelCodes = GetPanelCodes(template);
                if (string.IsNullOrEmpty(panelCodes))
                {
                    return false;
                }

                RackWorksheet rackWorksheet = RackWorksheet.CreateNew(template);
                if (rackWorksheet == null)
                {
                    return false;
                }

                if (model.WorkSheetSpecimen != null)
                {
                    foreach (var specimen in model.WorkSheetSpecimen)
                    {
                        if (specimen != null && !string.IsNullOrEmpty(specimen.AccessionNo))
                        {
                            rackWorksheet.AddSpecimen(specimen.AccessionNo, specimen.RackId, specimen.RackPosition, panelCodes);
                        }
                    }
                }

                var res = rackWorksheet.Save();
                return res != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateWorksheet(WorkSheetAddUpdateModel model)
        {
            try
            {
                if (model == null || model.Id <= 0 || model.RackWorkSheetTemplateId <= 0)
                {
                    return false;
                }

                var rackWorksheet = await Task.Run(() => RackWorksheet.Fetch(model.Id));
                if (rackWorksheet == null)
                {
                    return false;
                }               
                RackWorksheetTemplate template = RackWorksheetTemplate.Fetch(model.RackWorkSheetTemplateId);
                if (template == null)
                {
                    return false;
                }

                var panelCodes = GetPanelCodes(template);
                if (string.IsNullOrEmpty(panelCodes))
                {
                    return false;
                }

                if (model.WorkSheetSpecimen != null)
                {
                    var removeSpecimenList = model.WorkSheetSpecimen
                        .Where(x => x != null && x.IsMarkForDelete && x.ID != 0)
                        .ToList();

                    var addSpecimenList = model.WorkSheetSpecimen
                        .Where(x => x != null && !x.IsMarkForDelete && x.ID == 0)
                        .ToList();

                    foreach (var specimen in addSpecimenList)
                    {
                        if (!string.IsNullOrEmpty(specimen.AccessionNo))
                        {
                            rackWorksheet.AddSpecimen(specimen.AccessionNo, specimen.RackId, specimen.RackPosition, panelCodes);
                        }
                    }

                    foreach (var specimen in removeSpecimenList)
                    {
                        var rackWorksheetSpecimen = rackWorksheet.List?
                            .FirstOrDefault(x => x != null && x.ID == specimen.ID);

                        if (rackWorksheetSpecimen != null)
                        {
                            rackWorksheet.RemoveSpecimen(rackWorksheetSpecimen);
                        }
                    }
                }

                object res = rackWorksheet.Save();
                return res != null;
            }
            catch
            {
                return false;
            }
        }

        private string GetPanelCodes(RackWorksheetTemplate template)
        {
            try
            {
                if (template?.Analytes == null)
                {
                    return string.Empty;
                }

                var strList = new List<string>();
                var codeString = new StringBuilder();

                foreach (var objCode in template.Analytes)
                {
                    if (objCode != null && !string.IsNullOrEmpty(objCode.PanelCode) &&
                        !strList.Contains(objCode.PanelCode))
                    {
                        strList.Add(objCode.PanelCode);
                        if (codeString.Length > 0)
                        {
                            codeString.Append(",");
                        }
                        codeString.Append(objCode.PanelCode);
                    }
                }

                return codeString.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<List<RackWorkSheetTemplateModel>> GetWorkSheetsTemplates()
        {
            RackWorksheetTemplates worksheets = await WorkSheetTemplatesInfo();
            return _mapper.Map<List<RackWorkSheetTemplateModel>>(worksheets.List);
        }
        public async Task<RackWorksheetTemplates> WorkSheetTemplatesInfo()
        {
            return await Task.Run(() => RackWorksheetTemplates.Fetch());

        }

        public async Task<bool> ArchieveWorkSheet(int worksheetId)
        {
            RackWorksheet worksheet = null;
            bool isSuccess = false;

            try
            {
                worksheet = RackWorksheet.Fetch(worksheetId);
                var worksheetReportItems = await Task.Run(() => RackWorksheets.Fetch(worksheetId, 1, 100000));
                if (worksheet != null)
                {
                    foreach (RackWorksheetReportItem r in worksheetReportItems.List)
                    {
                        if (r.TransmitStatus == transmitStatusType.PendingRelease)
                        {
                            worksheet.RemoveSpecimen(r.SpecimenId);
                        }
                    }

                    worksheet.IsClosed = true;
                    worksheet.Save();
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
            }

            return isSuccess;
        }

        public async Task<AccessionOrderResultModel> IsAddSpecimen(WorkSheetSpecimenModel model, int worksheetTemplateId)
        {
            Model.Enum.AddAccessionStatusType status = Model.Enum.AddAccessionStatusType.InvalidStatus;
            AccessionOrderResultModel accessionOrderResultModel = new();
            RackWorksheetTemplate template = RackWorksheetTemplate.Fetch(worksheetTemplateId);
            if (template != null)
                {
                    var panelCodes = GetPanelCodes(template);
                    RackWorksheet rackWorksheet = RackWorksheet.CreateNew(template);

                    if (rackWorksheet != null)
                    {
                        LIS.AddAccessionStatusType finalStatus = rackWorksheet.AddSpecimen(model.AccessionNo, model.RackId, model.RackPosition, panelCodes);
                        accessionOrderResultModel = new AccessionOrderResultModel
                        {
                            AccessionNbr = model.AccessionNo,
                            IsControl = false,
                            StatusId = (int)finalStatus,
                            Status = finalStatus.ToString()
                        };

                        if (finalStatus == LIS.AddAccessionStatusType.Success)
                        {
                            foreach (RackWorksheetSpecimen rackWorksheetSpecimen in rackWorksheet.List)
                            {
                                if (rackWorksheetSpecimen.Has4kTest)
                                {
                                    accessionOrderResultModel.Has4kTest = rackWorksheetSpecimen.Has4kTest;
                                }
                            }  
                        }
                    }
                }
            return accessionOrderResultModel;
        }
        public async Task<bool> IsWorksheetArchieved(int workSheetId)
        {
            bool isArchieved = false;
            var returnValue = await Task.Run(() => RackWorksheets.Fetch(workSheetId, 1, 1000));
            if (returnValue != null && returnValue.IsClosed)
            {
                isArchieved = true;
            }

            return isArchieved;
        }

    }
}