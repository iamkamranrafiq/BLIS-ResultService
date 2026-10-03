using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Bioreference.Common;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderPatient : Person
    {

        #region Private Members

        private Order m_parent = null;
        private string m_patientId = ""; // Externally assigned patient id
        private string m_ageDescription = ""; // (ie. 45 years, 4 mos, 12 weeks, etc.)
        private IsFastingType m_isFasting = IsFastingType.Unknown;

        private int m_ageNbr = 0;
        private string m_ageType = string.Empty; // Y,M,W,D

        private string m_updateTrackingId = string.Empty;
        private List<string> m_auditItemList;
        private int m_prevAgeNbr = 0;
        private string m_prevAgeType = string.Empty;
        private string m_prevDob = string.Empty;
        private Gender m_prevGender;

        #endregion

        #region Constructor


        internal OrderPatient(Order parent, bool loadAudit) : base()
        {
            m_parent = parent;
            m_auditItemList = new List<string>();

            // AG - 11/5/09 - Remove requirement - cases where external system does not have.
            // Me.m_lastNameRequired = True
            LoadAudit = loadAudit;


        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the patient's dob back to blank
        /// </summary>
        /// <remarks></remarks>
        public void ResetDOB()
        {
            m_dob = "";
            base.Rules.Assert("DateOfBirth", "", Conversions.ToString(false));
            FlagDirty();
        }

        #endregion

        #region Public Properties

        [Audit("UpdateTrackingId")]
        public string UpdateTrackingId
        {
            get
            {
                return m_updateTrackingId;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_updateTrackingId ?? ""))
                {
                    m_updateTrackingId = value.Trim();
                    FlagDirty();
                }
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

        /// <summary>
        /// Externally assigned Patient Identification.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string PatientId
        {
            get
            {
                return m_patientId;
            }
            set
            {
                if ((m_patientId ?? "") != (value.Trim() ?? ""))
                {
                    m_patientId = value.Trim();
                    FlagDirty();
                }
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
                if ((m_firstName ?? "") != (value.Trim() ?? "")) // 'OrElse value.Trim() = "" Then
                {
                    m_firstName = value.Trim();
                    // Removed for now, failing on channel interface.
                    // MyBase.BaseRules.Assert("FirstName", "First name is invalid.", m_firstName = "")
                    FlagDirty();
                }
            }
        }

        [Audit("IsFasting")]
        public IsFastingType IsFasting
        {
            get
            {
                return m_isFasting;
            }
            set
            {
                if (m_isFasting != value)
                {
                    m_isFasting = value;
                    FlagDirty();
                }
            }
        }

        /// <summary>
        /// Used in cases where possibly birthdate is not known
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Not persisted to the database")]
        public string Age
        {
            get
            {
                return m_ageDescription;
            }
            set
            {
                if ((m_ageDescription ?? "") != (value.Trim() ?? ""))
                {
                    m_ageDescription = value.Trim();
                    // Me.FlagDirty()
                }
            }
        }

        public int PrevAgeNumber
        {
            get
            {
                return m_prevAgeNbr;
            }
        }

        [Audit("AgeNumber")]
        public int AgeNumber
        {
            get
            {
                return m_ageNbr;
            }
            set
            {
                if (m_ageNbr != value)
                {
                    m_ageNbr = value;
                    m_parent.UpdateStatus.AddProperty("OrderPatient.AgeNumber");
                    FlagDirty();
                }
            }
        }

        public string PrevAgeType
        {
            get
            {
                return m_prevAgeType;
            }
        }

        [Audit("AgeType")]
        public string AgeType
        {
            get
            {
                return m_ageType;
            }
            set
            {
                if ((m_ageType ?? "") != (value.Trim() ?? ""))
                {
                    m_ageType = value.Trim();
                    m_parent.UpdateStatus.AddProperty("OrderPatient.AgeType");
                    FlagDirty();
                }
            }
        }

        [Audit("LastName")]
        public override string LastName
        {
            get
            {
                return base.LastName;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_lastName ?? "")) // 'OrElse value.Trim() = "" Then
                {
                    base.LastName = value.Trim();
                    string err = "";

                    // Removed for now, failing on channel interface.
                    // If Not System.Text.RegularExpressions.Regex.IsMatch(m_lastName, "[A-Za-z]") AndAlso m_lastName <> "" Then
                    // err = "Last name is invalid."
                    if (m_lastName.Length < 2 && m_lastNameRequired)
                    {
                        err = "Patient last name must be at least 2 characters long.";
                    }
                    else
                    {
                        err = "";
                    }
                    base.Rules.Assert("LastName", err, Conversions.ToString(!string.IsNullOrEmpty(err)));
                    FlagDirty();
                }
            }
        }

        public string PrevDateOfBirth
        {
            get
            {
                return m_prevDob;
            }
        }

        [Audit("DateOfBirth")]
        public override string DateOfBirth
        {
            get
            {
                return m_dob;
            }
            set
            {

                if ((m_dob ?? "") != (value.Trim() ?? ""))
                {

                    string ruleError = string.Empty;
                    var dob = default(DateTime);

                    m_dob = value.Trim();
                    if (!string.IsNullOrEmpty(m_dob) && DateTime.ParseExact(m_dob, "MM/dd/yyyy", CultureInfo.InvariantCulture) != dob)
                    {

                        dob = DateTime.ParseExact(m_dob, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                        m_dob = dob.ToString("MM/dd/yyyy");
                        if (Regex.IsMatch(value.Trim(), @"\/\d{2}$"))	// 'If pass in 2 digit year
                        {
                            if (dob > DateTime.Now)
                            {
                                base.DateOfBirth = Conversions.ToString(dob.AddYears(-100));
                            }
                        }

                        m_parent.UpdateStatus.AddProperty("OrderPatient.DateOfBirth");

                        ruleError = "";
                    }
                    else
                    {
                        ruleError = "Date of birth is invalid.";
                    }

                    base.Rules.Assert("DateOfBirth", ruleError, Conversions.ToString(!string.IsNullOrEmpty(ruleError)));
                    FlagDirty();

                }

            }
        }

        public Gender PrevGender
        {
            get
            {
                return m_prevGender;
            }
        }

        [Audit("Gender")]
        public override Gender Gender
        {
            get
            {
                return m_gender;
            }
            set
            {
                if (m_gender != value)
                {

                    base.Gender = value;

                    m_parent.UpdateStatus.AddProperty("OrderPatient.Gender");

                    if (!IsRuleNull(base.Rules.FetchRule("Gender")))
                    {
                        m_parent.EvaluateRules(this, "Gender");
                    }

                }
            }
        }

        private bool IsRuleNull(Data.Rule r)
        {
            return r.Description == null && r.RuleName == null;
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

            m_id = Conversions.ToInteger(row["PatientID"]);
            m_patientId = Conversions.ToString(row["PatientIdentifier"]);
            m_isFasting = (IsFastingType)Conversions.ToInteger(row["IsFasting"]);
            m_ageNbr = Conversions.ToInteger(row["AgeNbr"]);
            m_ageType = Conversions.ToString(row["AgeType"]);
            m_updateTrackingId = Conversions.ToString(row["UpdateTrackingId"]);

            // This needs to be loaded after the identifier is set.
            base.Load(row);
            m_prevAgeNbr = m_ageNbr;
            m_prevAgeType = m_ageType;
            m_prevDob = m_dob;
            m_prevGender = base.Gender;
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

            paramList.Add((DbParameter)da.CreateParameter("@PatientId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@PatientIdentifier", DbType.String, m_patientId));
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

            paramList.Add((DbParameter)da.CreateParameter("@StreetLine1", DbType.String, PrimaryAddress.StreetLine1));
            paramList.Add((DbParameter)da.CreateParameter("@StreetLine2", DbType.String, PrimaryAddress.StreetLine2));
            paramList.Add((DbParameter)da.CreateParameter("@City", DbType.String, PrimaryAddress.City));
            paramList.Add((DbParameter)da.CreateParameter("@State", DbType.String, PrimaryAddress.State));
            paramList.Add((DbParameter)da.CreateParameter("@Zip", DbType.String, PrimaryAddress.ZipCode));
            paramList.Add((DbParameter)da.CreateParameter("@IsFasting", DbType.Int32, m_isFasting));
            paramList.Add((DbParameter)da.CreateParameter("@AgeNbr", DbType.String, m_ageNbr));
            paramList.Add((DbParameter)da.CreateParameter("@AgeType", DbType.String, m_ageType));
            paramList.Add((DbParameter)da.CreateParameter("@UpdateTrackingId", DbType.String, m_updateTrackingId));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name)); 

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OrderPatient_Save", @params)["@PatientId"].Value);

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

            private int m_custId;

            internal Criteria(int customerID)
            {
                m_custId = customerID;
            }

            internal object CustomerID
            {
                get
                {
                    return m_custId;
                }
            }

        }

        #endregion

    }
}