using System;
using System.Collections.Generic;
using Bioreference.Data;
using Microsoft.VisualBasic;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderComments : DataClassBase
    {

        #region Private Members
        private Order m_parent;
        private OrderCommentList m_list = null;
        #endregion

        #region Constructor
        internal OrderComments(Order parent)
        {
            m_parent = parent;
            m_list = new OrderCommentList();
        }
        #endregion

        #region Public Properties

        public OrderCommentList List
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
        public OrderComment AddComment(string comment)
        {

            return AddComment(comment, false, "", externalApplicationType.B2);

        }

        public OrderComment AddComment(string comment, bool isAutoAdded)
        {

            return AddComment(comment, isAutoAdded, "", externalApplicationType.B2);

        }

        public OrderComment AddComment(string comment, string extCommentCode, externalApplicationType extAppType)
        {

            return AddComment(comment, false, extCommentCode, extAppType);

        }

        internal OrderComment AddComment(string comment, bool isAutoAdded, string extCommentCode, externalApplicationType extAppType)
        {

            string commentToAdd = ReplaceVariables(comment, m_parent);

            // Prevent duplicates
            var c = FindByText(commentToAdd);
            if (c == null)
            {
                c = new OrderComment(m_parent, extAppType, isAutoAdded, extCommentCode);
                c.Text = commentToAdd;
                List.Add(c);
            }
            return c;

        }

        /// 
    /// <param name="commentId"></param>
        public void DeleteComment(int commentId)
        {

            foreach (OrderComment oc in m_list)
            {
                if (oc.ID == commentId)
                {
                    m_list.Remove(oc);
                    break;
                }
            }

        }

        public void DeleteComment(OrderComment comment)
        {

            List.Remove(comment);

        }

        public OrderComment Find(string externalCommentCode)
        {

            if (string.IsNullOrEmpty(externalCommentCode.Trim()))
                return null;

            foreach (OrderComment c in List)
            {
                if ((externalCommentCode ?? "") == (c.ExternalCommentCode ?? ""))
                {
                    return c;
                }
            }
            return null;
        }

        public OrderComment FindByText(string text)
        {

            foreach (OrderComment c in List)
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

            foreach (OrderComment c in List)
            {
                List.Remove(c);
                DeleteAll();
                break;
            }

        }

        #endregion

        #region Private/Friend Functions

        public static string ReplaceVariables(string comment, Order order)
        {

            string s = "";
            s = comment.Replace("#Gender#", order.Patient.Gender.ToString());
            s = s.Replace("#Dob#", order.Patient.DateOfBirth);
            return s;

        }

        internal void Update()
        {
            m_list.Update();
        }

        internal List<string> GetAuditItems()
        {
            var lst = new List<string>();

            foreach (OrderComment c in m_list)
                lst.AddRange(c.GetFormattedAuditItems);

            return lst;

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