using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderTestAnswerList : DataClassCollectionBase
    {

        private OrderTest m_parent;

        internal OrderTestAnswerList(OrderTest parent)
        {

            m_parent = parent;

        }

        internal void Add(OrderTestAnswer answer)
        {

            List.Add(answer);

        }

        internal void Remove(OrderTestAnswer answer)
        {

            List.Remove(answer);

        }

        public OrderTestAnswer this[int index]
        {
            get
            {
                return (OrderTestAnswer)List[index];
            }
        }

        internal void Update()
        {

            foreach (OrderTestAnswer i in List)
            {
                if (i.IsDirty) // i.IsNew OrElse
                {
                    i.Update();
                }
            }

        }

    }
}