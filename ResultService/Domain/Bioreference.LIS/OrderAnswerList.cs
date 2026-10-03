using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderAnswerList : DataClassCollectionBase
    {

        private Order m_parent;

        internal OrderAnswerList(Order parent)
        {

            m_parent = parent;

        }

        internal void Add(OrderAnswer answer)
        {

            List.Add(answer);

        }

        // Friend Sub Remove(ByVal answer As OrderAnswer)

        // Me.List.Remove(answer)

        // End Sub

        public OrderAnswer this[int index]
        {
            get
            {
                return (OrderAnswer)List[index];
            }
        }

        internal void Update()
        {

            foreach (OrderAnswer i in List)
            {
                if (i.IsDirty) // i.IsNew OrElse
                {
                    i.Update();
                }
            }

        }

    }
}