using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class COCApprovers : DataClassBase
    {

        #region Private Members

        private List<COCApprover> m_approverList = new List<COCApprover>();

        #endregion

        public List<COCApprover> ApproverList
        {
            get
            {
                return m_approverList;
            }
        }

        #region Public Methods
        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            try
            {
                DataTable[] dt = da.ExecuteProcedure("lis_CocApprovers_Fetch");

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

        public static COCApprovers Fetch()
        {

            return (COCApprovers)DataFactory.Fetch(new Criteria());

        }

        public static COCApprover CreateApprover(string cocApproverLoginId, string cocApproverFirstName, string cocApproverMiddleName, string cocApproverLastName, string cocApproverEmpNbr)
        {
            var objApprover = new COCApprover();

            objApprover.CocApproverLoginId = cocApproverLoginId;
            objApprover.CocApproverFirstName = cocApproverFirstName;
            objApprover.CocApproverMiddleName = cocApproverMiddleName;
            objApprover.CocApproverLastName = cocApproverLastName;
            objApprover.CocApproverEmpNbr = cocApproverEmpNbr;

            return objApprover;
        }

        public COCApprover AddApprover(string cocApproverLoginId, string cocApproverFirstName, string cocApproverMiddleName, string cocApproverLastName, string cocApproverEmpNbr)
        {
            var objApprover = new COCApprover();

            objApprover.CocApproverLoginId = cocApproverLoginId;
            objApprover.CocApproverFirstName = cocApproverFirstName;
            objApprover.CocApproverMiddleName = cocApproverMiddleName;
            objApprover.CocApproverLastName = cocApproverLastName;
            objApprover.CocApproverEmpNbr = cocApproverEmpNbr;

            m_approverList.Add(objApprover);

            return objApprover;

        }
        #endregion

        #region Private Methods

        internal COCApprovers()
        {
        }

        private void Load(DataTable table)
        {

            COCApprover p;
            foreach (DataRow r in table.Rows)
            {
                p = new COCApprover();
                p.Load(r);
                m_approverList.Add(p);
            }
        }
        #endregion

        [Serializable()]
        internal class Criteria
        {
            public Criteria()
            {
            }
        }

    }
}