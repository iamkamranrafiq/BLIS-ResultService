using Bioreference.Data;
using Bioreference.Data.Client;
using log4net;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Bioreference.LIS
{
    [Serializable()]
    public class QueueOrderIn : DataClassReadOnlyBase
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private List<QueueItemB2> m_queueItems;
        private List<QueueItemDetailB2> m_queueItemDetails;

        private QueueOrderIn()
        {
            m_queueItems = new List<QueueItemB2>();
            m_queueItemDetails = new List<QueueItemDetailB2>();
        }

        public QueueItemB2[] QueueItems
        {
            get
            {
                return m_queueItems.ToArray();
            }
        }

        public QueueItemDetailB2[] QueueItemDetails
        {
            get
            {
                return m_queueItemDetails.ToArray();
            }
        }

        /// <summary>
        /// Fetch queue orders by Order ID
        /// </summary>
        public static QueueOrderIn FetchByOrderId(long orderId, int status)
        {
            return (QueueOrderIn)DataFactory.Fetch(new Criteria(orderId, "", "", 0, status, 0));
        }
        /// <summary>
        /// Fetch queue orders by History ID
        /// </summary>
        public static QueueOrderIn FetchByOrderHistoryId(long orderHistoryId, int status)
        {
            return (QueueOrderIn)DataFactory.Fetch(new Criteria(0, "", "", 0, status, orderHistoryId));
        }

        /// <summary>
        /// Fetch queue orders by Queue Item ID
        /// </summary>
        public static QueueOrderIn FetchByQueueItemId(long queueItemId)
        {
            return (QueueOrderIn)DataFactory.Fetch(new Criteria(0, "", "", queueItemId, 0, 0));
        }

        protected override void DataFactory_Fetch(object criteria)
        {
            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.AppSettings.GetConnectionString("Bioreference.Order.ConnectionString") ?? "");
            var paramList = new List<DbParameter>();
            string proc = "lis_Queue_B2_Fetch_By_Id";

            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int64, c.OrderId));
            paramList.Add((DbParameter)da.CreateParameter("@AccessionNumber", DbType.String, c.AccessionNumber ?? ""));
            paramList.Add((DbParameter)da.CreateParameter("@DateOfService", DbType.String, c.DateOfService ?? ""));
            paramList.Add((DbParameter)da.CreateParameter("@QueueItemId_B2", DbType.Int64, c.QueueItemId));
            paramList.Add((DbParameter)da.CreateParameter("@status", DbType.Int32, c.Status));
            paramList.Add((DbParameter)da.CreateParameter("@OrderHistoryId", DbType.Int64, c.OrderHistoryId));

            Log.Debug(SharedFunctions.BuildDBLoggingMessage(proc, paramList));
            var start = DateTime.Now;

            DataTable[] dt = da.ExecuteProcedure(proc, paramList.ToArray());

            var elapsed = DateTime.Now.Subtract(start);

            Log.DebugFormat("exec {0} Elapsed Time: {1:0.000} sec.", proc, elapsed.TotalSeconds);

            if (dt.Length > 0)
            {
                LoadQueueItems(dt[0]);
            }

            if (dt.Length > 1)
            {
                LoadQueueItemDetails(dt[1]);
            }
        }
        private static T SafeGetValue<T>(DataRow row, string columnName, T defaultValue)
        {
            try
            {
                if (row[columnName] == DBNull.Value)
                    return defaultValue;

                object value = row[columnName];

                if (value == null)
                    return defaultValue;

                // If the type matches, return as-is
                if (value.GetType() == typeof(T))
                    return (T)value;

                // Handle nullable types
                Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                // Special handling for string conversion
                if (targetType == typeof(string))
                    return (T)(object)value.ToString();

                // Use Convert for other type conversions
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return defaultValue;
            }
        }

        protected void LoadQueueItems(DataTable dt)
        {
            foreach (DataRow r in dt.Rows)
            {
                var queueItem = new QueueItemB2
                {
                    QueueItemId_B2 = SafeGetValue(r, "QueueItemId_B2", 0L),
                    AccessionNumber = SafeGetValue(r, "AccessionNumber", ""),
                    AccountNumber = SafeGetValue(r, "AccountNumber", ""),
                    DateServiced = SafeGetValue(r, "DateServiced", (DateTime?)null),
                    DateCollected = SafeGetValue(r, "DateCollected", (DateTime?)null),
                    PatientLastName = SafeGetValue(r, "PatientLastName", ""),
                    PatientFirstName = SafeGetValue(r, "PatientFirstName", ""),
                    PatientMiddleName = SafeGetValue(r, "PatientMiddleName", ""),
                    PatientGender = SafeGetValue(r, "PatientGender", ""),
                    PatientDOB = SafeGetValue(r, "PatientDOB", (DateTime?)null),
                    OrderPriority = SafeGetValue(r, "OrderPriority", ""),
                    PhyCode = SafeGetValue(r, "PhyCode", ""),
                    PhyLastName = SafeGetValue(r, "PhyLastName", ""),
                    PhyFirstName = SafeGetValue(r, "PhyFirstName", ""),
                    CreatedBy = SafeGetValue(r, "CreatedBy", ""),
                    CreatedDate = SafeGetValue(r, "CreatedDate", (DateTime?)null),
                    UpdatedBy = SafeGetValue(r, "UpdatedBy", ""),
                    UpdatedDate = SafeGetValue(r, "UpdatedDate", (DateTime?)null),
                    Active = SafeGetValue(r, "Active", false),
                    PatientAge = SafeGetValue(r, "PatientAge", (int?)null),
                    AccountPriority = SafeGetValue(r, "AccountPriority", ""),
                    AccountMarketType = SafeGetValue(r, "AccountMarketType", ""),
                    DivisionId = SafeGetValue(r, "DivisionId", (int?)null),
                    DemoUpdateTrackingID = SafeGetValue(r, "DemoUpdateTrackingID", ""),
                    ReleaseResultsFlag = SafeGetValue(r, "ReleaseResultsFlag", ""),
                    CallingApplication = SafeGetValue(r, "CallingApplication", ""),
                    TimezoneId_Client = SafeGetValue(r, "TimezoneId_Client", ""),
                    ReportHoldFlag = SafeGetValue(r, "ReportHoldFlag", ""),
                    StudyNumber = SafeGetValue(r, "StudyNumber", ""),
                    VisitNumber = SafeGetValue(r, "VisitNumber", ""),
                    EUID = SafeGetValue(r, "EUID", ""),
                    SPMOrderId = SafeGetValue(r, "SPMOrderId", (long?)null),
                    SPMParentOrderId = SafeGetValue(r, "SPMParentOrderId", (long?)null),
                    EnterersLocationType = SafeGetValue(r, "EnterersLocationType", ""),
                    EnterersLocation = SafeGetValue(r, "EnterersLocation", ""),
                    EnterersLastName = SafeGetValue(r, "EnterersLastName", ""),
                    EnterersFirstName = SafeGetValue(r, "EnterersFirstName", ""),
                    ParentAccount = SafeGetValue(r, "ParentAccount", ""),
                    SpecimenUpdated = SafeGetValue(r, "SpecimenUpdated", (DateTime?)null),
                    MetaData = SafeGetValue(r, "MetaData", ""),
                    OrderHistoryId = SafeGetValue(r, "OrderHistoryId", (long?)null)
                };

                m_queueItems.Add(queueItem);
            }
        }

        protected void LoadQueueItemDetails(DataTable dt)
        {
            foreach (DataRow r in dt.Rows)
            {
                var queueItemDetail = new QueueItemDetailB2
                {
                    QueueItemDetailId_B2 = SafeGetValue(r, "QueueItemDetailId_B2", 0L),
                    QueueItemId_B2 = SafeGetValue(r, "QueueItemId_B2", 0L),
                    AccessionNumber = SafeGetValue(r, "AccessionNumber", ""),
                    TestCode = SafeGetValue(r, "TestCode", ""),
                    TestName = SafeGetValue(r, "TestName", ""),
                    CreatedBy = SafeGetValue(r, "CreatedBy", ""),
                    CreatedDate = SafeGetValue(r, "CreatedDate", (DateTime?)null),
                    UpdatedBy = SafeGetValue(r, "UpdatedBy", ""),
                    UpdatedDate = SafeGetValue(r, "UpdatedDate", (DateTime?)null),
                    Active = SafeGetValue(r, "Active", false),
                    Flag = SafeGetValue(r, "Flag", ""),
                    InternalFlag = SafeGetValue(r, "InternalFlag", ""),
                    Status = SafeGetValue(r, "Status", (int?)null),
                    MessageId = SafeGetValue(r, "MessageId", ""),
                    ApprovedBy = SafeGetValue(r, "ApprovedBy", ""),
                    ApprovedDate = SafeGetValue(r, "ApprovedDate", (DateTime?)null),
                    ResolutionType = SafeGetValue(r, "ResolutionType", ""),
                    ResolutionDescr = SafeGetValue(r, "ResolutionDescr", ""),
                    Result = SafeGetValue(r, "Result", ""),
                    OrderedTestCode = SafeGetValue(r, "OrderedTestCode", ""),
                    OrderedTestName = SafeGetValue(r, "OrderedTestName", ""),
                    ResubmitCount = SafeGetValue(r, "ResubmitCount", (int?)null),
                    Comments = SafeGetValue(r, "Comments", ""),
                    DetailType = SafeGetValue(r, "DetailType", ""),
                    AccessioningFacility = SafeGetValue(r, "AccessioningFacility", ""),
                    PerformingFacility = SafeGetValue(r, "PerformingFacility", ""),
                    IsOnHold = SafeGetValue(r, "IsOnHold", false),
                    HoldCode = SafeGetValue(r, "HoldCode", ""),
                    TNPHoldDate = SafeGetValue(r, "TNPHoldDate", (DateTime?)null),
                    TNPPendingResult = SafeGetValue(r, "TNPPendingResult", ""),
                    TNPDelayedProcessedDate = SafeGetValue(r, "TNPDelayedProcessedDate", (DateTime?)null),
                    TestUniqueId = SafeGetValue(r, "TestUniqueId", ""),
                    ParentTestUniqueId = SafeGetValue(r, "ParentTestUniqueId", ""),
                    SPMOrderTestId = SafeGetValue(r, "SPMOrderTestId", (long?)null),
                    TestResultHoldFlag = SafeGetValue(r, "TestResultHoldFlag", ""),
                    IsPresumptiveTest = SafeGetValue(r, "IsPresumptiveTest", false)
                };

                m_queueItemDetails.Add(queueItemDetail);
            }
        }

        [Serializable()]
        internal class Criteria
        {
            private long m_orderId = 0;
            private string m_accessionNumber = "";
            private string m_dateOfService = "";
            private long m_queueItemId = 0;
            private int m_status = 0;
            private long m_orderHistoryId = 0;

            public Criteria(long orderId, string accessionNumber, string dateOfService, long queueItemId, int status, long orderHistoryId)
            {
                m_orderId = orderId;
                m_accessionNumber = accessionNumber;
                m_dateOfService = dateOfService;
                m_queueItemId = queueItemId;
                m_status = status;
                m_orderHistoryId = orderHistoryId;
            }

            public long OrderId
            {
                get { return m_orderId; }
            }

            public string AccessionNumber
            {
                get { return m_accessionNumber; }
            }

            public string DateOfService
            {
                get { return m_dateOfService; }
            }

            public long QueueItemId
            {
                get { return m_queueItemId; }
            }

            public int Status
            {
                get { return m_status; }
            }
            public long OrderHistoryId
            {
                get { return m_orderHistoryId; }
            }
        }
    }

    [Serializable()]
    public class QueueItemB2
    {
        public long QueueItemId_B2 { get; set; }
        public string AccessionNumber { get; set; }
        public string AccountNumber { get; set; }
        public DateTime? DateServiced { get; set; }
        public DateTime? DateCollected { get; set; }
        public string PatientLastName { get; set; }
        public string PatientFirstName { get; set; }
        public string PatientMiddleName { get; set; }
        public string PatientGender { get; set; }
        public DateTime? PatientDOB { get; set; }
        public string OrderPriority { get; set; }
        public string PhyCode { get; set; }
        public string PhyLastName { get; set; }
        public string PhyFirstName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool Active { get; set; }
        public int? PatientAge { get; set; }
        public string AccountPriority { get; set; }
        public string AccountMarketType { get; set; }
        public int? DivisionId { get; set; }
        public string DemoUpdateTrackingID { get; set; }
        public string ReleaseResultsFlag { get; set; }
        public string CallingApplication { get; set; }
        public string TimezoneId_Client { get; set; }
        public string ReportHoldFlag { get; set; }
        public string StudyNumber { get; set; }
        public string VisitNumber { get; set; }
        public string EUID { get; set; }
        public long? SPMOrderId { get; set; }
        public long? SPMParentOrderId { get; set; }
        public string EnterersLocationType { get; set; }
        public string EnterersLocation { get; set; }
        public string EnterersLastName { get; set; }
        public string EnterersFirstName { get; set; }
        public string ParentAccount { get; set; }
        public DateTime? SpecimenUpdated { get; set; }
        public string MetaData { get; set; }
        public long? OrderHistoryId { get; set; }
    }

    [Serializable()]
    public class QueueItemDetailB2
    {
        public long QueueItemDetailId_B2 { get; set; }
        public long QueueItemId_B2 { get; set; }
        public string AccessionNumber { get; set; }
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool Active { get; set; }
        public string Flag { get; set; }
        public string InternalFlag { get; set; }
        public int? Status { get; set; }
        public string MessageId { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ResolutionType { get; set; }
        public string ResolutionDescr { get; set; }
        public string Result { get; set; }
        public string OrderedTestCode { get; set; }
        public string OrderedTestName { get; set; }
        public int? ResubmitCount { get; set; }
        public string Comments { get; set; }
        public string DetailType { get; set; }
        public string AccessioningFacility { get; set; }
        public string PerformingFacility { get; set; }
        public bool IsOnHold { get; set; }
        public string HoldCode { get; set; }
        public DateTime? TNPHoldDate { get; set; }
        public string TNPPendingResult { get; set; }
        public DateTime? TNPDelayedProcessedDate { get; set; }
        public string TestUniqueId { get; set; }
        public string ParentTestUniqueId { get; set; }
        public long? SPMOrderTestId { get; set; }
        public string TestResultHoldFlag { get; set; }
        public bool IsPresumptiveTest { get; set; }
    }
}
