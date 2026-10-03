using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ResultsReviewInstrumentList : DataClassCollectionBase
    {

        internal void Add(ResultsReviewInstrument instrument)
        {
            List.Add(instrument);
        }

        internal void Remove(ResultsReviewInstrument instrument)
        {
            List.Remove(instrument);
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (ResultsReviewInstrument i in List)
                {
                    if (i.IsDirty)
                        return true;
                }

                return false;

            }
        }

        public ResultsReviewInstrument Find(string instrumentId)
        {

            foreach (ResultsReviewInstrument i in List)
            {
                if ((i.InstrumentId ?? "") == (instrumentId ?? ""))
                {
                    return i;
                }
            }

            return null;

        }

        public ResultsReviewInstrument this[int index]
        {
            get
            {
                return (ResultsReviewInstrument)List[index];
            }
        }

        internal void Update()
        {

            foreach (ResultsReviewInstrument i in List)
            {
                if (i.IsDirty)
                {
                    i.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (ResultsReviewInstrument i in DeletedItemsList)
                {
                    if (i.IsNew == false)
                        i.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

    }
}