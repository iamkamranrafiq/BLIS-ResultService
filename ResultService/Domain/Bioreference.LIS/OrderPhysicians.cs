using System;
using System.Collections.Generic;
using System.Data;
using Bioreference.Common;
using Bioreference.Common.Client;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderPhysicians : DataClassBase
    {

        private OrderPhysicianList m_list;
        private Order m_parent;

        internal OrderPhysicians(Order parent)
        {
            m_parent = parent;
            m_list = new OrderPhysicianList(m_parent);
        }

        public OrderPhysicianList List
        {
            get
            {
                return m_list;
            }
        }

        internal void Load(DataTable table)
        {

            OrderPhysician op;

            foreach (DataRow r in table.Rows)
            {
                op = new OrderPhysician(m_parent);
                op.Load(r);
                m_list.Add(op);
            }

        }

        internal void Update()
        {

            m_list.Update();

        }

        internal List<string> GetAuditItems()
        {
            var lst = new List<string>();

            foreach (OrderPhysician c in m_list)
                lst.AddRange(c.GetFormattedAuditItems);

            return lst;

        }

        /// <summary>
    /// Will remove every instance of the physician passed in, excluding the primary physician.
    /// </summary>
    /// <param name="physicianId"></param>
    /// <remarks></remarks>
        public void RemovePhysician(int physicianId)
        {

            foreach (OrderPhysician p in m_list)
            {

                if (p.ID == physicianId)
                {
                    m_list.Remove(p);
                }

            }

        }

        public OrderPhysician AddPhysician(Physician physician, OrderPhysicianType @type)
        {

            if (!(physician == null))
            {

                var phys = new OrderPhysician(m_parent, type);

                phys.FirstName = physician.FirstName;
                phys.LastName = physician.LastName;
                phys.MiddleName = physician.MiddleName;
                phys.Title = physician.Title;
                phys.Suffix = physician.Suffix;
                phys.SocialSecurityNum = physician.SocialSecurityNum;
                phys.Gender = physician.Gender;
                phys.DateOfBirth = physician.DateOfBirth;
                phys.HomePhoneNumber = physician.HomePhoneNumber;
                phys.WorkPhoneNumber = physician.WorkPhoneNumber;
                phys.FaxNumber = physician.FaxNumber;
                phys.PrimaryAddress.StreetLine1 = physician.PrimaryAddress.StreetLine1;
                phys.PrimaryAddress.StreetLine2 = physician.PrimaryAddress.StreetLine2;
                phys.PrimaryAddress.City = physician.PrimaryAddress.City;
                phys.PrimaryAddress.State = physician.PrimaryAddress.State;
                // 'Rest of properties
                phys.PrimaryAddress.ZipCode = physician.PrimaryAddress.ZipCode;

                m_list.Add(phys);

                return phys;
            }

            else
            {
                return null;
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