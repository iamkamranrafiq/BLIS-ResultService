using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportAlertList : DataClassCollectionBase
    {

        internal void Add(ReportAlert flag)
        {
            List.Add(flag);
        }

        internal void Remove(ReportAlert flag)
        {
            List.Remove(flag);
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (ReportAlert f in List)
                {
                    if (f.IsDirty)
                        return true;
                }

                return false;

            }
        }

        public ReportAlert this[int index]
        {
            get
            {
                return (ReportAlert)List[index];
            }
        }

        internal void Update()
        {

            foreach (ReportAlert f in List)
            {
                if (f.IsDirty)
                {
                    f.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (ReportAlert f in DeletedItemsList)
                {
                    if (f.IsNew == false)
                        f.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

    }
}