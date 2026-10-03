using System;
using System.Collections.Generic;
using System.Data;
using Bioreference.Common.Client;
using Bioreference.Data;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class Pending : DataClassReadOnlyBase
    {

        #region Private Members

        private DateTime m_dateTime;
        private string m_accessionNbr;
        private string m_patientName;
        private string m_acctNbr;
        private OrderPriority m_priority = OrderPriority.Routine;
        private clientType m_clientType = clientType.None;
        private MarketType m_marketType = MarketType.Unknown;
        private string m_accountPriority = "";
        private List<TestInfo> m_testCodes = new List<TestInfo>();
        internal bool m_hasReportable = false;
        internal bool m_hasPending = false;
        private DateTime m_lastAddOn;
        private bool m_hasAddOn;
        private DateTime m_dateOfService;
        private DateTime m_dateOfCollection;
        private int m_reportId;
        private bool m_isClinicalTrial = false;
        private bool m_IsTestHold = false;
        private int m_rackWorksheetId;
        private DateTime m_requisitionDate = DateTime.Parse("1900-01-01");
        private resultStatusType m_resultStatus;
        private string m_specimenSummary;
        private List<string> m_listOfDepartmentShortName = new List<string>();

        #endregion

        #region Public Properties

        public DateTime DateOfService
        {
            get
            {
                return m_dateOfService;
            }
        }

        public DateTime DateCreated
        {
            get
            {
                return m_dateTime;
            }
        }

        public DateTime DateCollected
        {
            get
            {
                return m_dateOfCollection;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public string PatientName
        {
            get
            {
                return m_patientName;
            }
        }

        public string AccountNumber
        {
            get
            {
                return m_acctNbr;
            }
        }

        public int RackWorksheetId
        {
            get
            {
                return m_rackWorksheetId;
            }
        }
        public OrderPriority Priority
        {
            get
            {
                return m_priority;
            }
        }

        public clientType ClientType
        {
            get
            {
                return m_clientType;
            }
        }

        public MarketType MarketType
        {
            get
            {
                return m_marketType;
            }
        }

        public string AccountPriority
        {
            get
            {
                return m_accountPriority;
            }
        }

        public TestInfo[] TestCodes
        {
            get
            {
                return m_testCodes.ToArray();
            }
        }

        public bool HasReportable
        {
            get
            {
                return m_hasReportable;
            }
        }

        public bool HasPending
        {
            get
            {
                return m_hasPending;
            }
        }

        public DateTime LastAddOnDate
        {
            get
            {
                return m_lastAddOn;
            }
        }

        public bool HasAddOn
        {
            get
            {
                return m_hasAddOn;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }

        public bool IsClinicalTrial
        {
            get
            {
                return m_isClinicalTrial;
            }
        }

        public DateTime RequisitionDate
        {
            get
            {
                return m_requisitionDate;
            }
        }

        public resultStatusType ResultStatus
        {
            get
            {
                return m_resultStatus;
            }
        }

        public string SpecimenSummary
        {
            get
            {
                return m_specimenSummary;
            }
        }

        public List<string> DepartmentShortName
        {
            get
            {
                return m_listOfDepartmentShortName;
            }
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_dateTime = Conversions.ToDate(row["OrderCreateDate"]);
            m_accessionNbr = Conversions.ToString(row["accessionnbr"]);
            m_patientName = Conversions.ToString(row["PatientName"]);
            m_acctNbr = Conversions.ToString(row["AccountNumber"]);
            m_rackWorksheetId = Conversions.ToInteger(row["RackWorksheetId"]);
            m_priority = (OrderPriority)Conversions.ToInteger(row["Priority"]);
            m_clientType = (clientType)Conversions.ToInteger(row["ClientType"]);
            m_marketType = (MarketType)Conversions.ToInteger(row["MarketType"]);
            m_accountPriority = Conversions.ToString(row["AccountPriority"]);
            m_dateOfService = Conversions.ToDate(row["DateServiced"]);
            m_dateOfCollection = Conversions.ToDate(row["DateCollected"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);
            m_isClinicalTrial = Conversions.ToBoolean(row["IsClinicalTrial"]);
            m_IsTestHold = Conversions.ToBoolean(row["IsTestHold"]);
            if ((int)m_clientType == 1 && string.IsNullOrEmpty(m_accountPriority))
            {
                m_accountPriority = "BB";
            }
            if (!(row["RequisitionDate"] is DBNull))
            {
                m_requisitionDate = Conversions.ToDate(row["RequisitionDate"]);
            }
            m_resultStatus = (resultStatusType)Conversions.ToInteger(row["ResultStatus"]);
            m_specimenSummary = Conversions.ToString(row["SpecimenSummary"]).NormalizeToWindows();
        }

        #endregion

        #region Public Methods

        public TestInfo FindTest(string testCode)
        {

            foreach (TestInfo t in m_testCodes)
            {
                if ((t.TestCode ?? "") == (testCode ?? ""))
                    return t;
            }
            return null;

        }

        internal void AddTest(TestInfo test)
        {

            m_testCodes.Add(test);
            if (test.CreatedDate.AddHours(-1) > DateCreated)
            {
                test.SetIsAddOn(true);
                m_hasAddOn = true;
            }
            if (test.CreatedDate > m_lastAddOn)
                m_lastAddOn = test.CreatedDate;


        }

        #endregion

        #region TestInfo Class

        [Serializable()]
        public class TestInfo
        {

            private string m_testCode;
            private string m_resultValue;
            private string m_rapidResultId;
            private transmitStatusType m_transmitStatus = transmitStatusType.None;
            private resultStatusType m_resultStatus = resultStatusType.Pending;
            private DateTime m_createdDate;
            private bool m_isAddOn = false;
            private bool m_isReflex = false;
            private bool m_isReportable = true;
            private string m_performingLocation = "";
            private string m_rapidResultIdList;
            private bool m_isTestHold = false;
            private DateTime m_dateServiced;
            private SampleRequestType m_sampleStatus;
            private DateTime m_releaseDate;
            private int m_parentType;
            private string m_DepartmentShortName;

            public TestInfo(string testCode, string resultValue, string rapidResultId, transmitStatusType transmitStatus, DateTime createdDate, bool isReportable, string performingLocation, resultStatusType resultStatus, bool isTestHold, DateTime dateServiced, bool isReflex, SampleRequestType sampleStatus, DateTime releaseDate, int ParentType, string DepartmentShortName) : this(testCode, resultValue, rapidResultId, transmitStatus, createdDate, isReportable, performingLocation, resultStatus, "", isTestHold, dateServiced, isReflex, sampleStatus, releaseDate, ParentType, DepartmentShortName)
            {

                // m_testCode = testCode
                // m_resultValue = resultValue
                // m_rapidResultId = rapidResultId
                // m_transmitStatus = transmitStatus
                // m_createdDate = createdDate
                // m_isReportable = isReportable
                // m_performingLocation = IIf(performingLocation <> "", performingLocation, OrderManager.DefaultDivisionCodes(0))
                // m_resultStatus = resultStatus
            }

            public TestInfo(string testCode, string resultValue, string rapidResultId, transmitStatusType transmitStatus, DateTime createdDate, bool isReportable, string performingLocation, resultStatusType resultStatus, string rapidResultIdList, bool isTestHold, DateTime dateServiced, bool isReflex, SampleRequestType sampleStatus, DateTime releaseDate, int ParentType, string DepartmentShortName)
            {
                m_testCode = testCode;
                m_resultValue = resultValue;
                m_rapidResultId = rapidResultId;
                m_transmitStatus = transmitStatus;
                m_createdDate = createdDate;
                m_isReportable = isReportable;
                m_performingLocation = Conversions.ToString(Interaction.IIf(!string.IsNullOrEmpty(performingLocation), performingLocation, OrderManager.DefaultDivisionCodes[0]));
                m_resultStatus = resultStatus;
                m_rapidResultIdList = rapidResultIdList;
                m_isTestHold = isTestHold;
                m_dateServiced = dateServiced;
                m_sampleStatus = sampleStatus;
                m_isReflex = isReflex;
                m_releaseDate = releaseDate;
                m_parentType = ParentType;
                m_DepartmentShortName = DepartmentShortName;
            }

            public resultStatusType ResultStatus
            {
                get
                {
                    return m_resultStatus;
                }
            }
            public bool IsReportable
            {
                get
                {
                    return m_isReportable;
                }
            }
            public string TestCode
            {
                get
                {
                    return m_testCode;
                }
            }
            public string ResultValue
            {
                get
                {
                    return m_resultValue;
                }
            }
            public string RapidResultId
            {
                get
                {
                    return m_rapidResultId;
                }
            }
            public transmitStatusType TransmitStatus
            {
                get
                {
                    return m_transmitStatus;
                }
            }
            public DateTime CreatedDate
            {
                get
                {
                    return m_createdDate;
                }
            }
            public bool IsAddOn
            {
                get
                {
                    return m_isAddOn;
                }
            }
            public bool IsReflex
            {
                get
                {
                    return m_isReflex;
                }
            }
            public string PerformingLocation
            {
                get
                {
                    return m_performingLocation;
                }
            }

            internal void SetIsAddOn(bool @bool)
            {
                m_isAddOn = @bool;
            }
            public string RapidResultIdList
            {
                get
                {
                    return m_rapidResultIdList;
                }
            }
            public bool IsTestHold
            {
                get
                {
                    return m_isTestHold;
                }
            }
            public DateTime DateServiced
            {
                get
                {
                    return m_dateServiced;
                }
            }
            public SampleRequestType SampleStatus
            {
                get
                {
                    return m_sampleStatus;
                }
            }

            public DateTime ReleaseDate
            {
                get
                {
                    return m_releaseDate;
                }
            }

            public int ParentType
            {
                get
                {
                    return m_parentType;
                }
            }

            public string DepartmentShortName
            {
                get
                {
                    return m_DepartmentShortName;
                }
            }

        }


        #endregion

    }
}