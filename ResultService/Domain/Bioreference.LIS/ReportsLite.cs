using Bioreference.Data;
using Bioreference.Data.Client;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportsLite : DataClassReadOnlyBase
    {


        #region Private Members

        private ReportList m_list;
        private List<string> m_outboundReports = new List<string>();

        #endregion

        #region Constructor

        private ReportsLite()
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

        public static ReportsLite Fetch(int externalAppId, transmitStatusType status)
        {
            return (ReportsLite)DataFactory.Fetch(new Criteria(status, 0, externalAppId, default, default, false));
        }

        public static ReportsLite FetchDetail(int externalAppId, transmitStatusType status, DateTime fromDate, DateTime toDate)
        {

            return (ReportsLite)DataFactory.Fetch(new Criteria(status, 0, externalAppId, fromDate, toDate, true));

        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DataTable[] dt = null;

            try
            {

                var @param = new List<DbParameter>();
                @param.Add((DbParameter)da.CreateParameter("@TransmitStatus", DbType.Int32, c.Status));
                @param.Add((DbParameter)da.CreateParameter("@ExternalAppId", DbType.Int32, c.ExternalAppId));
                @param.Add((DbParameter)da.CreateParameter("@IsBlis", DbType.Boolean, true));

                if (!c.ReturnDetail)
                {
                    dt = da.ExecuteProcedure("lis_ReportsLite_Fetch", @param.ToArray());
                }
                else
                {
                    if (c.FromDate > DateTime.Parse("1900-01-01"))
                        @param.Add((DbParameter)da.CreateParameter("@FromDate", DbType.DateTime, c.FromDate));
                    if (c.ToDate > DateTime.Parse("1900-01-01"))
                        @param.Add((DbParameter)da.CreateParameter("@ToDate", DbType.DateTime, c.ToDate));
                    dt = da.ExecuteProcedure("lis_ReportsLiteDetail_Fetch", @param.ToArray());
                }

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0], c.ReturnDetail);
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
            private int m_externalAppId = 0;
            private DateTime m_fromDate;
            private DateTime m_toDate;
            private bool m_returnDetail = false;

            public Criteria(transmitStatusType status, int filterId, int externalAppId, DateTime fromDate, DateTime toDate, bool returnDetail)
            {
                m_transmitStatus = status;
                m_externalAppId = externalAppId;
                m_fromDate = fromDate;
                m_toDate = toDate;
                m_returnDetail = returnDetail;
            }

            public transmitStatusType Status
            {
                get
                {
                    return m_transmitStatus;
                }
            }

            public int ExternalAppId
            {
                get
                {
                    return m_externalAppId;
                }
            }

            public DateTime FromDate
            {
                get
                {
                    return m_fromDate;
                }
            }

            public DateTime ToDate
            {
                get
                {
                    return m_toDate;
                }
            }

            public bool ReturnDetail
            {
                get
                {
                    return m_returnDetail;
                }
            }

        }

        #endregion

    }
}