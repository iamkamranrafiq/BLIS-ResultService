using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.WorkSheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.WorkSheet
{
    public interface IWorkSheetService
    {
        public Task<List<WorkSheetReportModel>> GetWorkSheetById(int workSheetId, int pageNo, int pageSize);
        public Task<List<RackWorkSheetModel>> GetWorkSheets(WorkSheetSearchCriteria workSheetSearchRequest);
        public Task<bool> ReleaseCheckedWorkSheets(List<int> reportIds, int rackWorkSheetTemplateId, int workSheetId);
        public Task<bool> SaveWorksheet(WorkSheetAddUpdateModel model);
        public Task<bool> UpdateWorksheet(WorkSheetAddUpdateModel model);
        public Task<RackWorksheetTemplates> WorkSheetTemplatesInfo();
        public Task<bool> ArchieveWorkSheet(int worksheetId);
        public Task<AccessionOrderResultModel> IsAddSpecimen(WorkSheetSpecimenModel model, int worksheetTemplateId);
        public Task<List<RackWorkSheetTemplateModel>> GetWorkSheetsTemplates();
        public Task<bool> IsWorksheetArchieved(int workSheetId);
    }
}
