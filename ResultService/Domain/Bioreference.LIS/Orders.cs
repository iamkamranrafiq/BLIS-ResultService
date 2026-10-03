using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class Orders : DataClassBase
    {

        #region Private Members

        private List<OrderInfo> m_list;

        #endregion

        #region Constructor

        private Orders()
        {
            m_list = new List<OrderInfo>();
        }

        #endregion

        #region Public Functions

        public OrderInfo[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        public static Orders Fetch()
        {

            return (Orders)DataFactory.Fetch(new Criteria());

        }

        /// <summary>
    /// Returns a list of Orders that contain tests that belong the to the specified ReferenceLabID
    /// </summary>
    /// <param name="referenceLabId"></param>
    /// <param name="unSentOnly">Return only orders that have unsent tests.</param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static Orders Fetch(int referenceLabId, bool unSentOnly = true)
        {

            return (Orders)DataFactory.Fetch(new Criteria(referenceLabId, unSentOnly));

        }

        public static Orders Fetch(string accessionNumber)
        {

            return (Orders)DataFactory.Fetch(new Criteria(accessionNumber));

        }


        #endregion



        #region Data Functions

        /// <summary>
    /// When using the referencelabId from the criteria object, we fetch orders based on tests with that referencelabId
    /// </summary>
    /// <param name="criteria"></param>
    /// <remarks></remarks>
        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DataTable[] dt;
            try
            {
                if (c.RefLabId == 0 && string.IsNullOrEmpty(c.AccessionNumber))
                {
                    dt = da.ExecuteProcedure("lis_Orders_Fetch");
                }
                else if (!string.IsNullOrEmpty(c.AccessionNumber))
                {
                    var @params = new DbParameter[1];
                    @params[0] = (DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNumber);
                    dt = da.ExecuteProcedure("lis_Orders_FetchList", @params);
                }
                else
                {
                    var @params = new DbParameter[2];
                    @params[0] = (DbParameter)da.CreateParameter("@ReferenceLabId", DbType.Int32, c.RefLabId);
                    @params[1] = (DbParameter)da.CreateParameter("@UnSentOnly", DbType.Boolean, c.UnSentOnly);
                    dt = da.ExecuteProcedure("lis_ReferenceOrders_Fetch", @params);
                }

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

            OrderInfo oi = null;

            foreach (DataRow row in table.Rows)
            {

                oi = new OrderInfo(this);
                oi.Load(row);
                m_list.Add(oi);

            }

        }

        #endregion

        #region Inner Criteria Class

        [Serializable()]
        internal class Criteria
        {

            private int m_refLabId = 0;
            private bool m_unSentOnly = true;
            private string m_accessionNumber = "";

            public Criteria()
            {
            }

            public Criteria(int refLabId, bool unSentOnly)
            {
                m_refLabId = refLabId;
                m_unSentOnly = unSentOnly;
            }
            public Criteria(string accessionNumber)
            {
                m_accessionNumber = accessionNumber;
            }

            public int RefLabId
            {
                get
                {
                    return m_refLabId;
                }
            }

            public bool UnSentOnly
            {
                get
                {
                    return m_unSentOnly;
                }
            }
            public string AccessionNumber
            {
                get
                {
                    return m_accessionNumber;
                }
            }

        }

        #endregion

    }
}