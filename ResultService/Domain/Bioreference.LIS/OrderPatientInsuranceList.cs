using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderPatientInsuranceList : DataClassCollectionBase
    {

        private Order m_parent;

        internal OrderPatientInsuranceList(Order parent)
        {
            m_parent = parent;
        }

        internal void Add(OrderPatientInsurance insured)
        {

            List.Add(insured);

        }

        internal void Remove(OrderPatientInsurance insured)
        {

            List.Remove(insured);

        }

        public OrderPatientInsurance this[int index]
        {
            get
            {
                return (OrderPatientInsurance)List[index];
            }
        }

        internal void Update()
        {

            foreach (OrderPatientInsurance i in List)
            {
                if (i.IsDirty)
                    i.Update(); // OrElse i.IsNew
            }

        }

    }
}