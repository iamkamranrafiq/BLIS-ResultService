using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderICD9List : DataClassCollectionBase
    {

        private Order m_parent;

        internal OrderICD9List(Order parent)
        {
            m_parent = parent;
        }

        internal void Add(OrderICD9 icd9)
        {

            List.Add(icd9);

        }

        internal void Remove(OrderICD9 icd9)
        {

            List.Remove(icd9);

        }

        public OrderICD9 this[int index]
        {
            get
            {
                return (OrderICD9)List[index];
            }
        }

        internal void Update()
        {

            foreach (OrderICD9 i in List)
            {
                if (i.IsDirty)
                    i.Update(); // Or i.IsNew
            }

            if (!(DeletedItemsList == null))
            {
                foreach (OrderICD9 i in DeletedItemsList)
                {
                    if (i.IsNew == false)
                        i.Delete();
                }
                DeletedItemsList.Clear();
            }
        }

    }
}