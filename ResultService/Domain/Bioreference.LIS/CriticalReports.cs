using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CriticalReports : DataClassReadOnlyBase
    {

        #region Private Members

        private CriticalReportList m_list;
        private List<string> m_listAcctNbr;

        #endregion

        #region Constructor

        private CriticalReports()
        {
            m_list = new CriticalReportList();
            m_listAcctNbr = new List<string>();
        }

        #endregion

        #region Public Property

        public CriticalReportList List
        {
            get
            {
                return m_list;
            }
        }

        public string[] AccountNumbers
        {
            get
            {
                return m_listAcctNbr.ToArray();
            }
        }

        #endregion

        #region Public Methods

        public static CriticalReports Fetch(criticalType critical)
        {

            return (CriticalReports)DataFactory.Fetch(new Criteria(critical));

        }

        public static CriticalReports Fetch(criticalType critical, DateTime startDate)
        {

            return (CriticalReports)DataFactory.Fetch(new Criteria(critical, startDate));

        }

        public CriticalReport[] GetReports(string acctNbr)
        {

            var list = new List<CriticalReport>();
            foreach (CriticalReport c in m_list)
            {
                if ((c.AccountNumber ?? "") == (acctNbr ?? ""))
                    list.Add(c);
            }
            return list.ToArray();

        }


        #endregion

        #region Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var p = new DbParameter[2];

            p[0] = (DbParameter)da.CreateParameter("@CriticalType", DbType.Int32, c.Critical);
            p[1] = (DbParameter)da.CreateParameter("@StartDate", DbType.DateTime, c.StartDate);

            DataTable[] dt = da.ExecuteProcedure("lis_CriticalReports_Fetch", p);

            Load(dt[0]);

        }

        private void Load(DataTable table)
        {

            CriticalReport cr = null;
            int panelId = 0;
            RefAnalyte a = null;

            foreach (DataRow r in table.Rows)
            {

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(r["ReportAnalytePanelId"], 0, false)) || Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(panelId, r["ReportAnalytePanelId"], false)))
                {

                    cr = new CriticalReport();
                    cr.Load(r);
                    m_list.Add(cr);

                }

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectGreater(r["ReportAnalytePanelId"], 0, false)))
                {
                    cr.AddAnalyte(r["AnalyteCode"].ToString().PadLeft(4, '0'), Conversions.ToString(r["ResultValue"]), Conversions.ToString(r["AnalyteName"]));
                }

                if (m_listAcctNbr.Contains(cr.AccountNumber) == false)
                {
                    m_listAcctNbr.Add(cr.AccountNumber);
                }
                panelId = Conversions.ToInteger(r["ReportAnalytePanelId"]);
            }

        }

        #endregion

        #region Internal Criteria

        [Serializable()]
        internal class Criteria
        {

            private criticalType m_critical;
            private DateTime m_startDate;

            public Criteria(criticalType critical)
            {
                m_critical = critical;
                m_startDate = DateTime.Parse("1900-01-01");
            }

            public Criteria(criticalType critical, DateTime startDate)
            {
                m_critical = critical;
                m_startDate = startDate;
            }

            public criticalType Critical
            {
                get
                {
                    return m_critical;
                }
            }

            public DateTime StartDate
            {
                get
                {
                    return m_startDate;
                }
            }

        }

        #endregion

    }
}