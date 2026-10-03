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
    public class RapidResultTemplateControl : DataClassBase
    {

        #region Private Members

        private RapidResultTemplate m_parent;
        private int m_id = 0;
        private string m_name = "";

        #endregion

        #region Constructor

        internal RapidResultTemplateControl(RapidResultTemplate parent)
        {
            m_parent = parent;
            FlagChild();
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

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_name ?? ""))
                {
                    m_name = value.Trim();
                    FlagDirty();
                }
            }
        }

        #endregion


        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["ControlId"]);
            m_name = Conversions.ToString(row["Name"]);
            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ControlId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@RapidResultTemplateId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@Name", DbType.String, m_name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_RapidResultTemplateControl_Save", @params)["@ControlId"].Value);

            FlagClean();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@ControlId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_RapidResultTemplateControl_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion


    }
}