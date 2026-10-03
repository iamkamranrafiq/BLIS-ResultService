using System;
using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{
    public class TNPPending
    {
        private DateTime m_dos;
        private string m_accessionNbr;
        private resultStatusType m_resultStatus;
        private string m_testCode;
        private string m_panelCode;

        public DateTime Dos
        {
            get
            {
                return m_dos;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public resultStatusType ResultStatus
        {
            get
            {
                return m_resultStatus;
            }
        }


        public string PanelCode
        {
            get
            {
                return m_panelCode;
            }
        }

        public string TestCode
        {
            get
            {
                return m_testCode;
            }
        }

        public DateTime DateOfService { get; set; }

        public TNPPending(string testCode, DateTime dos, string accessionNbr, resultStatusType resultStatus, string panelCode)
        {
            m_dos = dos;
            m_accessionNbr = accessionNbr;
            m_resultStatus = resultStatus;
            m_testCode = testCode;
            m_panelCode = panelCode;

        }

        public TNPPending()
        {
        }

        #region Data Functions

        internal void Load(DataRow row)
        {
            m_accessionNbr = Conversions.ToString(row["accessionnbr"]);
            m_resultStatus = (resultStatusType)Conversions.ToInteger(row["ResultStatus"]);
            m_testCode = Conversions.ToString(row["AnalyteCode"]);
            m_panelCode = Conversions.ToString(row["PanelCode"]);
            if (!(row["DateServiced"] is DBNull))
            {
                m_dos = Conversions.ToDate(row["DateServiced"]);
            }

        }

        #endregion
    }
}