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
    public class RapidResults : DataClassReadOnlyBase
    {

        private List<RapidResult> m_list;

        private RapidResults()
        {
            m_list = new List<RapidResult>();
        }

        public RapidResult[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        public static RapidResults Fetch(bool isCompleted = false, int templateID = 0, int pageNo = 0, int pageSize = 0)
        {

            return (RapidResults)DataFactory.Fetch(new Criteria(isCompleted, templateID, pageNo, pageSize));

        }

        public static object FetchOutstanding(int templateID)
        {
            return DataFactory.Fetch(new Criteria(templateID, true));
        }

        public static object FetchOutstandingDownload(int templateID, int pastDays, bool IsDownload, int pageNo = 0, int pageSize = 0)
        {
            return DataFactory.Fetch(new Criteria(templateID, pastDays, IsDownload, pageNo, pageSize));
        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>(); ;
            DataTable[] dt;

            if (c.ReturnOutstanding == false)
            {
                @params.Add((DbParameter)da.CreateParameter("@IsCompleted", DbType.Boolean, c.IsCompleted));
                @params.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId));
                @params.Add((DbParameter)da.CreateParameter("@PageNo", DbType.Int32, c.PageNo));
                @params.Add((DbParameter)da.CreateParameter("@PageSize", DbType.Int32, c.PageSize));

                dt = da.ExecuteProcedure("lis_RapidResults_Fetch", @params.ToArray());
            }
            else if(c.IsDownload == true)
            {
                @params.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId));
                @params.Add((DbParameter)da.CreateParameter("@PastDays", DbType.Int32, c.PastDays));
                @params.Add((DbParameter)da.CreateParameter("@PageNo", DbType.Int32, c.PageNo));
                @params.Add((DbParameter)da.CreateParameter("@PageSize", DbType.Int32, c.PageSize));
                dt = da.ExecuteProcedure("lis_RapidResultsOutstandingDownload_Fetch", @params.ToArray());
            }
            else
            {
                @params.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId));
                dt = da.ExecuteProcedure("lis_RapidResultsOutstanding_Fetch", @params.ToArray());
            }

            Load(dt[0]);

        }

        protected void Load(DataTable dt)
        {

            RapidResult ws = null;
            int rapidResultId = 0;
            DataTable t = null;
            foreach (DataRow r in dt.Rows)
            {

                if (!r["RapidResultId"].Equals(rapidResultId))
                {

                    if (!(ws == null))
                    {
                        ws.Load(t);
                        m_list.Add(ws);
                    }

                    ws = new RapidResult(Conversions.ToInteger(r["RapidResultTemplateId"]));
                    t = dt.Clone();
                    rapidResultId = Conversions.ToInteger(r["RapidResultId"]);
                }

                t.ImportRow(r);
            }

            if (!(ws == null))
            {
                ws.Load(t);
                m_list.Add(ws);
            }

        }

        [Serializable()]
        internal class Criteria
        {
            private bool m_iscompleted = false;
            private int m_templateId = 0;
            private bool m_returnOutstanding = false;
            private bool m_isdownload = false;
            private int m_pastDays = 0; 
            private int m_pageNo = 0; 
            private int m_pageSize = 0; 

            public Criteria(bool isCompleted, int templateId, int pageNo = 0, int pageSize = 0)
            {
                m_iscompleted = isCompleted;
                m_templateId = templateId;
                m_pageNo = pageNo;
                m_pageSize = pageSize;  
            }
            public Criteria(int templateId, bool returnOutstanding)
            {
                m_templateId = templateId;
                m_returnOutstanding = returnOutstanding;
            }
            public Criteria(int templateId, int pastDays, bool IsDownload, int pageNo = 0, int pageSize = 0)
            {
                m_templateId = templateId;
                m_pastDays = pastDays;  
                m_isdownload = IsDownload; 
                m_pageNo = pageNo;
                m_pageSize = pageSize;
            }
            public int TemplateId
            {
                get
                {
                    return m_templateId;
                }
            }
            public bool IsCompleted
            {
                get
                {
                    return m_iscompleted;
                }
            }
            public bool ReturnOutstanding
            {
                get
                {
                    return m_returnOutstanding;
                }
            }
            public bool IsDownload
            {
                get
                {
                    return m_isdownload;
                }
            }
            public int PastDays
            {
                get
                {
                    return m_pastDays;
                }
            }
            public int PageNo
            {
                get
                {
                    return m_pageNo;
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