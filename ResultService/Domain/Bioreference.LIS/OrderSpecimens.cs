using System;
using System.Data;
using Bioreference.Common.Lab;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderSpecimens : DataClassBase
    {

        private OrderSpecimenList m_list;
        private Order m_parent;

        internal OrderSpecimens(Order parent)
        {
            m_parent = parent;
            m_list = new OrderSpecimenList(m_parent);
        }

        public OrderSpecimenList List
        {
            get
            {
                return m_list;
            }
        }

        internal void Load(DataTable table)
        {

            OrderSpecimen os;

            foreach (DataRow r in table.Rows)
            {
                os = new OrderSpecimen(m_parent);
                os.Load(r);
                m_list.Add(os);
            }

        }

        internal void Update()
        {

            m_list.Update();

        }

        public OrderSpecimen AddSpecimen(Specimen specimen)
        {

            var os = new OrderSpecimen(m_parent, specimen);
            m_list.Add(os);
            return os;

        }

        public void RemoveSpecimen(string specimenCode)
        {

            foreach (OrderSpecimen s in m_list)
            {

                if ((s.SpecimenCode ?? "") == (specimenCode ?? ""))
                {
                    m_list.Remove(s);
                    break;
                }

            }

        }

        public override bool IsValid
        {
            get
            {
                return base.IsValid && List.IsValid;
            }
        }

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_list.IsDirty;
            }
        }

    }
}