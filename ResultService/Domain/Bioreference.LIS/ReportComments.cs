using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Microsoft.VisualBasic;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportComments : DataClassBase
    {

        #region Private Members
        private DataClassBase m_parent;
        private ReportCommentList m_list = null;
        #endregion

        #region Constructor
        internal ReportComments(DataClassBase parent)
        {
            m_parent = parent;
            m_list = new ReportCommentList();
        }
        #endregion

        #region Public Properties

        public ReportCommentList List
        {
            get
            {
                return m_list;
            }
            set
            {
                m_list = value;
            }
        }

        #endregion

        #region Public Functions
        /// 
    /// <param name="comment"></param>
        public ReportComment AddComment(string comment, int priority = 0)
        {

            return AddComment(comment, "", false, "", Common.TestMaster.ExternalCommentType.Comment, priority: priority);

        }

        public ReportComment AddComment(string comment, bool isAutoAdded, bool resetStatusOnComment = true)
        {

            return AddComment(comment, "", isAutoAdded, "", Common.TestMaster.ExternalCommentType.Comment, resetStatusOnComment: resetStatusOnComment);

        }

        public ReportComment AddComment(ReportComment comment, bool isAutoAdded, int priority = 0)
        {

            return AddComment(comment.Text, comment.ExternalId, isAutoAdded, comment.ExternalCommentCode, comment.ExternalCommentType, priority: priority);

        }

        public ReportComment AddComment(Comment comment, bool isAutoAdded, int priority = 0)
        {

            return AddComment(comment.Text, comment.AssignedID, isAutoAdded, comment.ExternalCommentCode, (Common.TestMaster.ExternalCommentType)comment.ExternalCommentType, priority: priority);

        }

        public ReportComment AddComment(Common.TestMaster.Comment comment, bool isCommentPanel, bool isCommentChild)
        {

            return AddComment(comment.Text, comment.AssignedID, false, comment.ExternalCommentCode, comment.ExternalCommentType);

        }


        public ReportComment AddComment(Common.TestMaster.Comment comment, bool isAutoAdded, int priority = 0)
        {

            return AddComment(comment.Text, comment.AssignedID, isAutoAdded, comment.ExternalCommentCode, comment.ExternalCommentType, priority: priority);

        }

        internal ReportComment AddComment(string comment, string assignedId, bool isAutoAdded, string extCommentCode, Common.TestMaster.ExternalCommentType extCommentType, bool isCommentPanel = false, bool isCommentChild = false, bool isDemographicComment = false, bool resetStatusOnComment = true, int priority = 0)
        {

            string commentToAdd = string.Empty;
            ReportAnalyte a = null;
            ReportAnalytePanel p = null;

            // 'Check if Comment needs to be added to the Report Analyte's parent (Report or ReportAnalytePanel)
            if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalyte)))
            {

                a = (ReportAnalyte)m_parent;
                if (!isDemographicComment)
                {
                    commentToAdd = ReplaceVariables(comment, a);
                }
                else
                {
                    commentToAdd = comment;
                }
                // 'If a.HasBeenReleased() AndAlso a.IsPreviousStatusNotReleased() Then a.ResultStatus = resultStatusType.Corrected

                if (a.Analyte.AttachCommentToParent && ReferenceEquals(a.Parent.GetType(), typeof(ReportAnalytePanel)))
                {
                    return ((ReportAnalytePanel)a.Parent).Comments.AddComment(commentToAdd, assignedId, isAutoAdded, extCommentCode, extCommentType, isDemographicComment: isDemographicComment, priority: priority);
                    // 'ElseIf a.Parent.GetType() Is GetType(Report) Then
                    // '    Return CType(a.Parent, Report).Comments.AddComment(comment, assignedId, isAutoAdded)
                }
            }

            else if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
            {
                if (!isDemographicComment)
                {
                    commentToAdd = ReplaceVariables(comment, (ReportAnalytePanel)m_parent);
                }
                else
                {
                    commentToAdd = comment;
                }
                p = (ReportAnalytePanel)m_parent;
                isCommentPanel = true;
                isCommentChild = false;
            }
            else if (!isDemographicComment)
            {
                commentToAdd = ReplaceVariables(comment, (Report)m_parent);
            }
            else
            {
                commentToAdd = comment;
            }

            // Ultimately, this must execute.
            // 9/3/09 - Prevent duplicates

            var c = FindByText(commentToAdd);
            if (c == null)
            {
                c = new ReportComment(m_parent, assignedId, isAutoAdded, extCommentCode, extCommentType, priority);
                c.Text = commentToAdd;
                List.Add(c);
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                var lst = new List<ReportComment>();
                lst.Add(c);
                if (a is not null)
                {
                    a.Updated(new ReportAnalyte.ReportAnalyteUpdatedArg(comment: lst, isCommentChildUpdated: isCommentChild, isCommentPanelUpdated: isCommentPanel));
                }
                else if (p is not null)
                {
                    p.Updated(new ReportAnalytePanel.ReportAnalytePanelUpdatedArg(lst));
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                if (!(a == null))
                {
                    // NA: 10/13 we need to reset the Release Status of the analyte to pending.
                    // If a.HasBeenReleased() AndAlso a.IsPreviousStatusNotReleased() Then a.ResultStatus = resultStatusType.Corrected
                    if (resetStatusOnComment && a.HasBeenReleased() && a.IsPreviousStatusNotReleased())
                    {
                        a.ResultStatus = resultStatusType.Corrected;
                        a.SetTransmitStatus(transmitStatusType.PendingRelease);
                    }
                }
                else if (!(p == null))
                {
                    if (resetStatusOnComment)
                    {
                        foreach (ReportAnalyte an in p.Analytes.List)
                        {
                            // NA: 10/13 we need to reset the Release Status of the analyte to pending.
                            // If an.HasBeenReleased() AndAlso an.IsPreviousStatusNotReleased() Then an.ResultStatus = resultStatusType.Corrected
                            if (an.HasBeenReleased() && an.IsPreviousStatusNotReleased())
                            {
                                an.ResultStatus = resultStatusType.Corrected;
                                an.SetTransmitStatus(transmitStatusType.PendingRelease);
                                p.SetTransmitStatus(transmitStatusType.PendingRelease, false);
                            }
                        }
                    }
                }
            }
            return c;

        }

        public ReportComment AddCommentToAnalyte(string commentToAdd, string assignedId, bool isAutoAdded, string extCommentCode, Common.TestMaster.ExternalCommentType extCommentType, bool isCommentPanel = false, bool isCommentChild = false, int priority = 0)
        {
            ReportAnalyte a = (ReportAnalyte)m_parent;
            var c = FindByText(commentToAdd);
            if (c == null)
            {
                c = new ReportComment(m_parent, assignedId, isAutoAdded, extCommentCode, extCommentType, priority);
                c.Text = commentToAdd;
                List.Add(c);
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                var lst = new List<ReportComment>();
                lst.Add(c);
                if (a is not null)
                {
                    a.Updated(new ReportAnalyte.ReportAnalyteUpdatedArg(comment: lst, isCommentChildUpdated: isCommentChild, isCommentPanelUpdated: isCommentPanel));
                }
            }
            return c;
        }

        /// 
    /// <param name="commentId"></param>
        public void DeleteComment(int commentId)
        {

            foreach (ReportComment oc in m_list)
            {
                if (oc.ID == commentId)
                {
                    m_list.Remove(oc);
                    MarkParentCorrected();
                    break;
                }
            }

        }

        public void DeleteComment(ReportComment comment, bool changeStatus = true)
        {

            List.Remove(comment);
            if (changeStatus)
            {
                MarkParentCorrected();
            }


        }

        public ReportComment Find(string externalId)
        {

            if (string.IsNullOrEmpty(externalId.Trim()))
                return null;

            foreach (ReportComment c in List)
            {
                if ((externalId ?? "") == (c.ExternalId ?? ""))
                {
                    return c;
                }
            }
            return null;
        }

        public ReportComment FindByText(string text)
        {

            foreach (ReportComment c in List)
            {
                if ((text.Replace(Constants.vbCrLf, "").Trim() ?? "") == (c.Text.Replace(Constants.vbCrLf, "").Trim() ?? ""))
                {
                    return c;
                }
            }
            return null;

        }


        public void DeleteAll()
        {

            foreach (ReportComment c in List)
            {
                List.Remove(c);
                DeleteAll();
                break;
            }

            MarkParentCorrected();

        }

        public List<string> GetAuditItems()
        {
            var lst = new List<string>();

            foreach (ReportComment c in List)
                lst.AddRange(c.GetFormattedAuditItems);

            return lst;

        }

        public void ResetAudit()
        {
            foreach (ReportComment c in m_list)
                c.ResetAuditItems();
        }

        #endregion

        #region Private/Friend Functions

        private void MarkParentCorrected()
        {

            if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalyte)))
            {
                ReportAnalyte a = (ReportAnalyte)m_parent;
                // NA: need to also set the transmit status
                // If a.HasBeenReleased() AndAlso a.IsPreviousStatusNotReleased() Then a.ResultStatus = resultStatusType.Corrected
                if (a.HasBeenReleased() && a.IsPreviousStatusNotReleased())
                {
                    a.ResultStatus = resultStatusType.Corrected;
                    a.SetTransmitStatus(transmitStatusType.PendingRelease);
                }
            }
            else if (ReferenceEquals(m_parent.GetType(), typeof(ReportAnalytePanel)))
            {
                // NA: need to also set the transmit status
                ReportAnalytePanel p = (ReportAnalytePanel)m_parent;
                // If p.HasBeenReleased() OrElse p.GetStatus() = resultStatusType.Final Then p.SetStatusToCorrected()
                if (p.HasBeenReleased() || p.GetStatus() == resultStatusType.Final)
                {
                    p.SetStatusToCorrected();
                    p.SetTransmitStatus(transmitStatusType.PendingRelease, false);
                }
            }

        }

        public static string ReplaceVariables(string comment, ReportAnalyte analyte)
        {

            string s = comment;
            if (s.Contains("#"))
            {

                s = s.Replace("#ResultValue#", analyte.ResultValue);
                s = s.Replace("#Units#", analyte.Analyte.Units);
                s = s.Replace("#ResultDate#", analyte.ResultDate.ToString("MM/dd/yyyy"));
                s = s.Replace("#ReleasedDate#", analyte.ReleaseDate.ToString("MM/dd/yyyy"));
                s = s.Replace("#TestName#", analyte.Analyte.Name);
                s = s.Replace("#TestCode#", analyte.Code);
                s = s.Replace("#FlagValue#", analyte.FlagValue);
                s = s.Replace("#RefRange#", analyte.GetReferenceRange());
                s = s.Replace("#Gender#", analyte.GetParentReport().Gender.ToString());
                s = s.Replace("#Dob#", analyte.GetParentReport().DOB);
                s = s.Replace("#FlagValue#", analyte.FlagValue);
                return s;

            }

            s = ReplaceResultValues(s, analyte.GetParentReport());

            return s;

        }

        public static string ReplaceVariables(string comment, ReportAnalytePanel panel)
        {

            string s = comment;

            if (s.Contains("#"))
            {

                s = s.Replace("#ReleasedDate#", panel.ReleaseDate.ToString("MM/dd/yyyy"));
                s = s.Replace("#TestName#", panel.Panel.Name);
                s = s.Replace("#TestCode#", panel.PanelCode);
                s = s.Replace("#Gender#", panel.Parent.Gender.ToString());
                s = s.Replace("#Dob#", panel.Parent.DOB);

            }

            s = ReplaceResultValues(s, panel.Parent);
            return s;

        }

        public static string ReplaceVariables(string comment, Report report)
        {

            string s = "";
            s = comment.Replace("#Gender#", report.Gender.ToString());
            s = s.Replace("#Dob#", report.DOB);
            return s;

        }

        /// <summary>
    /// Replaces test codes (ie. [6324]) with result value of first analyte found in reportToSearch
    /// </summary>
    /// <param name="comment"></param>
    /// <param name="reportToSearch"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static string ReplaceResultValues(string comment, Report reportToSearch)
        {

            string s = comment;
            if (!s.Contains("["))
                return s;

            var mc = Regex.Matches(s, @"\[[\w\d]+\]");
            ReportAnalyte ra;

            foreach (Match m in mc)
            {

                ra = reportToSearch.FindAnalyte(m.Value.Replace("[", "").Replace("]", ""), true);
                if (!(ra == null))
                {
                    s = s.Replace(m.Value, ra.ResultValue);
                }
            }

            return s;

        }

        internal void Update()
        {
            m_list.Update();
        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                return m_list.IsDirty;
            }
        }

    }
} // OrderComments