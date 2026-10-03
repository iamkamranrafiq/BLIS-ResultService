using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Bioreference.Data;

namespace Bioreference.LIS
{

    public class TNPPendings : DataClassReadOnlyBase
    {

        private static readonly ILog Log = LogManager.GetLogger<Pendings>();
        private Common.TestMaster.PendingList m_pendingTestMasterList;
        private Common.TestMaster.PendingListLite m_pendingTestMasterListLite;
        private List<TNPPending> m_pendings = new List<TNPPending>();

        public TNPPending[] List
        {
            get
            {
                return m_pendings.ToArray();
            }
        }

        public static TNPPendings Fetch(DateTime startDate, DateTime endDate, Common.TestMaster.PendingListLite pendingListLite, string account, string texttestcodes, int pageNo, int pageSize)
        {
            // 'Get a total list of testCodes
            var testCodes = new List<string>();
            string testCodesstring = "";

            if(pendingListLite!=null)
            { 
            foreach (Common.TestMaster.PendingListItemLite plItem in pendingListLite.PendingListItemsLite)
                testCodes.AddRange(plItem.TestCodes);
            }
            if (testCodes.Count > 0)
            {
                testCodesstring = string.Join(",", testCodes.ToArray());
            }
            else
            {
                testCodesstring = texttestcodes;
            }
            TNPPendings p = (TNPPendings)DataFactory.Fetch(new Criteria(startDate, endDate, testCodesstring, account, new DataTable(),pageNo,pageSize));
            p.m_pendingTestMasterListLite = pendingListLite;
            return p;
        }

        #region Data Methods

        protected override void DataFactory_Fetch(object criteria)
        {
            var conn = new SqlConnection(Configuration.ConnectionString);
            SqlCommand cmd = null;
            Criteria c = (Criteria)criteria;
            var dtOut = new DataTable();
            SqlDataAdapter da;
            try
            {

                cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandTimeout = 180;
                cmd.CommandType = CommandType.StoredProcedure;
                if (c.ExcelDatatable.Rows.Count > 0)
                {
                    cmd.CommandText = "lis_TNPTool_Pendings_Excel_Fetch";
                    cmd.Parameters.AddWithValue("@TNPToolExcelInputData", SqlDbType.Structured).Value = c.ExcelDatatable;

                    da = new SqlDataAdapter(cmd);
                }
                else
                {
                    cmd.CommandText = "lis_TNPTool_Pendings_Fetch";
                    cmd.Parameters.AddWithValue("@StartDateDOS", DbType.DateTime).Value = c.StartDate;
                    cmd.Parameters.AddWithValue("@EndDateDOS", DbType.DateTime).Value = c.EndDate;
                    cmd.Parameters.AddWithValue("@TestCodes", DbType.String).Value = c.TestCodes;
                    cmd.Parameters.AddWithValue("@Account", DbType.String).Value = c.Account;
                    cmd.Parameters.AddWithValue("@PageNo", DbType.Int32).Value = c.PageNo;
                    cmd.Parameters.AddWithValue("@PageSize", DbType.Int32).Value = c.PageSize;
                    da = new SqlDataAdapter(cmd);
                }

                da.Fill(dtOut);
                Load(dtOut);
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Trace.Write(ex.ToString());
                throw;
            }
        }

        private void Load(DataTable table)
        {

            TNPPending p;
            foreach (DataRow r in table.Rows)
            {
                p = new TNPPending();
                p.Load(r);
                m_pendings.Add(p);
            }

        }

        #endregion

        #region Public Methods

        public object FindPending(string accessionNbr, DateTime dateServiced)
        {

            foreach (TNPPending p in m_pendings)
            {
                if ((p.AccessionNbr ?? "") == (accessionNbr ?? "") & p.DateOfService == dateServiced)
                    return p;
            }
            return null;

        }
        #endregion


        #region Criteria Class
        [Serializable()]
        public class Criteria
        {
            private string m_testCodes;
            private DateTime m_startDate;
            private DateTime m_endDate;
            private string m_account;
            private DataTable m_excelDatatable;
            private int m_pageNo = 0;
            private int m_pageSize = 0;

            public Criteria(DateTime startDate, DateTime endDate, string testCodes, string account, DataTable dt)
            {
                m_testCodes = testCodes;
                m_startDate = startDate;
                m_endDate = endDate;
                m_account = account;
                m_excelDatatable = dt;
            }

            public Criteria(DateTime startDate, DateTime endDate, string testCodes, string account, DataTable dt, int pageNo, int pageSize)
            {
                m_testCodes = testCodes;
                m_startDate = startDate;
                m_endDate = endDate;
                m_account = account;
                m_excelDatatable = dt;
                m_pageNo = pageNo;
                m_pageSize = pageSize;
            }


            public string TestCodes
            {
                get
                {
                    return m_testCodes;
                }
            }

            public DateTime StartDate
            {
                get
                {
                    return m_startDate;
                }
            }

            public DateTime EndDate
            {
                get
                {
                    return m_endDate;
                }
            }

            public string Account
            {
                get
                {
                    return m_account;
                }
            }

            public DataTable ExcelDatatable
            {
                get
                {
                    return m_excelDatatable;
                }
            }

            public int PageNo
            {
                get
                {
                    return m_pageNo;
                }
            }

            public int PageSize
            {
                get
                {
                    return m_pageSize;
                }
            }
        }

        public static TNPPendings Fetch(DataTable dt)
        {
            TNPPendings p = (TNPPendings)DataFactory.Fetch(new Criteria(DateTime.Parse("1900-01-01"), DateTime.Parse("1900-01-01"), "", "", dt));
            return p;
        }
        #endregion

    }
}