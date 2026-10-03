using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderSpecimen : DataClassBase
    {

        #region Private Members

        private Order m_parent;

        private int m_id = 0;
        private string m_code = "";
        private string m_name = "";

        #endregion

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string SpecimenCode
        {
            get
            {
                return m_code;
            }
            set
            {
                if ((m_code ?? "") != (value.Trim() ?? ""))
                {
                    m_code = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string SpecimenName
        {
            get
            {
                return m_name;
            }
            set
            {
                if ((m_name ?? "") != (value.Trim() ?? ""))
                {
                    m_name = value;
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Constructor

        internal OrderSpecimen(Order parent, Specimen specimen)
        {

            m_parent = parent;
            m_code = specimen.SpecimenCode;
            m_name = specimen.SpecimenName;
            FlagChild();
            FlagDirty();

        }

        internal OrderSpecimen(Order parent)
        {

            m_parent = parent;
            FlagChild();

        }

        #endregion

        #region Data Functions

        internal void Delete()
        {

            throw new Exception("OrderSpecimen Delete has not been implemented.");

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@OrderSpecimenId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@OrderId", DbType.Int32, m_parent.ID));
            paramList.Add((DbParameter)da.CreateParameter("@SpecimenCode", DbType.String, m_code));
            paramList.Add((DbParameter)da.CreateParameter("@SpecimenName", DbType.String, m_name));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OrderSpecimen_Save", @params)["@OrderSpecimenId"].Value);

            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["OrderSpecimenId"]);
            m_code = Conversions.ToString(row["SpecimenCode"]);
            m_name = Conversions.ToString(row["SpecimenName"]);

            FlagClean();

        }



        #endregion


    }
}