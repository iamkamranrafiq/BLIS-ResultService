using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Lab;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RapidResultAnalyte : AuditDataClassBase
    {

        #region Private Members

        private int m_id = 0;
        private RapidResult m_parent;

        private string m_dob = "";
        private int m_ageNbr = 0;
        private string m_ageType = "";
        private Common.Gender m_gender;
        private string m_patientName = "";
        private DateTime m_dateServiced;
        private DateTime m_dateCollected = DateTime.Now;

        private long m_reportAnalyteId = 0L;
        private int m_reportId = 0;

        private string m_analyteCode = "";
        private string m_analyteName = "";
        private string m_resultValue = "";
        private string m_accessionNbr = "";
        internal transmitStatusType m_transmitStatus = transmitStatusType.None;
        internal resultStatusType m_resultStatus = resultStatusType.Preliminary;
        internal string m_performingFacility = "";
        internal string m_accessioningFacility = "";

        private bool m_isDeactivated = false;
        private RefAnalyte m_analyte = null;

        private int m_analyteSortOrder = 0;
        private bool m_analyteIsReference = false;

        private List<ReportAnalyteHistory> m_listHistory = new List<ReportAnalyteHistory>();
        private List<ReportAnalyteHistory> m_listHistoryDelete = new List<ReportAnalyteHistory>();

        internal bool m_needToPushResult = false;  // Will let us know if results need to be pushed to report objects.

        private int m_accountStatusLevel = 0;

        private string m_commentsToAdd = "";

        private bool m_isControl = false;
        private int m_rowNumber = 0;

        private bool m_has4ktest = false;


        #endregion

        #region Constructor

        internal RapidResultAnalyte(RapidResult parent, string accessionNbr, bool isControl, int rowNumber) : this(parent, rowNumber)
        {
            m_accessionNbr = accessionNbr;
            m_isControl = isControl;
        }
        internal RapidResultAnalyte(RapidResult parent, int rowNumber)
        {
            m_parent = parent;
            m_rowNumber = rowNumber;
            FlagDirty();
            FlagChild();
        }

        #endregion

        #region Public Properties

        public string PerformingFacility
        {
            get
            {
                return m_performingFacility;
            }
        }
        public string AccessioningFacility
        {
            get
            {
                return m_accessioningFacility;
            }
        }
        public int RowNumber
        {
            get
            {
                return m_rowNumber;
            }
        }
        public bool IsControl
        {
            get
            {
                return m_isControl;
            }
        }
        public bool Has4kTest
        {
            get
            {
                return m_has4ktest;
            }
        }
        public string CommentsToAdd
        {
            get
            {
                return m_commentsToAdd;
            }
            set
            {
                if (IsReference)
                    return; // Don't update ReadOnly reference analytes
                if ((m_commentsToAdd ?? "") != (value.Trim() ?? ""))
                {
                    m_commentsToAdd = value.Trim();
                    m_needToPushResult = true;
                    FlagDirty();
                }
            }

        }

        public RapidResult Parent
        {
            get
            {
                return m_parent;
            }
        }

        public bool IsReference
        {
            get
            {
                return m_analyteIsReference;
            }
        }

        public int SortOrder
        {
            get
            {
                return m_analyteSortOrder;
            }
        }

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public long ReportAnalyteId
        {
            get
            {
                return m_reportAnalyteId;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }

        public string AnalyteCode
        {
            get
            {
                return m_analyteCode;
            }
        }

        public string AnalyteName
        {
            get
            {
                return m_analyteName;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public string AccessionIdentifier
        {
            get
            {
                return string.Concat(m_accessionNbr, "-", m_dateServiced.ToString("MMddyyyy"));
            }
        }

        /// <summary>
    /// Used as a indicator to display on RRE worksheet
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public object AccountStatusLevel
        {
            get
            {
                return m_accountStatusLevel;
            }
        }

        public ReportAnalyteHistory[] ResultHistory
        {
            get
            {
                return m_listHistory.ToArray();
            }
        }

        [Audit("ResultValue")]
        public string ResultValue
        {
            get
            {
                return m_resultValue;
            }
            set
            {
                if (IsReference)
                    return; // Don't update ReadOnly reference analytes
                if ((m_resultValue ?? "") != (value.Trim() ?? ""))
                {
                    m_resultValue = value.Trim();
                    m_needToPushResult = true;
                    FlagDirty();
                }
            }
        }

        [Audit("IsDeactivated")]
        public bool IsDeactivated
        {
            get
            {
                return m_isDeactivated;
            }
            set
            {
                if (m_isDeactivated != value)
                {
                    m_isDeactivated = value;
                    FlagDirty();
                }
            }
        }

        public resultStatusType ResultStatus
        {
            get
            {
                return m_resultStatus;
            }
        }

        public transmitStatusType TransmitStatus
        {
            get
            {
                return m_transmitStatus;
            }
        }

        public RefAnalyte Analyte
        {
            get
            {
                return m_analyte;
            }
        }

        public string DOB
        {
            get
            {
                return m_dob;
            }
        }

        public int AgeNbr
        {
            get
            {
                return m_ageNbr;
            }
        }

        public string AgeType
        {
            get
            {
                return m_ageType;
            }
        }

        public Common.Gender Gender
        {
            get
            {
                return m_gender;
            }
        }

        public string PatientName
        {
            get
            {
                return m_patientName;
            }
        }

        public DateTime DateServiced
        {
            get
            {
                return m_dateServiced;
            }
        }
        public DateTime CalcDobFromDate
        {
            get
            {
                return m_dateCollected;
            }
        }

        protected override object ParentIdentifierId
        {
            get
            {
                return $"{m_accessionNbr}-{m_dateServiced:MMddyyyy}";
            }
        }

        protected override parentIdentifierType ParentIdentifierType
        {
            get
            {
                return parentIdentifierType.AccessionNbr_ServiceDate;
            }
        }

        #endregion

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@RapidResultAnalyteId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@RapidResultId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, m_reportAnalyteId));
            paramList.Add((DbParameter)da.CreateParameter("@ReportId", DbType.Int32, m_reportId));
            paramList.Add((DbParameter)da.CreateParameter("@ResultValue", DbType.String, m_resultValue));
            paramList.Add((DbParameter)da.CreateParameter("@IsDeactivated", DbType.Boolean, m_isDeactivated));
            paramList.Add((DbParameter)da.CreateParameter("@IsReference", DbType.Boolean, m_analyteIsReference));
            paramList.Add((DbParameter)da.CreateParameter("@ControlNumber", DbType.String, Interaction.IIf(m_reportAnalyteId.Equals(0L), m_accessionNbr, "")));
            paramList.Add((DbParameter)da.CreateParameter("@TestCode", DbType.String, m_analyteCode));
            paramList.Add((DbParameter)da.CreateParameter("@RowNumber", DbType.Int32, m_rowNumber));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_RapidResultAnalyte_Save", @params)["@RapidResultAnalyteId"].Value);

            // 'If any report history was deleted from the RapidResultAnalyte, we call delete here.
            foreach (ReportAnalyteHistory h in m_listHistoryDelete)
                h.Delete();
            m_listHistoryDelete.Clear();

            FlagClean(true);

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@RapidResultAnalyteId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_RapidResultAnalyte_Delete", @param);

            FlagDeleted();
            FlagClean(true);

        }

        /// <summary>
    /// Used to Add from an existing RapidResultAnalyte
    /// </summary>
    /// <param name="a"></param>
    /// <remarks></remarks>
        internal void Load(RapidResultAnalyte a)
        {

            m_reportId = a.ReportId;
            m_reportAnalyteId = a.ReportAnalyteId;
            m_analyteCode = a.AnalyteCode;
            m_analyteName = a.AnalyteName;
            m_resultValue = a.ResultValue;
            m_analyteSortOrder = a.SortOrder;
            m_transmitStatus = a.TransmitStatus;
            m_resultStatus = a.ResultStatus;
            m_gender = a.Gender;
            m_dob = a.DOB;
            m_patientName = a.PatientName;
            m_dateServiced = a.DateServiced;
            m_dateCollected = a.CalcDobFromDate;
            m_analyteIsReference = a.IsReference;
            m_accountStatusLevel = Conversions.ToInteger(a.AccountStatusLevel);
            if (!m_isControl)
            {
                m_accessionNbr = a.AccessionNbr;
            }
            m_performingFacility = a.PerformingFacility;
            m_accessioningFacility = a.AccessioningFacility;
            m_analyte = a.Analyte;
            m_has4ktest = a.Has4kTest;

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["RapidResultAnalyteId"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);
            m_reportAnalyteId = Conversions.ToLong(row["ReportAnalyteId"]);
            if (m_reportAnalyteId == 0L)
                m_isControl = true;
            m_analyteCode = Conversions.ToString(row["AnalyteCode"]);
            m_analyteName = Conversions.ToString(row["AnalyteName"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            m_resultValue = Conversions.ToString(row["ResultValue"]);
            m_analyteSortOrder = Conversions.ToInteger(row["SortOrder"]);
            m_transmitStatus = (transmitStatusType)Conversions.ToInteger(row["TransmitStatus"]);
            m_patientName = Conversions.ToString(row["PatientName"]);
            m_dateServiced = Conversions.ToDate(row["DateServiced"]);
            m_dateCollected = Conversions.ToDate(row["DateCollected"]);
            m_isDeactivated = Conversions.ToBoolean(row["IsDeactivated"]);
            m_analyteIsReference = Conversions.ToBoolean(row["IsReference"]);
            m_accountStatusLevel = Conversions.ToInteger(row["AccountStatusLevel"]);
            m_rowNumber = Conversions.ToInteger(row["RowNumber"]);
            m_resultStatus = (resultStatusType)Conversions.ToInteger(row["ResultStatus"]);
            m_performingFacility = Conversions.ToString(row["PerformingFacility"]);
            m_accessioningFacility = Conversions.ToString(row["AccessioningFacility"]);

            m_has4ktest = Conversions.ToBoolean(row["Has4kTest"]);

            DateTime dob;
            if (DateTime.TryParse(Conversions.ToString(row["DOB"]), out dob))
                m_dob = Conversions.ToString(dob);

            m_ageNbr = Conversions.ToInteger(row["AgeNbr"]);
            m_ageType = Conversions.ToString(row["AgeType"]);

            m_gender = (Common.Gender)Conversions.ToInteger(row["GenderType"]);

            m_analyte = new RefAnalyte(null);
            m_analyte.Load(row);

            // If this is an existing RapidResultAnalyte
            if (m_id > 0)
            {
                FlagClean();
            }

        }

        public ComplexValue[] GetComplexDropDownList()
        {

            string flag = "";

            var f = SharedFunctions.GetMatchingResultFlag(m_analyte.ResultFlagRanges, m_gender, m_dob, m_ageNbr, m_ageType, default, m_performingFacility);

            if (!(f == null))
            {
                return f.Values.DropdownList;
            }
            else
            {
                // Return empty array
                var c = new List<ComplexValue>();
                return c.ToArray();
            }

        }

        #endregion

        public void SetPerformingFacility(string facilityCode)
        {
            if ((m_performingFacility ?? "") != (facilityCode ?? ""))
            {
                m_performingFacility = facilityCode;
            }
        }

        public RapidResultAnalyte Copy(bool resetValue)
        {

            RapidResultAnalyte r = (RapidResultAnalyte)MemberwiseClone();
            r.ResultValue = "";
            return r;

        }

        internal void AddHistory(ReportAnalyteHistory history)
        {
            m_listHistory.Add(history);
        }

        public void RemoveHistory(ReportAnalyteHistory history)
        {

            m_listHistory.Remove(history);
            m_listHistoryDelete.Add(history);

        }

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_listHistoryDelete.Count > 0;
            }
        }

    }
}