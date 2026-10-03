using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderPhysician : Person
    {


        #region Private Members

        private Order m_parent;
        private string m_accountNumber = "";
        private OrderPhysicianType m_type = OrderPhysicianType.Primary;
        private List<string> m_auditItemList;

        #endregion

        #region Constructor

        internal OrderPhysician(Order parent)
        {
            m_dobRequired = false;
            m_parent = parent;
            m_auditItemList = new List<string>();
            FlagChild();

        }

        internal OrderPhysician(Order parent, OrderPhysicianType @type)
        {
            m_dobRequired = false;
            m_parent = parent;
            m_type = type;
            m_auditItemList = new List<string>();
            FlagChild();

            // MyBase.New causes this object to be dirty. We don't want to force a record to be persisted.
            // Me.FlagClean()
            // Me.PrimaryAddress.FlagClean()

        }

        #endregion

        #region Public Properties

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        protected override object ParentIdentifierId
        {
            get
            {
                return m_parent.AccessionIdentifier;
            }
        }

        protected override parentIdentifierType ParentIdentifierType
        {
            get
            {
                return m_parent.AccessionIdentifierType;
            }
        }

        public string AccountNumber
        {
            get
            {
                return m_accountNumber;
            }
        }

        public OrderPhysicianType Type
        {
            get
            {
                return m_type;
            }
        }

        [Audit("FirstName")]
        public override string FirstName
        {
            get
            {
                return m_firstName;
            }
            set
            {
                if ((m_firstName ?? "") != (value.Trim() ?? ""))
                {
                    m_firstName = value.Trim();
                    FlagDirty();
                }
            }
        }

        [Audit("LastName")]
        public override string LastName
        {
            get
            {
                return m_lastName;
            }
            set
            {
                if ((m_lastName ?? "") != (value.Trim() ?? ""))
                {
                    m_lastName = value.Trim();
                    FlagDirty();
                }
            }
        }

        public List<string> GetFormattedAuditItems
        {
            // get the audit item of this object as well as any object in the Save stream.
            get
            {
                return m_auditItemList;
            }
        }

        #endregion

        #region Data Functions

        internal new void Load(DataRow row)
        {

            // First we load all the base properties
            base.Load(row);

            m_id = Conversions.ToInteger(row["OrderPhysicianId"]);
            m_type = (OrderPhysicianType)Conversions.ToInteger(row["PhysicianType"]);

            FlagClean();

        }

        internal void Delete()
        {

            throw new Exception("OrderPhysician Delete has not been implemented.");

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@PhysicianId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@FirstName", DbType.String, m_firstName));
            paramList.Add((DbParameter)da.CreateParameter("@MiddleName", DbType.String, m_middleName));
            paramList.Add((DbParameter)da.CreateParameter("@LastName", DbType.String, m_lastName));
            paramList.Add((DbParameter)da.CreateParameter("@Title", DbType.String, m_title));
            paramList.Add((DbParameter)da.CreateParameter("@Suffix", DbType.String, m_suffix));
            paramList.Add((DbParameter)da.CreateParameter("@SSN", DbType.String, m_ssn));
            paramList.Add((DbParameter)da.CreateParameter("@GenderType", DbType.Int32, m_gender));
            paramList.Add((DbParameter)da.CreateParameter("@DOB", DbType.String, m_dob));
            paramList.Add((DbParameter)da.CreateParameter("@HomePhone", DbType.String, m_homePhone));
            paramList.Add((DbParameter)da.CreateParameter("@WorkPhone", DbType.String, m_workPhone));
            paramList.Add((DbParameter)da.CreateParameter("@FaxNumber", DbType.String, m_faxNumber));

            paramList.Add((DbParameter)da.CreateParameter("@PhysicianType", DbType.Int32, m_type));

            paramList.Add((DbParameter)da.CreateParameter("@StreetLine1", DbType.String, PrimaryAddress.StreetLine1));
            paramList.Add((DbParameter)da.CreateParameter("@StreetLine2", DbType.String, PrimaryAddress.StreetLine2));
            paramList.Add((DbParameter)da.CreateParameter("@City", DbType.String, PrimaryAddress.City));
            paramList.Add((DbParameter)da.CreateParameter("@State", DbType.String, PrimaryAddress.State));
            paramList.Add((DbParameter)da.CreateParameter("@Zip", DbType.String, PrimaryAddress.ZipCode));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name)); 

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OrderPhysician_Save", @params)["@PhysicianId"].Value);

            m_auditItemList.AddRange(PrimaryAddress.GetAuditItems());   // this issues a clean at the same time.
                                                                        // Me.PrimaryAddress.FlagClean()

            m_auditItemList.AddRange(GetAuditItemsAndClean());
            // Me.FlagClean()

        }

        #endregion


    }
}