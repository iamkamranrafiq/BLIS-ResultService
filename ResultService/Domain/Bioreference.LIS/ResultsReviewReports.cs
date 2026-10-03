using System;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ResultsReviewReports : DataClassReadOnlyBase
    {

        private ResultsReviewReportList m_list;

        private ResultsReviewReports()
        {
            m_list = new ResultsReviewReportList();
        }

        public ResultsReviewReportList List
        {
            get
            {
                return m_list;
            }
        }

        public static object Fetch(int filterId)
        {
            return DataFactory.Fetch(new Criteria(filterId));
        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DbParameter @param;

            @param = (DbParameter)da.CreateParameter("@FilterId", DbType.Int32, c.FilterId);

            DataTable[] dt = da.ExecuteProcedure("lis_ResultsReviewReports_Fetch", @param);

            Load(dt[0]);

        }

        internal void Load(DataTable table)
        {

            ResultReviewReport r = null;

            foreach (DataRow row in table.Rows)
            {
                r = new ResultReviewReport();
                r.Load(row);
                m_list.Add(r);
            }

        }

        [Serializable()]
        internal class Criteria
        {
            private int m_filterId = 0;
            public Criteria(int filterId)
            {
                m_filterId = filterId;
            }
            public int FilterId
            {
                get
                {
                    return m_filterId;
                }
            }
        }

    }



    [Serializable()]
    public class ResultsReviewReportList : DataClassReadOnlyCollectionBase
    {

        public ResultReviewReport this[object index]
        {
            get
            {
                return (ResultReviewReport)List[Conversions.ToInteger(index)];
            }
        }

        internal void Add(ResultReviewReport report)
        {
            List.Add(report);
        }

    }

    [Serializable()]
    public class ResultReviewReport : DataClassReadOnlyBase
    {

        private int m_reportId = 0;
        private long m_reportAnalyteId = 0L;
        private int m_reportPanelId = 0;
        private string m_testCode = "";
        private string m_instrumentId = "";

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }
        public long ReportAnalyteId
        {
            get
            {
                return m_reportAnalyteId;
            }
        }
        public int ReportAnalytePanelId
        {
            get
            {
                return m_reportPanelId;
            }
        }
        public string TestCode
        {
            get
            {
                return m_testCode;
            }
        }
        public string InstrumentId
        {
            get
            {
                return m_instrumentId;
            }
        }
        internal ResultReviewReport()
        {
        }

        internal void Load(DataRow r)
        {

            m_reportId = Conversions.ToInteger(r["ReportId"]);
            m_reportAnalyteId = Conversions.ToLong(r["ReportAnalyteId"]);
            m_reportPanelId = Conversions.ToInteger(r["ReportAnalytePanelId"]);
            m_testCode = Conversions.ToString(r["AnalyteCode"]);
            m_instrumentId = Conversions.ToString(r["InstrumentId"]);

        }

    }
}