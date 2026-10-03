using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ResultsReviewFilterList : DataClassCollectionBase
    {

        internal void Add(ResultsReviewFilter filter)
        {
            List.Add(filter);
        }

        internal void Remove(ResultsReviewFilter filter)
        {
            List.Remove(filter);
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (ResultsReviewFilter i in List)
                {
                    if (i.IsDirty)
                        return true;
                }

                return false;

            }
        }

        public ResultsReviewFilter Find(string name)
        {

            foreach (ResultsReviewFilter i in List)
            {
                if ((i.Name ?? "") == (name ?? ""))
                {
                    return i;
                }
            }

            return null;

        }

        public ResultsReviewFilter Find(int filterId)
        {

            foreach (ResultsReviewFilter i in List)
            {
                if (i.Id == filterId)
                {
                    return i;
                }
            }

            return null;

        }

        public ResultsReviewFilter this[int index]
        {
            get
            {
                return (ResultsReviewFilter)List[index];
            }
        }

        internal void Update()
        {

            foreach (ResultsReviewFilter i in List)
            {
                if (i.IsDirty)
                {
                    i.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (ResultsReviewFilter i in DeletedItemsList)
                {
                    if (i.IsNew == false)
                        i.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

    }
}