using System;
using System.Collections.Generic;
using System.Data;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderAnswers : DataClassBase
    {

        private OrderAnswerList m_list;
        private Order m_parent;


        internal OrderAnswers(Order parent)
        {
            m_parent = parent;
            m_list = new OrderAnswerList(m_parent);
        }

        public OrderAnswerList List
        {
            get
            {
                return m_list;
            }
        }

        internal void Load(DataTable table)
        {

            OrderAnswer oa;
            foreach (DataRow r in table.Rows)
            {
                oa = new OrderAnswer(m_parent);
                oa.Load(r);
                m_list.Add(oa);
            }

        }

        internal void Update()
        {

            m_list.Update();

        }

        internal List<string> GetAuditItems()
        {
            var lst = new List<string>();

            foreach (OrderAnswer c in m_list)
                lst.AddRange(c.GetFormattedAuditItems);

            return lst;

        }

        public OrderTest Find(long ordertestID)
        {

            foreach (OrderTest t in m_list)
            {
                if (t.Id == ordertestID)
                    return t;
            }

            return null;

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