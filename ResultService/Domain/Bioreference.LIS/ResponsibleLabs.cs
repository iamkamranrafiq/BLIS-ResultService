using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    public class ResponsibleLabs : DataClassBase
    {
        #region Private Members

        private List<ResponsibleLab> m_list = new List<ResponsibleLab>();

        #endregion

        #region Properties
        public List<ResponsibleLab> List
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
                DataTable[] dt = da.ExecuteProcedure("lis_ResponsibleLab_Fetch");
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

            ResponsibleLab p;
            foreach (DataRow r in table.Rows)
            {
                p = new ResponsibleLab();
                p.Load(r);
                List.Add(p);
            }
        }
        public static ResponsibleLabs Fetch()
        {

            return (ResponsibleLabs)DataFactory.Fetch(new Criteria());

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