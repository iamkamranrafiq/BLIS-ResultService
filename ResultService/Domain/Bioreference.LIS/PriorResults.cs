using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class PriorResults : DataClassReadOnlyBase
    {

        private List<PriorResult> _list;

        private PriorResults()
        {
            _list = new List<PriorResult>();
        }

        public PriorResult[] List
        {
            get
            {
                return _list.ToArray();
            }
        }

        public static PriorResults Fetch(int ReportId, string AnalyteCode, bool IncludeTNPQNS, bool includeAllStatus)
        {
            return (PriorResults)DataFactory.Fetch(new Criteria() { ReportId = ReportId, AnalyteCode = AnalyteCode, IncludeTNPQNS = IncludeTNPQNS, IncludeAllStatus = includeAllStatus });
        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();
            DataTable[] dt;
            @params.Add((DbParameter)da.CreateParameter("@ReportId", DbType.Int32, c.ReportId));
            @params.Add((DbParameter)da.CreateParameter("@AnalyteCodes", DbType.String, c.AnalyteCode));
            @params.Add((DbParameter)da.CreateParameter("@LookBackMonths", DbType.Int32, 0));
            @params.Add((DbParameter)da.CreateParameter("@IncludeTNPQNS", DbType.Boolean, c.IncludeTNPQNS));
            @params.Add((DbParameter)da.CreateParameter("@IncludeAllStatus", DbType.Boolean, c.IncludeAllStatus));
            dt = da.ExecuteProcedure("lis_PriorResults_Fetch", @params.ToArray());
            Load(dt[0]);

        }

        protected void Load(DataTable dt)
        {

            foreach (DataRow r in dt.Rows)
                _list.Add(new PriorResult()
                {
                    ReportId = Conversions.ToInteger(r["ReportID"]),
                    ReportAnalyteId = Conversions.ToLong(r["ReportAnalyteID"]),
                    AnalyteCode = Conversions.ToString(r["AnalyteCode"]),
                    EUID = Conversions.ToLong(r["EUID"]),
                    LastName = Conversions.ToString(r["LastName"]),
                    FirstName = Conversions.ToString(r["FirstName"]),
                    ResultValue = Conversions.ToString(r["ResultValue"]),
                    ResultStatus = Conversions.ToInteger(r["ResultStatus"]),
                    ResultStatusStr = Conversions.ToString(r["ResultStatusStr"]),
                    OrderId = Conversions.ToInteger(r["OrderID"]),
                    AccessionNbr = Conversions.ToString(r["AccessionNbr"]),
                    DateServiced = (DateTime?)Interaction.IIf(ReferenceEquals(r["DateServiced"], DBNull.Value), default(DateTime?), r["DateServiced"]),
                    ReleaseDate = (DateTime?)Interaction.IIf(ReferenceEquals(r["ReleaseDate"], DBNull.Value), default(DateTime?), r["ReleaseDate"]),
                    ResultReleasedUser = Conversions.ToString(r["ResultReleasedUser"]),
                    PriorEuidResultValue = Conversions.ToString(r["PriorEUIDResultValue"]),
                    DOB = Conversions.ToString(r["DOB"]),
                    DeltaHoldRule = Conversions.ToString(r["DeltaHoldRule"])
                });

        }

        [Serializable()]
        internal class Criteria
        {
            public int ReportId;
            public string AnalyteCode;
            public bool IncludeTNPQNS;
            public bool IncludeAllStatus;
        }

    }

    [Serializable()]
    public class PriorResult
    {

        public int ReportId;
        public long ReportAnalyteId;
        public string AnalyteCode;
        public long EUID;
        public string LastName;
        public string FirstName;
        public string ResultValue;
        public int ResultStatus;
        public string ResultStatusStr;
        public int OrderId;
        public string AccessionNbr;
        public DateTime? DateServiced;
        public DateTime? ReleaseDate;
        public string ResultReleasedUser;
        public string PriorEuidResultValue;
        public string DOB;
        public string DeltaHoldRule;

        public string DeltaDifference()
        {

            var oldResult = default(decimal);
            var newResult = default(decimal);
            string retValue = "";
            if (string.IsNullOrEmpty(DeltaHoldRule) || string.IsNullOrEmpty(ResultValue) || string.IsNullOrEmpty(PriorEuidResultValue))
                return retValue;
            if (!SharedFunctions.ValidateAndClearInequality(PriorEuidResultValue, ref oldResult))
                return retValue;
            if (!SharedFunctions.ValidateAndClearInequality(ResultValue, ref newResult))
                return retValue;
            decimal diff = Math.Abs(newResult - oldResult);
            if (DeltaHoldRule.Contains("%"))
            {
                diff = diff / oldResult * 100m;
                retValue = $"{diff:#.#}%";
            }
            else
            {
                retValue = $"{diff}";
            }
            return retValue;

        }

    }
}