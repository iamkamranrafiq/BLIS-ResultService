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
    public class ResultsReviewTestCode : DataClassBase, IComparable
    {


        #region Private Members

        private int m_id = 0;
        private ResultsReviewFilter m_parent = null;
        private string m_testCode = "";
        private string m_abbreviation = "";
        private int m_sequence = 0;

        #endregion

        #region Constructor

        public ResultsReviewTestCode(ResultsReviewFilter parent)
        {
            m_parent = parent;
            FlagChild();
        }

        #endregion

        #region Public Properties

        public int CompareTo(object obj)
        {

            if (!ReferenceEquals(obj.GetType(), typeof(ResultsReviewTestCode)))
            {
                throw new ArgumentException();
            }

            ResultsReviewTestCode a = (ResultsReviewTestCode)obj;
            if (a.Sequence < m_sequence)
            {
                return 1;
            }
            else if (a.Sequence == m_sequence)
            {
                return 0;
            }
            else
            {
                return -1;
            }

        }

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
            set
            {
                if ((m_testCode ?? "") != (value.Trim() ?? ""))
                {
                    m_testCode = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string Abbreviation
        {
            get
            {
                return m_abbreviation;
            }
            set
            {
                if ((m_abbreviation ?? "") != (value.Trim() ?? ""))
                {
                    m_abbreviation = value.Trim();
                    FlagDirty();
                }
            }
        }

        public int Sequence
        {
            get
            {
                return m_sequence;
            }
            set
            {
                if (m_sequence != value)
                {
                    m_sequence = value;
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["ResultsReviewTestCodeId"]);
            m_testCode = Conversions.ToString(row["TestCode"]);
            m_abbreviation = Conversions.ToString(row["Abbreviation"]);
            m_sequence = Conversions.ToInteger(row["Seq"]);

            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ResultsReviewTestCodeId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@ResultsReviewFilterId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@TestCode", DbType.String, m_testCode));
            paramList.Add((DbParameter)da.CreateParameter("@Abbreviation", DbType.String, m_abbreviation));
            paramList.Add((DbParameter)da.CreateParameter("@Sequence", DbType.Int32, m_sequence));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_ResultsReviewTestCode_Save", @params)["@ResultsReviewTestCodeId"].Value);

            FlagClean();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@ResultsReviewTestCodeId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_ResultsReviewTestCode_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

    }
}