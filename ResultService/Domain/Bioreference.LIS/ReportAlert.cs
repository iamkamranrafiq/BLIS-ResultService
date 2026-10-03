using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Lab;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportAlert : AuditDataClassBase
    {

        #region Private Members

        private long m_id;
        private RefAlert m_refAlert;
        private string m_analyteCode = "";
        private ReportAnalyte m_parent;
        private List<string> m_auditItemList;
        #endregion

        #region Constructor

        // Create a new ReportAlert based on Alert
        public ReportAlert(ReportAnalyte parent, Alert flag)
        {
            m_auditItemList = new List<string>();
            m_parent = parent;
            m_refAlert = new RefAlert(this);
            m_refAlert.Load(flag);
            FlagDirty();
            FlagChild();

        }

        // Load from database
        internal ReportAlert(ReportAnalyte parent, bool loadAudit = false)
        {
            m_auditItemList = new List<string>();
            m_parent = parent;
            LoadAudit = loadAudit;
            FlagChild();
        }

        #endregion

        public long Id
        {
            get
            {
                return m_id;
            }
        }

        public Alert Alert
        {
            get
            {
                return m_refAlert;
            }
        }

        public string AlertCode
        {
            get
            {
                return m_refAlert.Code;
            }
        }

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public List<string> GetFormattedAuditItems
        {
            // get the audit item of this object as well as any object in the Save stream.
            get
            {
                return m_auditItemList;
            }
        }
        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ReportAlertId", DbType.Int64, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, m_parent.ID));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_ReportAlert_Save", @params)["@ReportAlertId"].Value);

            if (IsNew)
            {
                m_refAlert.Update();
                m_auditItemList.AddRange(m_refAlert.GetFormattedAuditItems);
            }

            // NA: Get the list of AuditItem in this object after issuing the Clean so all properties are correctly populated.
            m_auditItemList.AddRange(GetAuditItemsAndClean(true));
            // Me.FlagClean(True)

        }

        internal void Delete()
        {

            // Save should come from report, all wrapped in a transaction

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@ReportAlertId", DbType.Int64, m_id);

            da.ExecuteNonQuery("lis_ReportAlert_Delete", @param);

            FlagDeleted();
            m_auditItemList.AddRange(GetAuditItemsAndClean());
            // Me.FlagClean()

        }

        internal void Load(DataRow row)
        {

            m_refAlert = new RefAlert(this);
            m_id = Conversions.ToLong(row["ReportAlertId"]);
            m_refAlert.Load(row);

            FlagClean();

        }

        #endregion


        #region Compare

        public override string CompareObjectDiplayName
        {
            get
            {
                return AlertCode;
            }
        }

        #endregion

    }
}