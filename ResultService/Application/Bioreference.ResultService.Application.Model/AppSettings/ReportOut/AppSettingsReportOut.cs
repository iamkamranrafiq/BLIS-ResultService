using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.AppSettings.ReportOut
{
    public class AppSettingsReportOut
    {
        public const string SectionName = "Bioreference.ReportOutProcessor";
        public string? Environment { get; set; }
    }
}
