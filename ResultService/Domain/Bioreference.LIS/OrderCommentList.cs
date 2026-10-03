using System;
using System.Collections;
using System.Collections.Generic;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderCommentList : DataClassCollectionBase
    {

        private ArrayList m_originalList;

        internal OrderCommentList()
        {
            m_originalList = new ArrayList();
        }

        /// 
    /// <param name="comment"></param>
        internal void Add(OrderComment comment)
        {
            List.Add(comment);

            if (comment.IsNew == false && comment.IsDirty == false)
            {
                m_originalList.Add(comment);
            }
        }

        internal void AddToArchiveDeleteList(OrderComment comment)
        {
            ArchivedDeletedList.Add(comment);
        }

        /// <summary>
    /// Quick fix for audit display, should be built from base class.
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public OrderComment[] DeletedComments
        {
            get
            {
                var list = new List<OrderComment>();
                if (!(DeletedItemsList == null))
                {
                    foreach (object o in DeletedItemsList)
                        list.Add((OrderComment)o);
                }
                return list.ToArray();
            }
        }

        /// <summary>
    /// Allows us to undo OrderComment changes, should probably be implemented in base classes
    /// </summary>
    /// <remarks></remarks>
        public void UndoChanges()
        {

            List.Clear();
            DeletedItemsList.Clear();
            if (!(m_originalList == null))
            {
                foreach (OrderComment c in m_originalList)
                    List.Add(c);
            }

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

                foreach (OrderComment oc in List)
                {
                    if (oc.IsDirty)
                        return true;
                }

                return default;
            }
        }

        /// 
    /// <param name="index"></param>
        public OrderComment this[int index]
        {
            get
            {
                return (OrderComment)List[index];
            }
        }

        /// 
    /// <param name="comment"></param>
        internal void Remove(OrderComment comment)
        {
            List.Remove(comment);
        }

        internal void Update()
        {

            if (!(DeletedItemsList == null))
            {
                foreach (OrderComment oc in DeletedItemsList)
                {
                    if (!oc.IsNew)
                        oc.Delete();
                }
            }
            DeletedItemsList.Clear();

            foreach (OrderComment oc in List)
            {
                if (oc.IsDirty)
                    oc.Update();
            }

        }

    }
} // OrderCommentList