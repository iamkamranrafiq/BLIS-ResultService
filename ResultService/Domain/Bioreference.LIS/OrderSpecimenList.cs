using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderSpecimenList : DataClassCollectionBase
    {

        private Order m_parent;

        internal OrderSpecimenList(Order parent)
        {

            m_parent = parent;

        }

        internal void Add(OrderSpecimen specimen)
        {

            List.Add(specimen);

        }

        internal void Remove(OrderSpecimen specimen)
        {

            List.Remove(specimen);

        }

        public OrderSpecimen this[int index]
        {
            get
            {
                return (OrderSpecimen)List[index];
            }
        }

        internal void Update()
        {

            foreach (OrderSpecimen i in List)
            {
                if (i.IsDirty)
                    i.Update(); // OrElse i.IsNew
            }

            if (!(DeletedItemsList == null))
            {
                foreach (OrderSpecimen s in DeletedItemsList)
                {
                    if (s.IsNew == false)
                        s.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

    }
}