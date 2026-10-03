using System;
using System.Data;
using Bioreference.Common.Client;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderICD9s : DataClassBase
    {

        private OrderICD9List m_list;
        private Order m_parent;

        internal OrderICD9s(Order parent)
        {
            m_parent = parent;
            m_list = new OrderICD9List(m_parent);
        }

        public OrderICD9List List
        {
            get
            {
                return m_list;
            }
        }


        internal void Load(DataTable table)
        {

            OrderICD9 ot;

            foreach (DataRow r in table.Rows)
            {
                ot = new OrderICD9(m_parent);
                ot.Load(r);
                m_list.Add(ot);
            }

        }

        internal void Update()
        {

            m_list.Update();

        }

        public void RemoveICD9(string code)
        {

            foreach (OrderICD9 i in List)
            {
                if ((i.Code ?? "") == (code ?? ""))
                    List.Remove(i);
                return;
            }

        }

        public OrderICD9 AddICD9(ICD9 icd9)
        {

            var i = new OrderICD9(m_parent);
            m_list.Add(i);
            return i;

        }

        public OrderICD9 AddICD9(string code, string description)
        {

            var i = new OrderICD9(m_parent, code, description);
            m_list.Add(i);
            return i;

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


        /// <summary>
    /// Returns a comma delimited list of ICD9 codes
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
        public override string ToString()
        {

            var sb = new System.Text.StringBuilder();
            foreach (OrderICD9 i in List)
                sb.Append(string.Concat(i.Code, ","));

            return sb.ToString().Substring(0, sb.ToString().Length - 1);

        }

    }
}