using System;
using System.Collections.Generic;
using System.Data;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadResults : DataClassReadOnlyBase
    {


        private List<DiffPadResult> m_list;

        internal DiffPadResults()
        {
            m_list = new List<DiffPadResult>();
        }

        public DiffPadResult[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        public static DiffPadResults Fetch()
        {

            return (DiffPadResults)DataFactory.Fetch(new Criteria());

        }



        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            DataTable[] dt = da.ExecuteProcedure("lis_DiffPadResults_Fetch");

            Load(dt[0]);

        }

        private void Load(DataTable table)
        {

            DiffPadResult result;
            foreach (DataRow r in table.Rows)
            {

                result = new DiffPadResult();
                result.Load(r);

                m_list.Add(result);

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