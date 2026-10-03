using System;
using System.Data;
using Bioreference.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class VertexReleasedAnalyte : DataClassBase
    {

        #region Private Members

        private int m_vertexPendingID = 0;
        private string m_accessionNumber = "";
        private string m_testCode = "";
        private string m_testName = "";
        private string m_patientName = "";
        private string m_accountNumber = "";
        private DateTime m_dateOfService = DateTime.Parse("1900-01-01");
        private bool m_isReportable = true;
        private int m_resultStatus = 0;
        private DateTime m_releaseDate = DateTime.Parse("1900-01-01");
        private int m_transmitStatus = 0;
        private string m_recType = "";
        private int m_reportId = 0;

        #endregion

        #region Public Properties
        public int VertexPendingID
        {
            get
            {
                return m_vertexPendingID;
            }
        }

        public string AccessionNumber
        {
            get
            {
                return m_accessionNumber;
            }
        }

        public string TestCode
        {
            get
            {
                return m_testCode;
            }
        }

        public string TestName
        {
            get
            {
                return m_testName;
            }
        }

        public string PatientName
        {
            get
            {
                return m_patientName;
            }
        }

        public string AccountNumber
        {
            get
            {
                return m_accountNumber;
            }
        }

        public DateTime DateOfService
        {
            get
            {
                return m_dateOfService;
            }
        }

        public bool IsReportable
        {
            get
            {
                return m_isReportable;
            }
        }

        public int ResultStatus
        {
            get
            {
                return m_resultStatus;
            }
        }

        public DateTime ReleaseDate
        {
            get
            {
                return m_releaseDate;
            }
        }

        public int TransmitStatus
        {
            get
            {
                return m_transmitStatus;
            }
        }

        public string RecType
        {
            get
            {
                return m_recType;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }

        #endregion

        #region Constructor
        internal VertexReleasedAnalyte()
        {
        }
        #endregion


        internal void Load(DataRow row)
        {

            m_vertexPendingID = Conversions.ToInteger(row["VertexPendingID"]);
            m_accessionNumber = Conversions.ToString(row["AccessionNumber"]);
            m_testCode = Conversions.ToString(row["TestCode"]);
            m_testName = Conversions.ToString(row["TestName"]);
            m_patientName = Conversions.ToString(row["PatientName"]);
            m_accountNumber = Conversions.ToString(row["AccountNumber"]);
            m_dateOfService = Conversions.ToDate(row["DateOfService"]);
            m_isReportable = Conversions.ToBoolean(row["IsReportable"]);
            m_resultStatus = Conversions.ToInteger(row["ResultStatus"]);
            m_releaseDate = Conversions.ToDate(row["ReleaseDate"]);
            m_transmitStatus = Conversions.ToInteger(row["TransmitStatus"]);
            m_recType = Conversions.ToString(row["RecType"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);

            FlagClean();

        }
    }
}