using System;
using System.Data;
using Bioreference.Common;
using Bioreference.Common.Client;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderPatientInsurances : DataClassBase
    {

        private OrderPatientInsuranceList m_list;
        private Order m_parent;

        internal OrderPatientInsurances(Order parent)
        {
            m_parent = parent;
            m_list = new OrderPatientInsuranceList(m_parent);
        }

        public OrderPatientInsuranceList List
        {
            get
            {
                return m_list;
            }
        }


        internal void Load(DataTable table)
        {

            OrderPatientInsurance op;

            foreach (DataRow r in table.Rows)
            {
                op = new OrderPatientInsurance(m_parent);
                op.Load(r);
                m_list.Add(op);
            }

        }

        internal void Update()
        {

            m_list.Update();

        }

        public OrderPatientInsurance AddInsurance(InsuranceCompany insuranceCo, Relationship relation = Relationship.Unknown)
        {

            var insurance = new OrderPatientInsurance(m_parent, insuranceCo);
            insurance.Insured.Relation = relation;
            m_list.Add(insurance);

            return insurance;

        }

        public OrderPatientInsurance AddInsurance(InsuranceCompanyInfo insuranceCo, Relationship relation = Relationship.Unknown)
        {

            var insurance = new OrderPatientInsurance(m_parent);
            insurance.InsuranceCode = insuranceCo.Code;
            insurance.InsuranceCompany = insuranceCo.Name;
            insurance.Insured.Relation = relation;
            m_list.Add(insurance);

            return insurance;

        }

        public OrderPatientInsurance AddInsurance()
        {

            var insurance = new OrderPatientInsurance(m_parent);
            m_list.Add(insurance);

            return insurance;

        }

        public OrderPatientInsurance Find(int orderPatientInsuranceId)
        {

            foreach (OrderPatientInsurance o in List)
            {
                if (o.Id == orderPatientInsuranceId)
                {
                    return o;
                }
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