using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    public class CorrectedReasonsPrimary : DataClassBase
    {

        #region Private Members

        private List<CorrectedReasonPrimary> m_correctedReasonPrimaryList = new List<CorrectedReasonPrimary>();

        #endregion

        #region Properties

        public List<CorrectedReasonPrimary> CorrectedReasonPrimaryList
        {
            get
            {
                return m_correctedReasonPrimaryList;
            }
        }

        #endregion

        #region Public Methods

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ReasonId", DbType.Int64, Convert.ToInt64(c.ReasonId)));

            try
            {
                DataTable[] dt = da.ExecuteProcedure("lis_CorrectedReasonPrimary_Fetch", paramList.ToArray());
                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0]);
                }
            }


            catch (Exception ex)
            {

                Trace.Write(ex.StackTrace.ToString());

            }

        }

        private void Load(DataTable table)
        {

            CorrectedReasonPrimary p;
            foreach (DataRow r in table.Rows)
            {
                p = new CorrectedReasonPrimary();
                p.Load(r);
                m_correctedReasonPrimaryList.Add(p);
            }
        }
        public static CorrectedReasonsPrimary Fetch(int reasonid)
        {

            return (CorrectedReasonsPrimary)DataFactory.Fetch(new Criteria(reasonid));

        }

        #endregion

        internal class Criteria
        {
            private int m_reasonId;
            public Criteria(int reasonid)
            {
                m_reasonId = reasonid;
            }
            public Criteria()
            {
            }

            public int ReasonId
            {
                get
                {
                    return m_reasonId;
                }
            }

        }
    }
}