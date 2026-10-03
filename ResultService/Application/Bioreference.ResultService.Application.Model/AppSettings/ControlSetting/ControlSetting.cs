namespace Bioreference.ResultService.Application.Model
{
    public class ControlSetting
    {
        public bool Audit { get; set; } = false;
        public bool GenecysResults { get; set; } = false;
        public bool InstrumentMessages { get; set; } = false;
        public bool OrderOut { get; set; } = false;
        public bool OrderOutFibrosure { get; set; } = false;
        public bool Reflexes { get; set; } = false;
        public bool RequisitionStatus { get; set; } = false;
        public bool ResultsOut { get; set; } = false;
        public bool ReviseReport { get; set; } = false;
        public bool UnsolicitedMessages { get; set; } = false;
        public bool OutboundCHM { get; set; } = false;
        public bool ReportsOut { get; set; } = false;
        public bool ReportsOutNonCum { get; set; } = false;
        public bool IncidentRevisedResult { get; set; } = false;

    }
}
