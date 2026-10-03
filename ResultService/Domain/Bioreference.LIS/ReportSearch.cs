using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportSearch : DataClassReadOnlyBase
    {

        private string m_accessionNbr = "";
        private DateTime m_searchStartDate = DateTime.Parse("1900-01-01");
        private DateTime m_searchEndDate = DateTime.Parse("1900-01-01");
        private string m_patientName = "";
        private string m_acctNumber = "";
        private transmitStatusType m_transmitStatus = transmitStatusType.NotSet;
        private ClinicalTrialFlag m_clinicalTrial = ClinicalTrialFlag.All;
        private long m_euid;
        private List<string> m_userDivisionCodes = new List<string>();
        private bool m_isSearch = false;

        public enum ClinicalTrialFlag
        {
            NonClinicalTrial = 0,
            ClinicalTrial = 1,
            All = 2
        }

        public ReportSearch()
        {
        }

        public Reports Execute()
        {

            return Reports.Fetch(m_acctNumber, m_patientName, m_accessionNbr, m_searchStartDate, m_searchEndDate, m_transmitStatus, m_euid, m_userDivisionCodes);

        }

        public static bool ReportExists(string accessionNbr, int orderid = 0)
        {

            return ((ReportSearch)DataFactory.Fetch(new Criteria(accessionNbr, orderid))).m_reportExists;

        }

        #region Public Properties

        public string AccessionNumber
        {
            get
            {
                return m_accessionNbr;
            }
            set
            {
                m_accessionNbr = value;
            }
        }

        public DateTime StartDate
        {
            get
            {
                return m_searchStartDate;
            }
            set
            {
                m_searchStartDate = value;
            }
        }

        public DateTime EndDate
        {
            get
            {
                return m_searchEndDate;
            }
            set
            {
                m_searchEndDate = value;
            }
        }

        public string PatientName
        {
            get
            {
                return m_patientName;
            }
            set
            {
                m_patientName = value;
            }
        }

        public string AccountNumber
        {
            get
            {
                return m_acctNumber;
            }
            set
            {
                m_acctNumber = value;
            }
        }

        public transmitStatusType TransmitStatus
        {
            get
            {
                return m_transmitStatus;
            }
            set
            {
                m_transmitStatus = value;
            }
        }

        public ClinicalTrialFlag ClinicalTrial
        {
            get
            {
                return m_clinicalTrial;
            }
            set
            {
                m_clinicalTrial = value;
            }
        }

        public long EUID
        {
            get
            {
                return m_euid;
            }
            set
            {
                m_euid = value;
            }
        }

        public List<string> UserDivisionCodes
        {
            get
            {
                return m_userDivisionCodes;
            }
            set
            {
                m_userDivisionCodes = value;
            }
        }

        #endregion

        private bool m_reportExists = false;

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            List<DbParameter> @params;
            @params = new List<DbParameter>();

            try
            {
                @params.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr));
                @params.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, c.OrderId));
                DataTable[] dt = da.ExecuteProcedure("lis_ReportExists_Fetch", @params.ToArray());

                if (dt[0].Rows.Count > 0)
                {
                    m_reportExists = true;
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        #region Criteria Class
        [Serializable()]
        public class Criteria
        {
            private string m_accessionNbr = "";
            private int m_orderId = 0;

            public Criteria(string accessionNbr, int orderid)
            {
                m_accessionNbr = accessionNbr;
                m_orderId = orderid;
            }
            public string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }
            public int OrderId
            {
                get
                {
                    return m_orderId;
                }
            }
        }
        #endregion

    }
}