using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderICD9 : DataClassBase
    {

        private Order m_parent;

        #region Constructors

        internal OrderICD9(Order parent)
        {
            m_parent = parent;
            FlagChild();
        }

        internal OrderICD9(Order parent, string code, string description)
        {
            m_parent = parent;
            m_code = code;
            m_description = description?.NormalizeToWindows() ?? string.Empty;
            FlagChild();
            FlagDirty();
        }

        #endregion

        #region Private Members

        private int m_id = 0;
        private string m_code = "";
        private string m_description = "";

        #endregion

        #region Public Properties

        public string Code
        {
            get
            {
                return m_code;
            }
        }

        public string Description
        {
            get
            {
                return m_description;
            }
        }

        #endregion

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@OrderICD9Id", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@Code", DbType.String, m_code));
            paramList.Add((DbParameter)da.CreateParameter("@Description", DbType.String, m_description));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OrderICD9_Save", @params)["@OrderICD9Id"].Value);

            FlagClean();

        }

        internal void Delete()
        {

            throw new Exception("OrderICD9 Delete has not been implemented");

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["OrderICD9Id"]);
            m_code = Conversions.ToString(row["Code"]);

            m_description = Conversions.ToString(row["Description"]).NormalizeToWindows();

            FlagClean();

        }

        #endregion



    }
}