using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Lab;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RefAlert : Alert
    {

        private ReportAlert m_parent;
        private List<string> m_auditItemList;

        #region Constructor

        internal RefAlert(ReportAlert parent) : base()
        {
            m_auditItemList = new List<string>();
            m_parent = parent;
            FlagChild();
        }

        #endregion

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

            paramList.Add((DbParameter)da.CreateParameter("@ReportAlertId", DbType.Int64, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@Code", DbType.String, m_code));
            paramList.Add((DbParameter)da.CreateParameter("@Descr", DbType.String, m_descr));
            paramList.Add((DbParameter)da.CreateParameter("@Instruction", DbType.String, m_instruction));
            paramList.Add((DbParameter)da.CreateParameter("@BlockAutoRelease", DbType.Boolean, m_blockAutoRelease));

            DbParameter[] @params = paramList.ToArray();

            da.ExecuteNonQuery("lis_RefAlert_Save", @params);

            m_auditItemList.AddRange(GetAuditItemsAndClean());
            // Me.FlagClean()


        }

        internal void Load(DataRow row)
        {

            m_code = Conversions.ToString(row["Code"]);
            m_descr = Conversions.ToString(row["Descr"]);
            m_instruction = Conversions.ToString(row["Instruction"]).NormalizeToWindows();
            m_blockAutoRelease = Conversions.ToBoolean(row["BlockAutoRelease"]);

            FlagClean();

        }

        internal void Load(Alert flag)
        {

            m_code = flag.Code;
            m_descr = flag.Description;
            m_instruction = flag.Instruction?.NormalizeToWindows() ?? string.Empty;
            m_blockAutoRelease = Conversions.ToBoolean(flag.BlockAutoRelease);

        }


        #endregion

    }
}