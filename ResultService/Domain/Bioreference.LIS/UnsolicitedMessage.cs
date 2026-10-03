using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;

[Serializable]
public class UnsolicitedMessage : DataClassBase
{
    #region Constructors

    public UnsolicitedMessage(string accessionNbr, string hl7Message, DateTime expirationDate)
    {
        m_accessionNbr = accessionNbr;
        m_hl7Message = hl7Message.NormalizeToWindows();
        m_expirationDate = expirationDate;
        FlagDirty();
    }

    internal UnsolicitedMessage() { }

    internal UnsolicitedMessage(string accessionNbr)
    {
        m_accessionNbr = accessionNbr;
        FlagDirty();
    }

    #endregion

    #region Private Members

    private static List<UnsolicitedMessage> m_list;

    private long m_unsolicitedMessageId = 0;
    private string m_accessionNbr = "";
    private string m_hl7Message = "";
    private DateTime m_expirationDate = DateTime.MinValue;
    private DateTime m_sentToETS = DateTime.MinValue;
    private DateTime m_processedDate = DateTime.MinValue;

    #endregion

    #region Public Properties

    public static List<UnsolicitedMessage> List => m_list;

    public long Id => m_unsolicitedMessageId;
    public long UnsolicitedMessageId => m_unsolicitedMessageId;
    public string AccessionNbr => m_accessionNbr;
    public string HL7Message => m_hl7Message;
    public DateTime ExpirationDate => m_expirationDate;
    public DateTime SentToETS => m_sentToETS;
    public DateTime ProcessedDate => m_processedDate;

    #endregion

    #region Public Methods
    public enum UnsolicitedFetchType
    {
        All,
        NotSentToETS,
        NotProcessed
    }
    public static void SaveAll()
    {
        foreach (var um in m_list)
        {
            um.Save();
        }
    }

    public static void FetchUnSent(string accessionNbr = "")
    {
        DataFactory.Fetch(new UnsolicitedMessage.Criteria(accessionNbr, UnsolicitedFetchType.NotSentToETS));
    }

    public static void FetchUnProcessed(string accessionNbr = "")
    {
        DataFactory.Fetch(new UnsolicitedMessage.Criteria(accessionNbr, UnsolicitedFetchType.NotProcessed));
    }

    public void MarkSentToETS()
    {
        m_sentToETS = DateTime.Now;
        FlagDirty();
    }

    public void MarkProcessed()
    {
        m_processedDate = DateTime.Now;
        FlagDirty();
    }

    #endregion

    #region Data Functions

    protected override void DataFactory_Fetch(object criteria)
    {
        UnsolicitedMessage.Criteria c = (UnsolicitedMessage.Criteria)criteria;
        DataWrapper da = new DataWrapper(Bioreference.LIS.Configuration.ConnectionString);
        DataTable[] dt;
        try
        {
            List<DbParameter> paramList = new List<DbParameter>();
            if (!string.IsNullOrEmpty(c.AccessionNbr))
                paramList.Add(da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr));

            switch (c.FetchType)
            {
                case UnsolicitedFetchType.NotProcessed:
                    paramList.Add(da.CreateParameter("@IsProcessed", DbType.Boolean, false));
                    break;
                case UnsolicitedFetchType.NotSentToETS:
                    paramList.Add(da.CreateParameter("@IsSentToETS", DbType.Boolean, false));
                    break;
            }

            paramList.Add((DbParameter)da.CreateParameter("@IsBlis", DbType.Boolean, true));
            dt = da.ExecuteProcedure("lis_UnsolicitedMessage_Fetch", paramList.ToArray());
            m_list = new List<UnsolicitedMessage>();
            if (dt[0].Rows.Count > 0)
            {
                this.Load(dt[0]);
            }
        }
        catch (Exception ex)
        {
            //Log.Error(ex.Message, ex);
            throw;
        }
    }

    protected override void DataFactory_Save()
    {
        DataWrapper da = new DataWrapper(Bioreference.LIS.Configuration.ConnectionString);
        List<DbParameter> paramList = new List<DbParameter>();
        paramList.Add(da.CreateParameter("@UnsolicitedMessageId", DbType.Int64, m_unsolicitedMessageId, ParameterDirection.InputOutput));
        paramList.Add(da.CreateParameter("@AccessionNbr", DbType.String, m_accessionNbr));
        paramList.Add(da.CreateParameter("@HL7Message", DbType.String, m_hl7Message));
        paramList.Add(da.CreateParameter("@ExpirationDate", DbType.DateTime, m_expirationDate));

        if (m_sentToETS != DateTime.MinValue)
            paramList.Add(da.CreateParameter("@SentToETS", DbType.DateTime, m_sentToETS));

        if (m_processedDate != DateTime.MinValue)
            paramList.Add(da.CreateParameter("@ProcessedDate", DbType.DateTime, m_processedDate));

        DbParameter[] paramsArray = paramList.ToArray();
        m_unsolicitedMessageId = Convert.ToInt64(da.ExecuteNonQuery("lis_UnsolicitedMessage_Save", paramsArray)["@UnsolicitedMessageId"].Value);

        this.FlagClean();
    }

    internal void Load(DataTable table)
    {
        foreach (DataRow r in table.Rows)
        {
            UnsolicitedMessage um = new UnsolicitedMessage()
            {
                m_unsolicitedMessageId = (long)r["UnsolicitedMessageId"],
                m_accessionNbr = r["AccessionNbr"] as string,
                m_hl7Message = (r["HL7Message"] as string).NormalizeToWindows(),
                m_expirationDate = (DateTime)r["ExpirationDate"],
                m_sentToETS = !r.IsNull("SentToETS") ? (DateTime)r["SentToETS"] : DateTime.MinValue,
                m_processedDate = !r.IsNull("ProcessedDate") ? (DateTime)r["ProcessedDate"] : DateTime.MinValue
            };
            m_list.Add(um);
        }
        this.FlagClean();
    }

    #endregion

    #region "Criteria Class"

    [Serializable]
    internal class Criteria
    {
        private string m_accessionNbr = "";
        private UnsolicitedFetchType m_fetchType = UnsolicitedFetchType.All;

        public Criteria(string accessionNbr, UnsolicitedFetchType fetchType)
        {
            m_accessionNbr = accessionNbr;
            m_fetchType = fetchType;
        }

        public string AccessionNbr
        {
            get { return m_accessionNbr; }
        }

        public UnsolicitedFetchType FetchType
        {
            get { return m_fetchType; }
        }
    }

    #endregion
}
