using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadCellTemplateList : DataClassCollectionBase
    {

        internal void Add(DiffPadTemplateCell cell)
        {
            List.Add(cell);
        }

        internal void Remove(DiffPadTemplateCell cell)
        {
            List.Remove(cell);
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (DiffPadTemplateCell f in List)
                {
                    if (f.IsDirty)
                        return true;
                }

                return false;

            }
        }


        public DiffPadTemplateCell this[int index]
        {
            get
            {
                return (DiffPadTemplateCell)List[index];
            }
        }

        internal void Update()
        {

            foreach (DiffPadTemplateCell f in List)
            {
                if (f.IsDirty)
                {
                    f.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (DiffPadTemplateCell f in DeletedItemsList)
                {
                    if (f.IsNew == false)
                        f.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

    }
}