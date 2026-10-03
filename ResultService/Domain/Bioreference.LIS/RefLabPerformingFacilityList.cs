using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    public class RefLabPerformingFacilityList : DataClassBase
    {

        private List<RefLabPerformingFacility> m_refLabPerformingFacilityList = new List<RefLabPerformingFacility>();

        public List<RefLabPerformingFacility> List
        {
            get
            {
                return m_refLabPerformingFacilityList;
            }
        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();
            try
            {
                paramList.Add((DbParameter)da.CreateParameter("@refLabId", DbType.Int32, c.RefLabId));
                DataTable[] dt = da.ExecuteProcedure("lis_RefLabPerformingFacility_Fetch");
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
            RefLabPerformingFacility p;
            foreach (DataRow r in table.Rows)
            {
                p = new RefLabPerformingFacility();
                p.Load(r);
                m_refLabPerformingFacilityList.Add(p);
            }
        }

        public static RefLabPerformingFacility Filter(List<RefLabPerformingFacility> m_list, string filterString)
        {
            foreach (RefLabPerformingFacility item in m_list)
            {
                if ((filterString ?? "") == (item.FacilityCode ?? ""))
                {
                    return item;
                }
            }
            return null;
        }

        public static RefLabPerformingFacilityList FetchAll()
        {
            return (RefLabPerformingFacilityList)DataFactory.Fetch(new Criteria());
        }

        public static RefLabPerformingFacilityList Fetch(int referenceLabId)
        {
            return (RefLabPerformingFacilityList)DataFactory.Fetch(new Criteria(referenceLabId));
        }

        internal class Criteria
        {

            private int m_refLabId = 0;
            public Criteria()
            {
            }

            public Criteria(int m_refLabId)
            {
                this.m_refLabId = m_refLabId;
            }

            public int RefLabId
            {
                get
                {
                    return m_refLabId;
                }
            }
        }
    }
}