using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderPhysicianList : DataClassCollectionBase
    {

        private Order m_parent;

        internal OrderPhysicianList(Order parent)
        {
            m_parent = parent;
        }

        internal void Add(OrderPhysician physician)
        {

            List.Add(physician);

        }

        internal void Remove(OrderPhysician physician)
        {

            List.Remove(physician);

        }

        public OrderPhysician this[int index]
        {
            get
            {
                return (OrderPhysician)List[index];
            }
        }


        internal void Update()
        {

            foreach (OrderPhysician i in List)
            {
                if (i.IsDirty)
                    i.Save();
            }

            if (!(DeletedItemsList == null))
            {
                foreach (OrderPhysician i in DeletedItemsList)
                {
                    if (!i.IsNew)
                        i.Delete();
                }
                DeletedItemsList.Clear();
            }

        }


    }
}