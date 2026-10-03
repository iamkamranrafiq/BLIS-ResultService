using Bioreference.ResultService.Common.Enumerations;

namespace Bioreference.ResultService.Common.Common.RapidResult
{
    public class EventRapid
    {
        public EventRapidType Type { get; set; }
        public EventRapidData Data { get; set; }
        public int Sequence { get; set; }

    }
}
