using System;
using System.Data;
using Bioreference.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CocAudit : DataClassBase
    {

        #region Private Members

        private int m_cocAuditId = 0;
        private int m_cocBatchId = 0;
        private int m_reportId = 0;
        private bool m_isDeleted = false;
        private DateTime m_createdDate = DateTime.Parse("1900-01-01");
        private string m_createdBy = "";
        private DateTime m_updatedDate = DateTime.Parse("1900-01-01");
        private string m_updatedBy = "";

        #endregion

        #region Constructor
        internal CocAudit()
        {
        }
        #endregion

        #region Public Properties
        public int CocAuditId
        {
            get
            {
                return m_cocAuditId;
            }
        }

        public int CocBatchId
        {
            get
            {
                return m_cocBatchId;
            }
        }

        public string ReportId
        {
            get
            {
                return m_reportId.ToString();
            }

        }

        public DateTime CreatedDate
        {
            get
            {
                return m_createdDate;
            }
        }

        public string CreatedBy
        {
            get
            {
                return m_createdBy;
            }
        }

        public DateTime UpdatedDate
        {
            get
            {
                return m_updatedDate;
            }
        }

        public string UpdatedBy
        {
            get
            {
                return m_updatedBy;
            }
        }

        public bool IsBatchAccessionDeleted
        {
            get
            {
                return m_isDeleted;
            }
        }
        #endregion

        internal void Load(DataRow row)
        {

            m_cocAuditId = Conversions.ToInteger(row["CocAuditId"]);
            m_cocBatchId = Conversions.ToInteger(row["CocBatchId"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);
            m_isDeleted = Conversions.ToBoolean(row["isDeleted"]);
            m_createdDate = Conversions.ToDate(row["CreatedDate"]);
            m_createdBy = Conversions.ToString(row["CreatedBy"]);
            m_updatedDate = Conversions.ToDate(row["UpdatedDate"]);
            m_updatedBy = Conversions.ToString(row["UpdatedBy"]);

            FlagClean();

        }

    }
}