using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class Attachment : AuditDataClassBase
    {

        #region Private Members

        private long m_id;
        private int m_AttachmentTypeId;
        private string m_AttachmentTypeDescription = "";
        private string m_AttachmentDescription = "";
        private ReportAnalyte m_parent;
        private byte[] m_FileContents;

        private List<string> m_auditItemList;
        #endregion

        #region Constructor

        // Create a new ReportAlert based on Alert
        public Attachment(ReportAnalyte parent)
        {
            m_parent = parent;
            m_auditItemList = new List<string>();
            LoadAudit = LoadAudit;
            FlagDirty();
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

        public int AttachmentTypeId
        {
            get
            {
                return m_AttachmentTypeId;
            }
            set
            {
                m_AttachmentTypeId = value;
                FlagDirty();
            }
        }

        public string AttachmentTypeDescription
        {
            get
            {
                return m_AttachmentTypeDescription;
            }
        }

        [Audit("AttachmentDescription", true)]
        public string AttachmentDescription
        {
            get
            {
                return m_AttachmentDescription;
            }
            set
            {
                m_AttachmentDescription = value.NormalizeToWindows();
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

        public byte[] FileContents
        {
            get
            {
                return m_FileContents;
            }
            set
            {
                m_FileContents = value;
                FlagDirty();
            }
        }

        public string FileContentsBase64
        {
            get
            {
                if (m_FileContents is not null && m_FileContents.Length > 0)
                {
                    return Convert.ToBase64String(m_FileContents);
                }
                else
                {
                    return "";
                }
            }
        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteAttachmentId", DbType.Int64, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@AttachmentTypeId", DbType.Int32, m_AttachmentTypeId));
            paramList.Add((DbParameter)da.CreateParameter("@AttachmentDescription", DbType.String, m_AttachmentDescription));

            if (m_FileContents is not null && m_FileContents.Length > 0)
            {
                paramList.Add((DbParameter)da.CreateParameter("@FileContents", DbType.Binary, m_FileContents));
            }
            else
            {
                paramList.Add((DbParameter)da.CreateParameter("@FileContents", DbType.Binary, DBNull.Value));
            }

            if (!(CurrentUser == null))

                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_ReportAnalyteAttachment_Save", @params)["@ReportAnalyteAttachmentId"].Value);

            // NA: Get the list of AuditItem in this object after issuing the Clean so all properties are correctly populated.
            m_auditItemList.AddRange(GetAuditItemsAndClean(true));
            // Me.FlagClean()

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteAttachmentId", DbType.Int64, m_id));
            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            da.ExecuteNonQuery("lis_ReportAnalyteAttachment_Delete", paramList.ToArray());

            // NA: Get the list of AuditItem in this object after issuing the Clean so all properties are correctly populated.
            m_auditItemList.AddRange(GetAuditItemsAndClean());

            FlagDeleted();
            // Me.FlagClean()

        }


        internal void MarkParentCorrected()
        {
            var a = m_parent;
            // NA: need to also set the transmit status
            if (a.HasBeenReleased() && a.IsPreviousStatusNotReleased())
            {
                a.ResultStatus = resultStatusType.Corrected;
                a.SetTransmitStatus(transmitStatusType.PendingRelease);
            }
        }


        internal void Load(DataRow row)
        {

            m_id = Conversions.ToLong(row["ReportAnalyteAttachmentId"]);
            m_AttachmentTypeId = Conversions.ToInteger(row["AttachmentTypeId"]);
            m_AttachmentTypeDescription = Conversions.ToString(row["AttachmentType"]);
            m_AttachmentDescription = Conversions.ToString(row["AttachmentDescription"]).NormalizeToWindows();
            m_FileContents = (byte[])Interaction.IIf(row.IsNull("FileContents"), null, row["FileContents"]);

            FlagClean();


        }

        public List<string> GetFormattedAuditItems
        {
            // get the audit item of this object as well as any object in the Save stream.
            get
            {
                return m_auditItemList;
            }
        }


        public void ResetAuditItems()
        {
            m_auditItemList = new List<string>();
            ResetAudit();
        }




    }
}