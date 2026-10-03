namespace Bioreference.ResultService.Application.Model.AppSettings.OrderOut
{
    public class AppSettingsOrderEngine
    {
        public const string SectionName = "Bioreference.OrderOutJob";
        public string MSH_SendingApp { get; set; }
        public string MSH_SendingFacilityCode { get; set; }
        public string MSH_ReceivingApp { get; set; }
        public string MSH_ReceivingFacility { get; set; }
        public string OrderOut_ArchiveMsgPath { get; set; }
        public string OrderOut_StatusMsgPath { get; set; }
        public int ReferenceLabId { get; set; }
        public bool AddTestFromTestMaster { get; set; } 
        public string LabName { get; set; }

        public bool SupressTestCodes { get; set; }
        public List<string>? SupressTestCodes_AllowCodes { get; set; }
        public List<string>? SupressTestCodes_AcctNbrByPass { get; set; }

    }
}
