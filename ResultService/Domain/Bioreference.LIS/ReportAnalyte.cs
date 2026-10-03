using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Transactions;
using Bioreference.Common;
using Bioreference.Common.Lab;
using Bioreference.Common.TestMaster;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using iText.StyledXmlParser.Css.Util;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportAnalyte : AuditDataClassBase
    {

        #region Events

        public delegate void UpdateReportAnalyteDelegate(ReportAnalyte analyte, ReportAnalyteArg arg);

        [NonSerialized()]
        private List<UpdateReportAnalyteDelegate> mNonSerializableHandlers = new List<UpdateReportAnalyteDelegate>();

        // We need to declare this custom event because we need to mark the delegates as nonSerializable - so
        // that the target object will not be attempted to be serialized.
        public event UpdateReportAnalyteDelegate UpdateResultValueCompleted
        {

            add
            {

                if (mNonSerializableHandlers is null)
                {
                    mNonSerializableHandlers = new List<UpdateReportAnalyteDelegate>();
                }

                mNonSerializableHandlers.Add(value);

            }

            remove
            {

                if (!(mNonSerializableHandlers == null))
                {
                    mNonSerializableHandlers.Remove(value);
                }

            }

        }

        void OnUpdateResultValueCompleted(ReportAnalyte analyte, ReportAnalyteArg arg)
        {
            if (!(mNonSerializableHandlers == null))
            {
                foreach (UpdateReportAnalyteDelegate item in mNonSerializableHandlers)
                    item.Invoke(analyte, arg);
            }
        }

        [Serializable()]
        public class ReportAnalyteArg
        {
            private string m_oldValue;
            private string m_newValue;
            private ReportComment[] m_comment;
            private transmitStatusType m_oldTransmitStatus;
            private bool m_isCommentsUpdated = false;

            public ReportAnalyteArg(string oldValue, string newValue, transmitStatusType oldTransmitStatus, ReportComment[] comment = null, bool isCommentsUpdated = false)
            {
                m_oldValue = oldValue;
                m_newValue = newValue;
                m_comment = comment;
                m_oldTransmitStatus = oldTransmitStatus;
                m_isCommentsUpdated = isCommentsUpdated;
            }

            public string NewValue
            {
                get
                {
                    return m_newValue;
                }
            }
            public string OldValue
            {
                get
                {
                    return m_oldValue;
                }
            }
            public ReportComment[] Comments
            {
                get
                {
                    return m_comment;
                }
            }
            public transmitStatusType OldTransmitStatus
            {
                get
                {
                    return m_oldTransmitStatus;
                }
            }

            public bool IsCommentsUpdated
            {
                get
                {
                    return m_isCommentsUpdated;
                }
            }
        }


        public delegate void UpdatedReportAnalyteDelegate(ReportAnalyte analyte, ReportAnalyteUpdatedArg arg);
        public event UpdatedReportAnalyteDelegate CommentChanged;

        [Serializable()]
        public class ReportAnalyteUpdatedArg : EventArgs
        {

            private List<ReportComment> m_comment;
            private ReportAnalyte m_analyte;
            private ReportAnalytePanel m_analytePanel;
            private bool m_isAnalyteUpdated = false;
            private bool m_isPanelUpdated = false;
            private bool m_isCommentUpdated = false;
            private bool m_isCommentPanelUpdated = false;
            private bool m_isCommentChildUpdated = false;

            public ReportAnalyteUpdatedArg(ReportAnalytePanel analytePanel = null, ReportAnalyte analyte = null, List<ReportComment> comment = null, bool isCommentPanelUpdated = false, bool isCommentChildUpdated = false)
            {
                if (analytePanel is not null)
                {
                    m_isPanelUpdated = true;
                    m_analytePanel = analytePanel;
                }
                else
                {
                    m_isPanelUpdated = false;
                }

                if (analyte is not null)
                {
                    m_isAnalyteUpdated = true;
                    m_analyte = analyte;
                }
                else
                {
                    m_isAnalyteUpdated = false;
                }

                if (comment is not null)
                {
                    if (isCommentChildUpdated)
                    {
                        m_isCommentPanelUpdated = false;
                        m_isCommentChildUpdated = true;
                        m_isCommentUpdated = false;
                    }
                    else if (isCommentPanelUpdated)
                    {
                        m_isCommentPanelUpdated = true;
                        m_isCommentChildUpdated = false;
                        m_isCommentUpdated = false;
                    }
                    else
                    {
                        m_isCommentPanelUpdated = false;
                        m_isCommentChildUpdated = false;
                        m_isCommentUpdated = true;
                    }

                    m_comment = comment;
                }
                else
                {
                    m_isCommentUpdated = false;
                }
            }

            public List<ReportComment> Comments
            {
                get
                {
                    return m_comment;
                }
            }

            public ReportAnalyte Analyte
            {
                get
                {
                    return m_analyte;
                }
            }

            public ReportAnalytePanel AnalytePanel
            {
                get
                {
                    return m_analytePanel;
                }
            }

            public bool IsAnalyteUpdated
            {
                get
                {
                    return m_isAnalyteUpdated;
                }
            }

            public bool IsPanelUpdated
            {
                get
                {
                    return m_isPanelUpdated;
                }
            }

            public bool IsCommentUpdated
            {
                get
                {
                    if (m_comment is not null && m_comment.Count > 0)
                    {
                        return m_isCommentUpdated;
                    }
                    return false;
                }
            }

            public bool IsCommentPanelUpdated
            {
                get
                {
                    if (m_comment is not null && m_comment.Count > 0)
                    {
                        return m_isCommentPanelUpdated;
                    }
                    return false;
                }
            }

            public bool IsCommentChildUpdated
            {
                get
                {
                    if (m_comment is not null && m_comment.Count > 0)
                    {
                        return m_isCommentChildUpdated;
                    }
                    return false;
                }
            }

        }

        public delegate void AnalytePropertyChangedDelegate(ReportAnalyte analyte);

        public event AnalytePropertyChangedDelegate ResultChanged;
        public event AnalytePropertyChangedDelegate FlagChanged;
        public event AnalytePropertyChangedDelegate UnitsChanged;
        public event AnalytePropertyChangedDelegate RangeChanged;
        public event AnalytePropertyChangedDelegate Released;
        public event AnalytePropertyChangedDelegate StatusChanged;

        protected internal void Updated(ReportAnalyteUpdatedArg e)
        {
            // If Not IsNothing(Changed) Then
            CommentChanged?.Invoke(this, e);
            // End If
        }

        #endregion

        #region Private Members
        private DataClassBase m_parent = null; // Either Report or ReportAnalytePanel
        private RefAnalyte m_analyte = null;
        private ReportComments m_comments = null;
        private Attachments m_Attachments = null;

        private long m_id = 0L;
        private string m_resultValue = "";
        private string m_orgResultValue = ""; // Non-updatable result value as it is comes from the database..this is used to help build result value history.
        private resultStatusType m_resultStatus = resultStatusType.Pending;
        private resultStatusType m_orgResultStatus;
        private transmitStatusType m_transmitStatus = transmitStatusType.PendingRelease;
        private DateTime m_resultDate = DateTime.Parse("1900-01-01");
        private string m_flagValue = "";
        private string m_previousValue = ""; // 'Used for when value is reset before it is reported. Needs to be reviewed.

        private string m_performingFacility = "";
        private string m_accessioningFacility = "";

        private long m_spmOrderTestId = 0;

        private string m_instrumentId = "";

        private string m_AnalyticalUnitName;
        private string m_AnalyticalUnitSerial;
        private int m_InstDivisionId;
        private string m_AnalyzerName;
        private string m_AnalyzerModel;
        private string m_AnalyzerVendor;

        private string m_instrument = ""; // An optional property used to determine which intrument reported the result.
        private string m_instrumentAlt1 = "";
        private string m_instrumentAlt2 = "";
        private string m_instrumentAlt3 = "";
        private string m_resultAnalyzedTechUser = "";
        private string m_resultReleasedUser = "";

        private string m_specimenRackID = "";
        private string m_specimenRackPos = "";
        private string m_specimenRackSeq = "";
        private string m_specimenAlt1 = "";
        private string m_specimenAlt2 = "";

        private List<string> m_orderingAnalyteCodes = new List<string>();
        private bool m_sentOut = false; // Use to designate if an analyte was sent out
        private ReportAlerts m_alerts;

        private criticalType m_criticalType = criticalType.NotEvaluated;

        private bool m_resetReleaseDate = false; // This gets set to True when an Analyte is set to Release. The date is then set on the server.
        private DateTime m_releaseDate = DateTime.Parse("1900-01-01");
        private DateTime m_dateCreated = DateTime.Parse("1900-01-01");

        // 'Variables for RRE Worksheets
        private string m_rreType = "";
        private int m_rreId = 0;

        private bool m_isResultManual = false; // Determines if the result was manually entered by a user. Must use SetResultValue method insteat of ResultValue property.

        internal bool m_IsCommmentsUpdated = false; // Used In ResultsArg - lets us know if comments where updated.

        private bool m_isDeleted = false; // Need this to track if a ReportAnalyte is deleted. A report analyte can be updated even if deleted (ex. demographic updates)

        private string m_releasedValue = "";          // needed for CorrectedComments
        private string m_priorReleasedValue = "";     // needed for CorrectedComments
        private int m_releasedStatus = -2;        // needed for CorrectedComments
        private DateTime m_priorReleaseDate = DateTime.Parse("1900-01-01"); // needed for CorrectedComments

        private bool m_isCOCReviewed = false;
        private string m_COCApprover = "";    // username - populated only when a reviewer approves.
        private string m_CocApproverFirstName = "";   // ReadOnly - retreived from the reviewer table
        private string m_CocApproverLastName = "";    // ReadOnly - retreived from the reviewer table
        private string m_CocApproverMiddleName = "";  // ReadOnly - retreived from the reviewer table
        private string m_CocApproverEmpNbr = "";  // ReadOnly - retreived from the reviewer table
        private DateTime m_CocApprovedDate = DateTime.Parse("1900-01-01");    // ReadOnly - retreived from the reviewer table

        private string m_studyNumber = "";
        private string m_visitNumber = "";

        private List<string> m_auditItemList;

        private int m_codeTypeId = 0;
        private bool m_toFollowSent = false;
        private bool _trimInequality = false;

        private bool _isPreliminaryReleased = false;
        private string m_priorEUIDResultValue = "";
        private bool m_isCopiedAnalyte = false;
        private bool m_isTestHold = false;
        private bool m_orgIsTestHold;
        private bool m_statusUpdated = false;
        private bool m_isReportingHold = false;
        private string m_correctedResultReason = "";
        private readonly ILog Log = LogManager.GetLogger<ReportAnalyte>();
        private resultStatusType m_ResultStatusBeforeRules;
        private bool m_isRuleResultStatusSet;
        private bool m_isPOC = false;
        private bool m_isDoubleEntry = false;
        private string m_previousFlagValue = "";
        private string m_currentFlagValue = "";
        private bool m_auditDoubleEntry = false;
        private int m_RackWorksheetId;
        private bool m_reReleased = false;
        private int m_reReleaseStatus;
        private bool m_isNewAnalyteAdded = false;
        private bool m_isPresumptiveHold = false;
        private int m_sampleStatus;
        private bool m_releaseFromUI;
        private bool m_IsAnalyteResettingToPending = false;
        private DateTime m_sampleStatusUpdateDate = DateTime.Parse("1900-01-01");
        private DateTime m_instrumentLoadTime = DateTime.Parse("1900-01-01");
        private string m_specimenCodes = "";
        private string m_deltaHoldRule = "";
        private string m_parentTestCode = "";
        private SPMStatusValue m_spmStatus = SPMStatusValue.None;
        private int m_RefLabPerformingFacilityId = 0;

        private bool m_manualReReleaseFlag = false;

        #endregion

        #region Constructor

        public ReportAnalyte(DataClassBase parent, TestAnalyte analyte, string orderingAnalyteCode = "", SPMStatusValue spmStatus = SPMStatusValue.None)
        {
            m_orgResultStatus = m_resultStatus;
            m_orgIsTestHold = m_isTestHold;
            m_auditItemList = new List<string>();
            m_parent = parent;
            AddOrderingCode(orderingAnalyteCode);

            m_analyte = new RefAnalyte(this);
            m_analyte.Load(analyte);
            m_spmStatus = spmStatus;

            m_comments = new ReportComments(this);
            m_alerts = new ReportAlerts(this);
            m_Attachments = new Attachments(this);

            InitializeToFollowSent(null);
            FlagDirty();
            FlagChild();
        }

        public ReportAnalyte(DataClassBase parent, TestInfo testInfo, string orderingAnalyteCode = "", SPMStatusValue spmStatus = SPMStatusValue.None)
        {
            m_orgResultStatus = m_resultStatus;
            m_orgIsTestHold = m_isTestHold;
            m_auditItemList = new List<string>();
            m_parent = parent;

            AddOrderingCode(orderingAnalyteCode);

            m_analyte = new RefAnalyte(this);
            m_analyte.Load(testInfo);
            m_spmStatus = spmStatus;

            m_comments = new ReportComments(this);
            m_alerts = new ReportAlerts(this);
            m_Attachments = new Attachments(this);

            InitializeToFollowSent(testInfo);
            FlagDirty();
            FlagChild();
        }

        // Load from db
        internal ReportAnalyte(DataClassBase parent, bool loadAudit = false, SPMStatusValue spmStatus = SPMStatusValue.None)
        {
            m_orgResultStatus = m_resultStatus;
            m_orgIsTestHold = m_isTestHold;
            m_auditItemList = new List<string>();
            m_parent = parent;
            m_comments = new ReportComments(this);
            m_alerts = new ReportAlerts(this);
            m_Attachments = new Attachments(this);
            m_spmStatus = spmStatus;
            LoadAudit = loadAudit;
            m_analyte = new RefAnalyte(this);
            InitializeToFollowSent(null);
            FlagChild();
        }

        #endregion



        private void InitializeToFollowSent(TestInfo testinfo)
        {
            // ''''''''''''''''''''''''''''' To Follow'''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            if (!Configuration.ToFollowEnabled || !(testinfo == null) && testinfo.DoNotSendToFollow)
            {
                m_toFollowSent = true;
                return;
            }
            Report r = null;
            if (m_parent is Report)
            {
                r = (Report)m_parent;
            }
            else if (m_parent is ReportAnalytePanel)
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }

            if (!(r == null) && r.IsCOC)
            {
                ToFollowSent = true;
            }
            // ElseIf Not IsNothing(m_analyte) AndAlso m_analyte.Category.ToLower().Equals("allergen") Then
            // Me.ToFollowSent = True
            else if (!(m_analyte == null) && m_analyte.IsReportable && m_analyte.IsRequired)
            {
                m_toFollowSent = false;
            }
            else
            {
                m_toFollowSent = true;
            }
            // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        }

        #region Public Properties

        public RefAnalyte RefAnalyte
        {
            get
            {
                return m_analyte;
            }
        }

        public string PriorReleasedValue
        {
            get
            {
                return m_priorReleasedValue;
            }
        }

        public string ReleasedValue
        {
            get
            {
                return m_releasedValue;
            }
        }

        public int ReleasedStatus
        {
            get
            {
                return m_releasedStatus;
            }
        }

        public DateTime PriorReleaseDate
        {
            get
            {
                return m_priorReleaseDate;
            }
        }

        [Audit("PerformingFaciltiy")]
        [Obsolete("This property will be removed in the next release.  Use PerformingFacility instead.")]
        public string PerformingFaciltiy
        {
            get
            {
                return m_performingFacility;
            }
        }

        [Audit("PerformingFacility")]
        public string PerformingFacility
        {
            get
            {
                return m_performingFacility;
            }
        }

        [Audit("AccessioningFacility")]
        public string AccessioningFacility
        {
            get
            {
                return m_accessioningFacility;
            }
        }

        [Audit("RefLabPerformingFacilityId")]
        public int RefLabPerformingFacilityId
        {
            get
            {
                return m_RefLabPerformingFacilityId;
            }
            set
            {

                m_RefLabPerformingFacilityId = value;
                FlagDirty();
            }
        }

        [Audit("CorrectedResultReason")]
        public string CorrectedResultReason
        {
            get
            {
                return m_correctedResultReason;
            }
            set
            {

                m_correctedResultReason = value?.NormalizeToWindows() ?? string.Empty;

            }
        }

        public string PreviousFlagValue
        {
            get
            {
                return m_previousFlagValue;
            }
        }

        public int RackWorksheetId
        {
            get
            {
                return m_RackWorksheetId;
            }
        }

        public string CurrentFlagValue
        {
            get
            {
                return m_currentFlagValue;
            }
        }

        [Audit("SPMOrderTestId")]
        public string SPMOrderTestId
        {
            get
            {
                return m_spmOrderTestId.ToString();
            }
        }

        public string RapidResultsType
        {
            get
            {
                return m_rreType;
            }
        }

        public int RapidResultId
        {
            get
            {
                return m_rreId;
            }
        }

        public DateTime ReleaseDate
        {
            get
            {
                return m_releaseDate;
            }
        }

        public DataClassBase Parent
        {
            get
            {
                return m_parent;
            }
        }
        public ReportAnalytePanel ParentPanel
        {
            get
            {
                if (m_parent is ReportAnalytePanel)
                {
                    return (ReportAnalytePanel)m_parent;
                }
                return null;
            }
        }

        public long ID
        {
            get
            {
                return m_id;
            }
        }

        public string Code
        {
            get
            {
                return m_analyte.Code;
            }
        }

        public string AnalyteName
        {
            get
            {
                return m_analyte.Name;
            }
        }

        [Audit("CriticalType")]
        public criticalType CriticalType
        {
            get
            {
                return m_criticalType;
            }
        }

        public string[] OrderingAnalyteCodes
        {
            get
            {
                return m_orderingAnalyteCodes.ToArray();
            }
        }

        [Audit("OrderingCodes")]
        public string OrderingCodes
        {
            get
            {
                return string.Join(",", m_orderingAnalyteCodes.ToArray());
            }
        }

        [Audit("InstrumentId")]
        public string InstrumentId
        {
            get
            {
                return m_instrumentId;
            }
            set
            {
                if ((m_instrumentId ?? "") != (value ?? ""))
                {
                    m_instrumentId = value;
                    FlagDirty();
                }
            }
        }

        public string AnalyticalUnitName
        {
            get
            {
                return m_AnalyticalUnitName;
            }
            set
            {
                m_AnalyticalUnitName = value.Trim();
            }
        }

        public bool IsRuleResultStatusSet
        {
            get
            {
                return m_isRuleResultStatusSet;
            }
            set
            {
                m_isRuleResultStatusSet = value;
            }
        }

        public string AnalyticalUnitSerial
        {
            get
            {
                return m_AnalyticalUnitSerial;
            }
            set
            {
                m_AnalyticalUnitSerial = value.Trim();
            }
        }

        public int InstrumentDivisionId
        {
            get
            {
                return m_InstDivisionId;
            }
        }

        public string AnalyzerName
        {
            get
            {
                return m_AnalyzerName;
            }
            set
            {
                m_AnalyzerName = value.Trim();
            }
        }

        public string AnalyzerModel
        {
            get
            {
                return m_AnalyzerModel;
            }
            set
            {
                m_AnalyzerModel = value.Trim();
            }
        }

        public string AnalyzerVendor
        {
            get
            {
                return m_AnalyzerVendor;
            }
            set
            {
                m_AnalyzerVendor = value.Trim();
            }
        }


        [Audit("Instrument")]
        public string Instrument
        {
            get
            {
                return m_instrument;
            }
            set
            {
                if ((m_instrument ?? "") != (value.Trim() ?? ""))
                {
                    m_instrument = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("InstrumentAlt1")]
        public string InstrumentAlt1
        {
            get
            {
                return m_instrumentAlt1;
            }
            set
            {
                if ((m_instrumentAlt1 ?? "") != (value.Trim() ?? ""))
                {
                    m_instrumentAlt1 = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("InstrumentAlt2")]
        public string InstrumentAlt2
        {
            get
            {
                return m_instrumentAlt2;
            }
            set
            {
                if ((m_instrumentAlt2 ?? "") != (value.Trim() ?? ""))
                {
                    m_instrumentAlt2 = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("InstrumentAlt3")]
        public string InstrumentAlt3
        {
            get
            {
                return m_instrumentAlt3;
            }
            set
            {
                if ((m_instrumentAlt3 ?? "") != (value.Trim() ?? ""))
                {
                    m_instrumentAlt3 = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("FlagValue")]
        public string FlagValue
        {
            get
            {
                if (m_analyte.IsFlagDeleted) // TODO: flg
                {
                    return "";
                }

                if (m_flagValue.Equals(""))
                {
                    return GetFlaggedValue();
                }
                return m_flagValue;
            }
            set
            {
                if ((m_flagValue ?? "") != (value.Trim() ?? ""))
                {
                    m_flagValue = value.Trim();
                    FlagDirty();
                }
            }
        }

        /// <summary>
        /// Used for when value is reset before it is reported. Needs to be reviewed.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        [Audit("PreviousResultValue")]
        public string PreviousResultValue
        {
            get
            {
                return m_previousValue;
            }
        }

        [Audit("ResultValue", true)]
        public string ResultValue
        {
            get
            {
                return m_resultValue;
            }
            set
            {

                SetResultValue(value, false);

            }
        }

        public bool AuditDoubleEntry
        {
            get
            {
                return m_auditDoubleEntry;
            }
            set
            {

                m_auditDoubleEntry = value;

            }
        }

        public bool IsNewAnalyteAdded
        {
            get
            {
                return m_isNewAnalyteAdded;
            }
            set
            {

                m_isNewAnalyteAdded = value;

            }
        }


        #region Change Status to Corrected when set Flag, Range and Units

        public string Range
        {
            get
            {
                return m_analyte.OriginalTestRangeValue;
            }
        }

        public bool RangeUpdated
        {
            get
            {
                return m_analyte.RangeUpdated;
            }
        }

        public bool UnitsUpdated
        {
            get
            {
                return m_analyte.UnitsUpdated;
            }
        }

        public bool FlagUpdated
        {
            get
            {
                return m_analyte.FlagUpdated;
            }
        }

        public bool StatusUpdated
        {
            get
            {
                return m_statusUpdated;
            }
        }
        #endregion


        public bool IsResultManual
        {
            get
            {
                return m_isResultManual;
            }
        }

        public List<string> GetFormattedAuditItems
        {
            // get the audit item of this object as well as any object in the Save stream.
            get
            {
                return m_auditItemList;
            }
        }



        [Audit("IsCOCReviewed")]
        public bool IsCOCReviewed
        {
            get
            {
                return m_isCOCReviewed;
            }
            set
            {
                if (m_isCOCReviewed != value)
                {
                    m_isCOCReviewed = value;
                    FlagDirty();
                }
            }
        }

        [Audit("COCApprover")]
        public string COCApprover
        {
            get
            {
                return m_COCApprover;
            }
            set
            {
                if ((m_COCApprover ?? "") != (value.Trim() ?? ""))
                {
                    m_COCApprover = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string CocApproverFirstName
        {
            get
            {
                return m_CocApproverFirstName;
            }
        }

        public string CocApproverLastName
        {
            get
            {
                return m_CocApproverLastName;
            }
        }

        public string CocApproverMiddleName
        {
            get
            {
                return m_CocApproverMiddleName;
            }
        }

        public string CocApproverEmpNbr
        {
            get
            {
                return m_CocApproverEmpNbr;
            }
        }

        public DateTime CocApprovedDate
        {
            get
            {
                return m_CocApprovedDate;
            }
        }

        public bool IsFlagDeleted
        {
            get
            {
                return m_analyte.IsFlagDeleted;
            }
        }

        public bool ToFollowSent
        {
            get
            {
                return m_toFollowSent;
            }
            set
            {
                m_toFollowSent = value;
                FlagDirty();
            }
        }

        public int CodeTypeId
        {
            get
            {
                return m_codeTypeId;
            }
            set
            {
                m_codeTypeId = value;
            }
        }

        public string CalculatibleResultValue
        {
            get
            {
                if (_trimInequality)
                {
                    string currentValue;
                    currentValue = ResultValue.Trim();

                    while (currentValue.Length > 0 && "<>=".Contains(currentValue.Substring(0, 1)))
                        currentValue = currentValue.Substring(1);

                    return currentValue;
                }
                else
                {
                    return ResultValue;
                }
            }
        }

        public bool IsPreliminaryReleased
        {
            get
            {
                return _isPreliminaryReleased;
            }
        }

        [Audit("ReportingHold")]
        public bool ReportingHold
        {
            get
            {
                return m_isReportingHold;
            }
        }

        public bool IsCopiedAnalyte
        {
            get
            {
                return m_isCopiedAnalyte;
            }
            set
            {
                m_isCopiedAnalyte = value;
                FlagDirty();
            }
        }

        public bool IsTestHold
        {
            get
            {
                return m_isTestHold;
            }
        }

        public bool IsDeltaHold
        {
            get
            {
                return m_resultStatus == resultStatusType.DeltaHold;
            }
        }

        public resultStatusType ResultStatusBeforeRuleRun
        {
            get
            {
                return m_ResultStatusBeforeRules;
            }
        }

        public bool IsPOC
        {
            get
            {
                return m_isPOC;
            }
        }

        public bool IsDoubleEntry
        {
            get
            {
                return m_isDoubleEntry;
            }
        }

        public reReleaseStatusType ReReleaseStatus
        {
            get
            {
                return (reReleaseStatusType)m_reReleaseStatus;
            }
        }

        [Audit("SampleStatus")]
        public int SampleStatus
        {
            get
            {
                return m_sampleStatus;
            }
            set
            {
                m_sampleStatus = value;
                FlagDirty();
            }
        }

        public DateTime SampleStatusUpdateDate
        {
            get
            {
                return m_sampleStatusUpdateDate;
            }
        }

        public DateTime InstrumentLoadTime
        {
            get
            {
                return m_instrumentLoadTime;
            }
            set
            {
                m_instrumentLoadTime = value;
                FlagDirty();
            }
        }

        public bool ReleaseFromUI
        {
            get
            {
                return m_releaseFromUI;
            }
            set
            {
                m_releaseFromUI = value;
            }
        }

        [Audit("SPMStatus")]
        public SPMStatusValue SPMStatus
        {
            get
            {
                return m_spmStatus;
            }
            set
            {
                if (value != m_spmStatus)
                {
                    m_spmStatus = value;
                    FlagDirty();
                }
            }
        }

        public void SetResultValue(string value, bool isResultManual)
        {
            SetResultValue(value, isResultManual, "");
        }

        /// <summary>
        /// Should be used instead of the ResultValue property when result value
        /// is entered by an actual user.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isResultManual">Determines if added by an actual user.  Will disable autorelease
        /// and will force calculation type to keep result.</param>
        /// <remarks></remarks>
        public void SetResultValue(string value, bool isResultManual, string performingFacility)
        {

            if (value is null)
            {
                value = string.Empty;
            }
            // If Me.Analyte.IsCalculation Then
            // Throw New Exception("Unable to edit ResultValue for a Calculation.")
            // End If

            // Disable rounding for now - it will be implemented at a later date.
            // Must also uncomment precision set in RefAnalyte.
            // Only apply for calculations at this time.
            // ******************************************************************
            decimal roundedValue;
            // If Me.Analyte.IsCalculation AndAlso Double.TryParse(value, roundedValue) Then
            if (!value.Equals("Infinity") && decimal.TryParse(value, out roundedValue))
            {
                int precision = ((RefAnalyte)Analyte).Precision;
                // 'If precision value is negative, we do not apply it
                if (precision >= 0)
                {
                    value = decimal.Round(roundedValue, precision, MidpointRounding.AwayFromZero).ToString(string.Concat("F", precision));
                }
            }
            // *****************************************************************

            if ((value.Trim() ?? "") != (m_resultValue ?? ""))
            {

                m_isResultManual = isResultManual;
                if (!string.IsNullOrEmpty(performingFacility))
                    SetPerformingFacility(performingFacility);

                string oldVal = m_resultValue;
                var oldTransmitStauts = m_transmitStatus;
                var comments = new List<ReportComment>(); // Need this when raising event for comments that are automatically added.
                ReportComment correctedComment = null;

                // 'Moved up here
                if (!m_resultValue.Equals(""))
                    m_previousValue = m_resultValue;
                m_resultValue = value.Trim();
                Report parentReport = null;
                ReportAnalytePanel parentPanel = null;
                if (m_parent is Report)
                {
                    parentReport = (Report)m_parent;
                }
                else if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    parentPanel = (ReportAnalytePanel)m_parent;
                    parentReport = ((ReportAnalytePanel)m_parent).Parent;
                }
                var o = parentReport.ReferencedOrder;
                if (m_isResultManual && (value ?? "") == (m_orgResultValue ?? ""))
                {
                    m_resultStatus = m_orgResultStatus;
                    if (m_orgResultStatus != resultStatusType.Corrected)
                    {
                        CorrectedResultComment.RemoveCorrectedComments(this, false);
                    }
                }
                // End If

                // 'SETTING STATUSES*******************************************
                // 'AG - 10/8/2013 - Removed value.Trim() <> "" AndAlso
                else if (!(parentPanel == null) && !parentPanel.IsProcessing && parentPanel.HasBeenReleased() && parentPanel.GetStatus() >= resultStatusType.Final && parentPanel.GetStatus() != resultStatusType.OnHold || HasBeenPreviouslyReleased())
                {
                    // '(SharedFunctions.HasBeenReleased(m_transmitStatus) AndAlso Me.ResultStatus = resultStatusType.Final) Then
                    ResultStatus = resultStatusType.Corrected;
                    SetTransmitStatus(transmitStatusType.PendingRelease);
                    // If m_parent.GetType() Is GetType(ReportAnalytePanel) Then CType(m_parent, ReportAnalytePanel).SetTransmitStatus(transmitStatusType.PendingRelease)
                    if (!(parentPanel == null))
                    {
                        parentPanel.SetTransmitStatus(transmitStatusType.PendingRelease);
                        parentPanel.SetCriticalType(criticalType.NotEvaluated);
                    }

                    // This sets a dynamic comment based on the analyte
                    if (!(CurrentUser == null))
                    {
                        if (o.DemographicUpdate && !m_resultValue.Equals("") && !ReportingHold)
                        {
                            if (m_parent is ReportAnalytePanel)
                            {
                                AddCorrectedPanelTestList();
                            }
                            if (parentPanel == null)
                            {
                                correctedComment = CorrectedResultComment.AddCorrectedComment(this);
                            }
                            else if (m_analyte.IsReportable && m_analyte.IsRequired)
                            {
                                correctedComment = CorrectedResultComment.AddCorrectedComment(this, parentPanel.ReleaseDate);
                            }
                        }
                        // else if ((parentReport.IsRuleExecution && !(CodeTypeId == 5) || CurrentUser.IsInRole(RoleKeys.b2_Manage_Revised_Reason) && isResultManual) && !m_resultValue.Equals("") && !ReportingHold)
                        else if (((parentReport.IsRuleExecution && CodeTypeId != 5) || isResultManual) && !string.IsNullOrEmpty(m_resultValue) && !ReportingHold)
                        {
                            if (m_parent is ReportAnalytePanel)
                            {
                                AddCorrectedPanelTestList();
                            }
                            if (parentPanel == null)
                            {
                                correctedComment = CorrectedResultComment.AddCorrectedComment(this);
                            }
                            else
                            {
                                correctedComment = CorrectedResultComment.AddCorrectedComment(this, parentPanel.ReleaseDate);
                            }
                        }
                    }
                }

                else
                {
                    // m_previousValue = m_resultValue
                    ResultStatus = (resultStatusType)Conversions.ToInteger(Interaction.IIf(string.IsNullOrEmpty(value.Trim()), resultStatusType.Pending, resultStatusType.Preliminary));
                    SetTransmitStatus(transmitStatusType.PendingRelease);
                    // Reset the parent transmit type.
                    // If Me.m_parent.GetType Is GetType(ReportAnalytePanel) Then CType(m_parent, ReportAnalytePanel).SetTransmitStatus(transmitStatusType.PendingRelease)
                    if (!(parentPanel == null))
                    {
                        parentPanel.SetTransmitStatus(transmitStatusType.PendingRelease);
                        parentPanel.SetCriticalType(criticalType.NotEvaluated);
                    }
                }
                // '**********************************************************

                // If Not m_resultValue.Equals("") Then m_previousValue = m_resultValue
                // m_resultValue = value.Trim()
                m_criticalType = criticalType.NotEvaluated; // this needs to be reset so it can be reevaluated
                SetSampleStatus();
                if (!(m_sampleStatus == (int)SampleRequestType.None) && m_sampleStatus == (int)SampleRequestType.SampleRequest)
                {
                    ResultStatus = resultStatusType.Preliminary;
                }
                FlagDirty();

                // 'CALCULATIONS**********************************************
                if (!(parentReport == null) && parentReport.HasCalcAnalytes)
                {
                    PerformCalculation(parentReport, isResultManual);
                }
                // '**********************************************************

                comments = DoAutoComments();
                if (m_resultValue == "TNP")
                {

                    foreach (ReportComment comment in comments)
                        comment.SetCommentType(CommentType.TestNotPerformed);
                }

                // 'This was moved to MarkAsReleased method
                // 'RULES EVALUATION***********************************************
                Log.DebugFormat("RuleSetChangeResult analyte id={0}, code={1}, value={2}", ID, Code, ResultValue);
                Log.Debug("Calling EvaluateRulesEx with RuleSetChangeResult");
                parentReport.EvaluateRulesEx(this, "ResultValue", Configuration.RuleSetChangeResult);
                // '***************************************************************

                bool triggerReflex = false;
                if (triggerReflex)
                {
                    parentReport.SendAddTestToVertex("TBD");
                }

                // 'AUTORELEASING**************************************************
                CallAutoRelease();
                // '***************************************************************

                OnUpdateResultValueCompleted(this, new ReportAnalyteArg(oldVal, m_resultValue, oldTransmitStauts, comments.ToArray(), m_IsCommmentsUpdated));

                ResultChanged?.Invoke(this);
                if (!(comments == null) || !(correctedComment == null))
                {
                    if (comments == null)
                    {
                        comments = new List<ReportComment>();
                    }
                    if (!(correctedComment == null))
                    {
                        comments.Add(correctedComment);
                    }
                    if (comments.Count > 0)
                    {
                        Updated(new ReportAnalyteUpdatedArg(null, this, comments));
                    }
                }
            }
            else
            {
                ResultChanged?.Invoke(this);
            }    // ''' Raise the event if the values are iquals after round them
        }

        public void SetSampleStatus()
        {
            Report r = null;
            if (m_parent is Report)
            {
                r = (Report)m_parent;
            }
            else if (m_parent is ReportAnalytePanel)
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }
            var o = r.ReferencedOrder;
            if (o.AccountPriority == "HI" | o.AccountPriority == "H" && ResultValue == "QNS" && m_sampleStatus == (int)SampleRequestType.None)
            {
                m_sampleStatus = (int)SampleRequestType.SampleRequest;
            }
        }

        public void SetCorrectedStatusFlagForRules(resultStatusType resultStatus)
        {
            m_ResultStatusBeforeRules = resultStatus;
        }

        private void AddCorrectedTestList()
        {
            if (!RefAnalyte.IsReportable)
            {
                return;
            }
            Report r = null;
            if (m_parent is Report)
            {
                r = (Report)m_parent;
            }
            else if (m_parent is ReportAnalytePanel)
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }
            if (r.CorrectedTests.ContainsKey(Code))
            {
                r.CorrectedTests[Code] = AnalyteName;
            }
            else
            {
                r.CorrectedTests.Add(Code, AnalyteName);
            }
        }
        private void AddCorrectedPanelTestList()
        {
            if (!RefAnalyte.IsReportable)
            {
                return;
            }
            Report r = null;

            if (m_parent is ReportAnalytePanel)
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }
            if (r.CorrectedPanelTests.ContainsKey(AnalyteName))
            {
                r.CorrectedPanelTests[Code] = ReleasedValue;
            }
            else
            {
                r.CorrectedPanelTests.Add(AnalyteName, ReleasedValue);
            }
        }

        private void CallAutoRelease()
        {

            // 'If manually entered do not autorelease.
            if (!m_isResultManual)
            {
                // 'Added Calc check - if a value is set for a calculation, it should not automatically release until all 
                // 'component calc tests are released. They will trigger the calc to be released.
                if (Conversions.ToBoolean(!Analyte.IsCalculation && m_analyte.AutoRelease && !string.IsNullOrEmpty(m_resultValue) && m_resultStatus != resultStatusType.Pending && m_resultStatus != resultStatusType.DeltaHold && !IsSampleEnRoute() && CanAutoRelease()))
                {
                    MarkAsReleased();
                }
            }


        }

        private bool IsSampleEnRoute()
        {
            bool val = false;
            if (m_sampleStatus == (int)SampleRequestType.SampleEnRoute)
            {
                val = true;
            }
            return val;
        }

        internal List<ReportComment> DoAutoComments()
        {

            var comments = new List<ReportComment>();
            // 'AUTO COMMENTS AND FLAGGING *******************************
            // 6/14/10 Remove if condition - allow for Sendouts
            // If Me.Analyte.ReferenceLabId = 0 Then

            // Reset whether comments have been changed
            m_IsCommmentsUpdated = false;

            RemoveAutoAddedComments();

            // AG 2/23/09 - Need to return comments back that are dynamically added based on results.
            var fr = GetFlaggedValueAndComments();
            ReportComment r;

            if (fr == null)
            {
                m_flagValue = "";
            }
            else
            {
                // 'Do not set the FlagValue - it needs to be autogenerated if not overridden.

                foreach (DefaultComment c in fr.Comments)
                {

                    if (Comments.Find(c.AssignedID) == null)
                    {
                        r = Comments.AddComment(c, true);
                        comments.Add(r); // Add to this list so that we can use it to raise an event later.
                        m_IsCommmentsUpdated = true;
                    }

                }
            }
            // '***************************************************************

            return comments;

        }

        // Friend Function DoAutoComments() As List(Of ReportComment)

        // Dim comments As List(Of ReportComment) = New List(Of ReportComment)
        // ''AUTO COMMENTS AND FLAGGING *******************************
        // '6/14/10 Remove if condition - allow for Sendouts
        // 'If Me.Analyte.ReferenceLabId = 0 Then

        // 'Reset whether comments have been changed
        // m_IsCommmentsUpdated = False

        // RemoveAutoAddedComments()

        // 'Suppress automatic comments if the result value indicates that the test was not performed.
        // If Not Configuration.Settings.SuppressCommentByResult.Contains(Me.ResultValue.Trim()) Then
        // Dim fr As FlagResult = Me.GetFlaggedValueAndComments()
        // Dim r As ReportComment
        // If IsNothing(fr) Then
        // m_flagValue = ""
        // Else
        // ''Do not set the FlagValue - it needs to be autogenerated if not overridden.
        // For Each c As DefaultComment In fr.Comments
        // If IsNothing(Me.Comments.Find(c.AssignedID)) Then
        // r = Me.Comments.AddComment(c, True)
        // comments.Add(r) 'Add to this list so that we can use it to raise an event later.
        // Me.m_IsCommmentsUpdated = True
        // End If
        // Next
        // End If
        // Else
        // m_flagValue = ""
        // End If

        // Return comments

        // End Function

        [Audit("ResultDate")]
        public DateTime ResultDate
        {
            get
            {
                return m_resultDate;
            }
            set
            {
                if (value != m_resultDate)
                {
                    m_resultDate = value;
                    FlagDirty();
                }
            }
        }

        public ReportComments Comments
        {
            get
            {
                return m_comments;
            }
        }

        public Attachments AnalyteAttachments
        {
            get
            {
                return m_Attachments;
            }
        }

        [Audit("ResultStatus")]
        public resultStatusType ResultStatus
        {
            get
            {
                return m_resultStatus;
            }
            set
            {
                if (value != m_resultStatus)
                {
                    m_resultStatus = value;
                    FlagDirty();
                }
            }
        }

        [Audit("TransmitStatus")]
        public transmitStatusType TransmitStatus
        {
            get
            {
                return m_transmitStatus;
            }
        }

        [Audit("SpecimenRackId")]
        public string SpecimenRackId
        {
            get
            {
                return m_specimenRackID;
            }
            set
            {
                if ((value ?? "") != (m_specimenRackID.Trim() ?? ""))
                {
                    m_specimenRackID = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("SpecimenRackPosition")]
        public string SpecimenRackPosition
        {
            get
            {
                return m_specimenRackPos;
            }
            set
            {
                if ((value ?? "") != (m_specimenRackPos.Trim() ?? ""))
                {
                    m_specimenRackPos = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("SpecimenRackSequence")]
        public string SpecimenRackSequence
        {
            get
            {
                return m_specimenRackSeq;
            }
            set
            {
                if ((value ?? "") != (m_specimenRackSeq.Trim() ?? ""))
                {
                    m_specimenRackSeq = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("SpecimenAlt1")]
        public string SpecimenAlt1
        {
            get
            {
                return m_specimenAlt1;
            }
            set
            {
                if ((value ?? "") != (m_specimenAlt1.Trim() ?? ""))
                {
                    m_specimenAlt1 = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("SpecimenAlt2")]
        public string SpecimenAlt2
        {
            get
            {
                return m_specimenAlt2;
            }
            set
            {
                if ((value ?? "") != (m_specimenAlt2.Trim() ?? ""))
                {
                    m_specimenAlt2 = value.Trim();
                    FlagDirty();
                }
            }
        }

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        protected override long ObjectLookupIdentifier
        {
            get
            {
                return m_analyte.Id;
            }
        }

        protected override object ParentIdentifierId
        {
            get
            {
                return GetParentReport().AccessionIdentifier;
            }
        }

        protected override parentIdentifierType ParentIdentifierType
        {
            get
            {
                return GetParentReport().AccessionIdentifierType;
            }
        }

        public Analyte Analyte
        {
            get
            {
                return m_analyte;
            }
        }

        public ReportAlerts Alerts
        {
            get
            {
                return m_alerts;
            }
        }

        public bool IsAnalyteResettingToPending
        {
            get
            {
                return m_IsAnalyteResettingToPending;
            }
            set
            {
                m_IsAnalyteResettingToPending = value;
            }
        }

        // '*******************************************************
        // Security Implementation
        // 'Currently this is controlled by the UI.  This is not enforced by the object!

        private bool m_canRead = true;
        private bool m_canWrite = true;
        private bool m_canRelease = true;
        private bool m_isBlinded = false;

        public bool CanRead
        {
            get
            {
                return m_canRead;
            }
            set
            {
                m_canRead = value;
            }
        }

        public bool CanWrite
        {
            get
            {
                return m_canWrite;
            }
            set
            {
                m_canWrite = value;
            }
        }

        public bool CanRelease
        {
            get
            {
                return m_canRelease;
            }
            set
            {
                m_canRelease = value;
            }
        }

        public bool IsBlinded
        {
            get
            {
                return m_isBlinded;
            }
            set
            {
                m_isBlinded = value;
            }
        }

        // '*******************************************************

        [Audit("ResultReleasedUser")]
        public string ResultReleasedUser
        {
            get
            {
                return m_resultReleasedUser;
            }
            set
            {
                if ((m_resultReleasedUser ?? "") != (value.Trim() ?? ""))
                {
                    m_resultReleasedUser = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("ResultAnalyzedTechUser")]
        public string ResultAnalyzedTechUser
        {
            get
            {
                return m_resultAnalyzedTechUser;
            }
            set
            {
                if ((m_resultAnalyzedTechUser ?? "") != (value.Trim() ?? ""))
                {
                    m_resultAnalyzedTechUser = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string StudyNumber
        {
            get
            {
                return m_studyNumber;
            }
        }

        public string VisitNumber
        {
            get
            {
                return m_visitNumber;
            }
        }

        [Audit("PriorEUIDResultValue")]
        public string PriorEUIDResultValue
        {
            get
            {
                return m_priorEUIDResultValue;
            }
            private set
            {
                if ((m_priorEUIDResultValue ?? "") != (value.Trim() ?? ""))
                {
                    m_priorEUIDResultValue = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string TAT
        {
            get
            {
                decimal val = -1;
                if (ResultStatus == resultStatusType.Final | ResultStatus == resultStatusType.Corrected)
                {
                    if (m_releaseDate > DateTime.Parse("1900-01-01"))
                    {
                        val = (decimal)Math.Round(m_releaseDate.Subtract(m_dateCreated).TotalHours, 1);
                    }
                    else
                    {
                        var systemTime = B2SystemTime.Fetch();
                        val = (decimal)Math.Round(systemTime.CurrentB2DateTime.Subtract(m_dateCreated).TotalHours, 1);
                    }
                }
                return val.ToString();
            }
        }

        [Audit("IsPresumptiveHold")]
        public bool IsPresumptiveHold
        {
            get
            {
                return m_isPresumptiveHold;
            }
            set
            {
                if (value != m_isPresumptiveHold)
                {
                    m_isPresumptiveHold = value;
                    FlagDirty();
                }
            }
        }

        [Audit("SpecimenCodes")]
        public string SpecimenCodes
        {
            get
            {
                return m_specimenCodes;
            }
            set
            {
                if ((m_specimenCodes ?? "") != (value.Trim() ?? ""))
                {
                    m_specimenCodes = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("DeltaHoldRule")]
        public string DeltaHoldRule
        {
            get
            {
                return m_deltaHoldRule;
            }
            set
            {
                if ((m_deltaHoldRule ?? "") != (value.Trim() ?? ""))
                {
                    m_deltaHoldRule = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("ParentTestCode")]
        public string ParentTestCode
        {
            get
            {
                return m_parentTestCode;
            }
            set
            {
                if ((m_parentTestCode ?? "") != (value.Trim() ?? ""))
                {
                    m_parentTestCode = value.Trim();
                    FlagDirty();
                }
            }
        }

        public bool ManualReReleaseFlag
        {
            get
            {
                return m_manualReReleaseFlag;
            }
            set
            {
                m_manualReReleaseFlag = value;
            }
        }

        #endregion

        #region Public Methods

        public void SetAccessioningFacility(string facilityCode)
        {

            if ((m_accessioningFacility ?? "") != (facilityCode ?? ""))
            {
                m_accessioningFacility = facilityCode;
                FlagDirty();
            }

        }

        public void SetSPMOrderTestId(long spmOrderTestId)
        {

            if (m_spmOrderTestId != spmOrderTestId)
            {
                m_spmOrderTestId = spmOrderTestId;
                FlagDirty();
            }

        }

        public void SetPerformingFacility(string facilityCode)
        {

            if (m_isPOC && !string.IsNullOrEmpty(m_performingFacility))
            {
                return;
            }
            if ((m_performingFacility ?? "") != (facilityCode ?? "") & !m_isCopiedAnalyte)
            {
                m_performingFacility = facilityCode;
                FlagDirty();
            }

        }

        public void OverridePerformingFacility(string facilityCode)
        {

            if ((m_performingFacility ?? "") != (facilityCode ?? ""))
            {
                m_performingFacility = facilityCode;
                FlagDirty();
            }

        }

        public bool HasAlertsThatBlockAutoRelease()
        {

            foreach (ReportAlert a in Alerts.List)
            {
                if (Conversions.ToBoolean(a.Alert.BlockAutoRelease))
                    return true;
            }
            return false;

        }

        public bool CanAutoRelease()
        {
            if (IsTestHold)
            {
                return false;
            }
            if (Configuration.LISSettings.GetBool("DisableAutoReleaseWithAlerts")) // 'check to see if AutoReleasing is disabled for results with alerts
            {
                // Check if parent is a panel and any of its analytes has alerts.
                if (ReferenceEquals(Parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    if (((ReportAnalytePanel)Parent).HasAlertsThatBlockAutoRelease())
                    {
                        return false;
                    }
                }
                // Check if this analyte has alerts
                else if (HasAlertsThatBlockAutoRelease() == true)
                {
                    return false;
                }
            }

            return true;

        }

        public void AddOrderingCode(string testCode)
        {

            if (testCode.Equals(""))
                return;

            if (!m_orderingAnalyteCodes.Contains(testCode))
            {
                m_orderingAnalyteCodes.Add(testCode);

                if (HasBeenReleased())
                    MarkAsReleased();

                FlagDirty();
            }

        }

        public void RemoveOrderingCodes()
        {
            m_orderingAnalyteCodes.Clear();
        }

        public void RemoveOrderingCode(string testCode, bool deleteTestWithNoOrderables = true)
        {

            if (testCode.Equals(""))
                return;

            m_orderingAnalyteCodes.Remove(testCode);
            FlagDirty();

            if (deleteTestWithNoOrderables)
            {
                if (m_orderingAnalyteCodes.Count == 0)
                {

                    GetParentReport().RemoveAnalyte(ID);

                    // determine if this analyte is part of any calcs, if so, remove the calcs also.
                    RemoveCalculations();
                }
            }

        }

        private void RemoveCalculations()
        {
            // nart here
            var parentReport = GetParentReport();
            ReportAnalytePanel panel = null;
            ReportAnalyte analyte = null;
            var analyteList = new List<string>();
            var panelList = new List<string>();

            // Changed this function to first load the Analytes and Panels in lists, then loop over them.
            // This was done because calculations could cause rules to fire, which would remove Analytes/Panels and cause an enumeration exception

            // '**********************************************************************
            foreach (ReportAnalyte an in parentReport.Analytes.List)
                analyteList.Add(an.Code);

            // First we check for Analytes that exist directly on the report.
            foreach (string s in analyteList)
            {
                analyte = parentReport.FindAnalyte(s, false);
                if (!(analyte == null))
                {
                    RemoveCalc(parentReport, analyte);
                }
            }
            // '**********************************************************************

            // '**********************************************************************
            foreach (ReportAnalytePanel p in parentReport.AnalytePanels.List)
                panelList.Add(p.PanelCode);

            // We need to check ALL the panels to see if the analyte exists.
            foreach (string s in panelList)
            {
                panel = parentReport.FindAnalytePanel(s);
                if (!(panel == null))
                {
                    foreach (ReportAnalyte an in panel.Analytes.List)
                        RemoveCalc(parentReport, an);
                }
            }
            // '**********************************************************************
        }

        private void RemoveCalc(Report parentReport, ReportAnalyte an)
        {
            // RemoveOrderingCode does the actual deleting when the Analytes' OrderingCodes reaches zero.
            if (an.Analyte.IsCalculation && an.Analyte.Calculation.Exists(Analyte.Code))
            {
                if (an.OrderingAnalyteCodes.Length > 0)
                {
                    foreach (string orderingCode in an.OrderingAnalyteCodes)
                        an.RemoveOrderingCode(orderingCode);
                }
                else
                {
                    // can't send in a blank so we send in the same test code as the analyte
                    an.RemoveOrderingCode(an.Code);
                }
            }

        }

        public bool ContainsOrderCode(string testCode)
        {

            return m_orderingAnalyteCodes.Contains(testCode);

        }

        /// <summary>
        /// Returns true if has been released at any point
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool HasBeenPreviouslyReleased()
        {

            // ResultStatus = Final or Corrected
            if ((int)ResultStatus > 1 && !PreviousResultValue.Equals("") && ReleaseDate > DateTime.Parse("1900-01-01") && !IsTestHold)
            {
                return true;
            }
            return false;

        }

        /// <summary>
        /// Returns true if a result has been released but has not been reset
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool HasBeenReleased()
        {

            if (IsTestHold)
            {
                return false;
            }
            else if (m_transmitStatus == transmitStatusType.Released || m_transmitStatus == transmitStatusType.StatusSentToVertex || m_transmitStatus == transmitStatusType.SentToReporting || m_transmitStatus == transmitStatusType.ReadByReporting || m_transmitStatus == transmitStatusType.Reported)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        // NA 2/27/2014 Give ability to reset the Original Result Status after all updates are done.
        private bool m_mustSetOrigResultStatus = false;

        public void SetOrigResultStatus()
        {
            // to be done only after all updates are done including children.
            if (m_mustSetOrigResultStatus)
            {
                m_orgResultStatus = m_resultStatus;
                m_mustSetOrigResultStatus = false;
            }
        }

        public bool IsPreviousStatusNotReleased()
        {

            // NA 2/28/2014 if the status is Final but hasn't been sent to reporting, also return false.
            // If Me.m_orgResultStatus = resultStatusType.Pending OrElse Me.m_orgResultStatus = resultStatusType.Preliminary Then
            if (m_orgResultStatus == resultStatusType.Pending || m_orgResultStatus == resultStatusType.Preliminary || ResultStatus == resultStatusType.Final && TransmitStatus == transmitStatusType.Released)
            {
                return false;
            }
            return true;

        }

        /// <summary>
        /// Determines if the OrderAnalytes current values results in a flagged status.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool IsFlagged()
        {
            // analyte is flagged if FlagValue contains a value, and the value is not 'N'
            return !FlagValue.IsNothingOrEmpty() && !FlagValue.CompareText("N");
        }

        public void ResetAnalyte(Analyte analyte)
        {

            if ((analyte.Code ?? "") == (m_analyte.Code ?? ""))
            {
                m_analyte = new RefAnalyte(this);
                m_analyte.Load((TestAnalyte)analyte);
            }
            else
            {
                throw new Exception(string.Concat("Unable to set new analyte that has a different AnalyteCode. Code must be :", analyte.Code));
            }

        }

        public void SetPreliminaryReleased(bool val)
        {
            _isPreliminaryReleased = val;
            if (!val)
            {
                m_releaseDate = DateTime.Parse("1900-01-01");
            }
        }

        public void ResetAnalyte(TestInfo testInfo)
        {

            if ((Analyte.Code ?? "") == (m_analyte.Code ?? ""))
            {
                m_analyte = new RefAnalyte(this);
                m_analyte.Load(testInfo);
            }
            else
            {
                throw new Exception(string.Concat("Unable to set new analyte that has a different AnalyteCode. Code must be :", Analyte.Code));
            }

        }

        public void ResetAnalyte()
        {

            var td = TestDetail.Fetch(Code, 0);
            m_analyte = new RefAnalyte(this);
            m_analyte.Load(td.Analyte);

            // If Analyte.Code = m_analyte.Code Then
            // m_analyte = New RefAnalyte(Me)
            // m_analyte.Load(testInfo)
            // Else
            // Throw New Exception(String.Concat("Unable to set new analyte that has a different AnalyteCode. Code must be :", Analyte.Code))
            // End If

        }

        /// <summary>
        /// Exception will be thrown if ResultStatus is not Released Or SentToReporting.
        /// </summary>
        /// <remarks></remarks>
        public void MarkAsReported()
        {
            if (m_transmitStatus == transmitStatusType.SentToReporting || m_transmitStatus == transmitStatusType.Released)
            {
                ChangeTransmitStatus(transmitStatusType.Reported);
                FlagDirty();
                // Else
                // If Me.Analyte.IsRequired Then
                // Throw New Exception(String.Concat("Unable to set status to Reported if status is ", m_transmitStatus))
                // End If
            }
        }

        private void ChangeTransmitStatus(transmitStatusType transmitStatus)
        {
            // Check For Re-release
            CheckIfReReleased(m_transmitStatus, transmitStatus);
            m_transmitStatus = transmitStatus;
        }

        private void CheckIfReReleased(transmitStatusType currentTransmitStatus, transmitStatusType newTransmitStatus)
        {
            if ((int)currentTransmitStatus >= 3 && (int)currentTransmitStatus != 10 || m_reReleaseStatus == (int)reReleaseStatusType.ResultChange)
            {
                if (newTransmitStatus == transmitStatusType.PendingRelease)
                {
                    m_reReleaseStatus = (int)reReleaseStatusType.ResultChange;
                    m_reReleased = true;
                }
                else if (newTransmitStatus == transmitStatusType.Released)
                {
                    m_reReleaseStatus = (int)reReleaseStatusType.ReReleased;
                    m_reReleased = true;
                }
            }
        }

        /// <summary>
        /// Exception will be thrown if ResultStatus is not ReadByReporting.
        /// </summary>
        /// <remarks></remarks>
        public void MarkAsStatusSentToVertex()
        {
            if (m_transmitStatus == transmitStatusType.ReadByReporting || m_transmitStatus == transmitStatusType.SentToReporting)
            {
                ChangeTransmitStatus(transmitStatusType.StatusSentToVertex);
                FlagDirty();
                // Else
                // If Me.Analyte.IsRequired Then
                // Throw New Exception(String.Concat("Unable to set status to StatusSentToVertex if status is ", m_transmitStatus))
                // End If
            }
        }

        /// <summary>
        /// Exception will be thrown if ResultStatus is not SentToReporting.
        /// </summary>
        /// <remarks></remarks>
        public void MarkAsReadByReporting()
        {
            if (m_transmitStatus == transmitStatusType.SentToReporting)
            {
                ChangeTransmitStatus(transmitStatusType.ReadByReporting);
                FlagDirty();
                // Else
                // If Me.Analyte.IsRequired Then
                // Throw New Exception(String.Concat("Unable to set status to ReadByReporting if status is ", m_transmitStatus))
                // End If
            }
        }

        /// <summary>
        /// Exception will be thrown if ResultStatus is not Released Or SentToReporting.
        /// </summary>
        /// <remarks></remarks>
        public void MarkAsSentToReporting()
        {
            if (m_transmitStatus == transmitStatusType.SentToReporting || m_transmitStatus == transmitStatusType.Released)
            {
                ChangeTransmitStatus(transmitStatusType.SentToReporting);
                FlagDirty();
                // Else
                // If Me.Analyte.IsRequired Then
                // Throw New Exception(String.Concat("Unable to set status to SentToReporting if status is ", m_transmitStatus))
                // End If
            }
        }

        private int m_ruleExecuteAttempts = 0;

        public void SetFinalOrCorrected()
        {

            bool resultChanged = !string.IsNullOrEmpty(ReleasedValue) && (ResultValue ?? "") != (ReleasedValue ?? "") || !string.IsNullOrEmpty(PriorReleasedValue) && (ResultValue ?? "") != (PriorReleasedValue ?? "");

            if (ResultStatus == resultStatusType.Pending && resultChanged)
            {
                ResultStatus = resultStatusType.Corrected;
            }
            else if (ResultStatus == resultStatusType.Pending && !resultChanged)
            {
                ResultStatus = resultStatusType.Final;
            }
            else if (ManualReReleaseFlag)
            {
                ResultStatus = (resultStatusType)ReleasedStatus;
                ManualReReleaseFlag = false;
            }
            else if (ResultStatus != resultStatusType.Corrected)
            {
                ResultStatus = resultStatusType.Final;
            }

        }

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public void MarkAsReleased()
        {

            var originalResultStatus = ResultStatus;
            var originalTransmitStatus = TransmitStatus;
            var originalPriorReleaseDate = PriorReleaseDate;
            var originalReleaseDate = ReleaseDate;
            string originalResultReleasedUser = ResultReleasedUser;
            bool flagDeltaHold = ResultStatus == resultStatusType.DeltaHold;

            if (string.IsNullOrEmpty(m_resultValue))
            {
                Log.Debug(" m_resultValue = ()");
                // AG 9/17/2008 If this is an optional analyte (DisplayByDefault = False) then we don't throw 
                // an error. We just don't mark it as released so it doesn't get sent out.
                if (Analyte.IsRequired)
                {
                    throw new Exception(string.Concat(Code, ": Unable to MarkAsReleased if result value is blank."));
                }
            }
            else
            {
                Log.Debug(" m_resultValue = " + m_resultValue);
                // Removed if condition - need to be able to rerelease
                // If m_transmitStatus = transmitStatusType.Released OrElse m_transmitStatus = transmitStatusType.PendingRelease Then
                // For Corrected analyte, we dont have to update release dates for all analytes
                if (m_resultStatus == resultStatusType.Corrected)
                {
                    m_priorReleaseDate = m_releaseDate;
                    m_releaseDate = DateTime.Now;
                }
                else
                {
                    m_resetReleaseDate = true;
                }

                if (m_resultStatus == resultStatusType.Corrected || m_ResultStatusBeforeRules == resultStatusType.Corrected)
                {
                    if ((int)m_transmitStatus < 2)
                    {
                        AddCorrectedTestList();
                    }
                }

                if (!IsTestHold)
                {
                    ChangeTransmitStatus(transmitStatusType.Released);
                }

                // VC:8/11/2017 Assing the ResultReleasedUser by settings
                if (ResultReleasedUser.IsNothingOrEmpty() || Configuration.LISSettings.GetBool("OverrideReleaseUser"))
                {
                    if (CurrentUser is not null)
                    {
                        ResultReleasedUser = CurrentUser.Name;
                    }
                    else
                    {
                        ResultReleasedUser = "unknown";
                    }
                }
                if (m_resultStatus == resultStatusType.Preliminary | m_resultStatus == resultStatusType.DeltaHold)
                {
                    SetReleasedResultStatus();
                }

                m_isCOCReviewed = false;
                m_COCApprover = "";
                _isPreliminaryReleased = false;

                FlagDirty();

                // NA: 4/24/16 Calculations always trigger rules.
                if (!GetParentReport().BypassRules || Analyte.IsCalculation)
                {
                    // 'RULES EVALUATION***********************************************
                    // Don't let rules fire more than 3 times for any analyte

                    if (m_ruleExecuteAttempts < 3)
                    {
                        Log.Debug(" INTO If m_ruleExecuteAttempts < 3 Then m_ruleExecuteAttempts=" + m_ruleExecuteAttempts);
                        m_ruleExecuteAttempts += 1;
                        Log.Debug("Calling Evaluate Rules with RuleSetRelease");
                        GetParentReport().EvaluateRulesEx(this, "", Configuration.RuleSetRelease);
                    }
                    else
                    {
                        Log.Debug("In MarkAsReleased.  Bypassing rule evaluation due to exceeding count. ExecAttempts: " + m_ruleExecuteAttempts.ToString());
                    }

                }

                // if rule puts analyte in delta hold and not releasing from the UI, revert the release.
                if (!m_releaseFromUI && !flagDeltaHold && ResultStatus == resultStatusType.DeltaHold)
                {
                    m_transmitStatus = originalTransmitStatus;
                    m_priorReleaseDate = originalPriorReleaseDate;
                    m_releaseDate = originalReleaseDate;
                    m_resultReleasedUser = originalResultReleasedUser;
                    Log.Debug("Release aborted due to DeltaHold");
                    return;
                }

                if (m_releaseFromUI && ResultStatus == resultStatusType.DeltaHold)
                {
                    SetReleasedResultStatus();
                }

                // '***************************************************************
                // 'Release tests that are calculated from this analyte, if result exist
                Log.Debug(" BEFORE Me.ReleaseCalculated()");

                ReleaseCalculated();
                Log.Debug(" AFTER Me.ReleaseCalculated()");
                // 'Update the Panel once all the analytes are marked accordingly
                if (ReferenceEquals(Parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    Log.Debug(" INTO  If Me.Parent.GetType() Is GetType(ReportAnalytePanel) Then");
                    ReportAnalytePanel panel = (ReportAnalytePanel)Parent;
                    foreach (ReportAnalyte a in panel.Analytes.List)
                    {
                        // AG 9-11-09 - Added isRequired, can update panel when all required are released.
                        if ((a.TransmitStatus != TransmitStatus || a.IsTestHold) && a.Analyte.IsRequired)
                            return;
                    }
                    // 'updateAnalytes set to false as to not trigger update again.
                    panel.SetTransmitStatus(m_transmitStatus, false);
                }
                Released?.Invoke(this);
            }

        }

        public void MarkAsPreliminaryReleased()
        {
            if (!IsTestHold)
            {
                ChangeTransmitStatus(transmitStatusType.Released);
                m_resultStatus = resultStatusType.Preliminary;
                _isPreliminaryReleased = true;
                m_isCOCReviewed = false;
                m_COCApprover = "";
                m_resetReleaseDate = true;

                FlagDirty();
            }
            // ''Update the Panel once all the analytes are marked accordingly
            // If Me.Parent.GetType() Is GetType(ReportAnalytePanel) Then
            // Dim panel As ReportAnalytePanel = CType(Me.Parent, ReportAnalytePanel)
            // ''updateAnalytes set to false as to not trigger update again.
            // panel.SetTransmitStatus(Me.m_transmitStatus, False)
            // End If
        }

        public void MarkAsHeldForRerun()
        {
            if (m_transmitStatus == transmitStatusType.PendingRelease)
            {
                ChangeTransmitStatus(transmitStatusType.HeldForRerun);

                m_isCOCReviewed = false;
                m_COCApprover = "";

                FlagDirty();
            }
            else
            {
                throw new Exception("Status must be PendingRelease in order to Hold.");
            }
        }

        /// <summary>
        /// Exception will be thrown if ResultStatus is not Released Or PendingRelased Or HeldForRerun.
        /// </summary>
        /// <remarks></remarks>
        public void MarkAsPendingReleased()
        {
            if (m_transmitStatus == transmitStatusType.Released || m_transmitStatus == transmitStatusType.PendingRelease || Conversions.ToBoolean(transmitStatusType.HeldForRerun))
            {
                ChangeTransmitStatus(transmitStatusType.PendingRelease);
                if (m_resultStatus != resultStatusType.DeltaHold)
                {
                    m_resultStatus = (resultStatusType)Conversions.ToInteger(Interaction.IIf(string.IsNullOrEmpty(m_resultValue), resultStatusType.Pending, resultStatusType.Preliminary));
                }
                m_isCOCReviewed = false;
                m_COCApprover = "";

                FlagDirty();
            }
            else
            {
                throw new Exception(string.Concat("Unable to set status to PendingReleased if status is ", m_transmitStatus));
            }
        }

        public void ReRelease(bool isCoc)
        {
            // For now we are only allowing this to be done for COC re-release.
            if (!isCoc)
                return;

            // requirements
            // Must already be approved.
            // If Status is Sent To Vertex, we will bypass.
            // Status must be Sent To Reporting (assumption is something happened and Reporting either didn’t get it or we didn’t get the response that they got it)

            if (IsCOCReviewed && (TransmitStatus == transmitStatusType.SentToReporting || TransmitStatus == transmitStatusType.Released || TransmitStatus == transmitStatusType.ReadByReporting))
            {
                m_resetReleaseDate = true;
                ChangeTransmitStatus(transmitStatusType.Released);
                _isPreliminaryReleased = false;
                FlagDirty();

            }

        }

        internal void SetReportAnalyteId(long analyteId)
        {
            m_id = analyteId;
        }

        public void SetTransmitStatus(transmitStatusType status)
        {
            // 'Need to put some business rules in here.
            if (status != m_transmitStatus && !IsTestHold)
            {
                if (status == transmitStatusType.Released)
                {
                    SetReleasedResultStatus();
                    _isPreliminaryReleased = false;
                    m_previousValue = ""; // 'Reset previous value.
                }
                ChangeTransmitStatus(status);
                FlagDirty();
            }
        }

        internal void SetTestHold()
        {
            if (!IsTestHold && m_resultStatus != resultStatusType.Final && m_resultStatus != resultStatusType.Corrected)
            {
                m_isTestHold = true;
                FlagDirty();
            }
        }

        internal void RevokeTestHold()
        {
            // 'Need to put some business rules in here.
            if (IsTestHold)
            {
                m_isTestHold = false;
                m_orgIsTestHold = m_isTestHold;
                FlagDirty();
            }
        }

        internal void ReleaseAfterTestHold()
        {
            if (m_resultStatus == resultStatusType.Final)
            {
                MarkAsReleased();
            }
        }

        internal void SetReleasedResultStatus()
        {

            // If Me.Code <> Me.OrderingAnalyteCode AndAlso Not Me.OrderingAnalyteCode.Equals("") Then
            // Dim r As Report = GetParentReport()
            // If r.CheckOrderableStatusReleased(Me.OrderingAnalyteCode, Me.Code) Then
            // r.SetOrderableStatusFinal(Me.OrderingAnalyteCode)
            // End If
            // Else
            if (Analyte.IsCalculation)
            {
                SetSampleRequestStatusForCalculation();
            }
            if (!m_resultValue.ToLower().Equals("to follow"))
            {
                if (!m_releaseFromUI && HasSampleRequest())
                {
                    return;
                }
                bool wasSpmTNP = SharedFunctions.IsNotPerformedResult(m_releasedValue) && m_spmStatus != SPMStatusValue.None;
                bool corrected = (m_releasedStatus == (int)resultStatusType.Final && (m_resultValue ?? "") != (m_releasedValue ?? "") || m_releasedStatus == (int)resultStatusType.Corrected) && !wasSpmTNP;

                if (corrected)
                {
                    m_resultStatus = resultStatusType.Corrected;
                }
                else
                {
                    m_resultStatus = resultStatusType.Final;
                }
                if (HasSampleRequest())
                {
                    m_sampleStatus = (int)SampleRequestType.None;
                }
            }
            // End If

        }

        private bool SetSampleRequestStatusForCalculation()
        {
            foreach (string calcTestCode in Analyte.Calculation.TestCodeList)
            {
                var parentAnalyte = GetParentReport().FindAnalyte(calcTestCode, true);
                if (parentAnalyte is not null)
                {
                    SampleStatus = parentAnalyte.SampleStatus;
                    break;
                }
            }

            return default;
        }

        public void SetReportingHold()
        {
            if (!ReportingHold)
            {
                m_isReportingHold = true;
                FlagDirty();
            }
        }

        public void LiftReportingHold()
        {
            if (ReportingHold)
            {
                m_isReportingHold = false;
                FlagDirty();
            }
        }

        public void ProcessTNPFromReporting(string testCode)
        {
            SetResultValue("TNP", false);
            MarkAsReleased();
        }

        public void ProcessTNP4k(string testCode)
        {
            SetResultValue("TNP", false);
            RemoveAllComments();
            if ((Code ?? "") == (testCode ?? ""))
            {
                AddCommentForTNPStatusFromReporting();
            }
            MarkAsReleased();
        }


        public void AddCommentForTNPStatusFromReporting()
        {
            var comments = Common.TestMaster.Comments.Fetch((CommentType)commentType.TestNotPerformed);
            foreach (Common.TestMaster.Comment comment in comments.List)
            {
                if (comment.AssignedID == "21526")
                {
                    AddComment(comment.Text);
                    break;
                }
            }
        }

        public void AddCommentForCommentUpdate(string commenttype)
        {
            if (HasBeenReleased() && IsPreviousStatusNotReleased())
            {
                AddComment(Conversions.ToString(FormatCommentForCommentUpdate(Configuration.LISSettings.GetString("AutomatedRevisedComment"), commenttype)));
            }
        }

        public object FormatCommentForCommentUpdate(string commenttext, string commenttype)
        {
            string Comment = "";
            if (commenttype == "Add")
            {
                Comment = string.Format(commenttext, "added to", Strings.Format(ReleaseDate, "MM/dd/yyyy"));
            }
            else if (commenttype == "Delete")
            {
                Comment = string.Format(commenttext, "deleted from", Strings.Format(ReleaseDate, "MM/dd/yyyy"));
            }
            return SharedFunctions.FormatCommentText(Comment, Configuration.LISSettings.GetInt("CommentUpdateMaxLength"));

        }

        public void UpdateAnalyteName(string value)
        {
            m_analyte.UpdateAnalyteName(value);
        }

        public void UpdateFlagValue(string value)
        {
            // m_analyte.UpdateFlagValue(value)
            if (m_analyte.UpdateFlagValue(value))
            {
                ReportAnalytePanel p = null;
                if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    p = (ReportAnalytePanel)m_parent;
                }

                if (HasBeenReleased())
                {
                    ResultStatus = resultStatusType.Corrected;
                    SetTransmitStatus(transmitStatusType.PendingRelease);
                    if (!(p == null))
                    {
                        p.SetTransmitStatus(transmitStatusType.PendingRelease, false);
                    }

                }
                if ((m_analyte.PreviousFlagValue ?? "") != (m_analyte.OriginalFlagValue ?? "") | (value ?? "") != (m_analyte.PreviousFlagValue ?? ""))
                {
                    m_analyte.FlagUpdated = true;
                }
                FlagChanged?.Invoke(this);
            }
        }

        public void MarkFlagAsDeleted()
        {
            m_analyte.IsFlagDeleted = true;

            ReportAnalytePanel p = null;
            if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
            {
                p = (ReportAnalytePanel)m_parent;
            }
            if (HasBeenReleased())
            {
                ResultStatus = resultStatusType.Corrected;
                SetTransmitStatus(transmitStatusType.PendingRelease);
                if (!(p == null))
                {
                    p.SetTransmitStatus(transmitStatusType.PendingRelease, false);
                }
            }
        }

        public void MarkFlagAsUndeleted()
        {
            m_analyte.IsFlagDeleted = false;

            ReportAnalytePanel p = null;
            if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
            {
                p = (ReportAnalytePanel)m_parent;
            }
            if (HasBeenReleased())
            {
                ResultStatus = resultStatusType.Corrected;
                SetTransmitStatus(transmitStatusType.PendingRelease);
                if (!(p == null))
                {
                    p.SetTransmitStatus(transmitStatusType.PendingRelease, false);
                }
            }
        }


        public void UpdateUnits(string value)
        {
            if (m_analyte.UpdateUnits(value))
            {
                ReportAnalytePanel p = null;
                if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    p = (ReportAnalytePanel)m_parent;
                }

                if (HasBeenReleased())
                {
                    ResultStatus = resultStatusType.Corrected;
                    SetTransmitStatus(transmitStatusType.PendingRelease);
                    if (!(p == null))
                    {
                        p.SetTransmitStatus(transmitStatusType.PendingRelease, false);
                    }

                }
                if ((m_analyte.PreviousUnitValue ?? "") != (m_analyte.OriginalUnitValue ?? "") | (value ?? "") != (m_analyte.PreviousUnitValue ?? ""))
                {
                    m_analyte.UnitsUpdated = true;
                }
                UnitsChanged?.Invoke(this);
            }
        }

        public void UpdateReferenceRange(string value)
        {
            if (m_analyte.UpdateReferenceRange(value))
            {
                ReportAnalytePanel p = null;
                if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    p = (ReportAnalytePanel)m_parent;
                }

                if (HasBeenReleased())
                {
                    ResultStatus = resultStatusType.Corrected;
                    SetTransmitStatus(transmitStatusType.PendingRelease);
                    if (!(p == null))
                    {
                        p.SetTransmitStatus(transmitStatusType.PendingRelease, false);
                    }
                }
                if ((m_analyte.PreviousTestRangeValue ?? "") != (m_analyte.OriginalTestRangeValue ?? "") | (value ?? "") != (m_analyte.PreviousTestRangeValue ?? ""))
                {
                    m_analyte.RangeUpdated = true;
                }
                RangeChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Returns the value of the Flag for the current result value based on the ReferenceAnalyte.
        /// If a value is passed in, will return the Flag value of that Result value.
        /// If a value is not passed in and the Analyte does not have a ResultValue list or a ResultFlagRange list, the Analyte's FlagValue is returned.
        /// If ReferenceLabId is not 0, return analyte.FlagValue.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string GetFlaggedValue(string value = "")
        {

            // If from a reference lab and we're not using internal ref ranges, do not try to calculate the Flag!
            if (m_analyte.ReferenceLabId != 0 && !m_analyte.AllowInternalRefRanges)
                return m_flagValue;

            if (string.IsNullOrEmpty(value) && m_analyte.ResultValues.Length == 0 && m_analyte.ResultFlagRanges.Length == 0)
                return m_flagValue;

            string compareVal = Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(value), ResultValue, value));

            // This first checks for flags in the Ranges array (based on gender,age,etc.)
            if (!string.IsNullOrEmpty(compareVal) && m_analyte.ResultFlagRanges.Length > 0)
            {

                string v = Conversions.ToString(GetFlagFromRanges(compareVal, m_performingFacility).FlagValue);
                if (!string.IsNullOrEmpty(v))
                {
                    return v;
                }

            }

            // If a value is not returned above, we check the default flag on the ResultValue, if exists.
            foreach (Result r in m_analyte.ResultValues)
            {
                if ((r.Value ?? "") == (compareVal ?? ""))
                    return r.FlagValue;
            }
            return "";

        }

        public FlagResult GetFlaggedValueAndComments(string value = "")
        {

            if (string.IsNullOrEmpty(value) && m_analyte.ResultValues.Length == 0 && m_analyte.ResultFlagRanges.Length == 0)
                return null;

            string compareVal = Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(value), ResultValue, value));

            // This first checks for flags in the Ranges array (based on gender,age,etc.)
            if (!string.IsNullOrEmpty(compareVal) && m_analyte.ResultFlagRanges.Length > 0)
            {

                var v = GetFlagFromRanges(compareVal, m_performingFacility);
                return v;

            }

            // If a value is not returned above, we check the default flag on the ResultValue, if exists.
            foreach (Result r in m_analyte.ResultValues)
            {
                if ((r.Value ?? "") == (compareVal ?? ""))
                    return new FlagResult(r.FlagValue, new DefaultCommentList(null));
            }
            return null;

        }

        public void SetFlaggedValue(string value)
        {

            if ((m_flagValue ?? "") != (value.Trim() ?? ""))
            {
                m_flagValue = value;
                FlagDirty();
            }

        }

        public bool CanEnableInstrument()
        {
            return RefAnalyte.IsInterfaced && !(ResultValue.ToUpper() == "TNP" || ResultValue.ToUpper() == "QNS" || ResultValue.ToUpper() == "TO FOLLOW" || string.IsNullOrEmpty(ResultValue));
        }

        public bool CanEnableInstrument(string result)
        {
            return RefAnalyte.IsInterfaced && !(result.ToUpper() == "TNP" || result.ToUpper() == "QNS" || result.ToUpper() == "TO FOLLOW" || string.IsNullOrEmpty(result));
        }

        public void SetUnsolicitedResult(UnsolicitedResult result)
        {

            var statuses = new OrderManager.Statuses();

            ResultValue = result.ResultValue;
            ResultDate = result.ResultDate;

            // Instrument info
            Instrument = result.Instrument;
            InstrumentId = result.InstrumentId;
            InstrumentAlt1 = result.InstrumentAlt1;
            InstrumentAlt2 = result.InstrumentAlt2;
            InstrumentAlt3 = result.InstrumentAlt3;

            // Specimen info
            SpecimenRackId = result.SpecimenRackId;
            SpecimenRackPosition = result.SpecimenRackPosition;
            SpecimenRackSequence = result.SpecimenRackSeqeunce;
            m_specimenAlt1 = result.SpecimenAlt1;
            m_specimenAlt2 = result.SpecimenAlt2;

            // 'For reference analyte
            if (Analyte.ReferenceLabId != 0)
            {
                UpdateFlagValue(result.FlagValue);
                UpdateUnits(result.Units);
                UpdateReferenceRange(result.RefRange);
                UpdateAnalyteName(result.AnalyteName);
            }
            // .ReferenceLabId

            if (!(result.Comments == null))
            {
                foreach (string s in result.Comments)
                    Comments.AddComment(s);
            }

            // Report Alerts
            // *******************************************************
            if (!(result.ResultAlertCodes == null))
            {
                // Fetch Error Flags from TestMaster
                var alerts = Common.Lab.Alerts.Fetch();
                Alert alert;
                if (!(alerts == null))
                {
                    if (!(result.ResultAlertCodes == null))
                    {
                        foreach (string code in result.ResultAlertCodes)
                        {
                            alert = (Alert)alerts.List.Find(code);
                            if (!(alert == null))
                            {
                                // results.Flags.Add(flag)
                                Alerts.Add(alert);
                            }
                            else
                            {
                                // statuses.List.Add(New OrderManager.Status(True, String.Format("Unsolcited Results: Unable to find Alert with code {0} for {1}.", code, .AnalyteCode), Nothing))
                            }
                        }
                    }
                    else
                    {
                        // statuses.List.Add(New OrderManager.Status(True, "Unsolcited Results: Unable to retreive Error Alert Codes list.", Nothing))
                    }
                }

            }

        }

        public ReportComment AddComment(string comment)
        {

            return AddComment(comment, "");

        }

        public ReportComment AddAutoComment(string comment)
        {

            m_IsCommmentsUpdated = true;
            return AddComment(comment, "", true);

        }

        internal ReportComment AddComment(string comment, string assignedId, bool isAuto = false)
        {

            m_IsCommmentsUpdated = true;
            var r = Comments.AddComment(comment, assignedId, isAuto, "", ExternalCommentType.Comment);
            object l = new ReportComment[] { r };
            OnUpdateResultValueCompleted(this, new ReportAnalyteArg(m_resultValue, m_resultValue, (transmitStatusType)m_resultStatus, (ReportComment[])l, m_IsCommmentsUpdated));

            return r;

        }

        internal void DeleteComment(ReportComment comment)
        {

            m_IsCommmentsUpdated = true;
            Comments.DeleteComment(comment);
            OnUpdateResultValueCompleted(this, new ReportAnalyteArg(m_resultValue, m_resultValue, (transmitStatusType)m_resultStatus, null, m_IsCommmentsUpdated));

        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToLong(row["ReportAnalyteID"]);
            m_resultValue = Conversions.ToString(row["ResultValue"]);
            m_resultStatus = (resultStatusType)Conversions.ToInteger(row["ResultStatus"]);
            m_orgResultStatus = m_resultStatus;
            m_orgResultValue = m_resultValue;
            m_transmitStatus = (transmitStatusType)Conversions.ToInteger(row["AnalyteTransmitStatus"]);
            m_resultDate = Conversions.ToDate(row["ResultDate"]);
            m_flagValue = Conversions.ToString(row["FlagValue"]);
            m_previousValue = Conversions.ToString(row["PreviousValue"]);
            m_currentFlagValue = Conversions.ToString(row["FlagValue"]);
            m_specimenRackID = Conversions.ToString(row["SpecimenRackId"]);
            m_specimenRackPos = Conversions.ToString(row["SpecimenRackPosition"]);
            m_specimenRackSeq = Conversions.ToString(row["SpecimenRackSequence"]);
            m_specimenAlt1 = Conversions.ToString(row["SpecimenAlt1"]);
            m_specimenAlt2 = Conversions.ToString(row["SpecimenAlt2"]);
            m_instrument = Conversions.ToString(row["Instrument"]);
            m_instrumentId = Conversions.ToString(row["InstrumentId"]);
            m_AnalyticalUnitName = Conversions.ToString(row["AnalyticalUnitName"]);
            m_AnalyticalUnitSerial = Conversions.ToString(row["AnalyticalUnitSerial"]);
            m_InstDivisionId = Conversions.ToInteger(row["InstrumentDivisionId"]);
            m_AnalyzerName = Conversions.ToString(row["AnalyzerName"]);
            m_AnalyzerModel = Conversions.ToString(row["AnalyzerModel"]);
            m_AnalyzerVendor = Conversions.ToString(row["AnalyzerVendor"]);
            m_instrumentAlt1 = Conversions.ToString(row["InstrumentAlt1"]);
            m_instrumentAlt2 = Conversions.ToString(row["InstrumentAlt2"]);
            m_instrumentAlt3 = Conversions.ToString(row["InstrumentAlt3"]);
            m_orderingAnalyteCodes.AddRange(row["OrderingAnalyteCode"].ToString().Split(','));
            m_criticalType = (criticalType)Conversions.ToInteger(row["AnalyteCriticalType"]); // aaa'
            m_releaseDate = Conversions.ToDate(row["AnalyteReleaseDate"]);
            m_isResultManual = Conversions.ToBoolean(row["IsResultManual"]);
            m_rreId = Conversions.ToInteger(row["RapidResultId"]);
            m_rreType = Conversions.ToString(row["RapidResultTemplateName"]);
            m_resultAnalyzedTechUser = Conversions.ToString(row["ResultAnalyzedTechUser"]);
            m_resultReleasedUser = Conversions.ToString(row["ResultReleasedUser"]);
            m_isDeleted = Conversions.ToBoolean(row["IsAnalyteDeleted"]);
            m_accessioningFacility = Conversions.ToString(row["AccessioningFacility"]);
            m_performingFacility = Conversions.ToString(row["PerformingFacility"]);
            m_spmOrderTestId = Conversions.ToLong(row["SPMOrderTestId"]);
            // nart here
            m_priorReleasedValue = Conversions.ToString(row["PriorReleaseValue"]);
            m_releasedValue = Conversions.ToString(row["ReleasedValue"]);
            m_releasedStatus = Conversions.ToInteger(row["ReleasedStatus"]);
            m_priorReleaseDate = Conversions.ToDate(row["PriorReleaseDate"]);
            m_studyNumber = Conversions.ToString(row["StudyNumber"]);
            m_visitNumber = Conversions.ToString(row["VisitNumber"]);
            m_priorEUIDResultValue = Conversions.ToString(row["PriorEUIDResultValue"]);
            m_isReportingHold = Conversions.ToBoolean(row["IsReportingHold"]);
            m_isCopiedAnalyte = Conversions.ToBoolean(row["IsCopiedAnalyte"]);
            m_isTestHold = Conversions.ToBoolean(row["IsTestHold"]);
            m_orgIsTestHold = m_isTestHold;
            m_isPOC = Conversions.ToBoolean(row["IsPOC"]);
            m_isDoubleEntry = Conversions.ToBoolean(row["IsDoubleEntry"]);
            if (row.Table.Columns.Contains("IsPreliminaryReleased"))
            {
                _isPreliminaryReleased = Conversions.ToBoolean(row["IsPreliminaryReleased"]);
            }
            // For historical results prior to going live with the CorrectedResults modifications.
            // we need to try and seed the releasedValue and releasedStatus.  Cannot do anything about the priorReleasedValue
            if (string.IsNullOrEmpty(m_releasedValue))
            {
                if (m_resultStatus == resultStatusType.Final)
                {
                    if (HasBeenPreviouslyReleased())
                    {
                        m_releasedValue = m_resultValue;
                        m_releasedStatus = (int)resultStatusType.Final;
                    }
                }
                else if (m_resultStatus == resultStatusType.Corrected)
                {
                    if (HasBeenReleased())
                    {
                        m_releasedValue = m_previousValue;
                        m_releasedStatus = (int)resultStatusType.Corrected;
                    }
                }
            }
            m_isCOCReviewed = Conversions.ToBoolean(row["IsCOCReviewed"]);
            m_COCApprover = Conversions.ToString(row["COCApprover"]);
            m_CocApproverFirstName = Conversions.ToString(row["CocApproverFirstName"]);
            m_CocApproverLastName = Conversions.ToString(row["CocApproverLastName"]);
            m_CocApproverMiddleName = Conversions.ToString(row["CocApproverMiddleName"]);
            m_deltaHoldRule = Conversions.ToString(row["DeltaHoldRule"]);
            m_parentTestCode = Conversions.ToString(row["ParentTestCode"]);
            m_CocApproverEmpNbr = Conversions.ToString(row["CocApproverEmpNbr"]);
            m_CocApprovedDate = Conversions.ToDate(row["CocApprovedDate"]);
            m_toFollowSent = Conversions.ToBoolean(row["ToFollowSent"]);
            m_codeTypeId = Conversions.ToInteger(row["CodeTypeId"]);
            _trimInequality = Conversions.ToBoolean(row["TrimInequality"]);
            m_correctedResultReason = Conversions.ToString(row["CorrectedResultReason"]).NormalizeToWindows();
            m_previousFlagValue = Conversions.ToString(row["PreviousFlagValue"]);
            m_RackWorksheetId = Conversions.ToInteger(row["RackWorksheetId"]);
            m_reReleaseStatus = Conversions.ToInteger(row["ReReleaseStatus"]);
            m_isPresumptiveHold = Conversions.ToBoolean(row["IsPresumptiveHold"]);
            m_dateCreated = Conversions.ToDate(row["DateCreated"]);
            m_sampleStatus = Conversions.ToInteger(row["SampleStatus"]);
            m_sampleStatusUpdateDate = Conversions.ToDate(row["SampleStatusUpdateDate"]);
            m_instrumentLoadTime = Conversions.ToDate(row["InstrumentLoadTime"]);
            m_specimenCodes = Conversions.ToString(row["SpecimenCodes"]);
            m_spmStatus = (SPMStatusValue)Conversions.ToInteger(row["AnalyteSPMStatus"]);
            m_RefLabPerformingFacilityId = Conversions.ToInteger(row["RefLabPerformingFacilityId"]);
            if (m_analyte == null)
                m_analyte = new RefAnalyte(this);
            m_analyte.Load(row);
            if (m_resultStatus == resultStatusType.DeltaHold && !string.IsNullOrEmpty(m_releasedValue) && (m_releasedValue ?? "") != (m_resultValue ?? ""))
            {
                AddCorrectedTestList();
            }

            FlagClean();

        }

        internal object GetUpdateRow(ref DataRow row)
        {

            if (base.IsDirty)
            {

                long parentId = 0L;
                var parentType = orderParentType.Unknown;

                if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    parentType = orderParentType.ReportAnalytePanel;
                    parentId = ((ReportAnalytePanel)m_parent).Id;
                }
                else if (ReferenceEquals(m_parent.GetType(), typeof(Report)))
                {
                    parentType = orderParentType.Report;
                    parentId = ((Report)m_parent).ID;
                }

                var da = new DataWrapper(Configuration.ConnectionString);
                var paramList = new List<DbParameter>();

                var holdReleaseDate = m_releaseDate;

                // 'The date is reset in the update statement so that dates are consistent and do not come from the client application.
                // If m_resetReleaseDate Then m_releaseDate = DateTime.Now()

                // nart here
                bool bolUpdateReleaseData = false;
                if ((int)m_transmitStatus < 2 || (int)m_transmitStatus > 6)
                {
                    // reset the COC information for this analyte
                    m_isCOCReviewed = false;
                    m_CocApprovedDate = DateTime.Parse("1900-01-01");
                    m_COCApprover = "";
                }
                // With paramList
                row["ReportAnalyteId"] = m_id;
                row["ParentType"] = parentType;
                row["ParentId"] = parentId;
                row["ResultValue"] = m_resultValue;
                row["ResultStatus"] = m_resultStatus;
                row["TransmitStatus"] = m_transmitStatus;
                row["AnalyteCode"] = m_analyte.Code;
                if (m_resultDate > DateTime.Parse("1900-01-01"))
                    row["ResultDate"] = m_resultDate;
                row["FlagValue"] = m_flagValue;
                row["PreviousValue"] = m_previousValue;
                row["Instrument"] = m_instrument;
                row["InstrumentId"] = m_instrumentId;
                row["InstrumentAlt1"] = m_instrumentAlt1;
                row["InstrumentAlt2"] = m_instrumentAlt2;
                row["InstrumentAlt3"] = m_instrumentAlt3;
                row["SpecimenRackId"] = m_specimenRackID;
                row["SpecimenRackPosition"] = m_specimenRackPos;
                row["SpecimenRackSequence"] = m_specimenRackSeq;
                row["SpecimenAlt1"] = m_specimenAlt1;
                row["SpecimenAlt2"] = m_specimenAlt2;
                row["OrderingAnalyteCode"] = string.Join(",", m_orderingAnalyteCodes.ToArray());
                row["CriticalType"] = m_criticalType;
                row["IsResultManual"] = m_isResultManual;
                row["ResultAnalyzedTechUser"] = m_resultAnalyzedTechUser;
                row["ResultReleasedUser"] = m_resultReleasedUser;
                row["IsDeleted"] = m_isDeleted;
                row["IsPreliminaryReleased"] = _isPreliminaryReleased;
                row["PerformingFacility"] = m_performingFacility;
                row["AccessioningFacility"] = m_accessioningFacility;
                if (m_releaseDate > DateTime.Parse("1900-01-01"))
                    row["ReleaseDate"] = m_releaseDate;
                if (Analyte.IsReportable & TransmitStatus == transmitStatusType.Released)
                {
                    // add releasedValue parameter.
                    row["ReleasedValue"] = m_resultValue;
                    row["PriorReleaseValue"] = m_releasedValue;
                    row["ReleasedStatus"] = m_resultStatus;
                    row["PriorReleaseDate"] = holdReleaseDate;
                    bolUpdateReleaseData = true;
                }
                else
                {
                    // otherwise, keep the current data
                    row["ReleasedValue"] = m_releasedValue;
                    row["PriorReleaseValue"] = m_priorReleasedValue;
                    row["ReleasedStatus"] = m_releasedStatus;
                    row["PriorReleaseDate"] = m_priorReleaseDate;
                }
                row["IsCOCReviewed"] = m_isCOCReviewed;
                row["COCApprover"] = m_COCApprover;
                if (m_isCOCReviewed)
                {
                    // send in the current approved date if coc is approved.  otherwise, leave the parm out.  
                    row["COCApprovedDate"] = m_CocApprovedDate;
                }
                row["ResetReleaseDate"] = m_resetReleaseDate;
                if (!(CurrentUser == null))
                    row["UserName"] = CurrentUser.Name;
                row["ToFollowSent"] = m_toFollowSent;
                row["CodeTypeId"] = m_codeTypeId;
                row["PriorEUIDResultValue"] = m_priorEUIDResultValue;
                row["SPMOrderTestId"] = m_spmOrderTestId;
                row["IsReportingHold"] = m_isReportingHold;
                row["IsCopiedAnalyte"] = m_isCopiedAnalyte;
                row["IsTestHold"] = m_isTestHold;
                row["CorrectedResultReason"] = m_correctedResultReason;
                row["IsPresumptiveHold"] = m_isPresumptiveHold;
                row["SampleStatus"] = m_sampleStatus;
                row["SampleStatusUpdateDate"] = m_sampleStatusUpdateDate;
                row["InstrumentLoadTime"] = m_instrumentLoadTime;
                row["SpecimenCodes"] = m_specimenCodes;
                row["ParentTestCode"] = m_parentTestCode;
                row["DeltaHoldRule"] = m_deltaHoldRule;
                row["SPMStatus"] = m_spmStatus;

                // End With

                // Dim params() As DbParameter = paramList.ToArray()

                // m_id = da.ExecuteNonQuery("lis_ReportAnalyte_Save", params).Item("@ReportAnalyteId").Value
            }

            return default;

        }

        internal void Update(ref DataRow row)
        {

            if (base.IsDirty)
            {
                var holdReleaseDate = m_releaseDate;
                bool bolUpdateReleaseData = false;

                if (Analyte.IsReportable & TransmitStatus == transmitStatusType.Released)
                {
                    bolUpdateReleaseData = true;
                }

                // If the value changes, we save it in report history
                if ((m_resultValue.Trim() ?? "") != (m_orgResultValue.Trim() ?? ""))
                {
                    var rh = new ReportAnalyteHistory(this);
                    rh.Update();
                    m_orgResultValue = m_resultValue;
                }

                if (m_isTestHold != m_orgIsTestHold)
                {
                    m_orgIsTestHold = m_isTestHold;
                }

                // NA 2/27/2014 Cannot set this value at this point.  have to wait until all updates are done.
                m_mustSetOrigResultStatus = true;
                // Me.m_orgResultStatus = Me.m_resultStatus

                // Call this before FlagClean is called so that the ObjectLookupId can be set correctly from the RefAnalyte
                if (row is not null)
                {
                    UpdateReferenceAnalyte(ref row);
                }

                // set the priorReleasedValue, ReleasedStatus, ReleasedValue if reportable and released --> bolUpdateReleaseData = true.
                if (bolUpdateReleaseData)
                {
                    m_priorReleasedValue = m_releasedValue;
                    m_releasedValue = m_resultValue;
                    m_releasedStatus = (int)m_resultStatus;
                    m_priorReleaseDate = holdReleaseDate;
                }

                // NA: Get the list of AuditItem in this object after issuing the Clean so all properties are correctly populated.
                m_auditItemList.AddRange(GetAuditItemsAndClean(true));
            }
            // Me.FlagClean(True)
            // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            // SharedFunctions.DebugWrite("ReportAnalyte:UpdateReferenceAnalyte: After invoke UpdateCombined", Me.ToString())
            // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            else if (row is not null)
            {
                UpdateReferenceAnalyte(ref row);

            }
            int elapsedTime;
            // Dim timer As Stopwatch = New Stopwatch()
            // Timer.Start()
            m_alerts.Update();
            // m_auditItemList.AddRange(m_alerts.GetAuditItems())
            // Timer.Stop()
            // elapsedTime = timer.ElapsedMilliseconds
            // Log.Error(String.Format("Total time taken for alerts update for analyte code {0} is {1} ms.", Me.Code, elapsedTime))

            // Timer.Reset()
            // timer.Start()
            Comments.Update();
            m_auditItemList.AddRange(Comments.GetAuditItems());
            // timer.Stop()
            // elapsedTime = timer.ElapsedMilliseconds
            // Log.Error(String.Format("Total time taken for comments update for analyte code {0} is {1} ms.", Me.Code, elapsedTime))
            // NA 7/12/15 - must save the attachments

            // Timer.Reset()
            // Timer.Start()
            AnalyteAttachments.Update();
            // timer.Stop()
            // elapsedTime = timer.ElapsedMilliseconds
            // Log.Error(String.Format("Total time taken for alerts update for analyte code {0} is {1} ms.", Me.Code, elapsedTime))
            // not worrying about audit items yet.

            // NA 2/27/2014
            // We wait until now to reset the original result status.
            // Only do this for single analytes -- not panel components.
            if (ReferenceEquals(Parent.GetType(), typeof(Report)))
            {
                SetOrigResultStatus();
            }
        }


        internal void Update()
        {

            if (base.IsDirty)
            {

                long parentId = 0L;
                var parentType = orderParentType.Unknown;

                if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    parentType = orderParentType.ReportAnalytePanel;
                    parentId = ((ReportAnalytePanel)m_parent).Id;
                }
                else if (ReferenceEquals(m_parent.GetType(), typeof(Report)))
                {
                    parentType = orderParentType.Report;
                    parentId = ((Report)m_parent).ID;
                }

                var da = new DataWrapper(Configuration.ConnectionString);
                var paramList = new List<DbParameter>();

                var holdReleaseDate = m_releaseDate;

                // 'The date is reset in the update statement so that dates are consistent and do not come from the client application.
                // If m_resetReleaseDate Then m_releaseDate = DateTime.Now()

                // nart here
                bool bolUpdateReleaseData = false;

                if ((int)m_transmitStatus < 2 || (int)m_transmitStatus > 6)
                {
                    // reset the COC information for this analyte
                    m_isCOCReviewed = false;
                    m_CocApprovedDate = DateTime.Parse("1900-01-01");
                    m_COCApprover = "";
                }

                paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, m_id, ParameterDirection.InputOutput));
                paramList.Add((DbParameter)da.CreateParameter("@ParentType", DbType.Int32, parentType));
                paramList.Add((DbParameter)da.CreateParameter("@ParentId", DbType.Int64, parentId));
                paramList.Add((DbParameter)da.CreateParameter("@ResultValue", DbType.String, m_resultValue));
                paramList.Add((DbParameter)da.CreateParameter("@ResultStatus", DbType.Int32, m_resultStatus));
                paramList.Add((DbParameter)da.CreateParameter("@TransmitStatus", DbType.Int32, m_transmitStatus));
                paramList.Add((DbParameter)da.CreateParameter("@AnalyteCode", DbType.String, m_analyte.Code));
                if (m_resultDate > DateTime.Parse("1900-01-01"))
                    paramList.Add((DbParameter)da.CreateParameter("@ResultDate", DbType.DateTime, m_resultDate));
                paramList.Add((DbParameter)da.CreateParameter("@FlagValue", DbType.String, m_flagValue));
                paramList.Add((DbParameter)da.CreateParameter("@PreviousValue", DbType.String, m_previousValue));
                paramList.Add((DbParameter)da.CreateParameter("@Instrument", DbType.String, m_instrument));
                paramList.Add((DbParameter)da.CreateParameter("@InstrumentId", DbType.String, m_instrumentId));
                paramList.Add((DbParameter)da.CreateParameter("@InstrumentAlt1", DbType.String, m_instrumentAlt1));
                paramList.Add((DbParameter)da.CreateParameter("@InstrumentAlt2", DbType.String, m_instrumentAlt2));
                paramList.Add((DbParameter)da.CreateParameter("@InstrumentAlt3", DbType.String, m_instrumentAlt3));

                paramList.Add((DbParameter)da.CreateParameter("@SpecimenRackId", DbType.String, m_specimenRackID));
                paramList.Add((DbParameter)da.CreateParameter("@SpecimenRackPosition", DbType.String, m_specimenRackPos));
                paramList.Add((DbParameter)da.CreateParameter("@SpecimenRackSequence", DbType.String, m_specimenRackSeq));
                paramList.Add((DbParameter)da.CreateParameter("@SpecimenAlt1", DbType.String, m_specimenAlt1));
                paramList.Add((DbParameter)da.CreateParameter("@SpecimenAlt2", DbType.String, m_specimenAlt2));

                paramList.Add((DbParameter)da.CreateParameter("@OrderingAnalyteCode", DbType.String, string.Join(",", m_orderingAnalyteCodes.ToArray())));
                paramList.Add((DbParameter)da.CreateParameter("@CriticalType", DbType.Int32, m_criticalType)); // aaa'
                paramList.Add((DbParameter)da.CreateParameter("@IsResultManual", DbType.Boolean, m_isResultManual));
                paramList.Add((DbParameter)da.CreateParameter("@ResultAnalyzedTechUser", DbType.String, m_resultAnalyzedTechUser));
                paramList.Add((DbParameter)da.CreateParameter("@ResultReleasedUser", DbType.String, m_resultReleasedUser));
                paramList.Add((DbParameter)da.CreateParameter("@IsDeleted", DbType.Boolean, m_isDeleted));

                paramList.Add((DbParameter)da.CreateParameter("@IsPreliminaryReleased", DbType.Boolean, _isPreliminaryReleased));

                paramList.Add((DbParameter)da.CreateParameter("@PerformingFacility", DbType.String, m_performingFacility));
                paramList.Add((DbParameter)da.CreateParameter("@AccessioningFacility", DbType.String, m_accessioningFacility));

                if (m_releaseDate > DateTime.Parse("1900-01-01"))
                    paramList.Add((DbParameter)da.CreateParameter("@ReleaseDate", DbType.DateTime, m_releaseDate));

                if (Analyte.IsReportable & TransmitStatus == transmitStatusType.Released)
                {
                    // add releasedValue parameter.
                    paramList.Add((DbParameter)da.CreateParameter("@ReleasedValue", DbType.String, m_resultValue));
                    paramList.Add((DbParameter)da.CreateParameter("@PriorReleaseValue", DbType.String, m_releasedValue));
                    paramList.Add((DbParameter)da.CreateParameter("@ReleasedStatus", DbType.Int32, m_resultStatus));
                    paramList.Add((DbParameter)da.CreateParameter("@PriorReleaseDate", DbType.DateTime, holdReleaseDate));
                    bolUpdateReleaseData = true;
                }
                else
                {
                    // otherwise, keep the current data
                    paramList.Add((DbParameter)da.CreateParameter("@ReleasedValue", DbType.String, m_releasedValue));
                    paramList.Add((DbParameter)da.CreateParameter("@PriorReleaseValue", DbType.String, m_priorReleasedValue));
                    paramList.Add((DbParameter)da.CreateParameter("@ReleasedStatus", DbType.Int32, m_releasedStatus));
                    paramList.Add((DbParameter)da.CreateParameter("@PriorReleaseDate", DbType.DateTime, m_priorReleaseDate));
                }

                paramList.Add((DbParameter)da.CreateParameter("@IsCOCReviewed", DbType.Boolean, m_isCOCReviewed));
                paramList.Add((DbParameter)da.CreateParameter("@COCApprover", DbType.String, m_COCApprover));

                if (m_isCOCReviewed)
                {
                    // send in the current approved date if coc is approved.  otherwise, leave the parm out.  
                    paramList.Add((DbParameter)da.CreateParameter("@COCApprovedDate", DbType.DateTime, m_CocApprovedDate));
                }
                paramList.Add((DbParameter)da.CreateParameter("@ResetReleaseDate", DbType.Boolean, m_resetReleaseDate));


                if (!(CurrentUser == null))
                    paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

                paramList.Add((DbParameter)da.CreateParameter("@ToFollowSent", DbType.Boolean, m_toFollowSent));
                paramList.Add((DbParameter)da.CreateParameter("@CodeTypeId", DbType.Int32, m_codeTypeId));
                paramList.Add((DbParameter)da.CreateParameter("@PriorEUIDResultValue", DbType.String, m_priorEUIDResultValue));
                paramList.Add((DbParameter)da.CreateParameter("@SPMOrderTestId", DbType.Int64, m_spmOrderTestId));
                paramList.Add((DbParameter)da.CreateParameter("@IsReportingHold", DbType.Boolean, m_isReportingHold));
                paramList.Add((DbParameter)da.CreateParameter("@IsCopiedAnalyte", DbType.Boolean, m_isCopiedAnalyte));
                paramList.Add((DbParameter)da.CreateParameter("@IsTestHold", DbType.Boolean, m_isTestHold));
                paramList.Add((DbParameter)da.CreateParameter("@CorrectedResultReason", DbType.String, m_correctedResultReason));
                if ((m_currentFlagValue ?? "") != (FlagValue ?? ""))
                {
                    m_previousFlagValue = m_currentFlagValue;
                }
                paramList.Add((DbParameter)da.CreateParameter("@PreviousFlagValue", DbType.String, m_previousFlagValue));
                paramList.Add((DbParameter)da.CreateParameter("@IsPresumptiveHold", DbType.Boolean, m_isPresumptiveHold));
                paramList.Add((DbParameter)da.CreateParameter("@SampleStatus", DbType.Int32, m_sampleStatus));
                if (m_sampleStatusUpdateDate > DateTime.Parse("1900-01-01"))
                    paramList.Add((DbParameter)da.CreateParameter("@SampleStatusUpdateDate", DbType.DateTime, m_sampleStatusUpdateDate));
                if (m_instrumentLoadTime > DateTime.Parse("1900-01-01"))
                    paramList.Add((DbParameter)da.CreateParameter("@InstrumentLoadTime", DbType.DateTime, m_instrumentLoadTime));
                paramList.Add((DbParameter)da.CreateParameter("@SpecimenCodes", DbType.String, m_specimenCodes));
                paramList.Add((DbParameter)da.CreateParameter("@ParentTestCode", DbType.String, m_parentTestCode));
                paramList.Add((DbParameter)da.CreateParameter("@DeltaHoldRule", DbType.String, m_deltaHoldRule));
                paramList.Add((DbParameter)da.CreateParameter("@SPMStatus", DbType.Int32, m_spmStatus));
                paramList.Add((DbParameter)da.CreateParameter("@RefLabPerformingFacilityId", DbType.Int32, m_RefLabPerformingFacilityId));

                DbParameter[] @params = paramList.ToArray();

                m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_ReportAnalyte_Save", @params)["@ReportAnalyteId"].Value);

                // If the value changes, we save it in report history
                if ((m_resultValue.Trim() ?? "") != (m_orgResultValue.Trim() ?? ""))
                {
                    var rh = new ReportAnalyteHistory(this);
                    rh.Update();
                    m_orgResultValue = m_resultValue;
                }

                if (m_isTestHold != m_orgIsTestHold)
                {
                    m_orgIsTestHold = m_isTestHold;
                }

                // NA 2/27/2014 Cannot set this value at this point.  have to wait until all updates are done.
                m_mustSetOrigResultStatus = true;
                UpdateReferenceAnalyte();

                // set the priorReleasedValue, ReleasedStatus, ReleasedValue if reportable and released --> bolUpdateReleaseData = true.
                if (bolUpdateReleaseData)
                {
                    m_priorReleasedValue = m_releasedValue;
                    m_releasedValue = m_resultValue;
                    m_releasedStatus = (int)m_resultStatus;
                    m_priorReleaseDate = holdReleaseDate;
                }

                // NA: Get the list of AuditItem in this object after issuing the Clean so all properties are correctly populated.
                m_auditItemList.AddRange(GetAuditItemsAndClean(true));
            }
            // Me.FlagClean(True)
            // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            // SharedFunctions.DebugWrite("ReportAnalyte:UpdateReferenceAnalyte: After invoke UpdateCombined", Me.ToString())
            // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            else
            {

                UpdateReferenceAnalyte();
                // Me.m_orgResultStatus = Me.m_resultStatus

                // Call this before FlagClean is called so that the ObjectLookupId can be set correctly from the RefAnalyte

            }

            m_alerts.Update();

            if (m_auditDoubleEntry)
            {
                AuditManager.LogCustomObjectAction(this, "Double Entry: yes");
            }

            m_auditItemList.AddRange(m_alerts.GetAuditItems());

            Comments.Update();
            m_auditItemList.AddRange(Comments.GetAuditItems());

            // NA 7/12/15 - must save the attachments
            AnalyteAttachments.Update();
            // not worrying about audit items yet.

            // NA 2/27/2014
            // We wait until now to reset the original result status.
            // Only do this for single analytes -- not panel components.
            if (ReferenceEquals(Parent.GetType(), typeof(Report)))
            {
                SetOrigResultStatus();
            }
            if (m_reReleased)
            {
                SaveReReleaseReportAnalyte(m_id, m_reReleaseStatus);
            }


        }

        private void SaveReReleaseReportAnalyte(long reportAnalyteId, int reReleaseStatus)
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, reportAnalyteId));
            paramList.Add((DbParameter)da.CreateParameter("@CreatedBy", DbType.String, Interaction.IIf(CurrentUser == null, "", "")));

            paramList.Add((DbParameter)da.CreateParameter("@ReReleaseStatus", DbType.Int32, reReleaseStatus));

            DbParameter[] @params = paramList.ToArray();

            da.ExecuteNonQuery("lis_ReReleaseReportAnalyte_Save", @params);

        }

        public override void FlagUnDeleted()
        {

            m_isDeleted = false;
            base.FlagUnDeleted();
        }

        private void UpdateReferenceAnalyte(ref DataRow row)
        {

            // 'This is using the old update method where the refanalyte and mapping are updated in the same proc
            if (Configuration.AppSettings.GetBool("Bioreference.LIS:AddTestFromTestMaster"))
            {

                if (m_analyte.IsDirty)
                {
                    m_analyte.UpdateCombined(ref row);

                    // '''''''''''''' Modified to allow log '''''''''''
                    // ReferenceAnalyteId = m_analyte.Id = objLookupId
                    Log.Debug("ReportAnalyte:UpdateReferenceAnalyte: After invoke UpdateCombined");
                    var props = m_analyte.GetAudits();
                    if (props.Count > 0)
                    {
                        WriteAudit((int)m_analyte.Id, props);
                        m_analyte.ResetOriginalValues();
                    }
                }
                else    // ''''''''''''''''''''''''''''''''''''''''''''''''
                {
                    row = null;
                }
            }

            else
            {

                bool mapRef = m_analyte.IsNew; // If RefAnalyte is new (new ReportAnalyte) or it is updated below, we need to remap

                // The reference analyte CAN be updated, ie. sendout send update analyte values.
                if (m_analyte.Id == 0L || m_analyte.IsDirty)
                {
                    m_analyte.UpdateCore();
                    mapRef = true;
                }

                if (mapRef) // For a new ReportAnalyte or update of RefAnalyte, need to map RefAnalyte
                {

                    m_analyte.Update();

                }

            }

        }

        private void UpdateReferenceAnalyte()
        {

            // 'This is using the old update method where the refanalyte and mapping are updated in the same proc
            if (Configuration.AppSettings.GetBool("Bioreference.LIS:AddTestFromTestMaster"))
            {

                if (m_analyte.IsDirty)
                {
                    m_analyte.UpdateCombined();

                    // '''''''''''''' Modified to allow log '''''''''''
                    // ReferenceAnalyteId = m_analyte.Id = objLookupId
                    Log.Debug("ReportAnalyte:UpdateReferenceAnalyte: After invoke UpdateCombined");
                    var props = m_analyte.GetAudits();
                    if (props.Count > 0)
                    {
                        WriteAudit((int)m_analyte.Id, props);
                        m_analyte.ResetOriginalValues();
                    }
                    // ''''''''''''''''''''''''''''''''''''''''''''''''

                }
            }

            else
            {

                bool mapRef = m_analyte.IsNew; // If RefAnalyte is new (new ReportAnalyte) or it is updated below, we need to remap

                // The reference analyte CAN be updated, ie. sendout send update analyte values.
                if (m_analyte.Id == 0L || m_analyte.IsDirty)
                {
                    m_analyte.UpdateCore();
                    mapRef = true;
                }

                if (mapRef) // For a new ReportAnalyte or update of RefAnalyte, need to map RefAnalyte
                {

                    m_analyte.Update();

                }

            }

        }


        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, m_id);

            da.ExecuteNonQuery("lis_ReportAnalyte_Delete", @param);

            // 'Need to call update, comments can be added on deleted Analytes:)
            Comments.Update();

            FlagDeleted();
            FlagClean();

        }

        private static string LookupPriorResult(long euid, string analyteCode, int lookBack)
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new DbParameter[4];
            string resultValue = "";
            try
            {
                @params[0] = (DbParameter)da.CreateParameter("@analyteCode", DbType.String, analyteCode);
                @params[1] = (DbParameter)da.CreateParameter("@euid", DbType.Int64, euid);
                @params[2] = (DbParameter)da.CreateParameter("@lookBack", DbType.Int32, lookBack);
                @params[3] = (DbParameter)da.CreateParameter("@resultValue", DbType.String, resultValue, ParameterDirection.Output);
                @params[3].Size = 50;
                da.ExecuteNonQuery("lis_PriorResult", @params);
                resultValue = Conversions.ToString(@params[3].Value);
            }
            catch (Exception ex)
            {
                resultValue = "";
            }
            return resultValue;

        }

        #endregion

        #region Private Functions

        public Report GetParentReport()
        {

            Report parentReport = null;
            if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
            {
                parentReport = ((ReportAnalytePanel)m_parent).Parent;
            }
            else if (ReferenceEquals(m_parent.GetType(), typeof(Report)))
            {
                parentReport = (Report)m_parent;
            }

            return parentReport;

        }

        public void ReleaseAnyCalcsForAnalyte()
        {
            ReleaseCalculated();
        }
        private void ReleaseCalculated()
        {
            Log.Debug(" INTO ReleaseCalculated()");
            var parentReport = GetParentReport();
            ReportAnalyte ra;
            bool inComplete;

            if (!(parentReport == null) && parentReport.HasCalcAnalytes)
            {
                int cnt;
                // For Each a As ReportAnalyte In parentReport.Analytes.List
                cnt = parentReport.Analytes.List.Count - 1;
                for (int i = 0, loopTo = cnt; i <= loopTo; i++)
                {
                    var a = parentReport.Analytes.List[i];
                    inComplete = false;
                    if (Conversions.ToBoolean(a.Analyte.IsCalculation && ((RefAnalyte)a.Analyte).AutoRelease && a.Analyte.Calculation.Exists(Analyte.Code)))
                    {
                        foreach (string testCode in a.Analyte.Calculation.TestCodeList)
                        {
                            Log.Debug("For Each testCode As String In a.Analyte.Calculation.TestCodeList testCode=" + testCode);
                            ra = parentReport.FindAnalyte(testCode, true);
                            if (ra == null || ra.HasBeenReleased() == false)
                            {
                                inComplete = true;
                                Log.Debug("Exit For");
                                break;
                            }
                        }
                        Log.Debug("inComplete=" + inComplete.ToString());
                        if (inComplete)
                            continue;

                        if (!string.IsNullOrEmpty(a.ResultValue))
                        {
                            a.MarkAsReleased();
                        }

                    }
                }

                // We need to check ALL the panels to see if the analyte exists.
                cnt = parentReport.AnalytePanels.List.Count - 1;
                // For Each panel As ReportAnalytePanel In parentReport.AnalytePanels.List
                for (int i = 0, loopTo1 = cnt; i <= loopTo1; i++)
                {
                    var panel = parentReport.AnalytePanels.List[i];
                    if (!(panel == null))
                    {
                        foreach (ReportAnalyte a in panel.Analytes.List)
                        {
                            inComplete = false;
                            if (Conversions.ToBoolean(a.Analyte.IsCalculation && ((RefAnalyte)a.Analyte).AutoRelease && a.Analyte.Calculation.Exists(Analyte.Code)))
                            {

                                foreach (string testCode in a.Analyte.Calculation.TestCodeList)
                                {
                                    ra = parentReport.FindAnalyte(testCode, true);
                                    if (ra == null || ra.HasBeenReleased() == false)
                                    {
                                        inComplete = true;
                                        break;
                                    }
                                }
                                if (inComplete)
                                    continue;

                                if (!string.IsNullOrEmpty(a.ResultValue))
                                    a.MarkAsReleased();

                            }
                        }
                    }
                }
            }

        }

        internal void PerformCalculation(Report parentReport, bool isResultManual = false)
        {

            ReportAnalytePanel panel = null;
            ReportAnalyte analyte = null;
            var analyteList = new List<string>();
            var panelList = new List<string>();

            // Changed this function to first load the Analytes and Panels in lists, then loop over them.
            // This was done because calculations could cause rules to fire, which would remove Analytes/Panels and cause an enumeration exception

            // '**********************************************************************
            foreach (ReportAnalyte an in parentReport.Analytes.List)
                analyteList.Add(an.Code);

            // First we check for Analytes that exist directly on the report.
            foreach (string s in analyteList)
            {
                analyte = parentReport.FindAnalyte(s, false);
                if (!(analyte == null))
                {
                    PerformCalculation(parentReport, analyte, isResultManual);
                }
            }
            // '**********************************************************************

            // '**********************************************************************
            foreach (ReportAnalytePanel p in parentReport.AnalytePanels.List)
                panelList.Add(p.PanelCode);

            // We need to check ALL the panels to see if the analyte exists.
            foreach (string s in panelList)
            {
                panel = parentReport.FindAnalytePanel(s);
                if (!(panel == null))
                {
                    foreach (ReportAnalyte an in panel.Analytes.List)
                        PerformCalculation(parentReport, an, isResultManual);
                }
            }
            // '**********************************************************************

        }

        internal bool IsForceCalcToTNPOrQNS(Report parentReport, ReportAnalyte analyte, ref string returnResultValue)
        {
            int iTNPCount = 0;
            int iQNSCount = 0;

            bool bolForceToTNPorQNS = false;
            ReportAnalyte ra = null;
            string calc = analyte.Analyte.Calculation.Expression;
            foreach (string testCode in analyte.Analyte.Calculation.TestCodeList)
            {
                ra = analyte.FindAnalyte(testCode, true);    // search for testCode within the analyte's own panel (if any), then in the report.
                string nullReplace = string.Concat("IsNull(", testCode, ")"); // 'Is null function for tests that don't exist
                if (!(ra == null))
                {
                    // If SharedFunctions.IsNotPerformedResult(ra.ResultValue) Then
                    if (ra.ResultValue == "TNP")
                    {
                        iTNPCount += 1;
                        bolForceToTNPorQNS = true;
                    }
                    else if (ra.ResultValue == "QNS")
                    {
                        iQNSCount += 1;
                        bolForceToTNPorQNS = true;
                    }
                }
            }

            if (iTNPCount > 0)
            {
                returnResultValue = "TNP";
            }
            else if (iQNSCount > 0)
            {
                returnResultValue = "QNS";
            }
            else
            {
                returnResultValue = "";
            }

            return bolForceToTNPorQNS;
        }
        public void PerformCalculationApply(Report parentReport, bool isResultManual = false)
        {
            PerformCalculation(parentReport, isResultManual);
        }
        internal void PerformCalculation(Report parentReport, ReportAnalyte analyte, bool isResultManual = false)
        {
            bool err, inComplete;
            string defaultValue = "";

            err = false;
            inComplete = false;
            // Find all analytes that are calculations and this Analyte is part of its Expression.
            // 'If the result was manually entered, do not attempt to overwrite it. --REMOVED
            // 'a.IsResultManual = False _

            // If this is a CALC and one of the components has value of TNP or QNA, then the Calc value should be TNP
            // Dont bother with the rest of the Calculation
            // Dim isTNPorQNSCalc As Boolean = False
            string calValue = "";

            // If analyte.Analyte.IsCalculation Then
            // If IsForceCalcToTNPOrQNS(parentReport, analyte, calValue) Then
            // isTNPorQNSCalc = True
            // analyte.SetResultValue(calValue, isResultManual)
            // End If
            // End If

            if (analyte.Analyte.IsCalculation && analyte.Analyte.Calculation.Exists(Analyte.Code))
            {
                // If (Not isTNPorQNSCalc) AndAlso analyte.Analyte.IsCalculation AndAlso analyte.Analyte.Calculation.Exists(Me.Analyte.Code) Then
                if (IsAnalyteResettingToPending)
                {
                    analyte.ResetAnalyteToPending();
                }
                if (IsForceCalcToTNPOrQNS(parentReport, analyte, ref calValue))
                {
                    // isTNPorQNSCalc = True
                    analyte.SetResultValue(calValue, isResultManual);
                }
                else
                {
                    // If the result is blank, then blank out the calculation since it is not valid.
                    if (string.IsNullOrEmpty(ResultValue))
                    {
                        // a.ResultValue = ""
                        analyte.SetResultValue("", isResultManual);
                    }

                    ReportAnalyte ra = null;
                    double calcValue = 0d;
                    string calc = analyte.Analyte.Calculation.Expression;
                    foreach (string testCode in analyte.Analyte.Calculation.TestCodeList)
                    {
                        ra = analyte.FindAnalyte(testCode, true);    // search for component analyte within this analyte's own panel (if any), then in the report.
                        string nullReplace = string.Concat("IsNull(", testCode, ")"); // 'Is null function for tests that don't exist
                        if (ra == null) // 'OrElse ra.ResultValue = "" Then
                        {
                            if (calc.Contains(nullReplace))
                            {
                                calc = calc.Replace(nullReplace, "1=1");
                                calc = calc.Replace(string.Concat("[", testCode, "]"), "''");
                            }
                            else
                            {
                                inComplete = true;
                                break;
                            }
                        }
                        else if (string.IsNullOrEmpty(ra.ResultValue))
                        {
                            inComplete = true;
                            break;
                        }
                        else if (double.TryParse(ra.CalculatibleResultValue, out calcValue) == false)
                        {
                            calc = calc.Replace(nullReplace, "1=2");
                            calc = calc.Replace(string.Concat("[", ra.Analyte.Code, "]"), string.Concat("'", ra.CalculatibleResultValue, "'"));
                        }
                        // err = True
                        // Exit For
                        else
                        {
                            calc = calc.Replace(nullReplace, "1=2");
                            calc = calc.Replace(string.Concat("[", ra.Analyte.Code, "]"), ra.CalculatibleResultValue);
                        }
                    }
                    if (inComplete && !err)
                        return; // Continue For

                    // 'Set default value for errors*********
                    var defaultMatch = Regex.Match(analyte.Analyte.Calculation.Expression, @"\?(?:(?!:).)*$");
                    if (defaultMatch.Success)
                    {
                        defaultValue = defaultMatch.Value.Replace("?", "");
                    }
                    // '*************************************

                    if (err)
                    {
                        // a.ResultValue = defaultValue
                        analyte.SetResultValue(defaultValue, isResultManual);
                    }
                    // 'Continue For
                    else
                    {

                        calc = ReplaceCalcVariables(calc, parentReport, defaultValue);


                        var e = new Utilities.Evaluator();
                        // a.ResultValue = e.evaluate(calc)
                        // a.SetResultValue(e.evaluate(calc), isResultManual)

                        string rslt;
                        try
                        {
                            rslt = e.EvaluateAdvanced(calc, defaultValue);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Invalid Expresion for Test Code: " + analyte.Code + " Expresion:" + calc);
                        }

                        if (!rslt.Equals("-1.#IND"))
                        {
                            analyte.SetResultValue(rslt, isResultManual);
                        }
                        else
                        {
                            analyte.SetResultValue(defaultValue, isResultManual);
                        }
                    }
                }
            }

        }

        private string ReplaceCalcVariables(string calc, Report parentReport, string defaultValue)
        {

            // '******************************************************************************
            // 'Remove default from the calculation
            // 'ex. [x]+[y]?DefaultValue, don't want to replace ([x]+[y] ? a : b)
            calc = Regex.Replace(calc, @"\?(?:(?!:).)*$", "");

            calc = calc.Replace("[Age]", parentReport.Age("Y").ToString());
            calc = calc.Replace("[Gender]", string.Concat("'", parentReport.Gender.ToString(), "'"));

            calc = Regex.Replace(calc, @"\[Age\{(\w)\}\]", parentReport.Age("$1").ToString());

            // 'Added for backwards compatibility
            double dv;
            if (double.TryParse(defaultValue, out dv))
            {
                calc = calc.Replace("[Default]", dv.ToString());
            }
            else
            {
                calc = calc.Replace("[Default]", Conversions.ToString(Interaction.IIf(Regex.Match(defaultValue, "^'.*'$").Success, defaultValue, string.Concat("'", defaultValue, "'"))));
            }
            // '**********************************

            // [Gender{Male:30;Female:20;Unknown:10}]
            var matches = Regex.Matches(calc, @"\[Gender\{.*\}\]");
            if (matches.Count > 0)
            {
                var hash = new Hashtable();
                foreach (Match match in matches)
                {
                    string s = Regex.Replace(Regex.Match(match.Value, @"\{.*\}").Value, @"\{|\}", "");
                    string[] k = s.Split(';');
                    for (int i = 0, loopTo = k.Length - 1; i <= loopTo; i++)
                    {
                        string[] l = k[i].Split(':');
                        if (l.Length == 2)
                        {
                            hash.Add(l[0], l[1]);
                        }
                    }
                }
                if (hash.ContainsKey(parentReport.Gender.ToString()))
                {
                    calc = Regex.Replace(calc, @"\[Gender\{.*\}\]", hash[parentReport.Gender.ToString()].ToString());
                }
            }

            return calc;
            // '******************************************************************************

        }

        // Find related analytes by first looking within the panel this analyte is part of (if any), then from the parent report.

        public ReportAnalyte FindAnalyte(string analyteCode, bool searchPanels, int referenceLabId = 0)
        {

            return FindAnalyte(analyteCode, searchPanels, referenceLabId, transmitStatusType.NotSet);

        }

        public ReportAnalyte FindAnalyte(string analyteCode, bool searchPanels, int referenceLabId, transmitStatusType transmitStatus)
        {

            ReportAnalyte analyte = null;
            ReportAnalytePanel panel = null;
            Report rpt = null;

            if (m_parent is ReportAnalytePanel & searchPanels)
            {
                panel = (ReportAnalytePanel)m_parent;
                analyte = panel.Analytes.FindFirst(analyteCode, referenceLabId, transmitStatus);
                if (analyte is not null)
                    return analyte;
                rpt = panel.Parent;
            }
            else
            {
                rpt = (Report)m_parent;
            }
            analyte = rpt.Analytes.FindFirst(analyteCode, referenceLabId, transmitStatus);
            if (analyte is not null)
                return analyte;
            if (searchPanels)
            {
                foreach (ReportAnalytePanel currentPanel in rpt.AnalytePanels.List)
                {
                    panel = currentPanel;
                    analyte = panel.Analytes.FindFirst(analyteCode, referenceLabId, transmitStatus);
                    if (analyte is not null)
                        return analyte;
                }
            }
            return null;

        }

        public bool HasSampleRequest()
        {
            bool returnval = false;
            if (SampleStatus == (int)SampleRequestType.SampleRequest)
            {
                returnval = true;
            }
            return returnval;
        }

        public string GetOrgResultValue()
        {
            return m_orgResultValue;
        }

        public void ResetAnalyteToPending()
        {
            SampleStatus = (int)SampleRequestType.None;
            IsAnalyteResettingToPending = true;
            SetResultValue("", false);
            ResultStatus = resultStatusType.Pending;
            SetTransmitStatus(transmitStatusType.None);
            SetPreliminaryReleased(false);
            RemoveAllComments();
            AuditManager.LogCustomObjectAction(this, "SPM specimen update.");
        }

        public bool IsResultStatusChanged()
        {
            if (m_orgResultStatus != m_resultStatus)
            {
                return true;
            }
            return false;
        }

        public bool OrgResultStatus()
        {
            return Conversions.ToBoolean(m_orgResultStatus);
        }

        /// <summary>
        /// 
        /// Returns ReferenceRange Text based on  default on reference Analyte. If blank, based on complex ranges
        /// If ReferenceLabId is not 0, returns Range on analyte.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>

        private FlagResult GetFlagFromRanges(string value, string divisionCode)
        {

            Report r;
            if (ReferenceEquals(m_parent.GetType(), typeof(Report)))
            {
                r = (Report)m_parent;
            }
            else
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }

            string dob = r.DOB;
            var gender = r.Gender;
            var fromDate = r.CalcDobFromDate; // r.m_dateServiced
            int ageNumber = r.AgeNbr;
            string ageType = r.AgeType;

            string flag = "";

            return SharedFunctions.GetFlagFromRanges(m_analyte.ResultFlagRanges, value, gender, dob, fromDate, ageNumber, ageType, divisionCode);

        }

        public void RemoveAllComments()
        {
            foreach (ReportComment c in Comments.List)
            {
                Comments.List.Remove(c);
                m_IsCommmentsUpdated = true;
                RemoveAllComments();
                break;
            }
        }


        private void RemoveAutoAddedComments()
        {

            foreach (ReportComment c in Comments.List)
            {
                if (c.IsAutoAdded)
                {
                    // 'If is a demographic autoComment code, then don't remove.
                    if (!c.ExternalCommentCode.Equals(Configuration.LISSettings.GetString("DemographicUpdateAutoCommentCode")))
                    {
                        Comments.List.Remove(c);
                        m_IsCommmentsUpdated = true;
                        RemoveAutoAddedComments();
                        break;
                    }
                }
            }

        }

        // '''''''''''Modified to allow log'''''''''''''''''
        public void WriteAudit(int objLookupId, List<PropertyGroup> propertyGroups)
        {

            try
            {
                // this will hold all the data.
                AuditItems objAudit;
                // start the transaction here.
                var options = new TransactionOptions();
                options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
                options.Timeout = new TimeSpan(0, 2, 0);
                using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
                {

                    objAudit = AuditItems.CreateAuditItems();

                    // this is for one row in the file
                    // Dim objAuditItem As AuditItem = objAudit.AddAuditItem(DateTime.Now, Me.IdentifierId, "Bioreference.LIS.ReportAnalyte", auditActionType.ObjectUpdate, Me.ParentIdentifierId, Bioreference.Data.Audit.parentIdentifierType.AccessionNbr_ServiceDate, objLookupId, 0, Me.CurrentUser.Name, "")
                    var objAuditItem = objAudit.AddAuditItem(Conversions.ToString(IdentifierId), "Bioreference.LIS.ReportAnalyte", auditActionType.ObjectUpdate, Conversions.ToString(ParentIdentifierId), parentIdentifierType.AccessionNbr_ServiceDate, objLookupId, 0);
                    objAuditItem.UseGivenDate = false;
                    if (propertyGroups.Count > 0)
                    {
                        foreach (PropertyGroup pGroup in propertyGroups)
                        {
                            Log.Debug($"ReportAnalyte:WriteAudit: Writing property Name {pGroup.Name} Current '{pGroup.CurrentValue}' Previous '{pGroup.PreviousValue}'");
                            objAuditItem.CreateProperty(pGroup.Name, pGroup.PropertyType, pGroup.PreviousValue, pGroup.CurrentValue, pGroup.IsPriority);
                        }
                    }

                    objAudit.Save();
                    scope.Complete();
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
        // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        #endregion

        #region Compare

        public override string CompareObjectDiplayName
        {
            get
            {
                return AnalyteName;
            }
        }

        public override List<CompareResult> CompareObject(object obj)
        {

            ReportAnalyte analyte = (ReportAnalyte)obj;

            var list = new List<CompareResult>();

            // 'Get base compare values
            list.AddRange(base.CompareObject(obj));

            list.AddRange(CompareResult.CompareObjects(Comments.List, analyte.Comments.List, AnalyteName));
            list.AddRange(CompareResult.CompareObjects(Alerts.List, analyte.Alerts.List, AnalyteName));

            return list;

        }

        #endregion

        public ComplexValue[] GetComplexDropDownList()
        {

            // Dim r As Report
            // If m_parent.GetType() Is GetType(Report) Then
            // r = CType(m_parent, Report)
            // Else
            // r = CType(m_parent, ReportAnalytePanel).Parent
            // End If

            // Dim dob As String = r.DOB
            // Dim gender As Bioreference.Common.Gender = r.Gender
            // Dim flag As String = ""

            var f = GetMatchingResultFlag(); // SharedFunctions.GetMatchingResultFlag(m_analyte.ResultFlagRanges, gender, dob, r.AgeNbr, r.AgeType, r.CalcDobFromDate, m_performingFacility)

            if (!(f == null))
            {
                return f.Values.DropdownList;
            }
            else
            {
                // Return empty array
                var c = new List<ComplexValue>();
                return c.ToArray();
            }

        }

        public string GetInstrumentName()
        {
            return InstrumentId;
        }

        public string GetReferenceRange()
        {

            if (!m_analyte.AllowInternalRefRanges && (m_analyte.ReferenceLabId != 0 || !string.IsNullOrEmpty(m_analyte.ReferenceRange)))
            // '7/25/2011 - Now this always overrides complex - user can override.
            {
                return m_analyte.ReferenceRange;
            }

            var f = GetMatchingResultFlag();

            if (!(f == null))
            {
                return f.ReferenceRangeText;
                // 'Else
                // 'COMMENT OUT - THIS IS DONE IN THE GETMATCHINGRESULTFLAG METHOD
                // 'If a match isn't found we return the Default ReferenceRange
                // For Each rf As ComplexResultFlag In m_analyte.ResultFlagRanges
                // If rf.IsDefault Then Return rf.ReferenceRangeText
                // Next
            }

            // If Default not found, return Reference Range on analyte.
            return m_analyte.ReferenceRange;

        }

        public string GetReferenceRange(Gender gender, string dob, int ageNbr, string ageType)
        {
            var f = GetMatchinResultFlag(gender, dob, ageNbr, ageType);

            if (!(f == null))
            {
                return f.ReferenceRangeText;
                // 'Else
                // 'COMMENT OUT - THIS IS DONE IN THE GETMATCHINGRESULTFLAG METHOD
                // 'If a match isn't found we return the Default ReferenceRange
                // For Each rf As ComplexResultFlag In m_analyte.ResultFlagRanges
                // If rf.IsDefault Then Return rf.ReferenceRangeText
                // Next
            }

            // If Default not found, return Reference Range on analyte.
            return m_analyte.ReferenceRange;
        }

        private ComplexResultFlag GetMatchinResultFlag(Gender gender, string dob, int ageNbr, string ageType)
        {
            Report r;
            if (ReferenceEquals(m_parent.GetType(), typeof(Report)))
            {
                r = (Report)m_parent;
            }
            else
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }

            return SharedFunctions.GetMatchingResultFlag(m_analyte.ResultFlagRanges, gender, dob, ageNbr, ageType, r.CalcDobFromDate, m_performingFacility);
        }

        public FlagResult GetFlagFromRanges(Gender gender, string dob, int ageNbr, string ageType)
        {

            Report r;
            if (ReferenceEquals(m_parent.GetType(), typeof(Report)))
            {
                r = (Report)m_parent;
            }
            else
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }

            return SharedFunctions.GetFlagFromRanges(Analyte.ResultFlagRanges, ResultValue, gender, dob, default, ageNbr, ageType, PerformingFaciltiy);

        }

        public ComplexResultFlag GetMatchingResultFlag()
        {

            Report r;
            if (ReferenceEquals(m_parent.GetType(), typeof(Report)))
            {
                r = (Report)m_parent;
            }
            else
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }

            return SharedFunctions.GetMatchingResultFlag(m_analyte.ResultFlagRanges, r.Gender, r.DOB, r.AgeNbr, r.AgeType, r.CalcDobFromDate, m_performingFacility);

        }

        public FlagResult GetFlagFromRanges()
        {

            Report r;
            if (ReferenceEquals(m_parent.GetType(), typeof(Report)))
            {
                r = (Report)m_parent;
            }
            else
            {
                r = ((ReportAnalytePanel)m_parent).Parent;
            }

            return SharedFunctions.GetFlagFromRanges(Analyte.ResultFlagRanges, ResultValue, r.Gender, r.DOB, default, r.AgeNbr, r.AgeType, PerformingFacility);

        }


        internal void SetReferenceLab(int referenceLabId, string referenceLabTestCode)
        {

            // m_analyte.Code = String.Format("{0}-{1}", referenceLabId, referenceLabTestCode)
            m_analyte.ReferenceLabId = referenceLabId;
            m_analyte.ReferenceLabeAnalyteCode = referenceLabTestCode;
            FlagDirty();

        }

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_Attachments.IsDirty || m_comments.IsDirty || m_analyte.IsDirty;
            }
        }

        public void SetResultStatus(string resultStatus)
        {

            ResultStatus = (resultStatusType)Conversions.ToInteger(Enum.Parse(typeof(resultStatusType), resultStatus));

        }

        public bool AddAlert(object alertCode)
        {

            if (!AlertExists(alertCode))
            {
                var alerts = Common.Lab.Alerts.Fetch();
                var alert = alerts.List.Find((string)alertCode);
                if (!(alert == null))
                    Alerts.Add((Alert)alert);
            }

            return default;

        }

        public bool AlertExists(object alertCode)
        {

            foreach (ReportAlert ra in m_alerts.List)
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(ra.Alert.Code, alertCode, false)))
                    return true;
            }

            return false;

        }

        public bool AlertNotExists(object alertCode)
        {

            foreach (ReportAlert ra in m_alerts.List)
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(ra.Alert.Code, alertCode, false)))
                    return false;
            }

            return true;

        }

        public void ResetResultToPrevious(string result)
        {

            if ((m_resultValue ?? "") == (result ?? ""))
            {
                m_resultValue = m_previousValue;   // TODO: Future Set here the property instead of the member. It looks as nobady is using it
                                                   // Not marked flagged dirty - assume already dirty from result value update.
            }

        }

        /// <summary>
        /// Use by rules engine so that ordering analyte code is used.
        /// </summary>
        /// <param name="testCode"></param>
        /// <remarks></remarks>
        public void AddTestToParentReport(string testCode)
        {

            try
            {
                Log.DebugFormat("AddTestToParentReport {0} Start", testCode);
                var r = GetParentReport();
                // 'need to add for each orderable
                foreach (string s in OrderingAnalyteCodes)
                {
                    // r.AddByTestCodeAndOrderedCode(testCode, s, Me.AccessioningFacility, Me.PerformingFacility) 
                    Log.DebugFormat("AddTestToParentReport testCode='{0}', s='{1}', accessioningFacility='{2}', performingFacility='{3}'", testCode, s, AccessioningFacility, PerformingFacility);
                    if (r.AddByTestCodeAndOrderedCode(testCode, s, AccessioningFacility, PerformingFacility, spmStatus: SPMStatusValue.None))
                    {
                        foreach (ReportAnalyte ana in r.Analytes.List)
                        {
                            Log.DebugFormat("ana.Code='{0}'", ana.Code);
                            if (ana.Code.Equals(testCode))
                            {
                                ana.ToFollowSent = true;     // Set toFollowSent = True because this method should called only from analyte added by rules. Then we want bypass to follo processing
                            }
                        }
                    }
                }
                Report.CheckCalcPerfomingLocation(r);
                Log.DebugFormat("AddTestToParentReport Finish");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                throw;
            }
        }

        public void DeltaHoldCheck(string deltaHoldRule)
        {

            var r = GetParentReport();
            var o = r.ReferencedOrder;

            string priorResultValue;
            int rangeFactor = 1;
            bool pct = false;
            int lookback = 0;
            decimal range = 0m;
            var newValue = default(decimal);
            var priorValue = default(decimal);

            Log.DebugFormat("DeltaHoldCheck called on code '{0}', rule '{1}', Result='{2}', ResultStatus '{3}', PriorEUIDResultValue '{4}', IsTestHold '{5}'", m_analyte.Code, deltaHoldRule, ResultValue, ResultStatus, PriorEUIDResultValue, IsTestHold);

            if (o is not null && o.EUID == 0L)
                return;

            deltaHoldRule = deltaHoldRule.Trim().ToUpper();
            DeltaHoldRule = deltaHoldRule;
            if (deltaHoldRule == "N/A")
                deltaHoldRule = "";

            if (!string.IsNullOrEmpty(deltaHoldRule))
            {
                string[] tokens = deltaHoldRule.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                string rToken = "";
                string lbToken = "";
                if (tokens.Length > 0)
                    rToken = tokens[0].Trim();
                if (tokens.Length > 1)
                    lbToken = tokens[1].ToUpper().Trim();
                pct = rToken.EndsWith("%");
                if (pct)
                    rToken = rToken.Substring(0, rToken.Length - 1);
                if (!decimal.TryParse(rToken, out range))
                    return;
                if (lbToken.Length > 0 && lbToken != "N/A")
                {
                    string flag = lbToken.Substring(lbToken.Length - 1);
                    if ("DWY".Contains(flag))
                    {
                        switch (flag ?? "")
                        {
                            case "D":
                                {
                                    rangeFactor = 1;
                                    break;
                                }
                            case "W":
                                {
                                    rangeFactor = 7;
                                    break;
                                }
                            case "Y":
                                {
                                    rangeFactor = 365;
                                    break;
                                }
                        }
                        lbToken = lbToken.Substring(0, lbToken.Length - 1);
                    }
                    if (!int.TryParse(lbToken, out lookback))
                        return;
                    lookback *= rangeFactor;
                }
            }

            if (string.IsNullOrEmpty(PriorEUIDResultValue) | PriorEUIDResultValue == "N/A")
            {
                priorResultValue = LookupPriorResult(o.EUID, m_analyte.Code, lookback);
                if (string.IsNullOrEmpty(priorResultValue))
                    priorResultValue = "N/A";
                if ((priorResultValue ?? "") != (PriorEUIDResultValue ?? ""))
                {
                    PriorEUIDResultValue = priorResultValue;
                }
                else
                {
                    return;
                }
            }

            if (string.IsNullOrEmpty(deltaHoldRule))
            {
                // string value comparison
                if ((ResultValue ?? "") != (PriorEUIDResultValue ?? "") & PriorEUIDResultValue != "N/A")
                {
                    IsRuleResultStatusSet = true;
                    SetCorrectedStatusFlagForRules(ResultStatus);
                    ResultStatus = resultStatusType.DeltaHold;
                }
                else if (ResultStatus == resultStatusType.DeltaHold & (ResultValue ?? "") == (PriorReleasedValue ?? ""))
                {
                    if (string.IsNullOrEmpty(PriorReleasedValue))
                    {
                        ResultStatus = resultStatusType.Final;
                    }
                    else if (!IsTestHold)
                    {
                        ResultStatus = resultStatusType.Corrected;
                    }
                }
                Log.DebugFormat("DeltaHoldCheck ResultValue '{0}', PriorEUIDResultValue '{1}', PriorReleasedValue '{2}', ResultStatus '{3}'", ResultValue, PriorEUIDResultValue, PriorReleasedValue, ResultStatus);
            }
            else if (PriorEUIDResultValue != "N/A")
            {
                // range comparison
                SharedFunctions.ValidateAndClearInequality(ResultValue, ref newValue);
                SharedFunctions.ValidateAndClearInequality(PriorEUIDResultValue, ref priorValue);
                decimal delta = Math.Abs(newValue - priorValue);
                if (!pct || pct & priorValue != 0m)
                {
                    if (pct)
                        delta = delta / priorValue * 100m;
                    if (delta >= range & PriorEUIDResultValue != "N/A")
                    {
                        IsRuleResultStatusSet = true;
                        SetCorrectedStatusFlagForRules(ResultStatus);
                        ResultStatus = resultStatusType.DeltaHold;
                    }
                    else if (ResultStatus == resultStatusType.DeltaHold & (ResultValue ?? "") == (PriorReleasedValue ?? ""))
                    {
                        if (string.IsNullOrEmpty(PriorReleasedValue))
                        {
                            ResultStatus = resultStatusType.Final;
                        }
                        else if (!IsTestHold)
                        {
                            ResultStatus = resultStatusType.Corrected;
                        }
                    }
                }
                Log.DebugFormat("DeltaHoldCheck ResultValue '{0}' PriorEUIDResultValue '{1}', PriorReleasedValue '{2}', Range '{3}', Delta '{4}', Pct '{5}', Lookback '{6}'", ResultValue, PriorEUIDResultValue, PriorReleasedValue, range, delta, pct, lookback);
            }
            else
            {
                Log.DebugFormat("DeltaHoldCheck Bypassed due to lack of prior result value.");
            }

        }

        private string FlagCheck(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "N" : value.Trim();
        }

        public void DeltaHoldFlagCheck(string inputValue)
        {
            Report currentReport = this.GetParentReport();
            if (currentReport != null)
            {
                string flagValue = FlagCheck(FlagValue);
                Report priorReport = (Report)Report.Fetch(currentReport.ReferencedOrder.EUID, Code);
                if (priorReport != null)
                {
                    ReportAnalyte priorAnalyte = priorReport.FindAnalyte(this.Code, true);
                    if (priorAnalyte != null)
                    {
                        string priorFlagValue = FlagCheck(priorAnalyte.FlagValue);
                        if (priorFlagValue!=flagValue)
                        {
                            SetCorrectedStatusFlagForRules(ResultStatus);
                            this.ResultStatus = resultStatusType.DeltaHold;
                        }
                        else if (priorFlagValue==flagValue && this.ResultStatus == resultStatusType.DeltaHold)
                        {
                            if (string.IsNullOrEmpty(this.PriorReleasedValue))
                            {
                                this.ResultStatus = resultStatusType.Final;
                            }
                            else if (!IsTestHold)
                            {
                                this.ResultStatus = resultStatusType.Corrected;
                            }   
                        }
                    }
                }
            }
        }

        public void ResetAuditItems()
        {
            m_auditItemList = new List<string>();

            ResetAudit();
        }

        private bool m_doNotSendToFollow()
        {
            throw new NotImplementedException();
        }

    }
} // OrderAnalyte