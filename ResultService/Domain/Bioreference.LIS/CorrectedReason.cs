using System.Data;
using Bioreference.Data;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class CorrectedReason : DataClassBase
    {

        #region Private Members

        private int m_reasonTypeId;
        private int m_primaryReasonId;
        private int m_secondaryReasonId;
        private int m_deptId;
        private int m_significanceId;
        private int m_controllableId;
        private bool m_nCEField;
        private string m_description;
        private string m_comments;
        private string m_testCode;
        private long m_reportAnalyteId;
        private int m_reportId;
        private string m_accessionNbr;
        private int m_isAgencyReportable;



        #endregion

        #region Properties

        public int ReasonTypeId
        {
            get
            {
                return m_reasonTypeId;
            }
        }

        public int PrimaryReasonId
        {
            get
            {
                return m_primaryReasonId;
            }
        }

        public int SecondaryReasonId
        {
            get
            {
                return m_secondaryReasonId;
            }
        }

        public int DeptId
        {
            get
            {
                return m_deptId;
            }
        }

        public int SignificanceId
        {
            get
            {
                return m_significanceId;
            }
        }

        public int ControllableId
        {
            get
            {
                return m_controllableId;
            }
        }

        public bool NCEField
        {
            get
            {
                return m_nCEField;
            }
        }

        public string Description
        {
            get
            {
                return m_description;
            }
        }

        public string Comments
        {
            get
            {
                return m_comments;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }

        public long ReportAnalyteId
        {
            get
            {
                return m_reportAnalyteId;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public int IsAgencyReportable
        {
            get
            {
                return m_isAgencyReportable;
            }
        }

        #endregion

        internal void Load(DataRow row)
        {

            m_testCode = Conversions.ToString(row["TestCode"]);
            m_reasonTypeId = Conversions.ToInteger(row["ReasonTypeId"]);
            m_primaryReasonId = Conversions.ToInteger(row["PrimaryReasonId"]);
            m_secondaryReasonId = Conversions.ToInteger(row["SecondaryReasonId"]);
            m_deptId = Conversions.ToInteger(row["DeptId"]);
            m_controllableId = Conversions.ToInteger(row["ControllableId"]);
            m_significanceId = Conversions.ToInteger(row["SignificanceId"]);
            m_nCEField = Conversions.ToBoolean(row["NCEField"]);
            m_description = Conversions.ToString(row["Description"]).NormalizeToWindows();
            m_comments = Conversions.ToString(row["Comments"]).NormalizeToWindows();
            m_reportId = Conversions.ToInteger(row["ReportId"]);
            m_reportAnalyteId = Conversions.ToLong(row["ReportAnalyteId"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);

            m_isAgencyReportable = Conversions.ToInteger(row["IsAgencyReportable"]);

            FlagClean();

        }
    }
}