using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.AppSettings.QueueOrder
{
    public class AppSettingsQueueOrderInProcessor
    {
        public const string SectionName = "Bioreference.QueueOrderInProcessor";
        public List<string> AOEExclusionList {  get; set; }
        public string AltHoldCodes { get; set; }     

    }
}
