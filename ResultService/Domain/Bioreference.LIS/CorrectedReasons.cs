using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Bioreference.Data;

namespace Bioreference.LIS
{

    public class CorrectedReasons : DataClassBase
    {

        #region Private Members

        private string m_testCode;
        private int m_reasonTypeId;
        private int m_primaryReasonId;
        private int m_secondaryReasonId;
        private DataTable m_deptResponsible;
        private int m_significanceId;
        private int m_controllableId;
        private int m_nCEField;
        private string m_description;
        private string m_comments;
        private int m_reportId;
        private long m_reportAnalyteId;
        private string m_accessionNbr;
        private List<CorrectedReason> m_correctedReasonList = new List<CorrectedReason>();
        private int m_isAgencyReportable;
        private int m_causedBy;
        private bool m_criticalResult;
        private DataTable m_reportingDepartment;
        private string m_responsibleLab;
        private string m_orgPerformingFacility;
        private int m_NCECritical;


        #endregion

        #region Properties

        public List<CorrectedReason> CorrectedReasonList
        {
            get
            {
                return m_correctedReasonList;
            }
        }

        public string TestCode
        {
            get
            {
                return m_testCode;
            }
            set
            {
                m_testCode = value;
                FlagDirty();
            }
        }

        public int ReasonTypeId
        {
            get
            {
                return m_reasonTypeId;
            }
            set
            {
                m_reasonTypeId = value;
                FlagDirty();
            }
        }

        public int PrimaryReasonId
        {
            get
            {
                return m_primaryReasonId;
            }
            set
            {
                m_primaryReasonId = value;
                FlagDirty();
            }
        }

        public int SecondaryReasonId
        {
            get
            {
                return m_secondaryReasonId;
            }
            set
            {
                m_secondaryReasonId = value;
                FlagDirty();
            }
        }

        public DataTable DeptResponsible
        {
            get
            {
                return m_deptResponsible;
            }
            set
            {
                m_deptResponsible = value;
                FlagDirty();
            }
        }

        public int SignificanceId
        {
            get
            {
                return m_significanceId;
            }
            set
            {
                m_significanceId = value;
                FlagDirty();
            }
        }

        public int ControllableId
        {
            get
            {
                return m_controllableId;
            }
            set
            {
                m_controllableId = value;
                FlagDirty();
            }
        }

        public int CausedBy
        {
            get
            {
                return m_causedBy;
            }
            set
            {
                m_causedBy = value;
                FlagDirty();
            }
        }

        public int NCEField
        {
            get
            {
                return m_nCEField;
            }
            set
            {
                m_nCEField = value;
                FlagDirty();
            }
        }

        public bool CriticalResult
        {
            get
            {
                return m_criticalResult;
            }
            set
            {
                m_criticalResult = value;
                FlagDirty();
            }
        }

        public string Description
        {
            get
            {
                return m_description;
            }
            set
            {
                m_description = value;
                FlagDirty();
            }
        }

        public string Comments
        {
            get
            {
                return m_comments;
            }
            set
            {
                m_comments = value;
                FlagDirty();
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
            set
            {
                m_reportId = value;
                FlagDirty();
            }
        }

        public DataTable ReportingDepartment
        {
            get
            {
                return m_reportingDepartment;
            }
            set
            {
                m_reportingDepartment = value;
                FlagDirty();
            }
        }

        public long ReportAnalyteId
        {
            get
            {
                return m_reportAnalyteId;
            }
            set
            {
                m_reportAnalyteId = value;
                FlagDirty();
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
            set
            {
                m_accessionNbr = value;
                FlagDirty();
            }
        }

        public int IsAgencyReportable
        {
            get
            {
                return m_isAgencyReportable;
            }
            set
            {
                m_isAgencyReportable = value;
                FlagDirty();
            }
        }

        public string ResponsibleLab
        {
            get
            {
                return m_responsibleLab;

            }
            set
            {
                m_responsibleLab = value;

            }
        }

        public string OrgPerformingFacility
        {
            get
            {
                return m_orgPerformingFacility;

            }
            set
            {
                m_orgPerformingFacility = value;

            }
        }

        public int NCECritical
        {
            get
            {
                return m_NCECritical;

            }
            set
            {
                m_NCECritical = value;

            }
        }

        #endregion

        protected override void DataFactory_Save()
        {       
            var conn = new SqlConnection(Configuration.ConnectionString);
            SqlCommand cmd = null;
            string timestampId = "";

            try
            {

                conn.Open();
                cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "lis_CorrectedReason_Save";
                cmd.Parameters.AddWithValue("@TestCode", DbType.String).Value = m_testCode;
                cmd.Parameters.AddWithValue("@ReasonTypeId", DbType.Int64).Value = m_reasonTypeId;
                cmd.Parameters.AddWithValue("@PrimaryReasonId", DbType.Int64).Value = m_primaryReasonId;
                cmd.Parameters.AddWithValue("@SecondaryReasonId", DbType.Int64).Value = m_secondaryReasonId;
                cmd.Parameters.AddWithValue("@DeptResponsible", SqlDbType.Structured).Value = DatatableFilterbyTestCode(m_deptResponsible, m_testCode); ;
                cmd.Parameters.AddWithValue("@SignificanceId", DbType.Int64).Value = m_significanceId;
                cmd.Parameters.AddWithValue("@ControllableId", DbType.Int64).Value = m_controllableId;
                cmd.Parameters.AddWithValue("@NCEField", DbType.Int64).Value = m_nCEField;
                cmd.Parameters.AddWithValue("@Description", DbType.String).Value = m_description;
                cmd.Parameters.AddWithValue("@Comments", DbType.String).Value = m_comments ?? "";
                cmd.Parameters.AddWithValue("@ReportId", DbType.Int64).Value = m_reportId;
                cmd.Parameters.AddWithValue("@ReportAnalyteId", DbType.Int64).Value = m_reportAnalyteId;
                cmd.Parameters.AddWithValue("@AccessionNbr", DbType.String).Value = m_accessionNbr;
                cmd.Parameters.AddWithValue("@IsAgencyReportable", DbType.Int64).Value = m_isAgencyReportable;
                cmd.Parameters.AddWithValue("@CausedBy", DbType.Int64).Value = m_causedBy;
                cmd.Parameters.AddWithValue("@CriticalResult", DbType.Boolean).Value = m_criticalResult;
                cmd.Parameters.AddWithValue("@ReportingDepartment", SqlDbType.Structured).Value = DatatableFilterbyTestCode(m_reportingDepartment, m_testCode); ;
                cmd.Parameters.AddWithValue("@ResponsibleLab", DbType.String).Value = m_responsibleLab;
                cmd.Parameters.AddWithValue("OrgPerformingFacility", DbType.String).Value = m_orgPerformingFacility;
                cmd.Parameters.AddWithValue("NCECritical", DbType.String).Value = m_NCECritical;


                cmd.ExecuteReader();
            }

            catch (Exception ex)
            {
                ex.StackTrace.ToString();
            }
            finally
            {
                if (!(conn == null))
                {
                    conn.Close();
                    conn.Dispose();
                }
                if (!(cmd == null))
                {
                    cmd.Dispose();
                }
            }


        }
        private DataTable DatatableFilterbyTestCode(DataTable dt, string testCode)
        {
            DataTable datatble = new DataTable();

            // Copy columns from original DataTable
            foreach (DataColumn column in dt.Columns)
            {
                datatble.Columns.Add(column.ColumnName, column.DataType);
            }

            // Copy rows where TestCode matches
            foreach (DataRow row in dt.Rows)
            {
                if (row["TestCode"].ToString() == testCode)
                {
                    datatble.Rows.Add(row.ItemArray);
                }
            }

            return datatble;
        }     


        public void Load(DataTable table)
        {

            CorrectedReason p;
            foreach (DataRow r in table.Rows)
            {
                p = new CorrectedReason();
                p.Load(r);
                try
                {
                    m_correctedReasonList.Add(p);
                }
                catch (Exception ex)
                {
                    ex.StackTrace.ToString();
                }
            }
        }

    }
}