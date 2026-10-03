using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportCommentGroup : DataClassBase
    {

        private string m_externalId;
        private int m_position = 0;
        private Report m_parent;

        internal ReportCommentGroup(Report parent)
        {
            m_parent = parent;
        }

        internal ReportCommentGroup(Report parent, string externalId, int position)
        {
            FlagChild();
            m_parent = parent;
            m_externalId = externalId;
            m_position = position;
        }

        public string ExternalId
        {
            get
            {
                return m_externalId;
            }
        }

        public int Position
        {
            get
            {
                return m_position;
            }
        }

        internal void Load(DataRow row)
        {

            m_externalId = Conversions.ToString(row["ExternalId"]);
            m_position = Conversions.ToInteger(row["Position"]);

            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ReportId", DbType.Int64, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@ExternalId", DbType.String, m_externalId));
            paramList.Add((DbParameter)da.CreateParameter("@Position", DbType.Int32, m_position));

            DbParameter[] @params = paramList.ToArray();
            da.ExecuteNonQuery("lis_ReportCommentGroup_Save", @params);

            FlagClean();

        }

    }
}