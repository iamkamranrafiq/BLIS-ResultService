using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.AppSettings.ReportsOut
{
    public class AppSettingsReportsOut
    {
        public const string SectionName = "Bioreference.ReportsOutJob";
        public string? Environment { get; set; }
        public int? FetchCount { get;set; }
        public int? ProcessDataType { get; set; }
    }
}
