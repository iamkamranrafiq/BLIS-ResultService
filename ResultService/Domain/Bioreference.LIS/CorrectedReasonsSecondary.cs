using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    public class CorrectedReasonsSecondary : DataClassBase
    {

        #region Private Members

        private List<CorrectedReasonSecondary> m_correctedReasonSecondaryList = new List<CorrectedReasonSecondary>();

        #endregion

        #region Properties

        public List<CorrectedReasonSecondary> CorrectedReasonSecondaryList
        {
            get
            {
                return m_correctedReasonSecondaryList;
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

                DataTable[] dt = da.ExecuteProcedure("lis_CorrectedReasonSecondary_Fetch");
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

        private void Load(DataTable table)
        {

            CorrectedReasonSecondary p;
            foreach (DataRow r in table.Rows)
            {
                p = new CorrectedReasonSecondary();
                p.Load(r);
                m_correctedReasonSecondaryList.Add(p);
            }
        }
        public static CorrectedReasonsSecondary Fetch()
        {

            return (CorrectedReasonsSecondary)DataFactory.Fetch(new Criteria());

        }

        #endregion

        internal class Criteria
        {

            public Criteria()
            {
            }

        }
    }
}