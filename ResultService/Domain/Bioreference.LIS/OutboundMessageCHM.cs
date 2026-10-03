using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;

namespace Bioreference.LIS
{

    public class OutboundMessageCHM : DataClassBase
    {

        #region Private Members

        private string m_AccessionNumber = null;
        private string m_Message = null;
        private int m_MessageId = default;
        private bool m_CreateFile = false;
        private List<OutboundMessagesCHM> m_list = new List<OutboundMessagesCHM>();
        #endregion

        #region Public Properties
        public string AccessionNumber
        {
            get
            {
                return m_AccessionNumber;
            }
            set
            {
                m_AccessionNumber = value.Trim();
                FlagDirty();
            }
        }

        public string MessageId
        {
            get
            {
                return m_MessageId.ToString();
            }
        }

        public string Message
        {
            get
            {
                return m_Message;
            }
            set
            {
                m_Message = value.Trim().NormalizeToWindows();
                FlagDirty();
            }
        }

        public bool CreateFile
        {
            get
            {
                return m_CreateFile;
            }
            set
            {
                m_CreateFile = value;
                FlagDirty();
            }
        }

        public List<OutboundMessagesCHM> List
        {
            get
            {
                return m_list;
            }
        }
        #endregion

        #region Constructor
        public OutboundMessageCHM(string accessionNumber)
        {
            m_AccessionNumber = accessionNumber;
        }

        public OutboundMessageCHM()
        {
        }

        #endregion

        #region Public Methods
        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            try
            {
                paramList.Add((DbParameter)da.CreateParameter("@FetchCount", DbType.Int32, c.FetchCount));
                paramList.Add((DbParameter)da.CreateParameter("@IsBlis", DbType.Boolean, true));
                DbParameter[] @params = paramList.ToArray();

                DataTable[] dt = da.ExecuteProcedure("lis_OutboundMessageCHM_Fetch", @params);
                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0]);
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        private void Load(DataTable table)
        {

            OutboundMessagesCHM p;
            foreach (DataRow r in table.Rows)
            {
                p = new OutboundMessagesCHM();
                p.Load(r);
                m_list.Add(p);
            }

        }

        public static OutboundMessageCHM Fetch(int fetchcount)
        {

            return (OutboundMessageCHM)DataFactory.Fetch(new Criteria(fetchcount));

        }

        protected override void DataFactory_Save()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@AccessionNumber", DbType.String, AccessionNumber));
            paramList.Add((DbParameter)da.CreateParameter("@Message", DbType.String, Message));
            paramList.Add((DbParameter)da.CreateParameter("@CreateFile", DbType.String, CreateFile));

            DbParameter[] @params = paramList.ToArray();

            var returnParams = da.ExecuteNonQuery("lis_OutboundMessageCHM_Save", @params);

        }

        #endregion
        internal class Criteria
        {
            private int m_FetchCount = default;

            public Criteria(int fetchcount)
            {
                m_FetchCount = fetchcount;
            }

            public Criteria()
            {
            }

            public int FetchCount
            {
                get
                {
                    return m_FetchCount;
                }
            }

        }
    }
}