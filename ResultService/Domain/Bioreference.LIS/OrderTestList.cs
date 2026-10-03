using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderTestList : DataClassCollectionBase
    {

        private Order m_parent;
        private int placeholderId = 0; // Used as a dummy id for OrderAnswer prior to saving

        internal OrderTestList(Order parent)
        {

            m_parent = parent;

        }

        internal void Add(OrderTest test)
        {

            placeholderId += 1;
            test.m_placeholderId = placeholderId;
            List.Add(test);

        }

        internal void Remove(OrderTest test)
        {

            List.Remove(test);

        }

        public OrderTest this[int index]
        {
            get
            {
                return (OrderTest)List[index];
            }
        }

        internal void Update()
        {

            foreach (OrderTest i in List)
            {
                if (i.IsDirty) // i.IsNew OrElse
                {
                    i.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (OrderTest t in DeletedItemsList)
                {
                    if (t.IsNew == false)
                        t.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

    }
}