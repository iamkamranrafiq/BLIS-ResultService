using Microsoft.VisualBasic;

namespace Bioreference.ResultService.DI.Interface
{

    public class ReportAnalyte
    {
        private string sValueType = string.Empty;
        private string sCode = string.Empty;
        private string sDescription = string.Empty;
        private string sValue = string.Empty;
        private string sUnits = string.Empty;
        private string sRefRange = string.Empty;
        private string sStatus = string.Empty;
        private string sReportDate = string.Empty;
        private string sComments = string.Empty;
        private string sRefLabNum = string.Empty;
        private List<NTE> cNtes = null;
        private string sAlert = string.Empty;
        private string sFlag = string.Empty;
        private string sMachSeq = string.Empty;
        private string sRack = string.Empty;
        private string sRackPos = string.Empty;
        private string sInstrumentId = string.Empty;
        private string sTechUser = string.Empty;
        private string sReleaseUser = string.Empty;
        private string sPerformingFacility = string.Empty;
        private string sAccessioningFacility = string.Empty;

        private List<string> oAttacment = null;
        private string sAttachmentType = string.Empty;

        private string sCocApproverEmpNumber = string.Empty;
        private string sCocApproverFirstName = string.Empty;
        private string sCocApproverLastName = string.Empty;
        private string sCocApproverMiddleName = string.Empty;
        private DateTime dtCocApprovedDate = DateTime.Parse("1900-01-01");

        private string _panelCode = string.Empty;
        private int _panelSPMStatus = 0;
        private int m_RefLabPerformingFacilityId = 0;

        private List<string> m_commentIdMappings = new List<string>();
        private DateTime dt = DateTime.Parse("1900-01-01");

        public ReportAnalyte(string strCode, string strValue, string strReportDate, string strAnalyteComment = "", string strValueType = "", string strDescription = "", string strUnits = "", string strRefRange = "", string strStatus = "", string strRefLabNum = "", string strAlert = "", string strFlag = "", string strPerfFacility = "", string strAccFacility = "", int refLabPerformingFacilityId = 0)
        {
            cNtes = new List<NTE>();
            oAttacment = new List<string>();
            sCode = Common.PrependZeros(strCode);
            sValue = strValue;
            sReportDate = strReportDate;
            if (!string.IsNullOrEmpty(strAnalyteComment))
            {
                sComments = strAnalyteComment;
                AddNteInternal();
            }
            if (!string.IsNullOrEmpty(strValueType))
                sValueType = strValueType;
            if (!string.IsNullOrEmpty(strDescription))
                sDescription = strDescription;
            if (!string.IsNullOrEmpty(strUnits))
                sUnits = strUnits;
            if (!string.IsNullOrEmpty(strRefRange))
                sRefRange = strRefRange;
            if (!string.IsNullOrEmpty(strStatus))
                sStatus = strStatus;
            if (!string.IsNullOrEmpty(strRefLabNum))
                sRefLabNum = strRefLabNum;
            if (!string.IsNullOrEmpty(strAlert))
                sAlert = strAlert;
            if (!string.IsNullOrEmpty(strFlag))
                sFlag = strFlag;
            sPerformingFacility = strPerfFacility;
            sAccessioningFacility = strAccFacility;
            m_RefLabPerformingFacilityId = refLabPerformingFacilityId;
        }

        // Public Sub New(ByVal strCode As String, ByVal strValue As String, ByVal strReportDate As String, _
        // ByVal strApproverEmpNumber As String, ByVal strApproverFirstName As String, ByVal strApproverLastName As String, _
        // ByVal strApproverMiddleName As String, ByVal dtApprovedDate As String, _
        // Optional ByVal strAnalyteComment As String = "", Optional ByVal strValueType As String = "", _
        // Optional ByVal strDescription As String = "", Optional ByVal strUnits As String = "", _
        // Optional ByVal strRefRange As String = "", Optional ByVal strStatus As String = "", _
        // Optional ByVal strRefLabNum As String = "", Optional ByVal strAlert As String = "", Optional ByVal strFlag As String = "", _
        // Optional ByVal strPerfFacility As String = "", Optional ByVal strAccFacility As String = "")
        // cNtes = New List(Of NTE)
        // sCode = PrependZeros(strCode)
        // sValue = strValue
        // sReportDate = strReportDate
        // If Not (strAnalyteComment = "") Then sComments = strAnalyteComment : AddNteInternal()
        // If Not (strValueType = "") Then sValueType = strValueType
        // If Not (strDescription = "") Then sDescription = strDescription
        // If Not (strUnits = "") Then sUnits = strUnits
        // If Not (strRefRange = "") Then sRefRange = strRefRange
        // If Not (strStatus = "") Then sStatus = strStatus
        // If Not (strRefLabNum = "") Then sRefLabNum = strRefLabNum
        // If Not (strAlert = "") Then sAlert = strAlert
        // If Not (strFlag = "") Then sFlag = strFlag
        // sPerformingFacility = strPerfFacility
        // sAccessioningFacility = strAccFacility

        // sCocApproverEmpNumber = strApproverEmpNumber
        // sCocApproverFirstName = strApproverFirstName
        // sCocApproverLastName = strApproverLastName
        // sCocApproverMiddleName = strApproverMiddleName
        // dtCocApprovedDate = DateTime.Parse(dtApprovedDate)

        // End Sub

        public string PerformingFacility
        {
            get
            {
                return sPerformingFacility;
            }
            set
            {
                sPerformingFacility = value;
            }
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

        public List<string> CommentIdMappings
        {
            get
            {
                return m_commentIdMappings;
            }
        }

        public int RefLabPerformingFacilityId
        {
            get
            {
                return m_RefLabPerformingFacilityId;
            }
            set
            {
                m_RefLabPerformingFacilityId = value;
            }
        }

        public string ValueType
        {
            get
            {
                return sValueType;
            }
            set
            {
                sValueType = value;
            }
        }

        public string Code
        {
            get
            {
                return sCode;
            }
            set
            {
                sCode = Common.PrependZeros(value);
            }
        }

        public string Description
        {
            get
            {
                return sDescription;
            }
            set
            {
                sDescription = value;
            }
        }

        public string Value
        {
            get
            {
                return sValue;
            }
            set
            {
                sValue = value;
            }
        }

        public string Units
        {
            get
            {
                return sUnits;
            }
            set
            {
                sUnits = value;
            }
        }

        public string ReferenceRange
        {
            get
            {
                return sRefRange;
            }
            set
            {
                sRefRange = value;
            }
        }

        public string Flag
        {
            get
            {
                return sFlag;
            }
        }

        public string Status
        {
            get
            {
                return sStatus;
            }
            set
            {
                sStatus = value;
            }
        }

        public string ReportDate
        {
            get
            {
                return sReportDate;
            }
            set
            {
                sReportDate = value;
            }
        }

        public string AnalyteComment
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

        public string ReferenceLabNumber
        {
            get
            {
                return sRefLabNum;
            }
            set
            {
                sRefLabNum = value;
            }
        }

        public string InstrumentId
        {
            get
            {
                return sInstrumentId;
            }
            set
            {
                sInstrumentId = value;
            }
        }

        public string MachineSequence
        {
            get
            {
                return sMachSeq;
            }
            set
            {
                sMachSeq = value;
            }
        }

        public string Rack
        {
            get
            {
                return sRack;
            }
            set
            {
                sRack = value;
            }
        }

        public string RackPosition
        {
            get
            {
                return sRackPos;
            }
            set
            {
                sRackPos = value;
            }
        }

        public string TechUser
        {
            get
            {
                return sTechUser;
            }
            set
            {
                sTechUser = value;
            }
        }

        public string ReleaseUser
        {
            get
            {
                return sReleaseUser;
            }
            set
            {
                sReleaseUser = value;
            }
        }

        public List<NTE> NTEs
        {
            get
            {
                return cNtes;
            }
        }

        public string Alerts
        {
            get
            {
                return sAlert;
            }
        }

        public List<string> Attachment
        {
            get
            {
                return oAttacment;
            }
        }

        public string AttachmentType
        {
            get
            {
                return sAttachmentType;
            }
            set
            {
                sAttachmentType = value;
            }
        }

        public bool HasAttachment
        {
            get
            {
                bool hasAtt = false;
                if (!(oAttacment == null) && oAttacment.Count > 0)
                {
                    hasAtt = true;
                }
                return hasAtt;
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

        /// <summary>
    /// Add line to attachment list
    /// </summary>
    /// <param name="sLine"></param>
    /// <remarks></remarks>
        public void AddAttachmentLine(string sLine)
        {
            if (oAttacment is null)
                oAttacment = new List<string>();
            oAttacment.Add(sLine);
        }

        public void AddAlert(string value)
        {
            if (Strings.Len(value) <= 1)
            {
            }
            // Do nothing.  Don't want to send this
            else if (string.IsNullOrEmpty(sAlert))
            {
                sAlert = value;
            }
            else
            {
                sAlert += "," + value;
            }
        }

        public void AddFlag(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
            }
            // Do nothing.  Don't want to send this
            else if (string.IsNullOrEmpty(sFlag))
            {
                sFlag = value;
            }
            else
            {
                sFlag += "," + value;
            }
        }

        public string CocApproverEmpNumber
        {
            get
            {
                return sCocApproverEmpNumber;
            }
            set
            {
                sCocApproverEmpNumber = value;
            }
        }

        public string CocApproverFirstName
        {
            get
            {
                return sCocApproverFirstName;
            }
            set
            {
                sCocApproverFirstName = value;
            }
        }

        public string CocApproverLastName
        {
            get
            {
                return sCocApproverLastName;
            }
            set
            {
                sCocApproverLastName = value;
            }
        }

        public string CocApproverMiddleName
        {
            get
            {
                return sCocApproverMiddleName;
            }
            set
            {
                sCocApproverMiddleName = value;
            }
        }

        public DateTime CocApprovedDate
        {
            get
            {
                return dtCocApprovedDate;
            }
            set
            {
                dtCocApprovedDate = value;
            }
        }

        public string PanelCode
        {
            get
            {
                return _panelCode;
            }
            set
            {
                _panelCode = value;
            }
        }

        public int PanelSPMStatus
        {
            get
            {
                return _panelSPMStatus;
            }
            set
            {
                _panelSPMStatus = value;
            }
        }

    }
}