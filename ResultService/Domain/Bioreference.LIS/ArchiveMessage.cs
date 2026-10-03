using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;
using System.Data;
using System.Data.Common;

namespace Bioreference.LIS
{
    [Serializable()]
    public class ArchiveMessage : AuditDataClassBase
    {
        private long m_id = 0L;
        private string m_processName = string.Empty;
        private string m_message = string.Empty;
        private bool m_isProcessed = false;
        private bool m_isDeleted = false;

        private List<string> m_auditItemList;

        public ArchiveMessage()
        {
            m_auditItemList = new List<string>();
            FlagDirty();
        }
        public ArchiveMessage(string processName, string message, bool isProcessed, bool isDeleted)
        {
            m_auditItemList = new List<string>();
            m_processName = processName ?? string.Empty;
            m_message = message ?? string.Empty;
            m_isProcessed = isProcessed;
            m_isDeleted = isDeleted;
            FlagDirty();
        }

        internal ArchiveMessage(bool loadAudit = false)
        {
            m_auditItemList = new List<string>();
            LoadAudit = loadAudit;
            FlagChild();
        }

        public long ID
        {
            get { return m_id; }
        }

        public string ProcessName
        {
            get { return m_processName; }
            set
            {
                m_processName = value ?? string.Empty;
                FlagDirty();
            }
        }

        public string Message
        {
            get { return m_message; }
            set
            {
                m_message = value ?? string.Empty;
                FlagDirty();
            }
        }

        public bool IsProcessed
        {
            get { return m_isProcessed; }
            set
            {
                m_isProcessed = value;
                FlagDirty();
            }
        }

        public new bool IsDeleted
        {
            get { return m_isDeleted; }
            set
            {
                m_isDeleted = value;
                FlagDirty();
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
            get { return m_auditItemList; }
        }
        public override bool IsDirty
        {
            get
            {
                return base.IsDirty;
            }
        }
        protected override void DataFactory_Save()
        {
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            // ArchiveMessageID as InputOutput
            paramList.Add((DbParameter)da.CreateParameter("@ArchiveMessageID", DbType.Int64, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@ProcessName", DbType.String, m_processName));
            paramList.Add((DbParameter)da.CreateParameter("@Message", DbType.String, m_message));
            paramList.Add((DbParameter)da.CreateParameter("@IsProcessed", DbType.Boolean, m_isProcessed));
            paramList.Add((DbParameter)da.CreateParameter("@IsDeleted", DbType.Boolean, m_isDeleted));

            DbParameter[] @params = paramList.ToArray();

            var returnParams = da.ExecuteNonQuery("lis_ArchiveMessage_Save", @params);

            m_id = Conversions.ToLong(returnParams["@ArchiveMessageID"].Value);

            FlagClean();
        }
    }
}
