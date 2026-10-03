using System;
using System.Collections;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportCommentList : DataClassCollectionBase
    {

        private ArrayList m_originalList;

        internal ReportCommentList()
        {
            m_originalList = new ArrayList();
        }

        /// 
    /// <param name="comment"></param>
        internal void Add(ReportComment comment)
        {
            List.Add(comment);

            if (comment.IsNew == false && comment.IsDirty == false)
            {
                m_originalList.Add(comment);
            }
        }

        internal void AddToArchiveDeleteList(ReportComment comment)
        {
            ArchivedDeletedList.Add(comment);
        }


        // Public ReadOnly Property DeletedComments() As ReportComment()
        // Get
        // Dim list As List(Of ReportComment) = New List(Of ReportComment)
        // If Not IsNothing(Me.DeletedItemsList) Then
        // For Each o As Object In Me.DeletedItemsList
        // list.Add(o)
        // Next
        // End If
        // Return list.ToArray()
        // End Get
        // End Property

        /// <summary>
    /// Allows us to undo ReportComment changes, should probably be implemented in base classes
    /// </summary>
    /// <remarks></remarks>
        public void UndoChanges()
        {
            if (!(m_originalList == null) & m_originalList.Count > 0)
            {
                List.Clear();
                foreach (ReportComment c in m_originalList)
                    List.Add(c);
            }
            if (DeletedItemsList.Count > 0)
            {
                List.Clear();
                foreach (ReportComment c in DeletedItemsList)
                    List.Add(c);
            }
            DeletedItemsList.Clear();
        }

        /// <summary>
    /// derived from list of OrderComment
    /// </summary>
        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (ReportComment oc in List)
                {
                    if (oc.IsDirty)
                        return true;
                }

                return default;
            }
        }

        /// 
    /// <param name="index"></param>
        public ReportComment this[int index]
        {
            get
            {
                return (ReportComment)List[index];
            }
        }

        /// 
    /// <param name="comment"></param>
        internal void Remove(ReportComment comment)
        {
            List.Remove(comment);
        }

        internal void Update()
        {

            if (!(DeletedItemsList == null))
            {
                foreach (ReportComment oc in DeletedItemsList)
                {
                    if (!oc.IsNew)
                        oc.Delete();
                }
            }
            DeletedItemsList.Clear();

            foreach (ReportComment oc in List)
            {
                if (oc.IsDirty)
                    oc.Update();
            }

        }

    }
} // OrderCommentList