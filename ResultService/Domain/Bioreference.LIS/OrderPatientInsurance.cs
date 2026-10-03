using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Client;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderPatientInsurance : DataClassBase
    {


        #region Private/Protected Members

        private int m_id = 0;
        private Order m_parent = null;
        private string m_insuranceCode = "";
        private string m_insuranceCompany = "";
        private string m_insuranceId = "";
        private OrderInsured m_insured;
        private string m_insuranceGroup = "";
        private OrderPatientInsuranceAnswerList m_insuranceAnswers;

        #endregion

        #region Constructor

        internal OrderPatientInsurance(Order parent)
        {
            m_parent = parent;
            m_insured = new OrderInsured(this);
            m_insuranceAnswers = new OrderPatientInsuranceAnswerList(this);
            FlagChild();
        }

        internal OrderPatientInsurance(Order parent, InsuranceCompany insurancCompany)
        {
            InsuranceCode = insurancCompany.Code;
            InsuranceCompany = insurancCompany.Name;
            m_parent = parent;
            m_insured = new OrderInsured(this);
            FlagChild();
            FlagDirty();
        }

        #endregion

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public OrderPatientInsuranceAnswerList AOEs
        {
            get
            {
                return m_insuranceAnswers;
            }
        }

        public OrderInsured Insured
        {
            get
            {
                return m_insured;
            }
        }

        public Order Parent
        {
            get
            {
                return m_parent;
            }
        }

        public string InsuranceCode
        {
            get
            {
                return m_insuranceCode;
            }
            set
            {
                if ((m_insuranceCode ?? "") != (value.Trim() ?? ""))
                {
                    m_insuranceCode = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string InsuranceID
        {
            get
            {
                return m_insuranceId;
            }
            set
            {
                if ((m_insuranceId ?? "") != (value.Trim() ?? ""))
                {
                    m_insuranceId = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string InsuranceCompany
        {
            get
            {
                return m_insuranceCompany;
            }
            set
            {
                if ((m_insuranceCompany ?? "") != (value.Trim() ?? ""))
                {
                    m_insuranceCompany = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string InsuranceGroup
        {
            get
            {
                return m_insuranceGroup;
            }
            set
            {
                if ((m_insuranceGroup ?? "") != (value.Trim() ?? ""))
                {
                    m_insuranceGroup = value.Trim();
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Public Functions

        public void SetInsuranceCompany(InsuranceCompany insuranceCo)
        {

            InsuranceCode = insuranceCo.Code;
            InsuranceCompany = insuranceCo.Name;

            m_insuranceAnswers.Clear();

            foreach (InsuranceCompanyQuestion i in insuranceCo.Questions)
            {
                var q = new OrderPatientInsuranceAnswer(this, i);
                m_insuranceAnswers.Add(q);
            }

        }

        public void Delete()
        {

            DataFactory.Delete(new Criteria(m_id));

        }

        #endregion

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();


            paramList.Add((DbParameter)da.CreateParameter("@InsuranceCompany", DbType.String, m_insuranceCompany));
            paramList.Add((DbParameter)da.CreateParameter("@InsuranceID", DbType.String, m_insuranceId));
            paramList.Add((DbParameter)da.CreateParameter("@InsuranceCode", DbType.String, m_insuranceCode));
            paramList.Add((DbParameter)da.CreateParameter("@InsuranceGroup", DbType.String, m_insuranceGroup));
            paramList.Add((DbParameter)da.CreateParameter("@OrderPatientInsuranceId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@FirstName", DbType.String, Insured.FirstName));
            paramList.Add((DbParameter)da.CreateParameter("@MiddleName", DbType.String, Insured.MiddleName));
            paramList.Add((DbParameter)da.CreateParameter("@LastName", DbType.String, Insured.LastName));
            paramList.Add((DbParameter)da.CreateParameter("@Title", DbType.String, Insured.Title));
            paramList.Add((DbParameter)da.CreateParameter("@Suffix", DbType.String, Insured.Suffix));
            paramList.Add((DbParameter)da.CreateParameter("@SSN", DbType.String, Insured.SocialSecurityNum));
            paramList.Add((DbParameter)da.CreateParameter("@GenderType", DbType.Int32, Insured.Gender));
            paramList.Add((DbParameter)da.CreateParameter("@DOB", DbType.String, Insured.DateOfBirth));
            paramList.Add((DbParameter)da.CreateParameter("@HomePhone", DbType.String, Insured.HomePhoneNumber));
            paramList.Add((DbParameter)da.CreateParameter("@WorkPhone", DbType.String, Insured.WorkPhoneNumber));
            paramList.Add((DbParameter)da.CreateParameter("@RelationType", DbType.Int32, Insured.Relation));

            paramList.Add((DbParameter)da.CreateParameter("@StreetLine1", DbType.String, Insured.PrimaryAddress.StreetLine1));
            paramList.Add((DbParameter)da.CreateParameter("@StreetLine2", DbType.String, Insured.PrimaryAddress.StreetLine2));
            paramList.Add((DbParameter)da.CreateParameter("@City", DbType.String, Insured.PrimaryAddress.City));
            paramList.Add((DbParameter)da.CreateParameter("@State", DbType.String, Insured.PrimaryAddress.State));
            paramList.Add((DbParameter)da.CreateParameter("@Zip", DbType.String, Insured.PrimaryAddress.ZipCode));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OrderPatientInsurance_Save", @params)["@OrderPatientInsuranceId"].Value);

            FlagClean();

            m_insuranceAnswers.Update();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["OrderPatientInsuranceId"]);
            m_insuranceCode = Conversions.ToString(row["InsuranceCode"]);
            m_insuranceCompany = Conversions.ToString(row["InsuranceCompany"]);
            m_insuranceId = Conversions.ToString(row["InsuranceId"]);
            m_insuranceGroup = Conversions.ToString(row["InsuranceGroup"]);

            m_insured.Load(row);

            FlagClean();

        }


        #endregion

        public override bool IsValid
        {
            get
            {
                return base.IsValid && Insured.IsValid;
            }
        }

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || Insured.IsDirty;
            }
        }

        #region Internal Criteria Class

        [Serializable()]
        internal class Criteria
        {

            private int m_id;

            internal Criteria(int insuredId)
            {
                m_id = insuredId;
            }

            internal object InsuredId
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