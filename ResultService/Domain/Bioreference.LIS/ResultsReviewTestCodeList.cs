using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ResultsReviewTestCodeList : DataClassCollectionBase
    {

        internal void Add(ResultsReviewTestCode testCode)
        {
            List.Add(testCode);
        }

        internal void Remove(ResultsReviewTestCode testCode)
        {
            List.Remove(testCode);
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (ResultsReviewTestCode t in List)
                {
                    if (t.IsDirty)
                        return true;
                }

                return false;

            }
        }

        public void Sort()
        {

            InnerList.Sort();

        }

        public ResultsReviewTestCode Find(string code)
        {

            foreach (ResultsReviewTestCode t in List)
            {
                if ((t.TestCode ?? "") == (code ?? ""))
                {
                    return t;
                }
            }

            return null;

        }

        public ResultsReviewTestCode this[int index]
        {
            get
            {
                return (ResultsReviewTestCode)List[index];
            }
        }

        internal void Update()
        {

            foreach (ResultsReviewTestCode t in List)
            {
                if (t.IsDirty)
                {
                    t.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (ResultsReviewTestCode t in DeletedItemsList)
                {
                    if (t.IsNew == false)
                        t.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

    }
}