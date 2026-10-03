using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    public class CorrectecCauseReasons : DataClassBase
    {
        #region Private Members

        private List<CorrectedCauseReason> m_list = new List<CorrectedCauseReason>();

        #endregion

        #region Properties
        public List<CorrectedCauseReason> List
        {
            get
            {
                return m_list;
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
                DataTable[] dt = da.ExecuteProcedure("lis_CausedByMaster_Fetch");
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

            CorrectedCauseReason p;
            foreach (DataRow r in table.Rows)
            {
                p = new CorrectedCauseReason();
                p.Load(r);
                List.Add(p);
            }
        }
        public static CorrectecCauseReasons Fetch()
        {

            return (CorrectecCauseReasons)DataFactory.Fetch(new Criteria());

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