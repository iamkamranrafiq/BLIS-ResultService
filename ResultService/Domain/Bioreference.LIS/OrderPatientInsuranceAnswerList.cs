using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderPatientInsuranceAnswerList : DataClassCollectionBase
    {

        private OrderPatientInsurance m_parent;

        internal OrderPatientInsuranceAnswerList(OrderPatientInsurance parent)
        {

            m_parent = parent;

        }

        internal void Add(OrderPatientInsuranceAnswer answer)
        {

            List.Add(answer);

        }

        internal void Remove(OrderPatientInsuranceAnswer answer)
        {

            Remove(answer);

        }

        public OrderPatientInsuranceAnswer this[int index]
        {
            get
            {
                return (OrderPatientInsuranceAnswer)List[index];
            }
        }

        internal void Update()
        {

            foreach (OrderPatientInsuranceAnswer i in List)
            {
                if (i.IsDirty) // i.IsNew OrElse
                {
                    i.Update();
                }
            }

        }

    }
}