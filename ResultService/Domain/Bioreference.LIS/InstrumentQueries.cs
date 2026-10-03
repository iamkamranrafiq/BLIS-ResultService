using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Transactions;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class InstrumentQueries : DataClassReadOnlyBase
    {

        private List<InstrumentQuery> _list = new List<InstrumentQuery>();
        private static readonly ILog Log = LogManager.GetLogger<InstrumentQueries>();


        public static InstrumentQueries Fetch(long id)
        {
            return (InstrumentQueries)DataFactory.Fetch(new Criteria(id, "", false));
        }

        public static InstrumentQueries Fetch(string accessionNbr, bool sentToETS)
        {
            return (InstrumentQueries)DataFactory.Fetch(new Criteria(0L, accessionNbr, sentToETS));
        }

        public static InstrumentQueries Fetch()
        {
            InstrumentQueries iqs = (InstrumentQueries)DataFactory.Fetch(new Criteria(0L, "", false));
            return iqs;
        }

        public List<InstrumentQuery> List
        {
            get
            {
                return _list;
            }
        }

        public static void Insert(string accession, List<string> analyteList, string instrumentName, string queryTime, DateTime expirationDate)
        {
            try
            {
                var acc = new AccessionNbr(accession);
                if (acc.IsValid)
                {
                    var iq = new InstrumentQuery(acc, DateTime.Now, default, analyteList, instrumentName, queryTime, expirationDate);
                    iq.Update();
                }
                else
                {
                    Log.Debug($"Unable to insert Instrument Query Item, Invalid Accession '{accession}'");
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Unable to insert Instrument Query Item, Accession '{accession}': {ex.Message}");
            }
        }

        public static void ClearExpired(int minutes)
        {
            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();
            @params.Add((DbParameter)da.CreateParameter("@Minutes", DbType.Int32, minutes, ParameterDirection.Input));
            var options = new TransactionOptions();
            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
            options.Timeout = new TimeSpan(0, 2, 0);
            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {
                da.ExecuteNonQuery("lis_InstrumentQuery_Clear", @params.ToArray());
                scope.Complete();
            }
        }

        protected override void DataFactory_Fetch(object criteria)
        {
            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();
            @params.Add((DbParameter)da.CreateParameter("@Id", DbType.Int64, c.Id, ParameterDirection.Input));
            @params.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr, ParameterDirection.Input));
            @params.Add((DbParameter)da.CreateParameter("@SentToETS", DbType.Boolean, c.SentToETS, ParameterDirection.Input));
            @params.Add((DbParameter)da.CreateParameter("@IsBlis", DbType.Boolean, true, ParameterDirection.Input));
            DataTable[] dt = da.ExecuteProcedure("lis_InstrumentQuery_Fetch", @params.ToArray());
            Load(dt[0]);
        }

        private void Load(DataTable table)
        {
            InstrumentQuery o = null;
            foreach (DataRow row in table.Rows)
            {
                o = new InstrumentQuery();
                o.Load(row);
                _list.Add(o);
            }
        }

        [Serializable()]
        internal class Criteria
        {

            private long _id = 0L;
            private string _accessionNbr = "";
            private bool _sentToETS = false;

            public Criteria(long id, string accessionNbr, bool sentToETS)
            {
                _id = id;
                _accessionNbr = accessionNbr;
                _sentToETS = sentToETS;
            }

            public long Id
            {
                get
                {
                    return _id;
                }
            }

            public string AccessionNbr
            {
                get
                {
                    return _accessionNbr;
                }
            }

            public bool SentToETS
            {
                get
                {
                    return _sentToETS;
                }
            }

        }

    }

    [Serializable()]
    public class InstrumentQuery : DataClassBase
    {

        private long _id = 0L;
        private string _accessionNbr = "";
        private DateTime _created = DateTime.Parse("1900-01-01");
        private DateTime _sentToETS = DateTime.Parse("1900-01-01");
        private List<string> _analyteList = new List<string>();
        private string _instrumentName = "";
        private string _queryTime = "";
        private DateTime _expirationDate;
        private DateTime _processedDate = DateTime.MinValue;

        public InstrumentQuery(AccessionNbr acc, DateTime created, DateTime sentToETS, List<string> analyteList, string instrumentName, string queryTime, DateTime expirationDate)
        {
            _id = 0L;
            _accessionNbr = acc.ToString();
            _created = created;
            _sentToETS = sentToETS;
            _analyteList = analyteList;
            _instrumentName = instrumentName;
            _queryTime = queryTime;
            _expirationDate = expirationDate;
            FlagDirty();
        }
        internal InstrumentQuery()
        {
        }

        public long Id
        {
            get
            {
                return _id;
            }
        }
        public string AccessionNbr
        {
            get
            {
                return _accessionNbr;
            }
        }

        public DateTime Created
        {
            get
            {
                return _created;
            }
        }

        public DateTime SentToETS
        {
            get
            {
                return _sentToETS;
            }
        }

        public List<string> AnalyteList
        {
            get
            {
                return _analyteList;
            }
        }

        public string InstrumentName
        {
            get
            {
                return _instrumentName;
            }
        }

        public string QueryTime
        {
            get
            {
                return _queryTime;
            }
        }
        public DateTime ExpirationDate
        {
            get
            {
                return _expirationDate;
            }
        }

        public void MarkAsSentToETS()
        {

            _sentToETS = DateTime.Now;
            FlagDirty();

        }

        public void MarkProcessed()
        {

            _processedDate = DateTime.Now;
            FlagDirty();

        }

        #region Data Functions

        internal void Load(DataRow row)
        {
            _id = Conversions.ToLong(row["Id"]);
            _accessionNbr = row["AccessionNbr"].ToString();
            _created = Conversions.ToDate(row["Created"]);
            _sentToETS = Conversions.ToDate(row["SentToETS"]);
            _analyteList.AddRange(row["AnalyteList"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            _instrumentName = row["InstrumentName"].ToString();
            _queryTime = row["QueryTime"].ToString();
            _expirationDate = Convert.ToDateTime(row["ExpirationDate"]);
            _processedDate = row.IsNull("ProcessedDate") ? DateTime.MinValue : Convert.ToDateTime(row["ProcessedDate"]);

            FlagClean();
        }

        protected override void DataFactory_Save()
        {
            Update();
        }

        internal void Update()
        {
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramsList = new List<DbParameter>();
            var analyteList = string.Join(",", _analyteList);

            paramsList.Add(da.CreateParameter("@Id", DbType.Int64, _id, ParameterDirection.InputOutput));
            paramsList.Add(da.CreateParameter("@AccessionNbr", DbType.String, _accessionNbr, ParameterDirection.Input));
            paramsList.Add(da.CreateParameter("@Created", DbType.DateTime, _created, ParameterDirection.Input));

            if (_sentToETS > new DateTime(1900, 1, 1))
            {
                paramsList.Add(da.CreateParameter("@SentToETS", DbType.DateTime, _sentToETS, ParameterDirection.Input));
            }

            paramsList.Add(da.CreateParameter("@AnalyteList", DbType.String, analyteList, ParameterDirection.Input));
            paramsList.Add(da.CreateParameter("@InstrumentName", DbType.String, _instrumentName, ParameterDirection.Input));
            paramsList.Add(da.CreateParameter("@QueryTime", DbType.String, _queryTime, ParameterDirection.Input));
            paramsList.Add(da.CreateParameter("@ExpirationDate", DbType.DateTime, _expirationDate, ParameterDirection.Input));

            if (_processedDate > new DateTime(1900, 1, 1))
            {
                paramsList.Add(da.CreateParameter("@ProcessedDate", DbType.DateTime, _processedDate, ParameterDirection.Input));
            }

            var outputParams = da.ExecuteNonQuery("lis_InstrumentQuery_Save", paramsList.ToArray());
            _id = Convert.ToInt64(outputParams["@Id"].Value);

            FlagClean();
        }

        #endregion

    }
}