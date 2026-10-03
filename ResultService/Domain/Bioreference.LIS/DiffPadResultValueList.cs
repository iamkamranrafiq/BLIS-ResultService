using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadResultValueList : DataClassCollectionBase
    {

        internal void Add(DiffPadResultValue value)
        {
            List.Add(value);
        }

        internal void Remove(DiffPadResultValue value)
        {
            List.Remove(value);
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (DiffPadResultValue f in List)
                {
                    if (f.IsDirty)
                        return true;
                }

                return false;

            }
        }

        public DiffPadResultValue this[int index]
        {
            get
            {
                return (DiffPadResultValue)List[index];
            }
        }

    }
}