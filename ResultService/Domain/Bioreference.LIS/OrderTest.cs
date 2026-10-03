using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{


    [Serializable()]
    public class OrderTest : DataClassBase
    {


        #region Private Members

        private long m_id;
        private Order m_parent;
        private string m_testCode;
        private string m_testName;
        private OrderTestAnswerList m_answerList;

        private string m_orderedTestCode;
        private string m_orderedTestName;
        private SPMStatusValue m_SPMStatus = SPMStatusValue.None;

        internal long m_placeholderId = 0L;
        private bool m_isSentOut = false; // Determines if was sent to outside lab.

        internal bool m_loadedFromDatabase = false; // Use to determine if a test was added after Order creation.

        #endregion

        #region Constructor

        internal OrderTest(Order parent)
        {

            FlagChild();
            m_parent = parent;
            m_answerList = new OrderTestAnswerList(this);

        }

        internal OrderTest(Order parent, string testCode, string testName, string orderedTestCode, string orderedTestName, SPMStatusValue spmStatus)
        {

            FlagChild();
            m_parent = parent;

            m_testCode = testCode;
            m_testName = testName;
            m_orderedTestCode = orderedTestCode;
            m_orderedTestName = orderedTestName;
            m_SPMStatus = spmStatus;
            FlagDirty();

        }

        internal OrderTest(Order parent, Test test)
        {

            FlagChild();
            m_parent = parent;
            // 'We need to add functionality to process test here.

            m_testCode = test.TestCode;
            m_testName = test.Description;

            m_answerList = new OrderTestAnswerList(this);

            foreach (Question q in test.Questions)
            {

                var ota = new OrderTestAnswer(this, (TestQuestion)q);
                AOEs.Add(ota);

            }

        }

        #endregion

        #region Public Properties

        /// <summary>
    /// Temporary id assigned prior to the OrderAnswer being saved.
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public long PlaceholderId
        {
            get
            {
                return m_placeholderId;
            }
        }

        public long Id
        {
            get
            {
                return m_id;
            }
        }

        public string OrderedTestCode
        {
            get
            {
                return m_orderedTestCode;
            }
        }

        public string OrderedTestName
        {
            get
            {
                return m_orderedTestName;
            }
        }

        public string TestCode
        {
            get
            {
                return m_testCode;
            }
        }

        public string TestName
        {
            get
            {
                return m_testName;
            }
        }

        public OrderTestAnswerList AOEs
        {
            get
            {
                return m_answerList;
            }
        }

        public Order Parent
        {
            get
            {
                return m_parent;
            }
        }

        public SPMStatusValue SPMStatus
        {
            get
            {
                return m_SPMStatus;
            }
        }

        /// <summary>
    /// Determines if the Test was sent to an outside lab for processing.
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public bool IsSentOut
        {
            get
            {
                return m_isSentOut;
            }
            set
            {
                if (value != m_isSentOut)
                {
                    m_isSentOut = value;
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Data Functions

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@OrderTestId", DbType.Int64, m_id);

            da.ExecuteNonQuery("lis_OrderTest_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@OrderTestId", DbType.Int64, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@TestCode", DbType.String, m_testCode));
            paramList.Add((DbParameter)da.CreateParameter("@OrderedTestCode", DbType.String, m_orderedTestCode));
            paramList.Add((DbParameter)da.CreateParameter("@OrderedTestName", DbType.String, m_orderedTestName));
            paramList.Add((DbParameter)da.CreateParameter("@TestName", DbType.String, m_testName));
            paramList.Add((DbParameter)da.CreateParameter("@IsSentOut", DbType.Boolean, m_isSentOut));
            paramList.Add((DbParameter)da.CreateParameter("@SPMStatus", DbType.Int32, m_SPMStatus));

      //      if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, ""));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_OrderTest_Save", @params)["@OrderTestId"].Value);

            if (!(m_answerList == null))
                m_answerList.Update();

            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToLong(row["OrderTestId"]);
            m_testCode = Conversions.ToString(row["TestCode"]);
            m_testName = Conversions.ToString(row["TestName"]);
            m_isSentOut = Conversions.ToBoolean(row["IsSentOut"]);
            m_orderedTestCode = Conversions.ToString(row["OrderedTestCode"]);
            m_orderedTestName = Conversions.ToString(row["OrderedTestName"]);
            m_SPMStatus = (SPMStatusValue)Conversions.ToInteger(row["SPMStatus"]);

            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(row["OrderTestAnswerId"], 0, false)))
            {
                var q = new OrderTestAnswer(this);
                q.Load(row);
                m_answerList.Add(q);
            }

            m_loadedFromDatabase = true;

            FlagClean();

        }

        #endregion

    }
}