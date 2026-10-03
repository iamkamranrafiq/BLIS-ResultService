using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CocBatchAccession : AuditDataClassBase
    {

        private CocBatch m_parent;
        private int m_id = 0;
        private int m_reportId = 0;
        private string m_accessionNbr = "";
        private DateTime m_dateOfService;
        private string m_patientName;
        private bool m_isDeleted = false;

        #region Constructor

        internal CocBatchAccession(CocBatch parent)
        {
            m_parent = parent;
            FlagDirty();
            FlagChild();
        }

        internal CocBatchAccession(CocBatch parent, int reportId, string accessionNbr, DateTime dateOfService, string patientName)
        {
            m_parent = parent;
            m_reportId = reportId;
            m_dateOfService = dateOfService;
            m_accessionNbr = accessionNbr;
            m_patientName = patientName;
            FlagDirty();
            FlagChild();
        }

        #endregion

        #region Public Properties
        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public bool IsMarkedForDelete
        {
            get
            {
                return m_isDeleted;
            }
        }

        public int ID
        {
            get
            {
                return m_id;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }

        public DateTime DateOfService
        {
            get
            {
                return m_dateOfService;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public string PatientName
        {
            get
            {
                return m_patientName;
            }
        }

        #endregion


        public void MarkForDelete()
        {
            m_isDeleted = true;
        }

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@CocBatchAccessionId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@CocBatchId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@ReportId", DbType.Int32, m_reportId));
            paramList.Add((DbParameter)da.CreateParameter("@IsDeleted", DbType.Int32, m_isDeleted));

            if (!(CurrentUser == null))
            {
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));
            }
            else
            {
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, ""));
            }

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_CocBatchAccession_Save", @params)["@CocBatchAccessionId"].Value);

            FlagClean(true);

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[2];

            @param[0] = (DbParameter)da.CreateParameter("@CocBatchAccessionId", DbType.Int32, m_id);
            if (!(CurrentUser == null))
            {
                @param[1] = (DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name);
            }
            else
            {
                @param[1] = (DbParameter)da.CreateParameter("@UserName", DbType.String, "");
            }
            da.ExecuteNonQuery("lis_CocBatchAccession_Delete", @param);

            FlagDeleted();
            FlagClean(true);

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["CocBatchAccessionId"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);
            m_isDeleted = Conversions.ToBoolean(row["IsDeleted"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            m_dateOfService = Conversions.ToDate(row["DateServiced"]);
            m_patientName = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(row["LastName"], ", "), row["FirstName"]));

            FlagClean();

        }

        #endregion

    }
}