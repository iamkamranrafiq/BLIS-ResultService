namespace Bioreference.ResultService.DI.Interface
{
    public enum ResultStatusType
    {
        None = 0,
        TNP = 1,
        QNS = 2,
        SeeBelow = 3
    }

    public class ReportStatus
    {
        private string sAccessionNum = string.Empty;
        private string sTestCode = string.Empty;
        private bool sTNP = false;
        private DateTime? sResultDate = null;
        private ResultStatusType m_resultStatus = ResultStatusType.None;
        private string sStatus = string.Empty;
        private bool bIsPrelim = false;
        private bool bIsToFollow = false;
        private string sLocationCode = string.Empty;
        private string reportHold = string.Empty;

        public string LocationCode
        {
            get => sLocationCode;
            set => sLocationCode = value;
        }

        public string AccessionNumber
        {
            get => sAccessionNum;
            set => sAccessionNum = value;
        }

        public string TestCode
        {
            get => sTestCode;
            set => sTestCode = value;
        }

        public ResultStatusType Status
        {
            get => m_resultStatus;
            set => m_resultStatus = value;
        }

        public string StrStatus
        {
            get => sStatus;
            set => sStatus = value;
        }

        public string ReportHoldStatus
        {
            get => reportHold;
            set => reportHold = value;
        }

        [Obsolete("Use ResultStatus instead.")]
        public bool TNP
        {
            get => sTNP;
            set => sTNP = value;
        }

        public DateTime? ResultDate
        {
            get => sResultDate;
            set => sResultDate = value;
        }

        public bool IsPreliminary
        {
            get => bIsPrelim;
            set => bIsPrelim = value;
        }

        public bool IsToFollow
        {
            get => bIsToFollow;
            set => bIsToFollow = value;
        }

        public string GetMessage()
        {
            string myMsg;
            string sCompleted;
            if (bIsPrelim)
            {
                sCompleted = "PRINT";
            }
            else if (reportHold == "HOLD")
            {
                sCompleted = "ReportingHold";
            }
            else if (reportHold == "RELEASEHOLD")
            {
                sCompleted = "ReportingHoldReleased";
            }
            else if (sTNP || m_resultStatus == ResultStatusType.TNP)
            {
                sCompleted = "TNP";
            }
            else if (m_resultStatus == ResultStatusType.QNS)
            {
                sCompleted = "TNP";
            }
            else // SeeBelow also gets marked as completed
            {
                sCompleted = "COMPLETED";
            }

            string dateString = sResultDate.HasValue
                ? sResultDate.Value.ToString("M/d/yyyy HH:mm:ss")
                : DateTime.Now.ToString("M/d/yyyy HH:mm:ss");

            myMsg = $"{sAccessionNum}|{sTestCode}|{dateString}|{sCompleted}|{sLocationCode}";
            return myMsg;
        }

        public string GetMessage2(string SendingApplication, string ReceivingApplication)
        {
            string myMsg = "MSH|^~\\&|{0}|{1}|||{2}||ORU^R01|{3}\r" +
                          "OBR|1|{4}||||{5}\r" +
                          "OBX|1||{6}||{7}\x1C";

            string sCompleted = (sTNP || m_resultStatus == ResultStatusType.TNP) ? "TNP" :
                               m_resultStatus == ResultStatusType.QNS ? "TNP" :
                               "COMPLETED"; // SeeBelow also gets marked as completed

            myMsg = string.Format(myMsg,
                SendingApplication,
                ReceivingApplication,
                DateTime.Now.ToString("yyyyMMddHHmmss"),
                DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                sAccessionNum,
                sResultDate?.ToString("yyyyMMddHHmmss") ?? DateTime.Now.ToString("yyyyMMddHHmmss"),
                sTestCode,
                sCompleted);

            return myMsg;
        }
    }
}
