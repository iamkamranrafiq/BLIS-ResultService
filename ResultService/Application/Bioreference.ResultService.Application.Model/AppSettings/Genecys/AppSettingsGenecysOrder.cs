using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.AppSettings.Genecys
{
    public class AppSettingsGenecysOrder
    {
        public const string SectionName = "Bioreference.GenecysOrderProcessor";
        public string? Environment { get; set; }
    }
}
