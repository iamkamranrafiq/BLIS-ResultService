using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CocAudits : DataClassBase
    {

        #region Private Members

        private List<CocAudit> m_cocAuditList = new List<CocAudit>();

        #endregion

        #region Properties

        public List<CocAudit> CocAuditList
        {
            get
            {
                return m_cocAuditList;
            }
        }

        #endregion

        #region Public Methods
        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            try
            {
                DataTable[] dt = da.ExecuteProcedure("lis_CocAudits_Fetch");

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0]);
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        public static CocAudits Fetch()
        {

            return (CocAudits)DataFactory.Fetch(new Criteria());

        }

        public void SetAsProcessed(List<int> lstCocAuditIdList)
        {
            if (lstCocAuditIdList.Count > 0)
            {
                var sList = new List<string>();
                foreach (int i in lstCocAuditIdList)
                    sList.Add(i.ToString());

                string s = string.Join(",", sList.ToArray());

                var da = new DataWrapper(Configuration.ConnectionString);
                var paramList = new List<DbParameter>();

                paramList.Add((DbParameter)da.CreateParameter("@CocAuditIdList", DbType.String, s));
                
                if (!(CurrentUser == null)) 
                    paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

                DbParameter[] @params = paramList.ToArray();

                da.ExecuteNonQuery("lis_CocAudits_ProcessComplete", @params);
            }
        }
        #endregion

        private void Load(DataTable table)
        {

            CocAudit p;
            foreach (DataRow r in table.Rows)
            {
                p = new CocAudit();
                p.Load(r);
                m_cocAuditList.Add(p);
            }
        }

        [Serializable()]
        internal class Criteria
        {
            public Criteria()
            {
            }
        }

    }
}