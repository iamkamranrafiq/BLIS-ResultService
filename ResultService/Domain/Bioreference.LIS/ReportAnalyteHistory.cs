using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportAnalyteHistory : AuditDataClassBase
    {

        #region Private members

        private long m_reportAnalyteId = 0L;
        private long m_id = 0L;
        private string m_resultValue = string.Empty;
        private DateTime m_enteredDate;
        private string m_enteredBy = string.Empty;
        private long m_reportId = 0L;

        #endregion

        #region Constructor

        internal ReportAnalyteHistory(ReportAnalyte analyte)
        {

            if (ReferenceEquals(analyte.Parent.GetType(), typeof(ReportAnalytePanel)))
            {
                m_reportId = ((ReportAnalytePanel)analyte.Parent).Parent.ID;
            }
            else if (ReferenceEquals(analyte.Parent.GetType(), typeof(Report)))
            {
                m_reportId = ((Report)analyte.Parent).ID;
            }

            m_reportAnalyteId = analyte.ID;
            m_resultValue = analyte.ResultValue;


        }

        internal ReportAnalyteHistory()
        {

        }

        internal ReportAnalyteHistory(int reportId, long reportAnalyteId, string resultValue)
        {
            m_reportId = reportId;
            m_reportAnalyteId = reportAnalyteId;
            m_resultValue = resultValue;
        }

        #endregion

        #region Public Properties

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public long ReportAnalyteId
        {
            get
            {
                return m_reportAnalyteId;
            }
        }

        public string ResultValue
        {
            get
            {
                return m_resultValue;
            }
        }

        public DateTime EnteredDate
        {
            get
            {
                return m_enteredDate;
            }
        }

        public string EnteredBy
        {
            get
            {
                return m_enteredBy;
            }
        }


        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToLong(row["ReportAnalyteHistoryId"]);
            m_reportAnalyteId = Conversions.ToLong(row["ReportAnalyteId"]);
            m_reportId = Conversions.ToLong(row["ReportId"]);
            m_resultValue = Conversions.ToString(row["ResultValue"]);
            m_enteredDate = Conversions.ToDate(row["EnteredDate"]);
            m_enteredBy = Conversions.ToString(row["EnteredBy"]);

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteHistoryId", DbType.Int64, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, m_reportAnalyteId));
            paramList.Add((DbParameter)da.CreateParameter("@ReportId", DbType.Int64, m_reportId));
            paramList.Add((DbParameter)da.CreateParameter("@ResultValue", DbType.String, m_resultValue));
            
            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@EnteredBy", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_ReportAnalyteHistory_Save", @params)["@ReportAnalyteHistoryId"].Value);

            FlagClean();

        }

        protected override void DataFactory_Delete(object criteria)
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];
            Criteria c = (Criteria)criteria;

            @param[0] = (DbParameter)da.CreateParameter("@ReportAnalyteHistoryId", DbType.Int64, c.ReportAnalyteHistoryId);

            da.ExecuteNonQuery("lis_ReportAnalyteHistory_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

        public void Delete()
        {

            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectGreater(IdentifierId, 0, false)))
            {
                DataFactory.Delete(new Criteria(Conversions.ToLong(IdentifierId)));
            }

        }

        [Serializable()]
        internal class Criteria
        {
            private long m_id = 0L;
            internal Criteria(long id)
            {
                m_id = id;
            }
            public long ReportAnalyteHistoryId
            {
                get
                {
                    return m_id;
                }
            }
        }

    }
}