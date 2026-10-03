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
    public class OutboundMessages : DataClassReadOnlyBase
    {

        private List<OutboundMessage> m_list = new List<OutboundMessage>();

        public static OutboundMessages Fetch(outBoundMessageSendTo sendTo, bool isProcessed)
        {

            return (OutboundMessages)DataFactory.Fetch(new Criteria(sendTo, isProcessed));

        }

        public OutboundMessage[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[4];

            @param[0] = (DbParameter)da.CreateParameter("@SendTo", DbType.Int32, c.SendTo);
            @param[1] = (DbParameter)da.CreateParameter("@IsProcessed", DbType.String, c.IsProcessed);
            @param[2] = ((DbParameter)da.CreateParameter("@IsBlis", DbType.Boolean, true));


            DataTable[] dt = da.ExecuteProcedure("lis_OutboundMessages_Fetch", @param);

            Load(dt[0]);

        }

        private void Load(DataTable table)
        {

            OutboundMessage o = null;
            foreach (DataRow row in table.Rows)
            {
                o = new OutboundMessage();
                o.Load(row);
                m_list.Add(o);
            }

        }


        [Serializable()]
        internal class Criteria
        {

            private outBoundMessageSendTo m_sendTo = outBoundMessageSendTo.Vertex;
            private bool m_isProcessed = false;

            public Criteria(outBoundMessageSendTo sendTo, bool isProcessed)
            {
                m_sendTo = sendTo;
                m_isProcessed = isProcessed;
            }

            public outBoundMessageSendTo SendTo
            {
                get
                {
                    return m_sendTo;
                }
            }
            public bool IsProcessed
            {
                get
                {
                    return m_isProcessed;
                }
            }

        }

    }

    [Serializable()]
    public class OutboundMessage : DataClassBase
    {

        private int m_id = 0;
        private string m_accessionNbr = string.Empty;
        private string m_message = string.Empty;
        private DateTime m_dateCreated = DateTime.Parse("1900-01-01");
        private DateTime m_dateProcessed = DateTime.Parse("1900-01-01");
        private bool m_isProcessed = false;
        private outBoundMessageSendTo m_sendTo = outBoundMessageSendTo.None;

        public string ReflexCode { get; set; }
        public string OrderedCode { get; set; }
        public string DateOrdered { get; set; }
        public string ActionType { get; set; }

        public OutboundMessage(string accessionNbr, string message, outBoundMessageSendTo sendTo)
        {
            m_accessionNbr = accessionNbr;
            m_message = message.NormalizeToWindows();
            m_sendTo = sendTo;
            FlagDirty();
        }
        internal OutboundMessage()
        {
        }

        public outBoundMessageSendTo SendTo
        {
            get
            {
                return m_sendTo;
            }
        }

        public int Id
        {
            get
            {
                return m_id;
            }
        }
        public string AccessionNumber
        {
            get
            {
                return m_accessionNbr;
            }
        }
        public string Message
        {
            get
            {
                return m_message;
            }
        }
        public DateTime DateCreated
        {
            get
            {
                return m_dateCreated;
            }
        }
        public DateTime DateProcessed
        {
            get
            {
                return m_dateProcessed;
            }
        }
        public bool IsProcessed
        {
            get
            {
                return m_isProcessed;
            }
        }

        public void MarkAsProcessed()
        {

            m_isProcessed = true;
            FlagDirty();

        }


        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["OutboundMessageId"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            m_message = Conversions.ToString(row["MessageText"]).NormalizeToWindows();
            m_dateCreated = Conversions.ToDate(row["DateCreated"]);
            m_dateProcessed = Conversions.ToDate(row["DateProcessed"]);
            m_isProcessed = Conversions.ToBoolean(row["IsProcessed"]);
            m_sendTo = (outBoundMessageSendTo)Conversions.ToInteger(row["SendTo"]);

            FlagClean();

        }

        protected override void DataFactory_Save()
        {

            Update();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            if (m_id == 0)
                m_dateCreated = DateTime.Now;
            if (m_isProcessed == true && m_dateProcessed == DateTime.Parse("1900-01-01"))
                m_dateProcessed = DateTime.Now;


            paramList.Add((DbParameter)da.CreateParameter("@OutboundMessageId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, m_accessionNbr));
            paramList.Add((DbParameter)da.CreateParameter("@MessageText", DbType.String, m_message));
            paramList.Add((DbParameter)da.CreateParameter("@IsProcessed", DbType.Boolean, m_isProcessed));
            paramList.Add((DbParameter)da.CreateParameter("@SendTo", DbType.Int32, m_sendTo));

            // 'Create dates with in the stored proc - this way dates are from sql server machine,
            // 'not from machine object was saved on.
            if (m_dateCreated > DateTime.Parse("1900-01-01"))
            {
                paramList.Add((DbParameter)da.CreateParameter("@DateCreated", DbType.DateTime, m_dateCreated));
            }
            if (m_dateProcessed > DateTime.Parse("1900-01-01"))
            {
                paramList.Add((DbParameter)da.CreateParameter("@DateProcessed", DbType.DateTime, m_dateProcessed));
            }

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_OutboundMessage_Save", @params)["@OutboundMessageId"].Value);

            FlagClean();

        }


        #endregion

    }
}