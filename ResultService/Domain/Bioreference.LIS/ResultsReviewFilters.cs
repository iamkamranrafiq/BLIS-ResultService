using System;
using System.Data;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ResultsReviewFilters : DataClassBase
    {

        private ResultsReviewFilterList m_list;

        private ResultsReviewFilters()
        {
            m_list = new ResultsReviewFilterList();
        }

        public ResultsReviewFilterList List
        {
            get
            {
                return m_list;
            }
        }

        public static ResultsReviewFilters Fetch()
        {
            return (ResultsReviewFilters)DataFactory.Fetch(new Criteria());
        }

        public ResultsReviewFilter Add(string filterName)
        {

            var filter = new ResultsReviewFilter();
            filter.Name = filterName;
            m_list.Add(filter);
            return filter;

        }

        public void Remove(ResultsReviewFilter filter)
        {

            List.Remove(filter);

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            DataTable[] dt = da.ExecuteProcedure("lis_ResultReviewFilters_Fetch");

            Load(dt);

        }

        protected override void DataFactory_Save()
        {

            m_list.Update();

        }

        private void Load(DataTable[] tables)
        {

            ResultsReviewFilter filter = null;
            ResultsReviewInstrument inst = null;
            ResultsReviewTestCode code = null;

            foreach (DataRow r in tables[0].Rows)
            {
                filter = new ResultsReviewFilter();
                filter.Load(r);
                m_list.Add(filter);
            }

            // For TestCodes
            foreach (DataRow r in tables[1].Rows)
            {
                filter = m_list.Find((string)r["ResultsReviewFilterId"]);
                if (!(filter == null))
                {
                    code = new ResultsReviewTestCode(filter);
                    code.Load(r);
                    filter.TestCodeList.Add(code);
                }
            }

            // For Instruments
            // For Each r As DataRow In tables(2).Rows
            // filter = m_list.Find(r.Item("ResultsReviewFilterId"))
            // If Not IsNothing(filter) Then
            // inst = New ResultsReviewInstrument(filter)
            // inst.Load(r)
            // filter.InstrumentList.Add(inst)
            // End If
            // Next

        }

        public override bool IsDirty
        {
            get
            {
                return m_list.IsDirty;
            }
        }


        #region Criteria Class

        [Serializable()]
        internal class Criteria
        {
            public Criteria()
            {
            }
        }

        #endregion

    }
}