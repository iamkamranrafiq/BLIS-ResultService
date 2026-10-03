using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderGuarantor : Person
    {

        #region Private/Protected Members

        private Relationship m_relation = Relationship.Unknown;
        private Order m_parent = null;
        private List<string> m_auditItemList;
        #endregion

        #region Constructor

        internal OrderGuarantor(Order parent)
        {
            m_parent = parent;
            m_auditItemList = new List<string>();
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

        public Order Parent
        {
            get
            {
                return m_parent;
            }
        }

        public Relationship Relation
        {
            get
            {
                return m_relation;
            }
            set
            {
                m_relation = value;
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

        #region Public Functions

        public void Delete()
        {

            DataFactory.Delete(new Criteria(m_id));

        }

        #endregion

        #region Data Functions

        internal new void Load(DataRow row)
        {

            // First we load all the base properties
            base.Load(row);

            m_relation = (Relationship)Conversions.ToInteger(row["RelationType"]);

            FlagClean();

        }

        protected override void DataFactory_Save()
        {

            Update();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);

            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@GuarantorId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@RelationType", DbType.Int32, m_relation));
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

            paramList.Add((DbParameter)da.CreateParameter("@StreetLine1", DbType.String, PrimaryAddress.StreetLine1));
            paramList.Add((DbParameter)da.CreateParameter("@StreetLine2", DbType.String, PrimaryAddress.StreetLine2));
            paramList.Add((DbParameter)da.CreateParameter("@City", DbType.String, PrimaryAddress.City));
            paramList.Add((DbParameter)da.CreateParameter("@State", DbType.String, PrimaryAddress.State));
            paramList.Add((DbParameter)da.CreateParameter("@Zip", DbType.String, PrimaryAddress.ZipCode));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OrderGuarantor_Save", @params)["@GuarantorId"].Value);

            m_auditItemList.AddRange(PrimaryAddress.GetAuditItems());   // this issues a clean at the same time.
                                                                        // Me.PrimaryAddress.FlagClean()

            m_auditItemList.AddRange(GetAuditItemsAndClean());
            // Me.FlagClean()

        }

        #endregion

        #region Internal Criteria Class

        [Serializable()]
        internal class Criteria
        {

            private int m_id;

            internal Criteria(int guarantorId)
            {
                m_id = guarantorId;
            }

            internal object GuarantorID
            {
                get
                {
                    return m_id;
                }
            }

        }

        #endregion

    }
}