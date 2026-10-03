using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OutboundAllergenQueue : DataClassBase
    {

        private List<OutboundAllergenQueueItem> m_list = new List<OutboundAllergenQueueItem>();

        public OutboundAllergenQueueItem[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        internal OutboundAllergenQueue()
        {
        }

        public static OutboundAllergenQueueItem[] FetchOutbound(int returnCount, int allergenReportType, int totalChannelCount = 1, int currentChannelIndex = 0)
        {

            return ((OutboundAllergenQueue)DataFactory.Fetch(new Criteria(returnCount, allergenReportType, totalChannelCount, currentChannelIndex))).m_list.ToArray();

        }

        #region Criteria Class

        [Serializable()]
        public class Criteria
        {

            private int m_outboundReportType = 1;
            private int m_totalChannelCount = 1;
            private int m_currentChannelIndex = 0;
            private int m_returnCount = 0;

            public Criteria(int returnCount, int outboundReportType, int totalChannelCount, int currentChannelIndex)
            {
                m_returnCount = returnCount;
                m_outboundReportType = outboundReportType;
                m_totalChannelCount = totalChannelCount;
                m_currentChannelIndex = currentChannelIndex;
            }

            public int TotalChannelCount
            {
                get
                {
                    return m_totalChannelCount;
                }
            }

            public int CurrentChannelIndex
            {
                get
                {
                    return m_currentChannelIndex;
                }
            }

            public int OutboundReportType
            {
                get
                {
                    return m_outboundReportType;
                }
            }

            public int ReturnCount
            {
                get
                {
                    return m_returnCount;
                }
            }
        }

        #endregion

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DataTable[] dt;

            try
            {
                var @param = new DbParameter[5];

                @param[0] = (DbParameter)da.CreateParameter("@AllergenReportType", DbType.Int32, c.OutboundReportType);
                @param[1] = (DbParameter)da.CreateParameter("@ReturnCount", DbType.Int32, c.ReturnCount);
                @param[2] = (DbParameter)da.CreateParameter("@TotalChannelCount", DbType.Int32, c.TotalChannelCount);
                @param[3] = (DbParameter)da.CreateParameter("@CurrentChannelIndex", DbType.Int32, c.CurrentChannelIndex);
                @param[4] = ((DbParameter)da.CreateParameter("@IsBlis", DbType.Boolean, true));

                dt = da.ExecuteProcedure("lis_OutboundAllergenQueue_Fetch", @param);

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0]);
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());
                throw;

            }

        }
        private void Load(DataTable dt)
        {

            OutboundAllergenQueueItem o = null;
            foreach (DataRow row in dt.Rows)
            {
                o = new OutboundAllergenQueueItem();
                o.Load(row);
                m_list.Add(o);
            }

        }
    }

    [Serializable()]
    public class OutboundAllergenQueueItem : DataClassBase
    {

        #region Private Members

        private int m_allergenReportId;
        private string m_accessionNbr;
        private int m_allergenReportType;
        private int m_reportId;
        #endregion


        internal OutboundAllergenQueueItem()
        {
        }

        internal OutboundAllergenQueueItem(string sAccessionNbr, int reportId)
        {
            m_accessionNbr = sAccessionNbr;
            m_reportId = reportId;
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, m_accessionNbr));
            paramList.Add((DbParameter)da.CreateParameter("@ReportId", DbType.Int32, m_reportId));

            DbParameter[] @params = paramList.ToArray();

            da.ExecuteNonQuery("lis_OutboundAllergenQueue_Save", @params);

            FlagClean();

        }

        internal void Load(DataRow dr)
        {

            m_allergenReportId = Conversions.ToInteger(dr["AllergenReportId"]);
            m_accessionNbr = Conversions.ToString(dr["AccessionNbr"]);
            m_allergenReportType = Conversions.ToInteger(dr["AllergenReportType"]);
            m_reportId = Conversions.ToInteger(dr["ReportId"]);

            FlagClean();
        }
    }
}