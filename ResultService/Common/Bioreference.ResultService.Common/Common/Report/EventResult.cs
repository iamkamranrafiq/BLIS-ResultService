using Bioreference.ResultService.Common.Enumerations;

namespace Bioreference.ResultService.Common.Common.Report
{
    public class EventResult
    {
        public EventType Type { get; set; } // Enum for event type
        public EventResultData Data { get; set; }
        public int Sequence { get; set; }

    }
}
