using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Common.Enumerations
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EventType
    {
        ResultUpdate,
        AddComment,
        MarkAsRelease,
        MarkAsPreliminaryRelease,
        MarkAsPendingRelease,
        SaveReport,
        DeleteComment,
        AddTest,
        DeletePanel,
        DeleteAnalyte,
        AddNote,
        ResetStatus,
        FlagUpdate,
        MarkFlagAsDelete,
        MarkFlagAsUnDelete,
        UnitUpdate,
        ReferenceRangeUpdate,
        PerformingFacilityUpdate,
        DoubleEntry

    }
}
