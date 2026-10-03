using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Bioreference.Utilities;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderTestAnswer : DataClassBase
    {

        #region Private Members

        private OrderTest m_parent;
        private int m_id = 0;
        private int m_questionId = 0;
        private string m_question = "";
        private string m_answer = "";
        private FormatType m_formatType = FormatType.None;
        private string m_questionCode = "";

        #endregion

        #region Constructor

        internal OrderTestAnswer(OrderTest parent)
        {
            m_parent = parent;
        }

        internal OrderTestAnswer(OrderTest parent, TestQuestion question)
        {
            m_parent = parent;
            m_questionId = Conversions.ToInteger(question.Id);
            m_question = question.Text?.NormalizeToWindows() ?? string.Empty;
            m_formatType = question.Format;
            m_questionCode = question.Code;
            // If the answer maps back to an Order property, we set it here.
            if (!string.IsNullOrEmpty(question.DataMappingProperty))
            {
                Answer = m_parent.Parent.GetPropertyValue(question.DataMappingProperty);
            }
            else
            {
                Answer = "";
            }
            FlagChild();
            FlagDirty();
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

        public int QuestionId
        {
            get
            {
                return m_questionId;
            }
        }

        public string Question
        {
            get
            {
                return m_question;
            }
        }

        public string QuestionCode
        {
            get
            {
                return m_questionCode;
            }
        }

        public string Answer
        {
            get
            {
                return m_answer;
            }
            set
            {
                if ((m_answer ?? "") != (value.Trim() ?? ""))
                {
                    m_answer = value.Trim().NormalizeToWindows();
                    FlagDirty();
                    // Dim r As FormatResults = Formatter.Format(value, m_formatType)
                    // m_answer = IIf(r.IsError, value, r.Result)
                    // Rules.Assert(m_question, String.Format("Invalid format. {0}.", m_formatType.ToString()), r.IsError)
                    // Me.FlagDirty()
                }
            }
        }

        public OrderTest Parent
        {
            get
            {
                return m_parent;
            }
        }

        public FormatType Format
        {
            get
            {
                return m_formatType;
            }
        }


        #endregion

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@OrderTestAnswerId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderTestId", DbType.Int64, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@QuestionId", DbType.Int32, m_questionId));
            paramList.Add((DbParameter)da.CreateParameter("@QuestionText", DbType.String, m_question));
            paramList.Add((DbParameter)da.CreateParameter("@AnswerText", DbType.String, m_answer));
            paramList.Add((DbParameter)da.CreateParameter("@Format", DbType.Int32, m_formatType));
            paramList.Add((DbParameter)da.CreateParameter("@QuestionCode", DbType.String, m_questionCode));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OrderTestAnswer_Save", @params)["@OrderTestAnswerId"].Value);

            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["OrderTestAnswerId"]);
            m_questionId = Conversions.ToInteger(row["QuestionId"]);
            m_question = Conversions.ToString(row["QuestionText"]).NormalizeToWindows();
            m_answer = Conversions.ToString(row["AnswerText"]).NormalizeToWindows();
            m_formatType = (FormatType)Conversions.ToInteger(row["Format"]);
            m_questionCode = Conversions.ToString(row["QuestionCode"]);

            FlagClean();

        }

        #endregion


    }
}