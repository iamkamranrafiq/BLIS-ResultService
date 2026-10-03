using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class TestLookupComponent : DataClassBase
    {


        #region Private Members

        private int m_id = 0;
        private TestLookup m_parent = null;
        private string m_testCode = "";
        private object m_altOutboundTestCode = "";
        private object m_altOutboundTestDescr = "";
        private object m_altInboundTestCode = "";

        #endregion

        #region Constructor

        internal TestLookupComponent(TestLookup parent)
        {
            FlagChild();
        }

        internal TestLookupComponent(TestLookup parent, string testCode, string altOutboundTestCode, string altOutboundTestDescr, string altInboundTestCode)
        {
            m_parent = parent;
            m_testCode = testCode;
            m_altOutboundTestCode = altOutboundTestCode;
            m_altOutboundTestDescr = altOutboundTestDescr;
            m_altInboundTestCode = altInboundTestCode;
            FlagChild();
        }

        #endregion

        #region Public Properties


        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string TestCode
        {
            get
            {
                return m_testCode;
            }
        }

        public string AltOutboundTestCode
        {
            get
            {
                return Conversions.ToString(m_altOutboundTestCode);
            }
        }

        public string AltOutboundTestDescr
        {
            get
            {
                return Conversions.ToString(m_altOutboundTestDescr);
            }
        }

        public string AltInboundTestCode
        {
            get
            {
                return Conversions.ToString(m_altInboundTestCode);
            }
        }

        #endregion

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@TestLookupId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@TestCode", DbType.String, m_testCode));
            paramList.Add((DbParameter)da.CreateParameter("@AltOutboundTestCode", DbType.String, m_altOutboundTestCode));
            paramList.Add((DbParameter)da.CreateParameter("@AltOutboundTestDescr", DbType.String, m_altOutboundTestDescr));
            paramList.Add((DbParameter)da.CreateParameter("@AltInboundTestCode", DbType.String, m_altInboundTestCode));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_TestLookupComponent_Save", @params)["@Id"].Value);

            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["Id"]);
            m_testCode = Conversions.ToString(row["TestCode"]);
            m_altOutboundTestCode = row["AltOutboundTestCode"];
            m_altOutboundTestDescr = row["AltOutboundTestDescr"];
            m_altInboundTestCode = row["AltInboundTestCode"];

            FlagClean();

        }

        #endregion

    }
}