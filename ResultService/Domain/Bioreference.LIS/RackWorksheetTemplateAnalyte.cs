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
    public class RackWorksheetTemplateAnalyte : DataClassBase
    {

        #region Private Members

        private int m_id = 0;
        private string m_analyteCode = "";
        private string m_panelCode = "";
        private RackWorksheetTemplate m_parent;

        #endregion

        #region Constructor

        internal RackWorksheetTemplateAnalyte(RackWorksheetTemplate parent)
        {
            m_parent = parent;
            FlagChild();
        }

        internal RackWorksheetTemplateAnalyte(RackWorksheetTemplate parent, string analyteCode)
        {
            m_parent = parent;
            m_analyteCode = analyteCode;
            FlagChild();
            FlagDirty();
        }

        internal RackWorksheetTemplateAnalyte(RackWorksheetTemplate parent, string analyteCode, string panelCode)
        {
            m_parent = parent;
            m_analyteCode = analyteCode;
            m_panelCode = panelCode;
            FlagChild();
            FlagDirty();
        }
        #endregion

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string AnalyteCode
        {
            get
            {
                return m_analyteCode;
            }
        }

        public string PanelCode
        {
            get
            {
                return m_panelCode;
            }
        }
        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["RackWorksheetTemplateAnalyteId"]);
            m_analyteCode = Conversions.ToString(row["AnalyteCode"]);
            m_panelCode = Conversions.ToString(row["PanelCode"]);

            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@AnalyteId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@AnalyteCode", DbType.String, m_analyteCode));
            paramList.Add((DbParameter)da.CreateParameter("@PanelCode", DbType.String, m_panelCode));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_RackWorksheetTemplateAnalyte_Save", @params)["@AnalyteId"].Value);

            FlagClean();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@AnalyteId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_RackWorksheetTemplateAnalyte_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

    }
}