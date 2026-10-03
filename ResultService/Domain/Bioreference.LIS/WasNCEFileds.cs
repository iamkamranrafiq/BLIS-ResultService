using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    public class WasNCEFileds : DataClassBase
    {
        #region Private Members

        private string m_Id;
        private string m_Name;
        private List<WasNCEFiled> m_list = new List<WasNCEFiled>();

        #endregion

        #region Properties
        public List<WasNCEFiled> List
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
                DataTable[] dt = da.ExecuteProcedure("lis_WasNCEFiledMaster_Fetch");
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

            WasNCEFiled p;
            foreach (DataRow r in table.Rows)
            {
                p = new WasNCEFiled();
                p.Load(r);
                List.Add(p);
            }
        }

        public static WasNCEFileds Fetch()
        {

            return (WasNCEFileds)DataFactory.Fetch(new Criteria());

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