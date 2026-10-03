using System;
using System.Data;
using Bioreference.Data;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportInfo : DataClassReadOnlyBase
    {


        #region Private Members

        private int m_orderId = 0;
        private DateTime m_orderDate;
        private OrderPriority m_priority = OrderPriority.Unknown;
        private int m_pendingCount = 0;
        private int m_reportId = 0;
        private string m_accessionNbr = "";
        private resultStatusType m_resultStatus = resultStatusType.Pending;
        private int m_flagCount = 0;
        private transmitStatusType m_transmitStatus = transmitStatusType.None;
        private bool m_isClinicalTrial = false;
        private bool m_hasPrevValue = false;
        private long m_euid;
        private string m_patientName = "";
        private string m_acctNbr = "";
        private string m_divisionList = "";

        #endregion

        #region Constructor

        internal ReportInfo()
        {
        }

        internal ReportInfo(int orderId, int reportId, DateTime orderDate, OrderPriority priority, int pendingCount, resultStatusType resultStatus, string accessionNbr, int flagCount, transmitStatusType transmitStatus, bool isClinicalTrial, bool hasPreviousResultValue, long euid, string patientName, string acctNbr)
        {
            m_orderId = orderId;
            m_orderDate = orderDate;
            m_priority = priority;
            m_pendingCount = pendingCount;
            m_reportId = reportId;
            m_accessionNbr = accessionNbr;
            m_resultStatus = resultStatus;
            m_flagCount = flagCount;
            m_transmitStatus = transmitStatus;
            m_isClinicalTrial = isClinicalTrial;
            m_hasPrevValue = hasPreviousResultValue;
            m_euid = euid;
            m_patientName = patientName;
            m_acctNbr = acctNbr;
        }

        #endregion

        #region Public Properties

        public int OrderId
        {
            get
            {
                return m_orderId;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }

        public DateTime OrderDate
        {
            get
            {
                return m_orderDate;
            }
        }

        public OrderPriority Priority
        {
            get
            {
                return m_priority;
            }
        }

        public int PendingCount
        {
            get
            {
                return m_pendingCount;
            }
        }

        public resultStatusType ResultStatus
        {
            get
            {
                return m_resultStatus;
            }
        }

        public int FlagCount
        {
            get
            {
                return m_flagCount;
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
                return m_acctNbr;
            }
        }

        public bool HasPreviousResultValue
        {
            get
            {
                return m_hasPrevValue;
            }
        }

        /// <summary>
    /// Accession Number assigned on order creation.
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public long EUID
        {
            get
            {
                return m_euid;
            }
        }

        public transmitStatusType TransmitStatus
        {
            get
            {
                return m_transmitStatus;
            }
        }

        public bool IsClinicalTrial
        {
            get
            {
                return m_isClinicalTrial;
            }
        }

        public string DivisionList
        {
            get
            {
                return m_divisionList;
            }
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_orderId = Conversions.ToInteger(row["OrderId"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);
            m_orderDate = Conversions.ToDate(row["OrderDate"]);
            m_priority = (OrderPriority)Conversions.ToInteger(row["Priority"]);
            m_pendingCount = Conversions.ToInteger(row["PendingCount"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            m_resultStatus = (resultStatusType)Conversions.ToInteger(row["ResultStatus"]);
            m_flagCount = Conversions.ToInteger(row["FlagCount"]);
            m_transmitStatus = (transmitStatusType)Conversions.ToInteger(row["TransmitStatus"]);
            m_patientName = Conversions.ToString(row["PatientName"]);
            m_acctNbr = Conversions.ToString(row["AccountNumber"]);
            m_isClinicalTrial = Conversions.ToBoolean(row["IsClinicalTrial"]);
            m_hasPrevValue = Conversions.ToBoolean(Interaction.IIf(Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(row["PreviousValue"], "", false)), false, true));
            if (row.Table.Columns.Contains("EUID"))
                m_euid = Conversions.ToLong(row["EUID"]);
            if (row.Table.Columns.Contains("DivisionList"))
                m_divisionList = Conversions.ToString(row["DivisionList"]);

        }

        #endregion

    }
}