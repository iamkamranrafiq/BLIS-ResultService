using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class NoBillCodeList : DataClassCollectionBase
    {

        internal NoBillCodeList()
        {
        }

        internal void Add(NoBillCode noBill)
        {

            List.Add(noBill);

        }

        internal void Remove(NoBillCode noBill)
        {
            List.Remove(noBill);
        }

        public NoBillCode this[int index]
        {
            get
            {
                return (NoBillCode)List[index];
            }
        }

        internal object Find(string tstCode)
        {

            foreach (NoBillCode noBill in List)
            {
                if ((noBill.TestCode ?? "") == (tstCode ?? ""))
                {
                    return noBill;
                }
            }

            return null;

        }

        internal object Find(int noBillId)
        {

            foreach (NoBillCode noBill in List)
            {
                if (noBill.NoBillId == noBillId)
                {
                    return noBill;
                }
            }

            return null;

        }

        internal void Update()
        {

            foreach (NoBillCode i in List)
            {
                if (i.IsDirty)
                {
                    i.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (NoBillCode noBill in DeletedItemsList)
                {
                    if (noBill.IsNew == false)
                        noBill.Delete();
                }
                DeletedItemsList.Clear();
            }

        }


    }
}