using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;
using System.Transactions;

namespace Bioreference.LIS
{

    [Serializable()]
    public class UnsolicitedResults : DataClassBase
    {


        #region Private Members

        private UnsolicitedResultList m_list;

        #endregion

        #region Constructor

        internal UnsolicitedResults()
        {
            m_list = new UnsolicitedResultList();
        }

        #endregion

        #region Public Functions


        public static UnsolicitedResults Fetch(string accessionNbr)
        {

            return (UnsolicitedResults)DataFactory.Fetch(new Criteria(accessionNbr));

        }

        public static UnsolicitedResults Fetch()
        {

            return (UnsolicitedResults)DataFactory.Fetch(new Criteria(""));

        }
        public static void ClearExpired(int minutes)
        {
            var da = new DataWrapper(Configuration.ConnectionString);
            var parameters = new List<DbParameter>
            {
                da.CreateParameter("@Minutes", DbType.Int32, minutes, ParameterDirection.Input)
            };

            var options = new TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromMinutes(2)
            };

            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {
                da.ExecuteNonQuery("lis_UnsolicitedResult_Clear", parameters.ToArray());
                scope.Complete();
            }
        }


        public UnsolicitedResult AddResult(string accessionNbr, OrderManager.Result result, DateTime resultsExpireDate)
        {

            var ur = new UnsolicitedResult(accessionNbr, result, resultsExpireDate);
            m_list.Add(ur);
            return ur;

        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Save()
        {

            m_list.Update();

        }

        protected override void DataFactory_Fetch(object criteria)
        {


            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            try
            {

                @param[0] = (DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr);
                DataTable[] dt = da.ExecuteProcedure("lis_UnsolicitedResults_Fetch", @param);

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt);
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }


        }

        private void Load(DataTable[] table)
        {

            UnsolicitedResult ur;
            foreach (DataRow r in table[0].Rows)
            {
                ur = new UnsolicitedResult();
                ur.Load(r);
                m_list.Add(ur);
            }

        }

        public static void MarkSentToETS(string accessionNbr, DateTime sentToETS)
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();
            @params.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, accessionNbr, ParameterDirection.Input));
            if (sentToETS > DateTime.Parse("1900-01-01"))
            {
                @params.Add((DbParameter)da.CreateParameter("@SentToETS", DbType.DateTime, sentToETS, ParameterDirection.Input));
            }
            da.ExecuteNonQuery("lis_UnsolicitedResult_SentToETS", @params.ToArray());

        }

        #endregion

        public UnsolicitedResultList List
        {
            get
            {
                return m_list;
            }
        }

        public override bool IsDirty
        {
            get
            {
                return m_list.IsDirty;
            }
        }

        #region Internal Criteria

        [Serializable()]
        internal class Criteria
        {

            private string m_accessionNbr;

            public Criteria(string accessionNbr)
            {
                m_accessionNbr = accessionNbr;
            }

            public string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }

        }

        #endregion

    }
}