using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadCellList : DataClassCollectionBase
    {

        internal void Add(DiffPadCell cell)
        {
            List.Add(cell);
        }

        internal void Remove(DiffPadCell cell)
        {
            List.Remove(cell);
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (DiffPadCell f in List)
                {
                    if (f.IsDirty)
                        return true;
                }

                return false;

            }
        }

        public DiffPadCell Find(string code)
        {

            foreach (DiffPadCell d in List)
            {
                if ((d.Code ?? "") == (code ?? ""))
                {
                    return d;
                }
            }

            return null;

        }

        public DiffPadCell this[int index]
        {
            get
            {
                return (DiffPadCell)List[index];
            }
        }

        internal void Update()
        {

            foreach (DiffPadCell f in List)
            {
                if (f.IsDirty)
                {
                    f.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (DiffPadCell f in DeletedItemsList)
                {
                    if (f.IsNew == false)
                        f.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

    }
}