using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CorrectedResultComment : AuditDataClassBase
    {

        #region Constructors
        internal CorrectedResultComment()
        {
            FlagChild();
        }
        internal CorrectedResultComment(string testCode)
        {
            m_testcode = testCode;
            FlagChild();
            FlagDirty();
        }
        #endregion

        #region Private members
        internal int m_id = 0;
        private string m_text = "";
        private string m_assignedID = "";
        private string m_testcode = "";
        private bool m_isForPanel = false;

        #endregion

        #region Public Properties

        [Audit("TestCode")]
        public string TestCode
        {
            get
            {
                return m_testcode;
            }
            set
            {
                if ((m_testcode ?? "") != (value.Trim() ?? ""))
                {
                    m_testcode = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("AssignedID")]
        public string AssignedID
        {
            get
            {
                return m_assignedID;
            }
            set
            {
                if ((m_assignedID ?? "") != (value.Trim() ?? ""))
                {
                    m_assignedID = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("Text")]
        public string Text
        {
            get
            {
                return m_text;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_text ?? ""))
                {
                    m_text = value.Trim().NormalizeToWindows();
                    FlagDirty();
                }
            }
        }

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        [Audit("Text")]
        public bool IsForPanel
        {
            get
            {
                return m_isForPanel;
            }
            set
            {
                if (value != m_isForPanel)
                {
                    m_isForPanel = value;
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Public Methods

        public static CorrectedResultComment Fetch(string testCode, bool isForPanel)
        {

            return (CorrectedResultComment)DataFactory.Fetch(new Criteria(testCode, isForPanel));

        }

        public static ReportComment AddCorrectedComment(ReportAnalyte analyte, DateTime panelReleaseDate = default)
        {

            bool isPanel = false;
            string s = string.Empty;
            string c = string.Empty;

            ReportComment correctedComment = null;
            // If analyte.Parent.GetType() Is GetType(ReportAnalytePanel) Then
            // Dim p As ReportAnalytePanel = CType(analyte.Parent, ReportAnalytePanel)
            // c = FetchComment(analyte, True)
            // Dim CommentText As String = FormatCorrectedPanelComment(CType(p.Parent, Report), analyte, c.Text)
            // CorrectedResultComment.RemoveCorrectedComments(p)
            // p.Comments.AddComment(CommentText, "C", False, "", Common.TestMaster.ExternalCommentType.Comment)
            // Else
            c = Configuration.LISSettings.GetString("AutomatedCorrectedResultComment");
            s = c.Replace("#ResultValue#", analyte.ReleasedValue);
            s = s.Replace("#ReleasedDate#", analyte.ReleaseDate.ToString("MM/dd/yyyy"));
            // CorrectedResultComment.RemoveCorrectedComments(analyte, False)

            var releaseDate = analyte.ReleaseDate;
            if (!(panelReleaseDate == null))
            {
                releaseDate = panelReleaseDate;
            }

            if (analyte.IsNewAnalyteAdded)
            {
                correctedComment = analyte.Comments.AddCommentToAnalyte(string.Format(Configuration.LISSettings.GetString("AutomatedRevisedAnalyteComment"), releaseDate.ToString("MM/dd/yyyy")), "C", false, "", Common.TestMaster.ExternalCommentType.Comment, priority: 1);
            }
            else
            {
                correctedComment = analyte.Comments.AddCommentToAnalyte(s, "C", false, "", Common.TestMaster.ExternalCommentType.Comment, priority: 1);
            }
            return correctedComment;
        }

        // Private Shared Function FormatCorrectedPanelComment(ByVal report As Report, ByVal analyte As ReportAnalyte, ByVal ctext As String) As String
        // Dim sb As StringBuilder = New StringBuilder()
        // Dim i As Integer = 0

        // If (report.CorrectedPanelTests.Count > 0) Then
        // sb.AppendLine(String.Format(ctext, analyte.ReleaseDate.ToString("MM/dd/yyyy")))
        // For Each pair As KeyValuePair(Of String, String) In report.CorrectedPanelTests
        // i = i + 1
        // sb.Append(String.Format("{0}: {1}       ", pair.Key, pair.Value))
        // If (i Mod 4 = 0) Then
        // sb.Append(Environment.NewLine)
        // End If
        // Next
        // End If

        // Return sb.ToString()
        // End Function
        private static CorrectedResultComment FetchComment(ReportAnalyte analyte, bool IsForPanel)
        {
            return Fetch(analyte.Code, IsForPanel);
        }

        public static void RemoveCorrectedComments(ReportAnalyte analyte, bool includeParentPanel = true)
        {

            foreach (ReportComment c in analyte.Comments.List)
            {
                if (c.ExternalId == "C")
                {
                    analyte.DeleteComment(c);
                    RemoveCorrectedComments(analyte, includeParentPanel);
                    break;
                }
            }

            if (includeParentPanel)
            {
                if (ReferenceEquals(analyte.Parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    ReportAnalytePanel p = (ReportAnalytePanel)analyte.Parent;
                    RemoveCorrectedComments(p);
                }
            }

        }

        public static void RemoveCorrectedComments(ReportAnalytePanel panel)
        {

            foreach (ReportAnalyte a in panel.Analytes.List)
                RemoveCorrectedComments(a, false);

            foreach (ReportComment c in panel.Comments.List)
            {
                if (c.ExternalId == "C")
                {
                    panel.DeleteComment(c);
                    RemoveCorrectedComments(panel);
                    break;
                }
            }

        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var p = new DbParameter[2];

            p[0] = (DbParameter)da.CreateParameter("@TestCode", DbType.String, c.TestCode);
            p[1] = (DbParameter)da.CreateParameter("@IsForPanel", DbType.Boolean, c.IsForPanel);


            DataTable[] dt = da.ExecuteProcedure("lis_CorrectedResultComment_Fetch", p);

            if (dt[0].Rows.Count > 0)
            {
                Load(dt[0].Rows[0]);
            }

        }

        // Protected Overrides Sub DataFactory_Save()
        // Me.Update()
        // End Sub

        internal virtual void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@CorrectedResultCommentId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@CommentText", DbType.String, m_text));
            paramList.Add((DbParameter)da.CreateParameter("@AssignedId", DbType.String, m_assignedID));
            paramList.Add((DbParameter)da.CreateParameter("@TestCode", DbType.String, m_testcode));
            paramList.Add((DbParameter)da.CreateParameter("@IsForPanel", DbType.Boolean, m_isForPanel));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_CorrectedResultComment_Save", @params)["@CorrectedResultCommentId"].Value);

            FlagClean();

        }

        internal virtual void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@CorrectedResultCommentId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_CorrectedResultComment_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["CorrectedResultCommentId"]);
            m_text = Conversions.ToString(row["CommentText"]).NormalizeToWindows();
            m_testcode = Conversions.ToString(row["TestCode"]);
            m_assignedID = Conversions.ToString(row["AssignedID"]);
            m_isForPanel = Conversions.ToBoolean(row["IsForPanel"]);

            FlagClean();

        }

        #endregion

        #region Internal Criteria

        [Serializable()]
        internal class Criteria
        {

            private string m_testCode;
            private bool m_isForPanel;

            public Criteria(string testCode, bool isForPanel)
            {
                m_testCode = testCode;
                m_isForPanel = isForPanel;
            }

            public string TestCode
            {
                get
                {
                    return m_testCode;
                }
            }
            public bool IsForPanel
            {
                get
                {
                    return m_isForPanel;
                }
            }

        }

        #endregion

    }
}