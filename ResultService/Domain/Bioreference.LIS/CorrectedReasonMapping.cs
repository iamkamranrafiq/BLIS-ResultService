using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class CorrectedReasonMappings : DataClassBase
    {

        #region Private Members

        private List<CorrectedReasonMapping> _correctedReasonMappingList = new List<CorrectedReasonMapping>();

        #endregion

        #region Properties

        public List<CorrectedReasonMapping> List
        {
            get
            {
                return _correctedReasonMappingList;
            }
        }

        #endregion

        #region Public Methods

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();
            paramList.Add((DbParameter)da.CreateParameter("@CorrectedReasonMappingId", DbType.Int64, Convert.ToInt64(c.CorrectedReasonMappingId)));
            try
            {
                DataTable[] dt = da.ExecuteProcedure("lis_CorrectedReasonMapping_Fetch", paramList.ToArray());
                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0]);
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.StackTrace.ToString());
            }

        }

        private void Load(DataTable table)
        {

            CorrectedReasonMapping p;
            foreach (DataRow r in table.Rows)
            {
                p = new CorrectedReasonMapping();
                p.Load(r);
                _correctedReasonMappingList.Add(p);
            }
        }

        public static CorrectedReasonMappings Fetch(int correctedReasonMappingId)
        {
            return (CorrectedReasonMappings)DataFactory.Fetch(new Criteria(correctedReasonMappingId));
        }

        public static CorrectedReasonMappings Fetch()
        {
            return (CorrectedReasonMappings)DataFactory.Fetch(new Criteria(0));
        }

        #endregion

        internal class Criteria
        {
            private int _correctedReasonMappingId;
            public Criteria(int correctedReasonMappingId)
            {
                _correctedReasonMappingId = correctedReasonMappingId;
            }
            public Criteria()
            {
            }

            public int CorrectedReasonMappingId
            {
                get
                {
                    return _correctedReasonMappingId;
                }
            }

        }
    }

    public class CorrectedReasonMapping : DataClassBase
    {

        #region Private Members

        private int _correctedReasonMappingId;
        private int _secondaryId;
        private string _secondaryReasonName;
        private int _phaseId;
        private string _phaseName;
        private int _primaryId;
        private string _primaryReasonName;
        private int _causedById;
        private string _causedByName;
        private int _controllableId;
        private string _controllableName;

        #endregion

        #region Properties

        public int CorrectedReasonMappingId
        {
            get
            {
                return _correctedReasonMappingId;
            }
        }

        public int SecondaryId
        {
            get
            {
                return _secondaryId;
            }
        }

        public object SecondaryReasonName
        {
            get
            {
                return _secondaryReasonName;
            }
        }

        public int PhaseId
        {
            get
            {
                return _phaseId;
            }
        }

        public object PhaseName
        {
            get
            {
                return _phaseName;
            }
        }

        public int PrimaryId
        {
            get
            {
                return _primaryId;
            }
        }

        public object PrimaryReasonName
        {
            get
            {
                return _primaryReasonName;
            }
        }

        public int CausedById
        {
            get
            {
                return _causedById;
            }
        }

        public object CausedByName
        {
            get
            {
                return _causedByName;
            }
        }

        public int ControllableId
        {
            get
            {
                return _controllableId;
            }
        }

        public object ControllableName
        {
            get
            {
                return _controllableName;
            }
        }

        #endregion

        #region Constructor

        internal CorrectedReasonMapping()
        {
        }

        #endregion

        internal void Load(DataRow row)
        {
            _correctedReasonMappingId = Conversions.ToInteger(row["CorrectedReasonMappingId"]);
            _secondaryId = Conversions.ToInteger(row["SecondaryId"]);
            _secondaryReasonName = Conversions.ToString(row["SecondaryReasonName"]);
            _phaseId = Conversions.ToInteger(row["PhaseId"]);
            _phaseName = Conversions.ToString(row["PhaseName"]);
            _primaryId = Conversions.ToInteger(row["PrimaryId"]);
            _primaryReasonName = Conversions.ToString(row["PrimaryReasonName"]);
            _causedById = Conversions.ToInteger(row["CausedById"]);
            _causedByName = Conversions.ToString(row["CausedByName"]);
            _controllableId = Conversions.ToInteger(row["ControllableId"]);
            _controllableName = Conversions.ToString(row["ControllableName"]);
        }

    }
}