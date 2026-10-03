using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class Reports : DataClassBase
    {

        public enum processDataType
        {
            TestData = -1,
            LiveData = 1,
            All = 0
        }

        public enum ClinicalTrialFlag
        {
            NonClinicalTrial = 0,
            ClinicalTrial = 1,
            All = 2
        }

        #region Private Members

        private ReportList m_list;
        private List<string> m_outboundReports = new List<string>();
        private List<OutboundReport> m_outboundReportObjs = new List<OutboundReport>();

        #endregion

        #region Constructor

        private Reports()
        {
            m_list = new ReportList();
        }

        #endregion

        #region Public Properties

        public ReportList List
        {
            get
            {
                return m_list;
            }
        }

        #endregion

        #region Public Functions

        /// <summary>
    /// Returns reports with the specified analyteCode
    /// </summary>
    /// <param name="analyteCode"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static Reports Fetch(string analyteCode, transmitStatusType status, bool searchPanels = false)
        {

            return (Reports)DataFactory.Fetch(new Criteria(analyteCode, status, searchPanels, DateTime.Parse("1900-01-01")));

        }

        public static Reports Fetch(string analyteCode, transmitStatusType status, DateTime afterDate, bool searchPanels = false)
        {

            return (Reports)DataFactory.Fetch(new Criteria(analyteCode, status, searchPanels, afterDate));

        }

        public static Reports Fetch()
        {
            return (Reports)DataFactory.Fetch(new Criteria(transmitStatusType.None, 0, 0, 0));
        }

        public static Reports Fetch(transmitStatusType status, int filterId)
        {
            return (Reports)DataFactory.Fetch(new Criteria(status, filterId, 0, 0));
        }

        public static Reports Fetch(int externalAppId, transmitStatusType status, int outboundChannelId = 0)
        {
            return (Reports)DataFactory.Fetch(new Criteria(status, 0, externalAppId, outboundChannelId));
        }

        public static OutboundReport[] FetchOutbound(int returnCount, int externalAppId, bool peek, processDataType processDataType = processDataType.All, int totalChannelCount = 1, int currentChannelIndex = 0)

        {

            return ((Reports)DataFactory.Fetch(new Criteria(returnCount, peek, externalAppId, totalChannelCount, currentChannelIndex, processDataType))).m_outboundReportObjs.ToArray();

        }

        public static void OutboundLogDelete(List<int> outboundOrderQueueIds)
        {
            DataFactory.Delete(new Criteria(outboundOrderQueueIds));
        }     

        /// <summary>
    /// 
    /// </summary>
    /// <param name="filterId">Id from ReportFilter object.</param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static Reports Fetch(int filterId)
        {
            return (Reports)DataFactory.Fetch(new Criteria(transmitStatusType.NotSet, filterId, 0, 0));
        }

        internal static Reports Fetch(string accountNbr, string patientName, string accessionNbr, DateTime startDate, DateTime endDate, transmitStatusType transmitStatus, long euid, List<string> divisionCodes)
        {
            return (Reports)DataFactory.Fetch(new Criteria(accountNbr, accessionNbr, patientName, startDate, endDate, transmitStatus, euid, divisionCodes));
        }

        #endregion

        #region Data Functions
        protected override void DataFactory_Delete(object criteria)
        {
            if (!(criteria is Criteria c))
                return;

            List<int> outboundOrderQueueIds = c.OutboundOrderQueueIds;

            if (outboundOrderQueueIds == null || outboundOrderQueueIds.Count == 0)
                return;

            try
            {
                DataTable table = new DataTable();
                table.Columns.Add("OutboundOrderQueueId", typeof(int));

                foreach (int id in outboundOrderQueueIds.Distinct())
                {
                    table.Rows.Add(id);
                }

                using (SqlConnection connection = new SqlConnection(Configuration.ConnectionString))
                using (SqlCommand command = new SqlCommand("dbo.lis_ReportsOutbound_Delete", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter parameter = command.Parameters.Add("@Ids", SqlDbType.Structured);
                    parameter.TypeName = "dbo.OutboundOrderQueueIdList";
                    parameter.Value = table;

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.ToString());
                throw ex;
            }
        }
        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DataTable[] dt = null;
            List<DbParameter> @params;

            try
            {

                if (c.IsSearch)
                {
                    @params = new List<DbParameter>();
                    // If analyteCode is passed in, do an analyte Search
                    if (!string.IsNullOrEmpty(c.AnalyteCode))
                    {
                        @params.Add((DbParameter)da.CreateParameter("@TransmitStatus", DbType.Int32, c.Status));
                        @params.Add((DbParameter)da.CreateParameter("@AnalyteCode", DbType.String, c.AnalyteCode));
                        @params.Add((DbParameter)da.CreateParameter("@SearchPanel", DbType.Boolean, c.SearchPanel));
                        if (c.AnalyteCreatedAfter > DateTime.Parse("1900-01-01"))
                            @params.Add((DbParameter)da.CreateParameter("@AnalyteCreatedAfter", DbType.DateTime, c.AnalyteCreatedAfter));
                        dt = da.ExecuteProcedure("lis_ReportsAnalyteSearch_Fetch", @params.ToArray());
                        if (dt[0].Rows.Count > 0)
                        {
                            Load(dt[0], true);
                        }
                    }
                    else
                    {
                        @params.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNumber));
                        if (c.StartDate > DateTime.Parse("1900-01-01"))
                            @params.Add((DbParameter)da.CreateParameter("@StartDate", DbType.DateTime, c.StartDate));
                        if (c.EndDate > DateTime.Parse("1900-01-01"))
                            @params.Add((DbParameter)da.CreateParameter("@EndDate", DbType.DateTime, c.EndDate));
                        @params.Add((DbParameter)da.CreateParameter("@PatientName", DbType.String, c.PatientName));
                        @params.Add((DbParameter)da.CreateParameter("@AccountNbr", DbType.String, c.AccountNumber));
                        @params.Add((DbParameter)da.CreateParameter("@ReportStatus", DbType.Int32, c.Status));
                        @params.Add((DbParameter)da.CreateParameter("@EUID", DbType.Int64, c.EUID));
                        @params.Add((DbParameter)da.CreateParameter("@DivisionCodes", DbType.String, string.Join(",", c.DivisionCodes)));
                        dt = da.ExecuteProcedure("lis_ReportsSearch_Fetch", @params.ToArray());
                        if (dt[0].Rows.Count > 0)
                        {
                            Load(dt[0], false);
                        }
                    }
                }

                // ElseIf c.ForOutBoundStatus Then

                // Dim param(3) As DbParameter
                // param(0) = da.CreateParameter("@ReturnCount", DbType.Int32, c.ReturnCount)
                // param(1) = da.CreateParameter("@Peek", DbType.Int32, c.Peek)
                // param(2) = da.CreateParameter("@ExternalAppId", DbType.Int32, c.ExternalAppId)
                // param(3) = da.CreateParameter("@OddEven", DbType.Int16, c.OddEven)
                // dt = da.ExecuteProcedure("lis_StatusOutbound_Fetch", param)

                // If dt(0).Rows.Count > 0 Then
                // For Each r As DataRow In dt(0).Rows
                // Me.m_outboundReports.Add(r.Item("AccessionNbr"))
                // Next

                // End If
                else if (c.ReturnOutboundResultList)  // if fetching array for outbound
                {
                    @params = new List<DbParameter>();
                    @params.Add((DbParameter)da.CreateParameter("@ReturnCount", DbType.Int32, c.ReturnCount));
                    @params.Add((DbParameter)da.CreateParameter("@Peek", DbType.Int32, c.Peek));
                    @params.Add((DbParameter)da.CreateParameter("@ExternalAppId", DbType.Int32, c.ExternalAppId));
                    // param(3) = da.CreateParameter("@OddEven", DbType.Int16, c.OddEven)
                    @params.Add((DbParameter)da.CreateParameter("@DataType", DbType.Int32, c.ProcessDataType));
                    @params.Add((DbParameter)da.CreateParameter("@TotalChannelCount", DbType.Int32, c.TotalChannelCount));
                    @params.Add((DbParameter)da.CreateParameter("@CurrentChannelIndex", DbType.Int32, c.CurrentChannelIndex));
                    @params.Add((DbParameter)da.CreateParameter("@IsBlis", DbType.Boolean, true));
                    dt = da.ExecuteProcedure("lis_ReportsOutbound_Fetch", @params.ToArray());
                    if (dt[0].Rows.Count > 0)
                    {
                        foreach (DataRow r in dt[0].Rows)
                            m_outboundReportObjs.Add(new OutboundReport(Conversions.ToString(r["AccessionNbr"]), Conversions.ToBoolean(r["IsReportable"]), Conversions.ToInteger(r["ReportId"]), Conversions.ToInteger(r["OutboundOrderQueueId"])));
                    }
                }
                else
                {
                    @params = new List<DbParameter>();
                    @params.Add((DbParameter)da.CreateParameter("@ReportStatus", DbType.Int32, c.Status));
                    @params.Add((DbParameter)da.CreateParameter("@ReportFilterId", DbType.Int32, c.FilterId));
                    @params.Add((DbParameter)da.CreateParameter("@ExternalAppId", DbType.Int32, c.ExternalAppId));
                    @params.Add((DbParameter)da.CreateParameter("@OutboundChannelId", DbType.Int32, c.OutboundChannelId));
                    dt = da.ExecuteProcedure("lis_Reports_Fetch", @params.ToArray());
                    if (dt[0].Rows.Count > 0)
                    {
                        Load(dt[0], false);
                    }
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());
                throw ex;

            }

        }

        private void Load(DataTable datatable, bool IsAnalyteSpecific)
        {

            if (!IsAnalyteSpecific)
            {

                ReportInfo resultInfo = null;

                foreach (DataRow r in datatable.Rows)
                {
                    resultInfo = new ReportInfo();
                    resultInfo.Load(r);
                    m_list.Add(resultInfo);
                }
            }

            else
            {

                ReportAnalyteInfo resultInfo = null;

                foreach (DataRow r in datatable.Rows)
                {
                    resultInfo = new ReportAnalyteInfo();
                    resultInfo.Load(r);
                    m_list.Add(resultInfo);
                }

            }

        }

        #endregion

        #region Criteria Class

        [Serializable()]
        public class Criteria
        {

            private transmitStatusType m_transmitStatus = transmitStatusType.None;
            private long m_euid;
            private int m_filterId = 0;
            private int m_refLabId = 0;
            private int m_externalAppId = 0;

            private string m_accessionNbr = "";
            private DateTime m_searchStartDate;
            private DateTime m_searchEndDate;
            private string m_patientName = "";
            private string m_acctNumber = "";
            private bool m_searchPanel = false;
            // Private m_oddEven As processOddEvenType

            private int m_totalChannelCount = 1;
            private int m_currentChannelIndex = 0;

            private processDataType m_processDataType;
            private List<string> m_divisionCodes = new List<string>();

            private int m_outboundChannelId = 0;
            // Private m_transmitStatus As transmitStatusType = transmitStatusType.NotSet

            // This variable is used when searching based on analyteCodes, should be used with TransmitStatus
            private string m_analyteCode = "";

            private int m_returnCount = 0;
            private bool m_peek = false;
            private bool m_returnOutboundResultList = false;

            private bool m_isSearch = false;
            private DateTime m_analyteCreatedAfter;

            private List<int> m_outboundOrderQueueIds;
            public Criteria(List<int> outboundOrderQueueIds)
            {
                m_outboundOrderQueueIds = outboundOrderQueueIds;
              
            }
            public Criteria(int returnCount, bool peek, int externalAppId, int totalChannelCount, int currentChannelIndex, processDataType processDataType)
            {
                m_returnOutboundResultList = true;
                m_returnCount = returnCount;
                m_peek = peek;
                m_externalAppId = externalAppId;
                // m_oddEven = oddEven
                m_totalChannelCount = totalChannelCount;
                m_currentChannelIndex = currentChannelIndex;
                m_processDataType = processDataType;
            }

            public Criteria(string accountNbr, string accessionNbr, string patientName, DateTime startDate, DateTime endDate, transmitStatusType transmitStatus, long euid, List<string> divisionCodes)
            {
                m_accessionNbr = accessionNbr;
                m_searchStartDate = startDate;
                m_searchEndDate = endDate;
                m_patientName = patientName;
                m_acctNumber = accountNbr;
                m_transmitStatus = transmitStatus;
                m_euid = euid;
                m_divisionCodes = divisionCodes;
                m_isSearch = true;
            }

            public Criteria(transmitStatusType status, int filterId, int externalAppId, int outboundchannelId)
            {
                m_transmitStatus = status;
                m_filterId = filterId;
                m_externalAppId = externalAppId;
                m_outboundChannelId = outboundchannelId;
            }

            public Criteria(string analyteCode, transmitStatusType transmitStatus, bool searchPanel, DateTime analyteCreatedAfter)
            {
                m_transmitStatus = transmitStatus;
                m_analyteCode = analyteCode;
                m_searchPanel = searchPanel;
                m_analyteCreatedAfter = analyteCreatedAfter;
                m_isSearch = true;
            }

            // ReadOnly Property OddEven() As processOddEvenType
            // Get
            // Return m_oddEven
            // End Get
            // End Property
            public List<int> OutboundOrderQueueIds
            {
                get
                {
                    return m_outboundOrderQueueIds;
                }
            }
            public int TotalChannelCount
            {
                get
                {
                    return m_totalChannelCount;
                }
            }

            public int CurrentChannelIndex
            {
                get
                {
                    return m_currentChannelIndex;
                }
            }

            public processDataType ProcessDataType
            {
                get
                {
                    return m_processDataType;
                }
            }

            public bool SearchPanel
            {
                get
                {
                    return m_searchPanel;
                }
            }

            public string AccessionNumber
            {
                get
                {
                    return m_accessionNbr;
                }
            }

            public DateTime StartDate
            {
                get
                {
                    return m_searchStartDate;
                }
            }

            public DateTime EndDate
            {
                get
                {
                    return m_searchEndDate;
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
                    return m_acctNumber;
                }
            }

            public bool IsSearch
            {
                get
                {
                    return m_isSearch;
                }
            }

            public transmitStatusType Status
            {
                get
                {
                    return m_transmitStatus;
                }
            }

            public long EUID
            {
                get
                {
                    return m_euid;
                }
            }

            public int FilterId
            {
                get
                {
                    return m_filterId;
                }
            }

            public string AnalyteCode
            {
                get
                {
                    return m_analyteCode;
                }
            }

            public List<string> DivisionCodes
            {
                get
                {
                    return m_divisionCodes;
                }
            }

            public int ExternalAppId
            {
                get
                {
                    return m_externalAppId;
                }
            }

            public int OutboundChannelId
            {
                get
                {
                    return m_outboundChannelId;
                }
            }

            public int ReturnCount
            {
                get
                {
                    return m_returnCount;
                }
            }

            public bool Peek
            {
                get
                {
                    return m_peek;
                }
            }

            public bool ReturnOutboundResultList
            {
                get
                {
                    return m_returnOutboundResultList;
                }
            }

            public DateTime AnalyteCreatedAfter
            {
                get
                {
                    return m_analyteCreatedAfter;
                }
            }

        }

        #endregion

        [Serializable()]
        public class OutboundReport
        {
            private string m_accessionNbr;
            private bool m_isReportable;
            private int m_reportId;
            private int m_outboundOrderQueueId;

            public OutboundReport(string accessionNbr, bool isReportable, int reportId)
            {
                m_accessionNbr = accessionNbr;
                m_isReportable = isReportable;
                m_reportId = reportId;
            }
            public OutboundReport(string accessionNbr, bool isReportable, int reportId, int outboundOrderQueueId)
            {
                m_accessionNbr = accessionNbr;
                m_isReportable = isReportable;
                m_reportId = reportId;
                m_outboundOrderQueueId = outboundOrderQueueId;
            }
            public string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }
            public bool IsReportable
            {
                get
                {
                    return m_isReportable;
                }
            }
            public int ReportId
            {
                get
                {
                    return m_reportId;
                }
            }
            public int OutboundOrderQueueId
            {
                get
                {
                    return m_outboundOrderQueueId;
                }
            }
        }

        ~Reports()
        {
        }
    }
}