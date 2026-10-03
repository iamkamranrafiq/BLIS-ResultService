using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CorrectedResultCommentList : DataClassCollectionBase
    {

        /// 
    /// <param name="parent"></param>
        internal CorrectedResultCommentList(CorrectedResultComments parent)
        {

        }

        /// 
    /// <param name="comment"></param>
        internal void Add(CorrectedResultComment comment)
        {
            List.Add(comment);
        }

        /// 
    /// <param name="comment"></param>
        internal void Remove(CorrectedResultComment comment)
        {
            List.Remove(comment);
        }

        /// 
    /// <param name="index"></param>
        public CorrectedResultComment this[int index]
        {
            get
            {
                return (CorrectedResultComment)List[index];
            }
        }

        internal void Update()
        {

            foreach (CorrectedResultComment c in List)
            {
                if (c.IsDirty)
                    c.Update();
            }

            if (!(DeletedItemsList == null))
            {
                foreach (CorrectedResultComment c in DeletedItemsList)
                {
                    if (c.IsNew == false)
                        c.Delete();
                }
            }

        }

        public CorrectedResultComment Find(string testCode, bool isForPanel)
        {

            foreach (CorrectedResultComment c in List)
            {
                if ((c.TestCode ?? "") == (testCode ?? "") && c.IsForPanel == isForPanel)
                {
                    return c;
                }
            }
            return null;

        }

        /// <summary>
    /// returns true if any child Comments are dirty
    /// </summary>
        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (CorrectedResultComment c in List)
                {
                    if (c.IsDirty)
                        return true;
                }

                return default;

            }
        }

    }
} // CommentList