using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using log4net;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RackWorksheets : DataClassReadOnlyBase
    {

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private List<RackWorksheetReport> m_list;
        private string m_assignedUser;

        private RackWorksheets()
        {
            m_list = new List<RackWorksheetReport>();
        }

        public RackWorksheetReport[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        public string AssignedUser
        {
            get
            {
                return m_assignedUser;
            }
            set
            {
                m_assignedUser = value;
            }
        }

        public static RackWorksheets Fetch(transmitStatusType transmitStatus, int templateId)
        {

            return (RackWorksheets)DataFactory.Fetch(new Criteria(transmitStatus, "", templateId, 0));

        }




        public static RackWorksheets Fetch(int templateId, DateTime dateFrom, DateTime dateTo, int pageNumber, int pageSize)
        {

            return (RackWorksheets)DataFactory.Fetch(new Criteria(templateId, dateFrom, dateTo, pageNumber, pageSize));

        }


        public static RackWorksheets Fetch(string accessionNbr)
        {

            return (RackWorksheets)DataFactory.Fetch(new Criteria(transmitStatusType.NotSet, accessionNbr, 0, 0));

        }

        /// <summary>
        /// Returns one single WorksheetReport
        /// </summary>
        /// <param name="worksheetID"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static RackWorksheetReport Fetch(int worksheetID, int pageNumber, int pageSize)
        {

            RackWorksheets ws = (RackWorksheets)DataFactory.Fetch(new Criteria(0, worksheetID, pageNumber, pageSize));

            if (ws.List.Length > 0)
            {
                return ws.List[0];
            }
            else
            {
                return null;
            }

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[4];
            var paramList = new List<DbParameter>();
            string proc = "lis_RackWorksheets_ResultService_Fetch";

            paramList.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId));
            paramList.Add((DbParameter)da.CreateParameter("@WorksheetId", DbType.Int32, c.WorksheetId));
            paramList.Add((DbParameter)da.CreateParameter("@DateFrom", DbType.DateTime, c.DateFrom));
            paramList.Add((DbParameter)da.CreateParameter("@DateTo", DbType.DateTime, c.DateTo));
          //  paramList.Add((DbParameter)da.CreateParameter("@TransmitStatus", DbType.String, c.Status));
            paramList.Add((DbParameter)da.CreateParameter("@PageNo", DbType.Int32, c.PageNumber));
            paramList.Add((DbParameter)da.CreateParameter("@PageSize", DbType.Int32, c.PageSize));

            Log.Debug(SharedFunctions.BuildDBLoggingMessage(proc, paramList));
            var start = DateTime.Now;

            DataTable[] dt = da.ExecuteProcedure(proc, paramList.ToArray());

            var elapsed = DateTime.Now.Subtract(start);

            Log.DebugFormat("exec {0} Elapsed Time: {1:0.000} sec.", proc, elapsed.TotalSeconds);

            Load(dt[0]);

        }

        protected void Load(DataTable dt)
        {

            RackWorksheetReport ws = null;
            RackWorksheetReportItem wsi = null;
            int wid = 0;
            foreach (DataRow r in dt.Rows)
            {

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(wid, r["RackWorksheetId"], false)))
                {
                    ws = new RackWorksheetReport(Conversions.ToInteger(r["RackWorksheetId"]), Conversions.ToDate(r["DateCreated"]), Conversions.ToString(r["CreatedBy"]), Conversions.ToInteger(r["RackWorksheetTemplateId"]), Conversions.ToBoolean(r["IsClosed"]), Conversions.ToString(r["AssignedUser"]));
                    m_list.Add(ws);
                    wid = Conversions.ToInteger(r["RackWorksheetId"]);
                }

                wsi = new RackWorksheetReportItem();
                wsi.Load(r);
                ws.List.Add(wsi);

            }

        }

        [Serializable()]
        internal class Criteria
        {
            private transmitStatusType m_transmitstatus = transmitStatusType.PendingRelease;
            private string m_accessionNbr = "";
            private int m_templateId = 0;
            private int m_worksheetId = 0;
            private int m_pageNumber = 1;
            private int m_pageSize = 10;
            private DateTime? m_dateFrom = null;
            private DateTime? m_dateTo = null;

            private bool m_checkExisting = false; // 'Used to check if an accession is already existing on a 

            public Criteria(transmitStatusType transmitStatus, string accessionNbr, int templateId, int worksheetId)
            {
                m_transmitstatus = transmitStatus;
                m_accessionNbr = accessionNbr;
                m_templateId = templateId;
                m_worksheetId = worksheetId;
            }

            public Criteria(int templateId, DateTime dateFrom, DateTime dateTo, int pageNumber, int pageSize)
            {
                m_templateId = templateId;
                m_dateFrom = dateFrom;
                m_dateTo = dateTo;
                m_pageNumber = pageNumber;
                m_pageSize = pageSize;
            }

            public Criteria(int templateId, int worksheetId, int pageNumber, int pageSize)
            {
                m_templateId = templateId;
                m_worksheetId = worksheetId;
                m_pageNumber = pageNumber;
                m_pageSize = pageSize;
            }


            public transmitStatusType TransmitStatus
            {
                get
                {
                    return m_transmitstatus;
                }
            }
            public string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }

            public int TemplateId
            {
                get
                {
                    return m_templateId;
                }
            }

            public int WorksheetId
            {
                get
                {
                    return m_worksheetId;
                }
            }

            public DateTime? DateFrom
            {
                get
                {
                    return m_dateFrom;
                }
            }
            public DateTime? DateTo
            {
                get
                {
                    return m_dateTo;
                }
            }

            public int PageNumber
            {
                get
                {
                    return m_pageNumber;
                }
            }

            public int PageSize
            {
                get
                {
                    return m_pageSize;
                }
            }

        }

    }
}