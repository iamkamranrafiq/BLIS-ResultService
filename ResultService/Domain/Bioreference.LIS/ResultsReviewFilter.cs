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
    public class ResultsReviewFilter : DataClassBase
    {

        #region Private Members

        private int m_id = 0;
        private string m_name = "";
        private ResultsReviewInstrumentList m_instruments = null;
        private ResultsReviewTestCodeList m_testCodes = null;

        #endregion

        #region Constructor
        internal ResultsReviewFilter()
        {
            m_instruments = new ResultsReviewInstrumentList();
            m_testCodes = new ResultsReviewTestCodeList();
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

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                if ((m_name ?? "") != (value.Trim() ?? ""))
                {
                    m_name = value.Trim();
                    FlagDirty();
                }
            }
        }

        public ResultsReviewInstrumentList InstrumentList
        {
            get
            {
                return m_instruments;
            }
        }

        public ResultsReviewTestCodeList TestCodeList
        {
            get
            {
                return m_testCodes;
            }
        }

        #endregion

        #region Public Functions

        public ResultsReviewTestCode AddTestCode(string testCode, string abbr)
        {

            var tc = new ResultsReviewTestCode(this);
            tc.TestCode = testCode;
            tc.Abbreviation = abbr;
            m_testCodes.Add(tc);

            return tc;

        }

        public void RemoveTestCode(ResultsReviewTestCode testCode)
        {

            m_testCodes.Remove(testCode);

        }

        #endregion

        #region Data Functions

        private void Load(DataTable[] tables)
        {

            ResultsReviewFilter filter = null;
            ResultsReviewInstrument inst = null;
            ResultsReviewTestCode code = null;

            {
                ref var withBlock = ref tables[0];
                Load(withBlock.Rows[0]);
            }

            // For TestCodes
            foreach (DataRow r in tables[1].Rows)
            {
                code = new ResultsReviewTestCode(this);
                code.Load(r);
                m_testCodes.Add(code);
            }

            // For Instruments
            // For Each r As DataRow In tables(2).Rows
            // inst = New ResultsReviewInstrument(Me)
            // inst.Load(r)
            // m_instruments.Add(inst)
            // Next

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["ResultsReviewFilterId"]);
            m_name = Conversions.ToString(row["FilterName"]);

            FlagClean();

        }

        protected override void DataFactory_Save()
        {

            Update();

        }

        protected override void DataFactory_Delete(object criteria)
        {

            Delete();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@Name", DbType.String, m_name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_ResultsReviewFilter_Save", @params)["@Id"].Value);

            m_testCodes.Update();

            FlagClean();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_ResultsReviewFilter_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@Id", DbType.Int32, c.FilterId);

            DataTable[] dt = da.ExecuteProcedure("lis_ResultReviewFilter_Fetch", @param);

            Load(dt);

        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_testCodes.IsDirty || m_instruments.IsDirty;
            }
        }

        #region Criteria Class

        [Serializable()]
        internal class Criteria
        {
            private int m_filterId;
            public Criteria(int filterId)
            {
                m_filterId = filterId;
            }
            public object FilterId
            {
                get
                {
                    return m_filterId;
                }
            }
        }

        #endregion

    }
}