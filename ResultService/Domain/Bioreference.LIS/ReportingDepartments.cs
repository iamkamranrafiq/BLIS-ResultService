using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    public class ReportingDepartments : DataClassBase
    {

        #region Private Members

        private List<ReportingDepartment> m_deptList = new List<ReportingDepartment>();

        #endregion

        #region Properties
        public List<ReportingDepartment> DeptList
        {
            get
            {
                return m_deptList;
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
                DataTable[] dt = da.ExecuteProcedure("lis_DepartmentResponsibleMaster_Fetch");
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

            ReportingDepartment p;
            foreach (DataRow r in table.Rows)
            {
                p = new ReportingDepartment();
                p.Load(r);
                m_deptList.Add(p);
            }
        }
        public static ReportingDepartments Fetch()
        {

            return (ReportingDepartments)DataFactory.Fetch(new Criteria());

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