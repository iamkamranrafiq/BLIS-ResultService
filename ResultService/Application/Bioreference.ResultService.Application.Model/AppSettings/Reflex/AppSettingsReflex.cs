using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.AppSettings.Reflex
{
    public class AppSettingsReflex
    {
        public const string SectionName = "Bioreference.ReflexProcessor";
        public string? Environment { get; set; }
    }
}
