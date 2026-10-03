using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CocBatches : DataClassReadOnlyBase
    {

        private List<CocBatchReport> m_list;

        private CocBatches()
        {
            m_list = new List<CocBatchReport>();
        }

        public CocBatchReport[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        public static CocBatches Fetch(bool isClosed = false)
        {

            return (CocBatches)DataFactory.Fetch(new Criteria(0, isClosed));

        }

        public static CocBatchReport Fetch(int cocBatchID)
        {

            CocBatches ws = (CocBatches)DataFactory.Fetch(new Criteria(cocBatchID, false));

            if (ws.List.Length > 0)
            {
                return ws.List[0];
            }
            else
            {
                return null;
            }

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@CocBatchId", DbType.Int32, c.CocBatchId));
            paramList.Add((DbParameter)da.CreateParameter("@IsClosed", DbType.Boolean, c.IsClosed));
            paramList.Add((DbParameter)da.CreateParameter("@PageNo", DbType.Int32, c.PageNo));
            paramList.Add((DbParameter)da.CreateParameter("@PageSize", DbType.Int32, c.PageSize));

            DbParameter[] @params = paramList.ToArray();

            DataTable[] dt = da.ExecuteProcedure("lis_CocBatches_Fetch", @params);

            Load(dt[0]);

        }

        protected void Load(DataTable dt)
        {

            CocBatchReport ws = null;
            CocBatchReportItem wsi = null;
            int wid = 0;
            foreach (DataRow r in dt.Rows)
            {

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(wid, r["CocBatchId"], false)))
                {
                    ws = new CocBatchReport(Conversions.ToInteger(r["CocBatchId"]), Conversions.ToString(r["CocBatchName"]), Conversions.ToString(r["AccessionStart"]), Conversions.ToString(r["AccessionEnd"]), Conversions.ToBoolean(r["IsClosed"]), Conversions.ToDate(r["CreatedDate"]), Conversions.ToString(r["CreatedBy"]));
                    m_list.Add(ws);
                    wid = Conversions.ToInteger(r["CocBatchId"]);
                }

                // we may have Batches without any Accessions.
                if (!r.IsNull("CocBatchAccessionId"))
                {
                    wsi = new CocBatchReportItem();
                    wsi.Load(r);
                    ws.List.Add(wsi);
                }
            }

        }

        [Serializable()]
        internal class Criteria
        {
            private int m_cocBatchId = 0;
            private bool m_isClosed = false;
            private int m_pageNo = 1;
            private int m_pageSize = 100000;
            public Criteria()
            {
            }
            public Criteria(int cocBatchId, bool isClosed)
            {
                m_cocBatchId = cocBatchId;
                m_isClosed = isClosed;
            }

            public int CocBatchId
            {
                get
                {
                    return m_cocBatchId;
                }
            }
            public bool IsClosed
            {
                get
                {
                    return m_isClosed;
                }
            }

            public int PageNo
            {
                get { return m_pageNo; }
            }

            public int PageSize
            {
                get { return m_pageSize; }
            }
        }
    }
}