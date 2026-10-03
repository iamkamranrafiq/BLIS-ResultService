using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Common.Enumerations
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportCommentType
    {
        PanelComment,
        AnalyteChildComment,
        AnalyteComment
    }
}
