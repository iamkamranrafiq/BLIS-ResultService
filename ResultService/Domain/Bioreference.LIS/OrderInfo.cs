using System;
using System.Data;
using Bioreference.Common;
using Bioreference.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderInfo : DataClassBase
    {


        #region Private Members

        private Orders m_parent;
        private int m_id = 0;
        private string m_accountNumber = "";
        private RequisitionType m_reqType = RequisitionType.Unknown;
        private BillingType m_billType = BillingType.Unknown;
        private DateTime m_dateCollected;
        private string m_timeCollected = "";
        private DateTime m_dateServiced;
        private string m_patientFullName = "";
        private bool m_IsReportHold = false;

        #endregion

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string AccountNumber
        {
            get
            {
                return m_accountNumber;
            }
        }

        public RequisitionType ReqType
        {
            get
            {
                return m_reqType;
            }
        }

        public BillingType BillType
        {
            get
            {
                return m_billType;
            }
        }

        public DateTime DateOfCollection
        {
            get
            {
                return m_dateCollected;
            }
        }

        public string TimeOfCollection
        {
            get
            {
                return m_timeCollected;
            }
        }

        public DateTime DateOfService
        {
            get
            {
                return m_dateServiced;
            }
        }

        public string PatientFullName
        {
            get
            {
                return m_patientFullName;
            }
        }
        public bool IsReportHold
        {
            get
            {
                return m_IsReportHold;
            }
        }

        #endregion

        #region Constructor

        internal OrderInfo(Orders parent)
        {

            m_parent = parent;

        }

        #endregion

        #region Data Functions


        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["Id"]);
            m_accountNumber = Conversions.ToString(row["AccountNumber"]);
            m_reqType = (RequisitionType)Conversions.ToInteger(row["ReqType"]);
            m_billType = (BillingType)Conversions.ToInteger(row["BillType"]);
            m_dateCollected = Conversions.ToDate(row["DateCollected"]);
            m_timeCollected = Conversions.ToString(row["TimeCollected"]);
            m_dateServiced = Conversions.ToDate(row["DateServiced"]);
            m_patientFullName = Conversions.ToString(row["PatientFullName"]);
            m_IsReportHold = Conversions.ToBoolean(row["IsReportHold"]);

        }

        #endregion

    }
}