using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Transactions;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class ReportCompleted : AuditDataClassBase
    {

        #region Private Members

        private long _id = 0L;
        private long _reportId = 0L;
        private string _accessionNumber;
        private DateTime _finalReportDate;
        private DateTime _serviceDate;
        private string _status;

        #endregion

        #region Public Properties

        public string AccessionNumber
        {
            get
            {
                return _accessionNumber;
            }
            set
            {
                _accessionNumber = value;
            }
        }

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

        public DateTime FinalReportDate
        {
            get
            {
                return _finalReportDate;
            }
            set
            {
                _finalReportDate = value;
            }
        }

        public DateTime ServiceDate
        {
            get
            {
                return _serviceDate;
            }
            set
            {
                _serviceDate = value;
            }
        }

        public string Status
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

        #region Override Properties

        public override object IdentifierId
        {
            get
            {
                return _id;
            }
        }

        #endregion

        #region Internal Criteria Class

        [Serializable()]
        internal class Criteria
        {

        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {
            throw new NotImplementedException();
        }

        internal void Load(DataRow row)
        {
            throw new NotImplementedException();
        }

        public void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            // Dim userName As String = ""

            // If Not IsNothing(MyBase.CurrentUser) Then
            // userName = MyBase.CurrentUser.Name
            // End If

            var options = new TransactionOptions();
            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
            options.Timeout = new TimeSpan(0, 2, 0);

            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {

                // Save master NoBillCode row
                var paramList = new List<DbParameter>();
                paramList.Add((DbParameter)da.CreateParameter("@ReportCompletedId", DbType.Int64, _id, ParameterDirection.InputOutput));
                paramList.Add((DbParameter)da.CreateParameter("@ReportId", DbType.Int64, _reportId));
                paramList.Add((DbParameter)da.CreateParameter("@AccessionNumber", DbType.String, _accessionNumber));
                paramList.Add((DbParameter)da.CreateParameter("@FinalReportDate", DbType.DateTime, _finalReportDate));
                paramList.Add((DbParameter)da.CreateParameter("@ServiceDate", DbType.DateTime, _serviceDate));
                paramList.Add((DbParameter)da.CreateParameter("@Status", DbType.String, _status));
                _id = Conversions.ToLong(da.ExecuteNonQuery("lis_ReportCompleted_Save", paramList.ToArray())["@ReportCompletedId"].Value);

                FlagClean();
                scope.Complete();
            }

        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}