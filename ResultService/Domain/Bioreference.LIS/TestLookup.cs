using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class TestLookup : AuditDataClassBase
    {


        #region Private Members

        private int m_id = 0;
        private string m_parentCode = "";
        private List<TestLookupComponent> m_componentCodes;
        private string m_refLabCode = "";
        private int m_refLabId = 0;
        private bool m_isProfile = false;
        private bool m_isPanel = false;

        #endregion

        #region Constructor

        internal TestLookup(string testCode, int refLabId, string refLabTestCode, bool isProfile, bool isPanel)
        {
            m_parentCode = testCode;
            m_refLabId = refLabId;
            m_refLabCode = refLabTestCode;
            m_isProfile = isProfile;
            m_isPanel = isPanel;
            m_componentCodes = new List<TestLookupComponent>();
            FlagDirty();
        }

        #endregion

        #region Public Properties

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string ParentCode
        {
            get
            {
                return m_parentCode;
            }
        }

        public List<TestLookupComponent> ComponentCodes
        {
            get
            {
                return m_componentCodes;
            }
        }

        public string RefLabCode
        {
            get
            {
                return m_parentCode;
            }
        }

        public int RefLabId
        {
            get
            {
                return m_refLabId;
            }
        }

        public bool IsProfile
        {
            get
            {
                return m_isProfile;
            }
        }

        public bool IsPanel
        {
            get
            {
                return m_isProfile;
            }
        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Save()
        {
            Update();
        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@TestLookupId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@ParentCode", DbType.String, m_parentCode));
            // '.Add(da.CreateParameter("@ComponentCodes", DbType.String, String.Join(",", m_componentCodes.ToArray())))
            paramList.Add((DbParameter)da.CreateParameter("@RefLabCode", DbType.String, m_refLabCode));
            paramList.Add((DbParameter)da.CreateParameter("@RefLabId", DbType.Int32, m_refLabId));
            paramList.Add((DbParameter)da.CreateParameter("@IsProfile", DbType.String, m_isProfile));
            paramList.Add((DbParameter)da.CreateParameter("@IsPanel", DbType.String, m_isPanel));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_TestLookup_Save", @params)["@TestLookupId"].Value);

            foreach (TestLookupComponent c in m_componentCodes)
                c.Update();

            FlagClean();

        }

        #endregion

    }
}