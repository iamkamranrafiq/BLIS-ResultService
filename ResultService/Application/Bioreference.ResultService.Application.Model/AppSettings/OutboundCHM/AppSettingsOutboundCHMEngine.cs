using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.AppSettings.OutboundCHM
{
    public class AppSettingsOutboundCHMEngine
    {
        public const string SectionName = "Bioreference.OutboundCHMJob";
        public string Outbound_StatusMessagePaths { get; set; }  
        public string BIOFilePrefix { get; set; } 
    }
}
