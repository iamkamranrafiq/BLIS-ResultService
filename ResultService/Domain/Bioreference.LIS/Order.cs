using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Transactions;
using Bioreference.Common;
using Bioreference.Common.Client;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Bioreference.RuleEngine;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace Bioreference.LIS
{

    [Serializable()]
    public class Order : AuditDataClassBase, IEvaluateRule
    {


        #region Private Members

        private string m_accessionNbr = "";
        private long m_id = 0L;
        private string m_accountNbr = "";
        private RequisitionType m_reqType = RequisitionType.Unknown;
        private BillingType m_billType = BillingType.Unknown;
        private DateTime m_dateCollected = DateTime.Parse("1900-01-01");
        private string m_timeCollected = "";
        private DateTime m_dateServiced = DateTime.Parse("1900-01-01");
        private string m_comments = "";
        private OrderPriority m_priority = OrderPriority.Routine;
        private clientType m_clientType = clientType.None;
        private MarketType m_marketType = MarketType.Unknown;
        private Report m_report;

        private OrderPatient m_patient;
        private OrderPhysician m_physician;
        private OrderGuarantor m_guarantor;
        private OrderTests m_tests;
        private OrderSpecimens m_specimens;
        private OrderPatientInsurances m_insurances;
        private OrderICD9s m_icd9s;
        private OrderPhysicians m_physicians; // Contains list of physicians including primary and copyto/faxto

        private ABNProvidedType m_abnProvided = ABNProvidedType.None;
        private bool m_physicianSignature = false;

        private OrderAnswers m_aoes;
        private int m_externalAppId = 0;
        private bool m_evalReport = false; // 'Determine if we need to run rules engine on the Report object.
        private bool m_isClinicalTrial = false;
        private bool m_isReportHold = false;
        private long m_euid = 0L;
        private int m_SPMOrderid = 0;
        private bool m_isReportHoldDirty = false;

        private string m_StudyNumber = "";
        private string m_VisitNumber = "";

        private OrderUpdateStatus m_updateStatus = new OrderUpdateStatus();
        private OrderComments m_orderComments;

        private List<string> m_auditItemList;

        // Re-release all results inidicator is used by InboundQueue Channel from SPM
        private bool m_reReleaseResults = false;
        private int m_SPMParentOrderId;
        private int m_ParentOrderId;
        private string m_ParentAccessionNbr = "";
        private int m_SPMChildOrderId;
        private int m_ChildOrderId;
        private string m_ChildAccessionNbr = "";

        private string m_enterersLocationType = "";
        private string m_enterersLocation = "";
        private string m_enterersFirstName = "";
        private string m_enterersLastName = "";
        private string m_accountPriority = "";
        private int m_DivisionId;
        private string m_prevReferenceRange;
        private string m_prevFlagValue;
        private DateTime m_prevReleaseDate;
        private string m_currentReferenceRange;
        private string m_currentFlagValue;
        private DateTime m_requisitionDate = DateTime.Parse("1900-01-01");
        private bool m_specimenUpdated = false;
        private bool m_DemographicUpdate = false;

        private readonly ILog Log = LogManager.GetLogger<Order>();

        #endregion

        public bool ReReleaseResults
        {
            set
            {
                m_reReleaseResults = value;
            }
        }

        public bool AreCommentsDirty
        {
            get
            {
                return m_orderComments.IsDirty;
            }
        }

        protected override object ParentIdentifierId
        {
            get
            {
                return AccessionIdentifier;
            }
        }

        public string AccessionIdentifier
        {
            get
            {
                return string.Concat(AccessionNbr, "-", DateOfService.ToString("MMddyyyy"));
            }
        }

        protected override parentIdentifierType ParentIdentifierType
        {
            get
            {
                return AccessionIdentifierType;
            }
        }

        public parentIdentifierType AccessionIdentifierType
        {
            get
            {
                return parentIdentifierType.AccessionNbr_ServiceDate;
            }
        }

        public override bool IsValid
        {
            get
            {
                return base.IsValid && m_tests.IsValid && m_specimens.IsValid && m_patient.IsValid && m_guarantor.IsValid && m_insurances.IsValid && m_physician.IsValid && m_physicians.IsValid && m_icd9s.IsValid;

            }
        }

        public Report Report
        {
            get
            {
                if (m_report is null)
                {
                    m_report = OrderManager.FetchReport(AccessionNbr, DateOfService, this);
                }
                return m_report;
            }
        }

        public bool IsReportHoldDirty
        {
            get
            {
                return m_isReportHoldDirty;
            }
            set
            {
                if (m_isReportHoldDirty != value)
                {
                    m_isReportHoldDirty = value;

                }
            }
        }

        public override void FlagClean()
        {

            // 'Clear updated Properties.  Used for demographic re-releasing
            m_updateStatus.UpdatedProperties.Clear();

            base.FlagClean();
        }

        public override void RelaxValidation()
        {

            base.RelaxValidation();
            // m_tests.RelaxValidation()
            // m_specimens.RelaxValidation()
            m_patient.RelaxValidation();
            m_guarantor.RelaxValidation();
            m_insurances.RelaxValidation();
            m_physician.RelaxValidation();
            m_physicians.RelaxValidation();
            m_icd9s.RelaxValidation();

        }

        private void DateServiceCollectionCheck()
        {

            // Disabled - validation done in SPM
            // Rules.Assert("DateServiceCollection", "Date of Collection cannot be greater than Date of Service.", DateTime.Parse(String.Format("{0} {1}", m_dateCollected.ToShortDateString(), m_timeCollected)) > m_dateServiced)

        }


        #region Public Properties

        public OrderUpdateStatus UpdateStatus
        {
            get
            {
                return m_updateStatus;
            }
        }

        public int ExternalApplicationId
        {
            get
            {
                return m_externalAppId;
            }
            set
            {
                if (m_externalAppId != value)
                {
                    m_externalAppId = value;
                    FlagDirty();
                }
            }
        }

        [Audit("AccountPriority")]
        public string AccountPriority
        {
            get
            {
                return m_accountPriority;
            }
            set
            {
                if ((m_accountPriority ?? "") != (value.Trim() ?? ""))
                {
                    m_accountPriority = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("EnterersLocationType")]
        public string EnterersLocationType
        {
            get
            {
                return m_enterersLocationType;
            }
            set
            {
                if ((m_enterersLocationType ?? "") != (value.Trim() ?? ""))
                {
                    m_enterersLocationType = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("EnterersLocation")]
        public string EnterersLocation
        {
            get
            {
                return m_enterersLocation;
            }
            set
            {
                if ((m_enterersLocation ?? "") != (value.Trim() ?? ""))
                {
                    m_enterersLocation = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("EnterersFirstName")]
        public string EnterersFirstName
        {
            get
            {
                return m_enterersFirstName;
            }
            set
            {
                if ((m_enterersFirstName ?? "") != (value.Trim() ?? ""))
                {
                    m_enterersFirstName = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("EnterersLastName")]
        public string EnterersLastName
        {
            get
            {
                return m_enterersLastName;
            }
            set
            {
                if ((m_enterersLastName ?? "") != (value.Trim() ?? ""))
                {
                    m_enterersLastName = value.Trim();
                    FlagDirty();
                }
            }
        }

        public int DivisionId
        {
            get
            {
                return m_DivisionId;
            }
            set
            {
                if (m_DivisionId != value)
                {
                    m_DivisionId = value;
                    FlagDirty();
                }
            }
        }

        [Audit("AccessionNbr")]
        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
            set
            {
                if ((m_accessionNbr ?? "") != (value.Trim() ?? ""))
                {
                    m_accessionNbr = value.Trim();
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

        public long ID
        {
            get
            {
                return m_id;
            }
        }

        /// <summary>
    /// Account Number assigned when creating an order.
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        [Audit("AccountNumber")]
        public string AccountNumber
        {
            get
            {
                return m_accountNbr;
            }
        }

        [Audit("Priority")]
        public OrderPriority Priority
        {
            get
            {
                return m_priority;
            }
            set
            {
                if (m_priority != value)
                {
                    m_priority = value;
                    FlagDirty();
                }
            }
        }

        [Audit("ClientType")]
        public clientType ClientType
        {
            get
            {
                return m_clientType;
            }
            set
            {
                if (m_clientType != value)
                {
                    m_clientType = value;
                    FlagDirty();
                }
            }
        }

        [Audit("MarketType")]
        public MarketType MarketType
        {
            get
            {
                return m_marketType;
            }
            set
            {
                if (m_marketType != value)
                {
                    m_marketType = value;
                    FlagDirty();
                }
            }
        }

        [Audit("BillingType")]
        public BillingType BillingType
        {
            get
            {
                return m_billType;
            }
            set
            {
                if (m_billType != value)
                {
                    m_billType = value;
                    Rules.Assert("BillingType", "Please select a valid billing type.", Conversions.ToString(base.IsValidationRelaxed == false && m_billType == BillingType.Unknown));
                    Rules.Assert("ABNProvided", "Must designate how ABN was provided if Bill Type = Medicare.", Conversions.ToString(Interaction.IIf(base.IsValidationRelaxed == false && m_abnProvided == ABNProvidedType.None && m_billType == BillingType.Medicare, true, false)));
                    FlagDirty();
                }
            }
        }

        [Audit("RequisitionType")]
        public RequisitionType RequisitionType
        {
            get
            {
                return m_reqType;
            }
            set
            {
                if (m_reqType != value)
                {
                    m_reqType = value;
                    Rules.Assert("RequisitionType", "Please select a valid requisition type.", Conversions.ToString(base.IsValidationRelaxed == false && m_reqType == RequisitionType.Unknown));
                    FlagDirty();
                }
            }
        }

        /// <summary>
    /// Also affects Rule "DateServiceCollection"
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        [Audit("DateOfCollection")]
        public DateTime DateOfCollection
        {
            get
            {
                return m_dateCollected;
            }
            set
            {
                if (m_dateCollected != value)
                {
                    m_dateCollected = value;

                    string brokenRule = "";
                    if (m_dateCollected > DateTime.Now)
                    {
                        brokenRule = "Date of Collection cannot be in the future.";
                    }
                    // ElseIf m_dateCollected < DateTime.Now.AddDays(-My.MySettings.Default.LimitDayRange) Then
                    // brokenRule = String.Format("Date of Collection cannot be older than {0} days.", My.MySettings.Default.LimitDayRange)
                    else
                    {
                        brokenRule = "";
                    }
                    Rules.Assert("DateOfCollection", brokenRule, Conversions.ToString(Interaction.IIf(!string.IsNullOrEmpty(brokenRule), true, false)));
                    UpdateStatus.AddProperty("Order.DateOfCollection");
                    DateServiceCollectionCheck();
                    FlagDirty();
                }
            }
        }

        /// <summary>
    /// Also affects Rule "DateServiceCollection"
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        [Audit("TimeOfCollection")]
        public string TimeOfCollection
        {
            get
            {
                return m_timeCollected;
            }
            set
            {
                if ((m_timeCollected ?? "") != (value.Trim() ?? ""))
                {
                    m_timeCollected = value.Trim();
                    Rules.Assert("TimeOfCollection", "Please enter a valid collection time.", Conversions.ToString(!string.IsNullOrEmpty(m_timeCollected) && !Regex.IsMatch(m_timeCollected, @"^(0?[1-9]|1[0-2]):?[0-5][0-9]\s[A|P]M$")));
                    DateServiceCollectionCheck();
                    FlagDirty();
                }
            }
        }

        /// <summary>
    /// Also affects Rule "DateServiceCollection"
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        [Audit("DateOfService")]
        public DateTime DateOfService
        {
            get
            {
                return m_dateServiced;
            }
            set
            {
                if (m_dateServiced != value)
                {
                    m_dateServiced = value;

                    string brokenRule = "";
                    // 'Add buffer to accomodate difference in time on servers.
                    if (m_dateServiced.AddHours(-1) > DateTime.Now)
                    {
                        brokenRule = "Date of Service cannot be in the future.";
                    }
                    else if (m_dateServiced < DateTime.Now.AddDays(-Configuration.AppSettings.GetInt("Bioreference.LIS:LimitDayRange")))
                    {
                        brokenRule = string.Format("Date of Service cannot be older than {0} days.", Configuration.AppSettings.GetInt("Bioreference.LIS:LimitDayRange"));
                    }
                    else
                    {
                        brokenRule = "";
                    }
                    Rules.Assert("DateOfService", brokenRule, Conversions.ToString(Interaction.IIf(!string.IsNullOrEmpty(brokenRule), true, false)));
                    DateServiceCollectionCheck();
                    FlagDirty();
                }
            }
        }

        [Audit("ABNProvided")]
        public ABNProvidedType ABNProvided
        {
            get
            {
                return m_abnProvided;
            }
            set
            {
                if (m_abnProvided != value)
                {
                    m_abnProvided = value;
                    Rules.Assert("ABNProvided", "Must designate how ABN was provided if bill type = Medicare.", Conversions.ToString(Interaction.IIf(base.IsValidationRelaxed == false && m_abnProvided == ABNProvidedType.None && m_billType == BillingType.Medicare, true, false)));
                    FlagDirty();
                }
            }
        }

        [Audit("HasPhysicianSignature")]
        public bool HasPhysicianSignature
        {
            get
            {
                return m_physicianSignature;
            }
            set
            {
                if (m_physicianSignature != value)
                {
                    m_physicianSignature = value;
                    FlagDirty();
                }
            }
        }

        [Audit("Comments")]
        public string Comments
        {
            get
            {
                return m_comments;
            }
            set
            {
                if ((m_comments ?? "") != (value.Trim() ?? ""))
                {
                    m_comments = value.Trim().NormalizeToWindows();
                    FlagDirty();
                }
            }
        }

        [Audit("IsClinicalTrial")]
        public bool IsClinicalTrial
        {
            get
            {
                return m_isClinicalTrial;
            }
            set
            {
                if (m_isClinicalTrial != value)
                {
                    m_isClinicalTrial = value;
                    FlagDirty();
                }
            }
        }
        [Audit("IsReportHold")]
        public bool IsReportHold
        {
            get
            {
                return m_isReportHold;
            }
            set
            {
                if (m_isReportHold != value)
                {
                    m_isReportHold = value;
                    FlagDirty();
                }
            }
        }
        [Audit("EUID")]
        public long EUID
        {
            get
            {
                return m_euid;
            }
            set
            {
                if (m_euid != value)
                {
                    m_euid = value;
                    FlagDirty();
                }
            }
        }
        [Audit("SPMOrderId")]
        public long SPMOrderId
        {
            get
            {
                return m_SPMOrderid;
            }
            set
            {
                if (m_SPMOrderid != value)
                {
                    m_SPMOrderid = (int)value;
                    FlagDirty();
                }
            }
        }

        [Audit("StudyNumber")]
        public string StudyNumber
        {
            get
            {
                return m_StudyNumber;
            }
            set
            {
                if ((m_StudyNumber ?? "") != (value ?? ""))
                {
                    m_StudyNumber = value;
                    FlagDirty();
                }
            }
        }

        [Audit("VisitNumber")]
        public string VisitNumber
        {
            get
            {
                return m_VisitNumber;
            }
            set
            {
                if ((m_VisitNumber ?? "") != (value ?? ""))
                {
                    m_VisitNumber = value;
                    FlagDirty();
                }
            }
        }

        public OrderTests Tests
        {
            get
            {
                return m_tests;
            }
        }

        public OrderSpecimens Specimens
        {
            get
            {
                return m_specimens;
            }
        }

        public OrderGuarantor Guarantor
        {
            get
            {
                return m_guarantor;
            }
        }

        public OrderPatientInsurances Insurances
        {
            get
            {
                return m_insurances;
            }
        }

        public OrderPatient Patient
        {
            get
            {
                return m_patient;
            }
        }

        public OrderICD9s ICD9s
        {
            get
            {
                return m_icd9s;
            }
        }

        public OrderPhysician PrimaryPhysician
        {
            get
            {
                // For Each p As OrderPhysician In m_physicians.List
                // If p.Type = PhysicianType.Primary Then
                // Return p
                // End If
                // Next
                // Return Nothing
                return m_physician;

            }
        }

        public OrderPhysicians SecondaryPhysicians
        {
            get
            {
                return m_physicians;
            }
        }

        public OrderAnswers AOEs
        {
            get
            {
                return m_aoes;
            }
        }

        public OrderComments OrderComments
        {
            get
            {
                return m_orderComments;
            }
        }

        public bool SpecimenUpdated
        {
            get
            {
                return m_specimenUpdated;
            }
            set
            {
                m_specimenUpdated = value;
            }
        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_patient.IsDirty || m_tests.IsDirty || m_specimens.IsDirty || m_guarantor.IsDirty || m_insurances.IsDirty || m_physician.IsDirty || m_physicians.IsDirty || m_icd9s.IsDirty || m_orderComments.IsDirty;

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

        [Audit("SPMParentOrderId")]
        public int SPMParentOrderId
        {
            get
            {
                return m_SPMParentOrderId;
            }
            set
            {
                if (m_SPMParentOrderId != value)
                {
                    m_SPMParentOrderId = value;
                    FlagDirty();
                }
            }
        }

        [Audit("ParentOrderId")]
        public int ParentOrderId
        {
            get
            {
                return m_ParentOrderId;
            }
            set
            {
                if (m_ParentOrderId != value)
                {
                    m_ParentOrderId = value;
                    FlagDirty();
                }
            }
        }

        [Audit("ParentAccessionNbr")]
        public string ParentAccessionNbr
        {
            get
            {
                return m_ParentAccessionNbr;
            }
            set
            {
                if ((m_ParentAccessionNbr ?? "") != (value ?? ""))
                {
                    m_ParentAccessionNbr = value.Trim();
                    FlagDirty();
                }
            }
        }

        public int SPMChildOrderId
        {
            get
            {
                return m_SPMChildOrderId;
            }
            private set
            {
                if (m_SPMChildOrderId != value)
                {
                    m_SPMChildOrderId = value;
                }
            }
        }

        public int ChildOrderId
        {
            get
            {
                return m_ChildOrderId;
            }
            private set
            {
                if (m_ChildOrderId != value)
                {
                    m_ChildOrderId = value;
                }
            }
        }

        public string ChildAccessionNbr
        {
            get
            {
                return m_ChildAccessionNbr;
            }
            private set
            {
                if ((m_ChildAccessionNbr ?? "") != (value ?? ""))
                {
                    m_ChildAccessionNbr = value.Trim();
                }
            }
        }

        public DateTime RequisitionDate
        {
            get
            {
                return m_requisitionDate;
            }
        }

        public string MetaData
        {
            get
            {
                return m_metaData;
            }
            set
            {
                m_metaData = value;
            }
        }

        public bool DemographicUpdate
        {
            get
            {
                return m_DemographicUpdate;
            }
            set
            {
                m_DemographicUpdate = value;
            }
        }


        #region Public Functions

        public void CleanUpOrder()
        {
            if (m_id == 0L)
            {
                return;
            }

            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();
            try
            {
                @params.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int64, m_id));
                @params.Add((DbParameter)da.CreateParameter("@UpdatedBy", DbType.String, Thread.CurrentPrincipal.Identity.Name));
                @params.Add((DbParameter)da.CreateParameter("@IsUpdated", DbType.Boolean, false, ParameterDirection.Output));
                var returnParams = da.ExecuteNonQuery("lis_Order_CleanUp", @params.ToArray());
                bool returnVal = Conversions.ToBoolean(returnParams["@IsUpdated"].Value);
                if (returnVal)
                {
                    AuditManager.LogCustomObjectAction(this, string.Format("CleanUp  Order #{0} by user {1}", m_id, Thread.CurrentPrincipal.Identity.Name));
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.ToString());
                throw;
            }

        }

        public string GetPropertyValue(string propertyName)
        {

            System.Reflection.PropertyInfo pi;
            object obj = null;

            // Need to change this so that it is more dynamic
            if (Regex.IsMatch(propertyName, "^Patient.PrimaryAddress."))
            {
                obj = Patient.PrimaryAddress;
            }
            else if (Regex.IsMatch(propertyName, "^Patient."))
            {
                obj = Patient;
            }

            if (!(obj == null))
            {

                string name = propertyName.Substring(propertyName.LastIndexOf(".") + 1);
                pi = obj.GetType().GetProperty(name);
                if (!(pi == null))
                {
                    return pi.GetValue(obj, null).ToString();
                }
            }

            return "";

        }

        /// <summary>
    /// Returns all the rules for the order, including all children
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
        public Data.Rules GetCompleteRules()
        {

            var r = new Data.Rules();

            r.MergeList(base.Rules);
            if (!(m_patient == null))
                r.MergeList(m_patient.Rules);
            if (!(m_guarantor == null))
                r.MergeList(m_guarantor.Rules);
            if (!(m_insurances == null))
                r.MergeList(m_insurances.Rules);
            if (!(m_tests == null))
                r.MergeList(m_tests.Rules);
            if (!(m_specimens == null))
                r.MergeList(m_specimens.Rules);
            if (!(m_physician == null))
                r.MergeList(m_physician.Rules);
            if (!(m_physicians == null))
                r.MergeList(m_physicians.Rules);
            if (!(m_icd9s == null))
                r.MergeList(m_icd9s.Rules);

            return r;

        }

        // This was originally created for order entry, but was marked as obsolete for reason below. Now we need to readd
        // it for the interface since it is only passing in an account number string.
        // <Obsolete("Setting the account on an order carries account specific questions with it and this is not done here. Create a new account using the OrderManger.")> _
        /// <summary>
    /// This will reset the account number, save the order (internally), and release all Report items, if not pending.
    /// Setting the account on an order carries account specific questions with it and this is not done here. 
    /// Create a new account using the OrderManger.CreateOrder(ByVal acct as Account) to attach questions.
    /// </summary>
    /// <param name="accountNumber"></param>
    /// <remarks></remarks>
        public Order SetAccountNumber(string accountNumber)
        {

            Order o = null;
            string flag = "";

            try
            {
                flag = "SetAccountNumber";
                if (string.IsNullOrEmpty(accountNumber.Trim()))
                {
                    flag = "No Account Number";
                    throw new Exception(string.Concat("Cannot set a blank accountNumber for existing Accession #:", AccessionNbr));
                }

                if ((AccountNumber ?? "") != (accountNumber ?? ""))
                {
                    flag = "Account Number Changed";
                    // Do any necessary action here to reset the account.
                    m_accountNbr = accountNumber;
                    FlagDirty();

                    flag = "Order Validity Check";
                    if (IsValid)
                    {
                        flag = "Order Save";
                        o = (Order)Save();
                    }
                    else
                    {
                        flag = "Order Not Valid";
                        throw new Exception(Rules.ToString());
                    }

                    flag = "Report Fetch";
                    var r = OrderManager.FetchReport(AccessionNbr, DateOfService);
                    if (!(r == null))
                    {
                        flag = "Report Fetched";
                        foreach (ReportAnalytePanel p in r.AnalytePanels.List)
                        {
                            flag = $"Panel {p.PanelCode}, MarkAsReleased";
                            if (p.TransmitStatus != transmitStatusType.PendingRelease)
                                p.MarkAsReleased();
                        }
                        foreach (ReportAnalyte a in r.Analytes.List)
                        {
                            flag = $"Analyte {a.Code}, MarkAsReleased";
                            if (a.TransmitStatus != transmitStatusType.PendingRelease)
                                a.MarkAsReleased();
                        }
                    }

                    flag = "Report Validity Check";
                    if (r.IsValid)
                    {
                        flag = "Report Save";
                        // r.EvaluateRules(Me, "")
                        r.Save();
                    }
                    else
                    {
                        flag = "Report Not Valid";
                        throw new Exception(r.Rules.ToString());
                    }

                }

                flag = "";
                return o;

                flag = "";
            }

            catch (Exception ex)
            {
                throw new Exception($"[Flag {flag}] -- {ex.Message}", ex);
            }

        }

        public OrderPhysician SetPrimaryPhysician(Physician physician)
        {

            if (!(physician == null))
            {

                {
                    ref var withBlock = ref m_physician;
                    withBlock.FirstName = physician.FirstName;
                    withBlock.LastName = physician.LastName;
                    withBlock.MiddleName = physician.MiddleName;
                    withBlock.Title = physician.Title;
                    withBlock.Suffix = physician.Suffix;
                    // 'Rest of properties
                }

                return m_physician;
            }
            else
            {
                return null;
            }

        }

        // Public Function AddGuarantor(ByVal relation As Relationship) As OrderGuarantor

        // Dim guarantor As OrderGuarantor = New OrderGuarantor(Me)
        // guarantor.Relation = relation

        // Return guarantor

        // End Function

        public string SetOrderTestQuestionValues(int questionId, string value)
        {

            foreach (OrderTest ot in m_tests.List)
            {
                foreach (OrderTestAnswer ota in ot.AOEs)
                {
                    if (ota.QuestionId == questionId)
                    {
                        ota.Answer = value;
                    }
                }
            }

            return "";

        }

        /// <summary>
    /// Will mark all ordered tests as IsSentOut = True
    /// </summary>
    /// <param name="isSentOut"></param>
    /// <remarks></remarks>
        public void SetIsSentOut(bool isSentOut)
        {

            foreach (OrderTest t in m_tests.List)
                // No need to check if it IsSentOut = True, if it is, will not mark object as dirty.
                t.IsSentOut = true;

        }


        #endregion

        #region Constructor


        internal Order()
        {

            m_patient = new OrderPatient(this, LoadAudit);
            m_insurances = new OrderPatientInsurances(this);
            m_guarantor = new OrderGuarantor(this);
            m_tests = new OrderTests(this);
            m_specimens = new OrderSpecimens(this);
            m_physician = new OrderPhysician(this, OrderPhysicianType.Primary);
            m_icd9s = new OrderICD9s(this);
            m_physicians = new OrderPhysicians(this);
            m_orderComments = new OrderComments(this);
            m_aoes = new OrderAnswers(this);

            m_auditItemList = new List<string>();

            // MyBase.SetAuditConnString(My.Settings.ConnString)
            SetConncurrencyConnString(Configuration.ConnectionString);

        }

        #endregion

        #region Data Functions

        internal static Order Fetch(long orderId, bool loadAudit)
        {

            return (Order)DataFactory.Fetch(new Criteria(orderId, "", 0, loadAudit));

        }

        public static Order FetchBySPMOrderId(long spmOrderId)
        {
            return (Order)DataFactory.Fetch(new Criteria(0L, "", DateTime.Parse("1900-01-01"), spmOrderId, false));
        }

        /// <summary>
    /// Used to fetch the next available order to send to ReferenceLab.  
    /// Order should be marked as IsSentOut and saved to make next order available.
    /// </summary>
    /// <param name="referenceLabId"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static Order FetchNextToSend(int referenceLabId)
        {

            Order o = (Order)DataFactory.Fetch(new Criteria(0L, "", referenceLabId, false));
            if (o.ID == 0L)
            {
                return null;
            }
            else
            {
                return o;
            }

        }

        internal static Order Fetch(string accessionNbr, DateTime dateOfService, long spmOrderId, bool loadAudit)
        {

            return (Order)DataFactory.Fetch(new Criteria(0L, accessionNbr, dateOfService, spmOrderId, false));

        }

        internal static Order Fetch(string accessionNbr, DateTime dateOfService, bool loadAudit)
        {

            return (Order)DataFactory.Fetch(new Criteria(0L, accessionNbr, dateOfService, false));

        }

        internal static Order Fetch(string accessionNbr, bool loadAudit)
        {

            return (Order)DataFactory.Fetch(new Criteria(0L, accessionNbr, DateTime.Parse("1900-01-01"), loadAudit));

        }

        internal static Order Create(string accountNbr)
        {

            if (string.IsNullOrEmpty(accountNbr.Trim()))
            {
                throw new Exception("Unable to create an Order with a blank Account Number.");
            }

            var order = new Order();
            order.m_accountNbr = accountNbr;

            return order;

        }

        internal static Order Create(Account account)
        {

            if (string.IsNullOrEmpty(account.AccountNumber.Trim()))
            {
                throw new Exception("Failed to create an Order. The account parameter has a blank Account Number.");
            }

            var order = new Order();
            order.m_accountNbr = account.AccountNumber;

            foreach (Question q in account.Questions)
                order.AOEs.List.Add(new OrderAnswer(order, q));

            return order;

        }

        public static void ReleaseToReporting(int orderID, bool isReportHold, string userName)
        {

            if (orderID == 0)
            {
                throw new Exception("Invalid order ID");
            }

            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();
            try
            {

                @params.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, orderID));
                @params.Add((DbParameter)da.CreateParameter("@IsReportHold", DbType.Boolean, isReportHold));
                @params.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, userName));
                da.ExecuteProcedure("lis_Order_ReleaseToReporting", @params.ToArray());
            }
            catch (Exception ex)
            {
                Trace.Write(ex.ToString());
                throw;
            }

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();
            DataTable[] dt;
            try
            {

                // If a referenceLabId is passed in, we return the next available Order to be Sent Out.
                if (c.ReferenceLabId == 0)
                {
                    @params.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int64, c.OrderID));
                    @params.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr));
                    if (c.DateOfService > DateTime.Parse("1900-01-01"))
                    {
                        @params.Add((DbParameter)da.CreateParameter("@DateOfService", DbType.DateTime, c.DateOfService));
                    }
                    if (c.SPMOrderId > 0L)
                    {
                        @params.Add((DbParameter)da.CreateParameter("@SPMOrderId", DbType.Int64, c.SPMOrderId));
                    }
                    dt = da.ExecuteProcedure("lis_Order_Fetch", @params.ToArray());
                }
                else
                {
                    @params.Add((DbParameter)da.CreateParameter("@ReferenceLabId", DbType.Int32, c.ReferenceLabId));
                    dt = da.ExecuteProcedure("lis_ReferenceOrder_Fetch", @params.ToArray());
                }

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt, c.LoadAudit);
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());
                throw;

            }

        }

        public static int UpdateEUID(string accessionNbr, DateTime DateServiced, long EUID)
        {

            try
            {
                if (string.IsNullOrEmpty(accessionNbr))
                {
                    throw new Exception("Invalid Accession Number");
                }

                accessionNbr = accessionNbr.Get7Digit();

                var da = new DataWrapper(Configuration.ConnectionString);
                var @params = new List<DbParameter>() { (DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, accessionNbr), (DbParameter)da.CreateParameter("@DateOfService", DbType.DateTime, DateServiced), (DbParameter)da.CreateParameter("@NewEUID", DbType.Int64, EUID), (DbParameter)da.CreateParameter("@UserName", DbType.String, Thread.CurrentPrincipal.Identity.Name) };
                DataTable[] dt = da.ExecuteProcedure("lis_NextGateUpdateEUID", @params.ToArray());
                if (dt[0].Rows.Count > 0)
                {
                    return Conversions.ToInteger(dt[0].Rows[0]["Status"]);
                }
                return -99;
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        protected override void DataFactory_Save()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            try
            {

                paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int64, m_id, ParameterDirection.InputOutput));
                paramList.Add((DbParameter)da.CreateParameter("@AccountNumber", DbType.String, m_accountNbr));
                paramList.Add((DbParameter)da.CreateParameter("@BillType", DbType.Int32, m_billType));
                paramList.Add((DbParameter)da.CreateParameter("@ReqType", DbType.Int32, m_reqType));
                paramList.Add((DbParameter)da.CreateParameter("@DateCollected", DbType.DateTime, m_dateCollected));
                paramList.Add((DbParameter)da.CreateParameter("@TimeCollected", DbType.String, m_timeCollected));
                paramList.Add((DbParameter)da.CreateParameter("@DateServiced", DbType.DateTime, m_dateServiced));
                paramList.Add((DbParameter)da.CreateParameter("@Comments", DbType.String, m_comments));
                paramList.Add((DbParameter)da.CreateParameter("@Priority", DbType.Int32, m_priority));
                paramList.Add((DbParameter)da.CreateParameter("@ABNProvided", DbType.Int32, m_abnProvided));
                paramList.Add((DbParameter)da.CreateParameter("@HasPhysicianSignature", DbType.Boolean, m_physicianSignature));
                paramList.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, m_accessionNbr));
                paramList.Add((DbParameter)da.CreateParameter("@ExternalAppId", DbType.Int32, m_externalAppId));
                paramList.Add((DbParameter)da.CreateParameter("@ClientType", DbType.Int32, m_clientType));
                paramList.Add((DbParameter)da.CreateParameter("@MarketType", DbType.Int32, m_marketType));
                paramList.Add((DbParameter)da.CreateParameter("@IsClinicalTrial", DbType.Boolean, m_isClinicalTrial));
                paramList.Add((DbParameter)da.CreateParameter("@IsReportHold", DbType.Boolean, m_isReportHold));
                paramList.Add((DbParameter)da.CreateParameter("@EUID", DbType.Int64, m_euid));
                paramList.Add((DbParameter)da.CreateParameter("@SPMOrderId", DbType.Int32, m_SPMOrderid));
                paramList.Add((DbParameter)da.CreateParameter("@StudyNumber", DbType.String, m_StudyNumber));
                paramList.Add((DbParameter)da.CreateParameter("@VisitNumber", DbType.String, m_VisitNumber));
                paramList.Add((DbParameter)da.CreateParameter("@SPMParentOrderId", DbType.Int32, m_SPMParentOrderId));
                paramList.Add((DbParameter)da.CreateParameter("@EnterersLocationType", DbType.String, m_enterersLocationType));
                paramList.Add((DbParameter)da.CreateParameter("@DivisionId", DbType.Int32, m_DivisionId));
                paramList.Add((DbParameter)da.CreateParameter("@EnterersLocation", DbType.String, m_enterersLocation));
                paramList.Add((DbParameter)da.CreateParameter("@EnterersLastName", DbType.String, m_enterersLastName));
                paramList.Add((DbParameter)da.CreateParameter("@EnterersFirstName", DbType.String, m_enterersFirstName));
                paramList.Add((DbParameter)da.CreateParameter("@AccountPriority", DbType.String, m_accountPriority));
                paramList.Add((DbParameter)da.CreateParameter("@MetaData", DbType.String, m_metaData));

                if (!(CurrentUser == null))
                    paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

                DbParameter[] @params = paramList.ToArray();


                // 'We use the transaction scope when save the Order. If any sql failures, all transaction should roll back.
                var options = new TransactionOptions();
                options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
                options.Timeout = new TimeSpan(0, 2, 0);

                Log.Debug($"{m_accessionNbr} Saving Order Started...");
                using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
                {

                    // Trace.Write("Saving order...")
                    var pc = da.ExecuteNonQuery("lis_Order_Save", @params);

                    // Trace.Write("Done saving order.")
                    // if the parameters collection is nothing, that means an error has occured and has been trapped.
                    if (!(pc == null))
                    {

                        m_id = Conversions.ToLong(pc["@orderid"].Value);

                        Log.Debug($"pc.Item('@orderid').Value={m_id}");

                        // Save all child objects - All wrapped in a TransactionScope
                        if (m_patient.IsDirty)
                        {
                            m_patient.Update();
                            // NA: Get the list of AuditItem in this object after issuing the Clean so all properties are correctly populated.
                            m_auditItemList.AddRange(m_patient.GetFormattedAuditItems);
                        }

                        if (m_guarantor.IsDirty)
                        {
                            m_guarantor.Update();
                            // NA: Get the list of AuditItem in this object after issuing the Clean so all properties are correctly populated.
                            m_auditItemList.AddRange(m_guarantor.GetFormattedAuditItems);
                        }

                        m_insurances.Update();
                        m_tests.Update();
                        m_specimens.Update();

                        if (m_physician.IsDirty)
                        {
                            m_physician.Update();
                            // NA: Get the list of AuditItem in this object after issuing the Clean so all properties are correctly populated.
                            m_auditItemList.AddRange(m_physician.GetFormattedAuditItems);
                        }
                        m_physicians.Update();
                        m_auditItemList.AddRange(m_physicians.GetAuditItems());
                        Log.Debug(" Before Update m_aoes");
                        m_aoes.Update();
                        m_auditItemList.AddRange(m_aoes.GetAuditItems());
                        m_icd9s.Update();

                        // 'Need to know if comments have been updated, if so, may need to update Report
                        bool commentsUpdated = m_orderComments.IsDirty;
                        m_orderComments.Update();
                        m_auditItemList.AddRange(m_orderComments.GetAuditItems());

                        // ************************************************
                        Report r = null;

                        // If need to fire rules or demographic update, need to load report object

                        // If Me.m_evalReport OrElse (Me.m_updateStatus.CheckForRelease) Then
                        if (m_evalReport || m_updateStatus.CheckForRelease || m_reReleaseResults)
                        {
                            Log.Debug(" INTO If Me.m_evalReport OrElse (Me.m_updateStatus.CheckForRelease) OrElse m_reReleaseResults Then");
                            // this will always retrieve the latest Accession.  However, when we have a new Duplicate Accession.
                            // The new one isn't in the DB yet for this Order.  So this will pull the OLD accession.
                            r = Report;

                            if (!(r == null) && ID != r.OrderId)
                            {
                                r = null;
                            }
                            if (m_updateStatus.UpdatedProperties.Count > 0 && !(r == null) && r.ID > 0L)
                            {
                                r.ReferencedOrder.DemographicUpdate = true;
                            }
                            if (!(r == null) && r.ID > 0L)
                            {
                                // If m_evalReport = true Or demographics have been updated
                                // If Me.m_evalReport Then
                                Log.Debug("Calling EvaluateRulesEx with RuleSetRelease");
                                r.EvaluateRulesEx(this, "", Configuration.RuleSetRelease);
                                // End If
                                // in certain instances we do not want to re-release based on Demo Update.
                                // i.e.  TOX.

                                // 'If certain demographics were updated we may need to re-release results or change range comments
                                if (m_updateStatus.UpdatedProperties.Count > 0)
                                {
                                    UpdatesBasedOnDemographics(r, Configuration.AppSettings.GetBool("Bioreference.LIS:ReleaseOnDemographicUpdate"));
                                }

                                // if reRelease flag is set, must resend all previously released results.
                                if (m_reReleaseResults)
                                {
                                    Log.Debug(" INTO If m_reReleaseResults Then");
                                    if (!r.IsCOC)
                                    {
                                        Log.Debug($" Loop into analytes count={r.Analytes.List.Count}");
                                        // Try
                                        foreach (ReportAnalyte a in r.Analytes.List)
                                        {
                                            Log.Debug($" analyte={a.Code}");

                                            if ((!string.IsNullOrEmpty(a.ResultValue) && a.HasBeenReleased() && a.ResultStatus != resultStatusType.OnHold) & a.ResultStatus != resultStatusType.Pending)
                                            {
                                                if (a.ResultStatus == resultStatusType.Preliminary)
                                                {
                                                    a.MarkAsPreliminaryReleased();
                                                }
                                                else
                                                {
                                                    Log.Debug(" analyte before MarkAsReleased");
                                                    a.MarkAsReleased();
                                                    Log.Debug(" analyte after MarkAsReleased");
                                                }
                                            }
                                            Log.Debug($"End iteration:  Now count={r.Analytes.List.Count}");
                                        }
                                        // Catch ex As Exception
                                        // SharedFunctions.DebugWrite(ex.ToString(), Me.ToString())
                                        // Throw ex
                                        // End Try
                                        // Throw New Exception("Test exception")
                                        Log.Debug(" Loop into panels");
                                        foreach (ReportAnalytePanel p in r.AnalytePanels.List)
                                        {
                                            foreach (ReportAnalyte a in p.Analytes.List)
                                            {
                                                if ((a.HasBeenReleased() && a.ResultStatus != resultStatusType.OnHold) & a.ResultStatus != resultStatusType.Pending)
                                                {
                                                    if (a.ResultStatus == resultStatusType.Preliminary)
                                                    {
                                                        a.MarkAsPreliminaryReleased();
                                                    }
                                                    else
                                                    {
                                                        a.MarkAsReleased();
                                                    }
                                                }
                                            }

                                        }
                                    }
                                }
                                // end change for re-release
                            }
                        }

                        if (commentsUpdated)
                        {
                            Log.Debug(" INTO If commentsUpdated Then");
                            if (r == null)
                            {
                                Log.Debug(" INTO If IsNothing(r) Then");
                                // this will always retrieve the latest Accession.  However, when we have a new Duplicate Accession.
                                // The new one isn't in the DB yet for this Order.  So this will pull the OLD accession.
                                r = Report;
                                Log.Debug(" AFTER  r = Report.Fetch(m_accessionNbr)");
                                if (!(r == null) && ID != r.OrderId)
                                {
                                    r = null;
                                }

                                if (!(r == null) && r.ID > 0L)
                                {
                                    Log.Debug(" INTO If Not IsNothing(r) AndAlso r.ID > 0 Then");
                                    r.CheckAndFlagIfResend();
                                    r.FlagDirty();
                                }
                            }
                        }
                        FlagClean();
                        if (!(r == null) && r.ID > 0L)
                        {

                            // If IsReportHoldDirty Then
                            // For Each reportholdAnalyte As ReportAnalyte In r.Analytes.List
                            // If reportholdAnalyte.ResultStatus = resultStatusType.Corrected Then
                            // reportholdAnalyte.ResultStatus = resultStatusType.Final
                            // End If
                            // Next
                            // For Each reportholdAnalytePanel As ReportAnalytePanel In r.AnalytePanels.List
                            // For Each reportholdAnalyte As ReportAnalyte In reportholdAnalytePanel.Analytes.List
                            // If reportholdAnalyte.ResultStatus = resultStatusType.Corrected Then
                            // reportholdAnalyte.ResultStatus = resultStatusType.Final
                            // End If
                            // Next
                            // Next
                            // End If

                            Log.Debug(" BEFORE r.Save()");
                            r.Save();
                            Log.Debug(" AFTER r.Save()");
                            m_auditItemList.AddRange(r.GetFormattedAuditItems);
                        }
                        // ************************************************

                    }

                    scope.Complete();

                }

                Log.Debug($"{m_accessionNbr} Saving Order Completed. Audit Started...");

                m_auditItemList.AddRange(GetAuditItemsAndClean(true));
                // Me.FlagClean(True)

                Log.Debug($"{m_accessionNbr} ORDER Audit Completed.");
            }

            catch (Exception ex)
            {

                Log.Error(ex.Message, ex);
                throw;

            }

        }

        private void Load(DataTable[] dt, bool loadAudit)
        {

            LoadAudit = loadAudit;

            // Load the Order from the first datatable, should only be one row.
            {
                var withBlock = dt[0].Rows[0];
                m_id = Conversions.ToLong(withBlock["OrderId"]);
                m_accountNbr = Conversions.ToString(withBlock["AccountNumber"]);
                m_reqType = (RequisitionType)Conversions.ToInteger(withBlock["ReqType"]);
                m_billType = (BillingType)Conversions.ToInteger(withBlock["BillType"]);
                m_priority = (OrderPriority)Conversions.ToInteger(withBlock["Priority"]);
                m_dateCollected = Conversions.ToDate(withBlock["DateCollected"]);
                m_timeCollected = Conversions.ToString(withBlock["TimeCollected"]);
                m_dateServiced = Conversions.ToDate(withBlock["DateServiced"]);
                m_comments = Conversions.ToString(withBlock["Comments"]).NormalizeToWindows();
                ABNProvided = (ABNProvidedType)Conversions.ToInteger(withBlock["ABNProvided"]);
                HasPhysicianSignature = Conversions.ToBoolean(withBlock["HasPhysicianSignature"]);
                AccessionNbr = Conversions.ToString(withBlock["AccessionNbr"]);
                ExternalApplicationId = Conversions.ToInteger(withBlock["ExternalAppId"]);
                MarketType = (MarketType)Conversions.ToInteger(withBlock["MarketType"]);
                ClientType = (clientType)Conversions.ToInteger(withBlock["ClientType"]);
                IsClinicalTrial = Conversions.ToBoolean(withBlock["IsClinicalTrial"]);
                IsReportHold = Conversions.ToBoolean(withBlock["IsReportHold"]);
                EUID = Conversions.ToLong(withBlock["EUID"]);
                SPMOrderId = Conversions.ToLong(withBlock["SPMOrderId"]);
                StudyNumber = Conversions.ToString(withBlock["StudyNumber"]);
                VisitNumber = Conversions.ToString(withBlock["VisitNumber"]);
                SPMParentOrderId = Conversions.ToInteger(withBlock["SPMParentOrderId"]);
                ParentOrderId = Conversions.ToInteger(withBlock["ParentOrderId"]);
                ParentAccessionNbr = Conversions.ToString(withBlock["ParentAccessionNbr"]);
                SPMChildOrderId = Conversions.ToInteger(withBlock["SPMChildOrderId"]);
                ChildOrderId = Conversions.ToInteger(withBlock["ChildOrderId"]);
                ChildAccessionNbr = Conversions.ToString(withBlock["ChildAccessionNbr"]);
                DivisionId = Conversions.ToInteger(withBlock["DivisionId"]);
                EnterersLocationType = Conversions.ToString(withBlock["EnterersLocationType"]);
                EnterersLocation = Conversions.ToString(withBlock["EnterersLocation"]);
                EnterersLastName = Conversions.ToString(withBlock["EnterersLastName"]);
                EnterersFirstName = Conversions.ToString(withBlock["EnterersFirstName"]);
                AccountPriority = Conversions.ToString(withBlock["AccountPriority"]);
                if (!(withBlock["RequisitionDate"] is DBNull))
                {
                    m_requisitionDate = Conversions.ToDate(withBlock["RequisitionDate"]);
                }
                MetaData = Conversions.ToString(withBlock["MetaData"]);
            }

            FlagClean(); // 'Force flag clean here to load concurrency object asap.

            // Patient
            if (dt[1].Rows.Count > 0)
            {
                m_patient = new OrderPatient(this, loadAudit);
                m_patient.Load(dt[1].Rows[0]);
            }

            // Physician
            if (Conversions.ToBoolean(dt[2].Rows.Count))
            {
                m_physician = new OrderPhysician(this, OrderPhysicianType.Primary);
                m_physician.Load(dt[2].Rows[0]);
            }

            // Physicians
            m_physicians = new OrderPhysicians(this);
            m_physicians.Load(dt[3]);

            // Guarantor
            if (dt[4].Rows.Count > 0)
            {
                m_guarantor = new OrderGuarantor(this);
                m_guarantor.Load(dt[4].Rows[0]);
            }

            // Tests
            m_tests = new OrderTests(this);
            m_tests.Load(dt[5]);

            // Specimens
            m_specimens = new OrderSpecimens(this);
            m_specimens.Load(dt[6]);

            // AOEs
            m_aoes = new OrderAnswers(this);
            m_aoes.Load(dt[7]);

            // ICD9s
            m_icd9s = new OrderICD9s(this);
            m_icd9s.Load(dt[8]);

            // Insurance
            m_insurances = new OrderPatientInsurances(this);
            m_insurances.Load(dt[9]);

            if (dt[10].Rows.Count > 0)
            {
                foreach (DataRow r in dt[10].Rows)
                {

                    var o = m_insurances.Find(Conversions.ToInteger(r["OrderPatientInsuranceId"]));
                    var a = new OrderPatientInsuranceAnswer(o, r);
                    o.AOEs.Add(a);

                }
            }

            // OrderComments
            OrderComment oc;
            m_orderComments = new OrderComments(this);
            if (dt.Length >= 10)
            {
                foreach (DataRow r in dt[11].Rows)
                {
                    oc = new OrderComment(this, loadAudit);
                    oc.Load(r);
                    if (!(bool)r["IsDeleted"])
                    {
                        m_orderComments.List.Add(oc);
                    }
                    else
                    {
                        m_orderComments.List.AddToArchiveDeleteList(oc);
                    }
                }
            }

            FlagClean();

        }

        #endregion

        #region Update Demographics Private members

        private void UpdateBasedOnDemographicsAnalyte(Report report, ReportAnalyte a, bool reRelease)
        {
            if (Configuration.LISSettings.GetList("BypassCatOnDemoUpdate").Contains(a.Analyte.Category.ToUpper()))
            {
                return;
            }

            if (report.HasCalcAnalytes)
            {
                a.PerformCalculation(report, false);
            }

            if (a.HasBeenReleased())
            {

                // If reRelease Then
                // m_updateStatus.m_testCodesRereleased.Add(a.Code)
                // a.MarkAsReleased()
                // End If
                FlagResult prevFlagResult, currentFlagResult;
                bool sameFlagComments;
                m_prevReferenceRange = a.GetReferenceRange(m_patient.PrevGender, m_patient.PrevDateOfBirth, m_patient.PrevAgeNumber, m_patient.PrevAgeType);
                prevFlagResult = a.GetFlagFromRanges(m_patient.PrevGender, m_patient.PrevDateOfBirth, m_patient.PrevAgeNumber, m_patient.PrevAgeType);
                m_prevFlagValue = Conversions.ToString(prevFlagResult.FlagValue);
                m_prevReleaseDate = Conversions.ToDate(a.ReleaseDate.ToString("MM/dd/yyyy"));

                if (AnalyteChangeRanges(a))
                {
                    m_currentReferenceRange = a.GetReferenceRange(m_patient.Gender, m_patient.DateOfBirth, m_patient.AgeNumber, m_patient.AgeType);
                    currentFlagResult = a.GetFlagFromRanges(m_patient.Gender, m_patient.DateOfBirth, m_patient.AgeNumber, m_patient.AgeType);
                    m_currentFlagValue = Conversions.ToString(currentFlagResult.FlagValue);
                    sameFlagComments = HasSameFlagComments(prevFlagResult, currentFlagResult);
                    // If (m_prevFlagValue <> m_currentFlagValue OrElse m_prevReferenceRange <> m_currentReferenceRange) AndAlso (Not a.IsPresumptiveHold) Then
                    if (((m_prevFlagValue ?? "") != (m_currentFlagValue ?? "") || (m_prevReferenceRange ?? "") != (m_currentReferenceRange ?? "") || !sameFlagComments) && !a.IsPresumptiveHold)
                    {
                        if (!a.ResultValue.Equals(""))
                        {
                            a.DoAutoComments();
                        }
                        if (!IsReportHoldDirty)
                        {
                            a.ResultStatus = resultStatusType.Corrected;
                            var rc = a.Comments.AddComment(FormatDemographicUpdateCommentForAnalyte(Conversions.ToString(m_prevReleaseDate), m_prevReferenceRange, m_prevFlagValue, m_currentReferenceRange, m_currentFlagValue, sameFlagComments), "", true, "", Common.TestMaster.ExternalCommentType.Comment);
                            rc.Priority = 1;
                        }
                    }
                }
                if (reRelease)
                {
                    m_updateStatus.m_testCodesRereleased.Add(a.Code);
                    a.MarkAsReleased();
                }
            }

        }

        private bool HasSameFlagComments(FlagResult prevFlagResult, FlagResult currentFlagResult)
        {
            bool returnValue = true;
            if (prevFlagResult.Comments.Count == currentFlagResult.Comments.Count)
            {
                int count = 0;
                foreach (DefaultComment comment in prevFlagResult.Comments)
                {
                    if ((comment.Text ?? "") != (currentFlagResult.Comments[count].Text ?? ""))
                    {
                        returnValue = false;
                        break;
                    }
                    count += 1;
                }
            }
            else if (prevFlagResult.Comments.Count != 0)
            {
                returnValue = false;
            }
            return returnValue;
        }

        private string FormatDemographicUpdateCommentForAnalyte(string pReleaseDate, string pRefRange, string pFlag, string cReferenceRange, string cFlagValue, bool sameFlagComments)
        {
            string changedProperty, changedValuesString, changedValue, returnValue;
            string commenttemplate;
            changedProperty = GetChangedPropertyString();
            if (sameFlagComments)
            {
                commenttemplate = Configuration.LISSettings.GetString("DemographicUpdateAutoComment");
                changedValuesString = GetChangedValuesString(pRefRange, pFlag, cReferenceRange, cFlagValue);
                changedValue = GetValuesStringSecond(pRefRange, pFlag, cReferenceRange, cFlagValue);
                returnValue = Conversions.ToString(WordWrap(string.Format(commenttemplate, changedProperty, changedValuesString, changedValue, pReleaseDate)));
            }
            else
            {
                commenttemplate = Configuration.LISSettings.GetString("DemoRefRangeTblChangeAutoComment");
                returnValue = Conversions.ToString(WordWrap(string.Format(commenttemplate, changedProperty, pReleaseDate)));
            }
            return returnValue;

        }

        public object WordWrap(string text, int maxLineLen = 71) // Reporting Has Limit of 71 Characters Per Line
        {
            if (string.IsNullOrEmpty(text) || Strings.Len(text) < maxLineLen)
            {
                return text;
            }
            int i;
            var loopTo = (int)Math.Round(Strings.Len(text) / (double)maxLineLen);
            for (i = 1; i <= loopTo; i++)
                text = Strings.Mid(text, 1, maxLineLen * i - 1) + Strings.Replace(text, " ", Constants.vbCrLf, maxLineLen * i, 1, Constants.vbTextCompare);
            return text;
        }

        private string GetChangedPropertyString()
        {
            string returnValue = string.Empty;
            bool Dobchk = m_updateStatus.UpdatedProperties.Contains("OrderPatient.AgeNumber") || m_updateStatus.UpdatedProperties.Contains("OrderPatient.AgeType") || m_updateStatus.UpdatedProperties.Contains("OrderPatient.DateOfBirth");

            if (Dobchk & m_updateStatus.UpdatedProperties.Contains("OrderPatient.Gender"))
            {
                returnValue = "DOB and Gender";
            }
            else if (Dobchk)
            {
                returnValue = "DOB";
            }
            else if (m_updateStatus.UpdatedProperties.Contains("OrderPatient.Gender"))
            {
                returnValue = "Gender";
            }
            return returnValue;
        }

        private string GetChangedValuesString(string pReferenceRange, string pFlagValue, string cReferenceRange, string cFlagValue)
        {
            string returnValue = string.Empty;
            if ((pReferenceRange ?? "") != (cReferenceRange ?? "") && (pFlagValue ?? "") != (cFlagValue ?? ""))
            {
                returnValue = "reference range and flagging";
            }
            else if ((pReferenceRange ?? "") != (cReferenceRange ?? ""))
            {
                returnValue = "reference range";
            }
            else if ((pFlagValue ?? "") != (cFlagValue ?? ""))
            {
                returnValue = "flagging";
            }
            return returnValue;
        }

        private string GetValuesStringSecond(string pReferenceRange, string pFlagValue, string cReferenceRange, string cFlagValue)
        {
            string returnValue = string.Empty;

            if ((pReferenceRange ?? "") != (cReferenceRange ?? "") && (cFlagValue ?? "") != (pFlagValue ?? ""))
            {
                returnValue = Conversions.ToString(Operators.AddObject("reference range of " + pReferenceRange + " and ", Interaction.IIf(string.IsNullOrEmpty(pFlagValue.Trim()), "normal flagging", "flagging of " + pFlagValue)));
            }
            else if ((pReferenceRange ?? "") != (cReferenceRange ?? ""))
            {
                returnValue = "reference range of " + pReferenceRange;
            }
            else if ((pFlagValue ?? "") != (cFlagValue ?? ""))
            {
                returnValue = Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(pFlagValue.Trim()), "normal flagging", "flagging of " + pFlagValue));
            }
            return returnValue;
        }

        private void UpdateBasedOnDemographicsPanel(Report report, ReportAnalytePanel p, bool reRelease)
        {

            foreach (ReportAnalyte an in p.Analytes.List)
            {
                if (Configuration.LISSettings.GetList("BypassCatOnDemoUpdate").Contains(an.Analyte.Category.ToUpper()))
                {
                    return;
                }
            }

            if (report.HasCalcAnalytes)
            {
                foreach (ReportAnalyte an in p.Analytes.List)
                    an.PerformCalculation(report, false);
            }

            if (p.HasBeenReleased())
            {

                // If reRelease Then
                // m_updateStatus.m_testCodesRereleased.Add(p.PanelCode)
                // If p.IsPreliminaryReleased Then
                // p.MarkAsPreliminaryReleased()
                // Else
                // p.MarkAsReleased()
                // End If
                // End If

                foreach (ReportAnalyte an in p.Analytes.List)
                {
                    FlagResult prevFlagResult, currentFlagResult;
                    bool sameFlagComments;
                    m_prevReferenceRange = an.GetReferenceRange(m_patient.PrevGender, m_patient.PrevDateOfBirth, m_patient.PrevAgeNumber, m_patient.PrevAgeType);
                    prevFlagResult = an.GetFlagFromRanges(m_patient.PrevGender, m_patient.PrevDateOfBirth, m_patient.PrevAgeNumber, m_patient.PrevAgeType);
                    m_prevFlagValue = Conversions.ToString(prevFlagResult.FlagValue);
                    m_prevReleaseDate = Conversions.ToDate(an.ReleaseDate.ToString("MM/dd/yyyy"));
                    if (AnalyteChangeRanges(an))
                    {
                        m_currentReferenceRange = an.GetReferenceRange(m_patient.Gender, m_patient.DateOfBirth, m_patient.AgeNumber, m_patient.AgeType);
                        currentFlagResult = an.GetFlagFromRanges(m_patient.Gender, m_patient.DateOfBirth, m_patient.AgeNumber, m_patient.AgeType);
                        m_currentFlagValue = Conversions.ToString(currentFlagResult.FlagValue);
                        sameFlagComments = HasSameFlagComments(prevFlagResult, currentFlagResult);
                        // If (m_prevFlagValue <> m_currentFlagValue OrElse m_prevReferenceRange <> m_currentReferenceRange) AndAlso (Not p.IsPresumptiveHold) Then
                        if (((m_prevFlagValue ?? "") != (m_currentFlagValue ?? "") || (m_prevReferenceRange ?? "") != (m_currentReferenceRange ?? "") || !sameFlagComments) && !p.IsPresumptiveHold)
                        {
                            if (!an.ResultValue.Equals(""))
                            {
                                an.DoAutoComments();
                            }
                            if (!p.IsPreliminaryReleased && !IsReportHoldDirty && !an.IsTestHold)
                            {
                                an.ResultStatus = resultStatusType.Corrected;
                                string analyteTag = an.AnalyteName + " [" + an.Code + "]";
                                string formattedComment = FormatDemographicUpdateCommentForPanel(Conversions.ToString(m_prevReleaseDate), m_prevReferenceRange, m_prevFlagValue, m_currentReferenceRange, m_currentFlagValue, analyteTag, sameFlagComments);
                                var rc = an.Comments.AddComment(formattedComment, "", true, "", Common.TestMaster.ExternalCommentType.Comment, isDemographicComment: true);
                                rc.Priority = 1;
                            }
                        }
                    }

                }

                if (reRelease)
                {
                    m_updateStatus.m_testCodesRereleased.Add(p.PanelCode);
                    if (p.IsPreliminaryReleased)
                    {

                        p.MarkAsPreliminaryReleased();
                    }
                    else
                    {
                        p.MarkAsReleased();
                    }
                }

            }

        }

        private string FormatDemographicUpdateCommentForPanel(string pReleaseDate, string pRefRange, string pFlag, string cReferenceRange, string cFlagValue, string cTestName, bool sameFlagComments)
        {
            string changedProperty, changedValuesString, changedValue, returnValue;
            changedProperty = GetChangedPropertyString();
            string commenttemplate = string.Empty;
            if (sameFlagComments)
            {
                commenttemplate = Configuration.LISSettings.GetString("DemographicUpdateAutoCommentForPanel");
                changedValuesString = GetChangedValuesString(pRefRange, pFlag, cReferenceRange, cFlagValue);
                changedValue = GetValuesStringSecond(pRefRange, pFlag, cReferenceRange, cFlagValue);
                returnValue = Conversions.ToString(WordWrap(string.Format(commenttemplate, changedProperty, changedValuesString, cTestName, changedValue, pReleaseDate)));
            }
            else
            {
                commenttemplate = Configuration.LISSettings.GetString("DemoRefRangeTblChangeAutoCommentPanel");
                returnValue = Conversions.ToString(WordWrap(string.Format(commenttemplate, changedProperty, pReleaseDate)));
            }
            return returnValue;
        }

        private void UpdatesBasedOnDemographics(Report report, bool reRelease)
        {

            ReportAnalyte a;
            // 'Replace foreach - MarkAsRelease may trigger a rule to fire that adds new analytes, causing an enum error.
            for (int i = 0, loopTo = report.Analytes.List.Count - 1; i <= loopTo; i++)
            {
                a = report.Analytes.List[i];
                UpdateBasedOnDemographicsAnalyte(report, a, reRelease);
            }
            ReportAnalytePanel p;
            // 'Replace foreach - MarkAsRelease may trigger a rule to fire that adds new panels, causing an enum error.
            for (int i = 0, loopTo1 = report.AnalytePanels.List.Count - 1; i <= loopTo1; i++)
            {
                p = report.AnalytePanels.List[i];
                UpdateBasedOnDemographicsPanel(report, p, reRelease);
            }


            // 'Update deleted - just in case they are undeleted. Can't do on undelete because demo change is not known
            for (int i = 0, loopTo2 = report.Analytes.List.ArchivedDeletedList.Count - 1; i <= loopTo2; i++)
            {
                a = (ReportAnalyte)report.Analytes.List.ArchivedDeletedList[i];
                UpdateBasedOnDemographicsAnalyte(report, a, false);
            }
            for (int i = 0, loopTo3 = report.AnalytePanels.List.ArchivedDeletedList.Count - 1; i <= loopTo3; i++)
            {
                p = (ReportAnalytePanel)report.AnalytePanels.List.ArchivedDeletedList[i];
                UpdateBasedOnDemographicsPanel(report, p, false);
            }

            // If m_updateStatus.m_testCodesRereleased.Count > 0 Then
            // r.Save()
            // End If

            // When DOB changes, trigger 4K recalculation
            if (m_updateStatus.UpdatedProperties.Contains("OrderPatient.DateOfBirth") || m_updateStatus.UpdatedProperties.Contains("OrderPatient.AgeNumber") || m_updateStatus.UpdatedProperties.Contains("OrderPatient.AgeType") || m_updateStatus.UpdatedProperties.Contains("Order.DateOfCollection"))
            {

                Recalculate4KResults(report);

            }
        }


        /// <summary>
    /// If 4K Score test exists then recalculate 4K results
    /// </summary>
        private void Recalculate4KResults(Report report)
        {
            var recalcBypass = Configuration.LISSettings.GetList("B24KRecalcBypass");
            if (AccessionNbr.Trim().Length == 9 && recalcBypass.Contains(AccessionNbr.Substring(0, 2)))
            {
                return;
            }
            string[] testCodes = Configuration.B24KSettings.GetString("4k_score_code").Split(',');
            ReportAnalytePanel panel;
            foreach (string tst in testCodes)
            {
                var analyte = report.FindAnalyte(tst, true);
                if (analyte is not null)
                {
                    // analyte.SetResultValue("", True)    'reset the value
                    report.PerformCalculation("4K");
                    if (analyte.Parent is ReportAnalytePanel)
                    {
                        panel = (ReportAnalytePanel)analyte.Parent;
                        panel.MarkAsReleased();
                    }
                    return;
                }
            }
        }


        private bool AnalyteChangeRanges(ReportAnalyte a)
        {

            if (a.Analyte.ResultFlagRanges.Length > 0)
            {
                string dCode = Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(a.PerformingFacility), OrderManager.DefaultDivisionCodes[0], a.PerformingFacility));

                foreach (ComplexResultFlag c in a.Analyte.ResultFlagRanges)
                {
                    if ((c.DivisionCode ?? "") == (dCode ?? ""))
                    {
                        if (c.UseAgeQualifier && (m_updateStatus.UpdatedProperties.Contains("OrderPatient.DateOfBirth") || m_updateStatus.UpdatedProperties.Contains("OrderPatient.AgeNumber") || m_updateStatus.UpdatedProperties.Contains("OrderPatient.AgeType")))
                        {
                            return true;
                            continue;
                        }
                        if (m_updateStatus.UpdatedProperties.Contains("OrderPatient.Gender") && c.Gender != genderFlagType.Both && (int)c.Gender != (int)Patient.Gender)
                        {
                            return true;
                        }
                    }
                }
                foreach (ComplexResultFlag c in a.Analyte.ResultFlagRanges)
                {
                    foreach (string div in OrderManager.DefaultDivisionCodes)
                    {
                        if ((div ?? "") != (dCode ?? ""))
                        {
                            if ((c.DivisionCode ?? "") == (div ?? ""))
                            {
                                if (c.UseAgeQualifier && (m_updateStatus.UpdatedProperties.Contains("OrderPatient.DateOfBirth") || m_updateStatus.UpdatedProperties.Contains("OrderPatient.AgeNumber") || m_updateStatus.UpdatedProperties.Contains("OrderPatient.AgeType")))
                                {
                                    return true;
                                    continue;
                                }
                                if (m_updateStatus.UpdatedProperties.Contains("OrderPatient.Gender") && c.Gender != genderFlagType.Both && (int)c.Gender != (int)Patient.Gender)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;

        }

        #endregion

        #region RuleEngine Evaluation

        public event EvaluateRulesDelegate EvaluateRulesCompleted;

        internal void EvaluateRules(object triggerObj, string propertyName)
        {

            m_evalReport = true; // Evaluate the Report - done on Order save.

        }

        void IEvaluateRule.EvaluateRules(object triggerObj, string propertyName) => EvaluateRules(triggerObj, propertyName);

        #endregion

        #region Internal Criteria Class

        [Serializable()]
        internal class Criteria
        {

            private long m_orderid;
            private long m_spmOrderId;
            private string m_accessionNbr;
            private int m_referenceLabId;
            private DateTime m_dateOfService;
            private bool m_loadAudit;

            internal Criteria(long orderID, string accessionNbr, DateTime dateOfService, bool loadAudit)
            {
                m_orderid = orderID;
                m_spmOrderId = 0L;
                m_accessionNbr = accessionNbr;
                m_referenceLabId = 0;
                m_dateOfService = dateOfService;
                m_loadAudit = loadAudit;
            }

            internal Criteria(long orderID, string accessionNbr, int refLabId, bool loadAudit)
            {
                m_orderid = orderID;
                m_spmOrderId = 0L;
                m_accessionNbr = accessionNbr;
                m_referenceLabId = refLabId;
                m_loadAudit = loadAudit;
            }

            internal Criteria(long orderId, string accessionNbr, DateTime dateOfService, long spmOrderId, bool loadAudit)
            {
                m_orderid = orderId;
                m_spmOrderId = spmOrderId;
                m_accessionNbr = accessionNbr;
                m_dateOfService = dateOfService;
                m_loadAudit = loadAudit;
            }

            internal long OrderID
            {
                get
                {
                    return m_orderid;
                }
            }

            internal long SPMOrderId
            {
                get
                {
                    return m_spmOrderId;
                }
            }

            internal string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }

            internal int ReferenceLabId
            {
                get
                {
                    return m_referenceLabId;
                }
            }

            internal DateTime DateOfService
            {
                get
                {
                    return m_dateOfService;
                }
            }

            internal bool LoadAudit
            {
                get
                {
                    return m_loadAudit;
                }
            }

        }

        #endregion

        #region IDisposeable Implementation

        private bool Disposed = false;
        private string m_metaData;

        protected new void Dispose(bool disposing)
        {

            if (!Disposed)
            {

                if (disposing)
                {

                    // Dispose managed resources.
                    if (!(m_patient == null))
                    {
                        m_patient.Dispose();
                        m_patient = null;
                    }

                    // Call the appropriate methods to clean up 
                    // unmanaged resources here.
                    // If disposing is false, 
                    // only the following code is executed.
                    Dispose();

                }

            }

            Disposed = true;

        }

        #endregion


        public bool IsRestrictedAccession()
        {
            bool returnval = false;
            if (!string.IsNullOrEmpty(MetaData))
            {
                var metaDataList = JsonConvert.DeserializeObject<List<OrderMetaData>>(MetaData);
                foreach (OrderMetaData mdata in metaDataList)
                {
                    if (mdata.Name == "IsRestrictedAccession")
                    {
                        returnval = Conversions.ToBoolean(Interaction.IIf(string.IsNullOrEmpty(mdata.Value), false, Conversions.ToBoolean(mdata.Value)));
                    }
                }
            }

            return returnval;
        }

        public bool IsHospitalAccount()
        {
            bool returnval = false;
            if (AccountPriority == "HI" | AccountPriority == "H")
            {
                returnval = true;
            }
            return returnval;
        }

    }
}