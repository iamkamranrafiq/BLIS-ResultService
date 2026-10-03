using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{
    // Imports System.Transactions

    [Serializable()]
    public class ExternalCalcLabQueue : AuditDataClassBase
    {

        #region  Private Members 

        private int _externalCalcLabQueueId = 0;
        private int _externalCalcLabId;
        private string _calculationName;
        private long _reportId;
        private string _userName;
        private string _requestData;
        private int _status;

        #endregion

        #region  Public Properties 


        // '''''''''''''''''''''''''''''''''''''''''''''
        // ''''' Property ExternalCalcLabQueueId
        // '''''''''''''''''''''''''''''''''''''''''''''    
        public int ExternalCalcLabQueueId
        {
            get
            {
                return _externalCalcLabQueueId;
            }
            set
            {
                _externalCalcLabQueueId = value;
            }
        }

        // '''''''''''''''''''''''''''''''''''''''''''''
        // ''''' Property ExternalCalcLabId
        // '''''''''''''''''''''''''''''''''''''''''''''    
        public int ExternalCalcLabId
        {
            get
            {
                return _externalCalcLabId;
            }
            set
            {
                _externalCalcLabId = value;
            }
        }

        // '''''''''''''''''''''''''''''''''''''''''''''
        // ''''' Property CalculationName
        // '''''''''''''''''''''''''''''''''''''''''''''    
        public string CalculationName
        {
            get
            {
                return _calculationName;
            }
            set
            {
                _calculationName = value;
            }
        }

        // '''''''''''''''''''''''''''''''''''''''''''''
        // ''''' Property ReportId
        // '''''''''''''''''''''''''''''''''''''''''''''    
        public long ReportId
        {
            get
            {
                return _reportId;
            }
            set
            {
                _reportId = value;
            }
        }

        // '''''''''''''''''''''''''''''''''''''''''''''
        // ''''' Property UserName
        // '''''''''''''''''''''''''''''''''''''''''''''    
        public string UserName
        {
            set
            {
                _userName = value;
            }
        }

        // '''''''''''''''''''''''''''''''''''''''''''''
        // ''''' Property RequestData
        // '''''''''''''''''''''''''''''''''''''''''''''    
        public string RequestData
        {
            get
            {
                return _requestData;
            }
            set
            {
                _requestData = value;
            }
        }

        // '''''''''''''''''''''''''''''''''''''''''''''
        // ''''' Property Status
        // '''''''''''''''''''''''''''''''''''''''''''''    
        public int Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
            }
        }


        #endregion

        #region  Overridden Properties 

        public override object IdentifierId
        {
            get
            {
                return _externalCalcLabQueueId;
            }
        }

        #endregion

        #region Constructor

        internal ExternalCalcLabQueue()
        {
            FlagDirty();
        }


        internal ExternalCalcLabQueue(int extCalcLabQueueId)
        {
            _externalCalcLabQueueId = extCalcLabQueueId;
            FlagDirty();
        }

        internal ExternalCalcLabQueue(int externalCalcLabId, string calculationName)
        {
            _externalCalcLabQueueId = 0;
            _externalCalcLabId = externalCalcLabId;
            _calculationName = calculationName;
            FlagDirty();
        }

        #endregion


        #region  Criteria Class 
        [Serializable()]
        internal class Criteria
        {
            private readonly int _externalCalcLabId;
            private readonly string _calculationName;

            public int ExternalCalcLabId
            {
                get
                {
                    return _externalCalcLabId;
                }
            }

            public string CalculationName
            {
                get
                {
                    return _calculationName;
                }
            }

            public Criteria(int extCalcLabId, string calName)
            {
                _externalCalcLabId = extCalcLabId;
                _calculationName = calName;
            }

        }

        #endregion


        #region Public Shared Methods
        public static ExternalCalcLabQueue CreateExternalCalcLabQueue(int externalCalcLabId, string calculationName)
        {
            // Note: do not call the fetch from this routine.  The fetch is only to be used by the Channel sending the data to Fibrosure.
            // and it will delete the row it fetches.

            // Dim objExternalCalcLabQueue As ExternalCalcLabQueue = Fetch(externalCalcLabId, calculationName)

            // If IsNothing(objExternalCalcLabQueue) Then
            return new ExternalCalcLabQueue(externalCalcLabId, calculationName);
            // End If

            // Return objExternalCalcLabQueue
        }

        public static ExternalCalcLabQueue Fetch(int externalCalcLabId, string calculationName)
        {

            ExternalCalcLabQueue objExternalCalcLabQueue = (ExternalCalcLabQueue)DataFactory.Fetch(new Criteria(externalCalcLabId, calculationName));
            // If objExternalCalcLabQueue Is Nothing OrElse objExternalCalcLabQueue.ExternalCalcLabQueueId = 0 Then
            // Return Nothing
            // Else
            return objExternalCalcLabQueue;
            // End If
        }

        #endregion


        #region  Overridden Methods 

        protected override void DataFactory_Fetch(object criteria)
        {
            // can only be used by the channel sending the information to Fibrosure.

            Criteria c = (Criteria)criteria;
            var @params = new List<DbParameter>();
            // Dim dt() As DataTable = Nothing
            var dw = new DataWrapper(Configuration.ConnectionString);

            @params.Add((DbParameter)dw.CreateParameter("@ExternalCalcLabId", DbType.Int32, c.ExternalCalcLabId));
            @params.Add((DbParameter)dw.CreateParameter("@CalculationName", DbType.String, c.CalculationName));

            DataTable[] dt = dw.ExecuteProcedure("lis_ExternalCalcLabQueue_FetchByLabIdandName", @params.ToArray());

            if (dt is not null && dt.Length > 0 && dt[0].Rows.Count > 0)
            {
                Load(dt[0].Rows[0]);
            }
        }

        public void Load(DataRow row)
        {
            _externalCalcLabQueueId = Conversions.ToInteger(row["RouteToClacLabQueueId"]);
            _externalCalcLabId = Conversions.ToInteger(row["ExternalCalcLabId"]);
            _calculationName = Conversions.ToString(row["CalculationName"]);
            _reportId = Conversions.ToLong(row["ReportId"]);
            _requestData = Conversions.ToString(row["RequestData"]);
            _status = Conversions.ToInteger(row["Status"]);
        }


        protected override void DataFactory_Save()
        {
            var dw = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();

            try
            {
                @params.Add((DbParameter)dw.CreateParameter("@ExternalCalcLabID", DbType.Int32, _externalCalcLabId));
                @params.Add((DbParameter)dw.CreateParameter("@CalculationName", DbType.String, _calculationName));
                @params.Add((DbParameter)dw.CreateParameter("@ReportId", DbType.Int64, _reportId));
                @params.Add((DbParameter)dw.CreateParameter("@UserName", DbType.String, _userName));
                @params.Add((DbParameter)dw.CreateParameter("@ExternalCalcQueueId", DbType.Int32, _externalCalcLabQueueId));
                @params.Add((DbParameter)dw.CreateParameter("@RequestData", DbType.String, _requestData));

                _externalCalcLabQueueId = Conversions.ToInteger(dw.ExecuteNonQuery("lis_ExternalCalcLabQueue_Save", @params.ToArray())["@ExternalCalcQueueId"].Value);
            }
            catch (Exception ex)
            {
                throw;
            }

        }



        #endregion

    }
}