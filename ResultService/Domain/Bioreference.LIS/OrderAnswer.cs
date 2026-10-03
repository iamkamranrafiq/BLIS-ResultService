using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Bioreference.Utilities;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderAnswer : AuditDataClassBase
    {

        #region Private Members

        private Order m_parent;
        private int m_id = 0;
        private int m_questionId = 0;
        private string m_question = "";
        private string m_questionCode = "";
        private string m_answer = "";
        private FormatType m_formatType = FormatType.None;
        private List<string> m_auditItemList;

        // Private m_answerList As List(Of Answer) 'Temp placeholder, does not save.
        // Private m_answerType As AnswerType

        #endregion

        #region Constructor

        internal OrderAnswer(Order parent)
        {
            m_parent = parent;
            m_auditItemList = new List<string>();
            // m_answerList = New List(Of Answer)
        }

        internal OrderAnswer(Order parent, Question question)
        {
            m_parent = parent;
            m_questionId = Conversions.ToInteger(question.Id);
            m_question = question.Text?.NormalizeToWindows() ?? string.Empty;
            m_formatType = question.Format;
            m_questionCode = question.Code;
            // If the answer maps back to an Order property, we set it here.
            if (!string.IsNullOrEmpty(question.DataMappingProperty))
            {
                Answer = m_parent.GetPropertyValue(question.DataMappingProperty);
            }
            else
            {
                Answer = "";
            }

            m_auditItemList = new List<string>();

            // m_answerList = question.Answers.ToList()
            // m_answerType = question.AnswerType

            FlagChild();
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

        public FormatType Format
        {
            get
            {
                return m_formatType;
            }
        }

        public List<string> GetFormattedAuditItems
        {
            // get the audit item of this object as well as any object in the Save stream.
            get
            {
                return m_auditItemList;
            }
        }
        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["OrderAnswerId"]);
            m_questionId = Conversions.ToInteger(row["QuestionId"]);
            m_question = Conversions.ToString(row["QuestionText"]).NormalizeToWindows();
            m_answer = Conversions.ToString(row["AnswerText"]).NormalizeToWindows();
            m_formatType = (FormatType)Conversions.ToInteger(row["Format"]);
            m_questionCode = Conversions.ToString(row["QuestionCode"]);

            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@OrderAnswerId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@QuestionId", DbType.Int32, m_questionId));
            paramList.Add((DbParameter)da.CreateParameter("@QuestionText", DbType.String, m_question));
            paramList.Add((DbParameter)da.CreateParameter("@AnswerText", DbType.String, m_answer));
            paramList.Add((DbParameter)da.CreateParameter("@Format", DbType.Int32, m_formatType));
            paramList.Add((DbParameter)da.CreateParameter("@QuestionCode", DbType.String, m_questionCode));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OrderAnswer_Save", @params)["@OrderAnswerId"].Value);

            m_auditItemList.AddRange(GetAuditItemsAndClean());
            // Me.FlagClean()

        }

        #endregion


    }
}