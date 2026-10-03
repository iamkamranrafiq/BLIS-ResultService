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
    public class ResultsReviewInstrument : DataClassBase
    {

        #region Private Members

        private int m_id = 0;
        private string m_instrumentId = "";
        private ResultsReviewFilter m_parent = null;

        #endregion

        #region Constructor

        internal ResultsReviewInstrument(ResultsReviewFilter parent)
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

        public string InstrumentId
        {
            get
            {
                return m_instrumentId;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_instrumentId ?? ""))
                {
                    m_instrumentId = value.Trim();
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["ResultsReviewInstrumentId"]);
            m_instrumentId = Conversions.ToString(row["InstrumentId"]);

            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@InstrumentId", DbType.String, m_instrumentId));
            paramList.Add((DbParameter)da.CreateParameter("@ResultReviewFilterId", DbType.Int32, m_parent.Id));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_ResultsReviewInstrument_Save", @params)["@Id"].Value);

            FlagClean();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@ResultReviewInstrumentId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_ResultsReviewInstrument_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

    }
}