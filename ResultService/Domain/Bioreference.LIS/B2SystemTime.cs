using System;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class B2SystemTime : DataClassBase
    {

        private DateTime m_currentB2DateTime;

        public DateTime CurrentB2DateTime
        {
            get
            {
                return m_currentB2DateTime;
            }
        }

        protected override void DataFactory_Fetch(object criteria)
        {

            var da = new DataWrapper(Configuration.ConnectionString);

            try
            {
                DataTable[] dt = da.ExecuteProcedure("b2_server_time_Fetch");

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

        public static B2SystemTime Fetch()
        {

            return (B2SystemTime)DataFactory.Fetch(new Criteria());

        }

        internal B2SystemTime()
        {
        }

        private void Load(DataTable table)
        {

            foreach (DataRow r in table.Rows)
            {
                m_currentB2DateTime = Conversions.ToDate(r["current_b2_date"]);
                break;
            }
        }

        [Serializable()]
        internal class Criteria
        {
            public Criteria()
            {
            }
        }
    }
}