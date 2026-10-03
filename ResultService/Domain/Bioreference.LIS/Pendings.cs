using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Bioreference.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    /// Put this here for now because the project is checked out and unable to add files
    [Serializable()]
    public class Pendings : DataClassReadOnlyBase
    {

        #region Private Members

        private static readonly ILog Log = LogManager.GetLogger<Pendings>();
        private Common.TestMaster.PendingList m_pendingTestMasterList;
        private Common.TestMaster.PendingListLite m_pendingTestMasterListLite;
        private List<Pending> m_pendings = new List<Pending>();
        private DateTime m_lastAddOn;

        #endregion

        #region Public Methods

        public static Pendings Fetch(DateTime startDate, DateTime endDate, Common.TestMaster.PendingList pendingList)
        {

            // 'Get a total list of testCodes
            var testCodes = new List<string>();

            foreach (Common.TestMaster.PendingListItem plItem in pendingList.PendingListItemList)
            {
                foreach (Common.TestMaster.PendingListItemTest plTest in plItem.PendingListItemTestList)
                    testCodes.Add(plTest.TestCode);
            }

            Pendings p = (Pendings)DataFactory.Fetch(new Criteria(startDate, endDate, testCodes));
            p.m_pendingTestMasterList = pendingList;
            return p;

        }

        public static Pendings Fetch(DateTime startDate, DateTime endDate, Common.TestMaster.PendingListLite pendingListLite)
        {
            // 'Get a total list of testCodes
            var testCodes = new List<string>();

            foreach (Common.TestMaster.PendingListItemLite plItem in pendingListLite.PendingListItemsLite)
                testCodes.AddRange(plItem.TestCodes);

            Pendings p = (Pendings)DataFactory.Fetch(new Criteria(startDate, endDate, testCodes));
            p.m_pendingTestMasterListLite = pendingListLite;
            return p;
        }

        public int GetReportableCount
        {
            get
            {
                int count = 0;
                foreach (Pending p in m_pendings)
                {
                    if (p.HasReportable)
                        count += 1;
                }
                return count;
            }
        }

        #endregion

        #region Public Properties

        public Pending[] List
        {
            get
            {
                return m_pendings.ToArray();
            }
        }

        #endregion

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
                cmd.CommandText = "lis_Reports_NotReleased_Fetch";
                cmd.Parameters.AddWithValue("@StartDate", DbType.DateTime).Value = c.StartDate;
                cmd.Parameters.AddWithValue("@EndDate", DbType.DateTime).Value = c.EndDate;
                cmd.Parameters.AddWithValue("@TestCodes", DbType.String).Value = string.Join(",", c.TestCodes.ToArray());
                da = new SqlDataAdapter(cmd);
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

            Pending p;
            DateTime dateServiced;
            var releaseDate = DateTime.Parse("1900-01-01");
            int ParentType;
            foreach (DataRow r in table.Rows)
            {
                dateServiced = (DateTime)r["DateServiced"];
                p = (Pending)FindPending(Conversions.ToString(r["AccessionNbr"]), dateServiced);
                if (p == null)
                {
                    p = new Pending();
                    p.Load(r);
                    m_pendings.Add(p);
                }
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(r["IsReportable"], 1, false)))
                    p.m_hasReportable = true;
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(r["ResultValue"], "", false)))
                    p.m_hasPending = true;
                ParentType = Conversions.ToInteger(r["ParentType"]);
                if (!(r["ReleaseDate"] is DBNull))
                {
                    releaseDate = (DateTime)r["ReleaseDate"];
                }
                if (!string.IsNullOrEmpty(Conversions.ToString(r["DepartmentShortName"])) && !p.DepartmentShortName.Contains(r["DepartmentShortName"].ToString()))
                {
                    p.DepartmentShortName.Add(r["DepartmentShortName"].ToString());
                }
                p.AddTest(new Pending.TestInfo(Conversions.ToString(r["Code"]), Conversions.ToString(r["ResultValue"]), Conversions.ToString(r["RapidResultId"]), (transmitStatusType)Conversions.ToInteger(r["TransmitStatus"]), Conversions.ToDate(r["DateCreated"]), Conversions.ToBoolean(r["IsReportable"]), Conversions.ToString(r["PerformingFacility"]), (resultStatusType)Conversions.ToInteger(r["ResultStatus"]), Conversions.ToString(r["RRIDList"]), Conversions.ToBoolean(r["IsTestHold"]), Conversions.ToDate(r["DateServiced"]), Conversions.ToBoolean(r["IsReflex"]), (SampleRequestType)Conversions.ToInteger(r["SampleStatus"]), releaseDate, ParentType, Conversions.ToString(r["DepartmentShortName"])));
            }

        }

        #endregion

        #region Public Methods

        public object FindPending(string accessionNbr, DateTime dateServiced)
        {

            foreach (Pending p in m_pendings)
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
            private List<string> m_testCodes;
            private DateTime m_startDate;
            private DateTime m_endDate;
            public Criteria(DateTime startDate, DateTime endDate, List<string> testCodes)
            {
                m_testCodes = testCodes;
                m_startDate = startDate;
                m_endDate = endDate;
            }
            public List<string> TestCodes
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
        }
        #endregion

    }
}