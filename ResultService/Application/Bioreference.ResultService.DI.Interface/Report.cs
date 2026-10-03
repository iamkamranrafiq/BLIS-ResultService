using Microsoft.VisualBasic;

namespace Bioreference.ResultService.DI.Interface
{
    public class Report
    {
        private string sTestCode = string.Empty;
        private string sTestDesc = string.Empty;
        private string sComments = string.Empty;
        private List<ReportAnalyte> cAnalyteList = null;
        private List<NTE> cNtes = null;
        private string sAccession = string.Empty;
        private string sPriority = string.Empty;
        private string sOrderDate = string.Empty;
        private string sCollectDate = string.Empty;
        private string sCollector = string.Empty;
        private string sReceiveDate = string.Empty;
        private string sPhysCode = string.Empty;
        private string sPhysLast = string.Empty;
        private string sPhysFirst = string.Empty;
        private string sCallBack = string.Empty;
        private string sResultDate = string.Empty;
        private string sDiagSvc = string.Empty;
        private string sReportStat = string.Empty;
        private bool bPreliminaryRelease = false;
        private bool m_addSuppress = false;
        private List<string> m_commentIdMappings = new List<string>();
        private string sAccessioningFacility = string.Empty;
        private string sOrderCode = string.Empty;
        private string sParentCode = string.Empty;
        private string sOBRComment = string.Empty;
        private string sResult = string.Empty;
        private bool sOBRProcessing = false;
        private string sLabReportStatus = string.Empty;


        public Report(string strTestCode = "", string strReportComment = "", string strTestDesc = "", string strAccession = "", string strPriority = "", string strOrderDate = "", string strCollectDate = "", string strCollector = "", string strReceiveDate = "", string strPhysCode = "", string strPhysLast = "", string strPhysFirst = "", string strCallBackPhn = "", string strResultDate = "", string strDiagService = "", string strReportStatus = "", string strParentCode = "")
        {
            cNtes = new List<NTE>();
            cAnalyteList = new List<ReportAnalyte>();
            if (!string.IsNullOrEmpty(strTestCode))
                sTestCode = Common.PrependZeros(strTestCode);
            if (!string.IsNullOrEmpty(strReportComment))
            {
                sComments = strReportComment;
                AddNteInternal();
            }
            if (!string.IsNullOrEmpty(strTestDesc))
                sTestDesc = strTestDesc;
            if (!string.IsNullOrEmpty(strAccession))
                sAccession = strAccession;
            if (!string.IsNullOrEmpty(strPriority))
                sPriority = strPriority;
            if (!string.IsNullOrEmpty(strOrderDate))
                sOrderDate = strOrderDate;
            if (!string.IsNullOrEmpty(strCollectDate))
                sCollectDate = strCollectDate;
            if (!string.IsNullOrEmpty(strCollector))
                sCollector = strCollector;
            if (!string.IsNullOrEmpty(strReceiveDate))
                sReceiveDate = strReceiveDate;
            if (!string.IsNullOrEmpty(strPhysCode))
                sPhysCode = strPhysCode;
            if (!string.IsNullOrEmpty(strPhysLast))
                sPhysLast = strPhysLast;
            if (!string.IsNullOrEmpty(strPhysFirst))
                sPhysFirst = strPhysFirst;
            if (!string.IsNullOrEmpty(strCallBackPhn))
                sCallBack = strCallBackPhn;
            if (!string.IsNullOrEmpty(strResultDate))
                sResultDate = strResultDate;
            else
                sResultDate = DateTime.Now.ToString("yyyyMMddhhmmss");
            if (!string.IsNullOrEmpty(strDiagService))
                sDiagSvc = strDiagService;
            if (!string.IsNullOrEmpty(strReportStatus))
                sReportStat = strReportStatus;
            if (!string.IsNullOrEmpty(strParentCode))
                sParentCode = strParentCode;
        }

        public string AccessioningFacility
        {
            get
            {
                return sAccessioningFacility;
            }
            set
            {
                sAccessioningFacility = value;
            }
        }

        public void AddAnalyte(ReportAnalyte Analyte)
        {
            if (cAnalyteList is null)
                cAnalyteList = new List<ReportAnalyte>();
            cAnalyteList.Add(Analyte);
        }

        public bool IsPreliminaryRelease
        {
            get
            {
                return bPreliminaryRelease;
            }
            set
            {
                bPreliminaryRelease = value;
            }
        }

        public bool AddSuppress
        {
            get
            {
                return m_addSuppress;
            }
            set
            {
                m_addSuppress = value;
            }
        }


        public List<string> CommentIdMappings
        {
            get
            {
                return m_commentIdMappings;
            }
        }

        public string TestCode
        {
            get
            {
                return sTestCode;
            }
            set
            {
                sTestCode = Common.PrependZeros(value);
            }
        }

        public string LabReportStatus
        {
            get
            {
                return sLabReportStatus;
            }
            set
            {
                sLabReportStatus = value;
            }
        }

        public string TestDescription
        {
            get
            {
                return sTestDesc;
            }
            set
            {
                sTestDesc = value;
            }
        }

        public string ResultComment
        {
            get
            {
                return sComments;
            }
            set
            {
                sComments = value;
                AddNteInternal();
            }
        }

        public string AccessionNum
        {
            get
            {
                return sAccession;
            }
            set
            {
                sAccession = value;
            }
        }

        public string Priority
        {
            get
            {
                return sPriority;
            }
            set
            {
                sPriority = value;
            }
        }

        public string OrderDate
        {
            get
            {
                return sOrderDate;
            }
            set
            {
                sOrderDate = value;
            }
        }

        public string CollectDate
        {
            get
            {
                return sCollectDate;
            }
            set
            {
                sCollectDate = value;
            }
        }

        public string Collector
        {
            get
            {
                return sCollector;
            }
            set
            {
                sCollector = value;
            }
        }

        public string ReceiveDate
        {
            get
            {
                return sReceiveDate;
            }
            set
            {
                sReceiveDate = value;
            }
        }

        public string PhysCode
        {
            get
            {
                return sPhysCode;
            }
            set
            {
                sPhysCode = value;
            }
        }

        public string PhysLastName
        {
            get
            {
                return sPhysLast;
            }
            set
            {
                sPhysLast = value;
            }
        }

        public string PhysFirstName
        {
            get
            {
                return sPhysFirst;
            }
            set
            {
                sPhysFirst = value;
            }
        }

        public string CallBackPhone
        {
            get
            {
                return sCallBack;
            }
            set
            {
                sCallBack = value;
            }
        }

        public string ResultDate
        {
            get
            {
                return sResultDate;
            }
            set
            {
                sResultDate = value;
            }
        }

        public string DiagnosticService
        {
            get
            {
                return sDiagSvc;
            }
            set
            {
                sDiagSvc = value;
            }
        }

        public string ReportStatus
        {
            get
            {
                return sReportStat;
            }
            set
            {
                sReportStat = value;
            }
        }

        public string OrderedCode
        {
            get
            {
                return sOrderCode;
            }
            set
            {
                sOrderCode = value;
            }
        }

        public string ParentCode
        {
            get
            {
                return sParentCode;
            }
            set
            {
                sParentCode = value;
            }
        }

        public string ReportResult
        {
            get
            {
                return sResult;
            }
            set
            {
                sResult = value;
            }
        }

        public string OBRComment
        {
            get
            {
                return sOBRComment;
            }
            set
            {
                sOBRComment = value;
            }
        }

        public bool IsOBRProcessing
        {
            get
            {
                return sOBRProcessing;
            }
            set
            {
                sOBRProcessing = value;
            }
        }

        public List<NTE> NTEs
        {
            get
            {
                return cNtes;
            }
        }

        public List<ReportAnalyte> AnalyteList
        {
            get
            {
                return cAnalyteList;
            }
        }

        private void AddNteInternal()
        {
            if (cNtes is null)
                cNtes = new List<NTE>();
            else
                cNtes.Clear();
            List<NTE> cComNte;
            cComNte = Common.GetNteFromComment(sComments);
            foreach (NTE Note in cComNte)
                cNtes.Add(Note);
        }

        public void AddNTE(NTE oNote)
        {
            if (cNtes is null)
                cNtes = new List<NTE>();
            cNtes.Add(oNote);
        }

        public void AddNTE(NTE oNote, ExcludedStrings oStrings)
        {
            bool foundFlag = false;
            if (cNtes is null)
                cNtes = new List<NTE>();
            foreach (string badString in oStrings.StringList)
            {
                if ((Strings.RTrim(oNote.Text) ?? "") == (badString ?? ""))
                {
                    foundFlag = true;
                    break;
                }
            }
            if (foundFlag == false)
                cNtes.Add(oNote);
        }

    }
}