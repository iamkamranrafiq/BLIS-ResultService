using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderComment : AuditDataClassBase
    {

        private Order m_parent = null;
        private long m_id = 0L;
        private string m_text = "";
        private externalApplicationType m_extAppType = externalApplicationType.B2;
        private bool m_isAutoAdded = false;
        private string m_externalCommentCode = "";
        private object m_dateCreated = DateTime.Parse("1900-01-01");
        private List<string> m_auditItemList;

        internal OrderComment(Order parent, bool loadAudit = false)
        {
            m_parent = parent;
            m_auditItemList = new List<string>();
            LoadAudit = loadAudit;
            FlagChild();
        }

        internal OrderComment(Order parent, externalApplicationType extAppType, bool isAutoAdded, string extCommentCode)
        {
            m_parent = parent;
            m_isAutoAdded = isAutoAdded;
            m_externalCommentCode = extCommentCode;
            m_extAppType = extAppType;
            m_auditItemList = new List<string>();
            FlagChild();
        }

        #region Public Properties

        /// <summary>
    /// Parent object can be Report,ReportAnalytePanel, or ReportAnalyte
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public Order Parent
        {
            get
            {
                return m_parent;
            }
        }

        public externalApplicationType ExtApplicationType
        {
            get
            {
                return m_extAppType;
            }
        }

        public bool IsAutoAdded
        {
            get
            {
                return m_isAutoAdded;
            }
        }

        [Audit("Text", true)]
        public string Text
        {
            get
            {
                return m_text;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_text ?? ""))
                {
                    m_text = value.Trim().NormalizeToWindows();

                    FlagDirty();
                }
            }
        }

        public DateTime DateCreated
        {
            get
            {
                return Conversions.ToDate(m_dateCreated);
            }
        }

        public long ID
        {
            get
            {
                return m_id;
            }
        }

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public string ExternalCommentCode
        {
            get
            {
                return m_externalCommentCode;
            }
        }

        protected override object ParentIdentifierId
        {
            get
            {
                return m_parent.AccessionIdentifier;
            }
        }

        protected override parentIdentifierType ParentIdentifierType
        {
            get
            {
                return m_parent.AccessionIdentifierType;
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
        #endregion

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@OrderCommentId", DbType.Int64, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int64, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@CommentText", DbType.String, m_text));
            paramList.Add((DbParameter)da.CreateParameter("@ExternalApplicationType", DbType.Int16, m_extAppType));
            paramList.Add((DbParameter)da.CreateParameter("@ExternalCommentCode", DbType.String, m_externalCommentCode));
            paramList.Add((DbParameter)da.CreateParameter("@IsAutoAdded", DbType.Boolean, m_isAutoAdded));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_OrderComment_Save", @params)["@OrderCommentId"].Value);

            m_auditItemList.AddRange(GetAuditItemsAndClean(true));
            // Me.FlagClean(True)

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@OrderCommentId", DbType.Int64, m_id);

            da.ExecuteNonQuery("lis_OrderComment_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToLong(row["OrderCommentId"]);
            m_text = Conversions.ToString(row["CommentText"]).NormalizeToWindows();
            m_dateCreated = row["DateCreated"];
            m_isAutoAdded = Conversions.ToBoolean(row["IsAutoAdded"]);
            m_externalCommentCode = Conversions.ToString(row["ExternalCommentCode"]);
            m_extAppType = (externalApplicationType)Conversions.ToInteger(row["ExternalApplicationType"]);

            FlagClean();

        }

        #endregion

        #region Compare

        public override string CompareObjectDiplayName
        {
            get
            {
                return Text;
            }
        }

        #endregion

    }
} // OrderComment