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

    [Serializable()]
    public class ReportFilters : DataClassBase
    {

        #region Private Members
        private ReportFilterList m_list;
        #endregion

        #region Constructor
        private ReportFilters()
        {
            m_list = new ReportFilterList();
        }
        #endregion

        #region Public Properties
        public ReportFilterList List
        {
            get
            {
                return m_list;
            }
        }
        #endregion

        #region Public Functions

        public static ReportFilters Fetch()
        {
            return (ReportFilters)DataFactory.Fetch(new Criteria());
        }

        public ReportFilter AddReportFilter()
        {

            var f = new ReportFilter();
            List.Add(f);

            return f;

        }

        public void DeleteReportFilter(int filterId)
        {

            foreach (ReportFilter f in List)
            {
                if (f.Id == filterId)
                    List.Remove(f);
                break;
            }

        }

        public void DeleteReportFilter(ReportFilter filter)
        {

            List.Remove(filter);

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

            try
            {
                DataTable[] dt = da.ExecuteProcedure("lis_ReportFilters_Fetch");

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt);
                }

                FlagClean();
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        internal void Load(DataTable[] table)
        {

            ReportFilter rf = null;
            ReportFilterItem rfi = null;
            int id = 0;

            foreach (DataRow r in table[0].Rows)
            {

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["ReportFilterId"], id, false)))
                {
                    rf = new ReportFilter();
                    rf.Load(r);
                    List.Add(rf);
                    id = Conversions.ToInteger(r["ReportFilterId"]);
                }

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["ReportFilterItemId"], 0, false)))
                {
                    rfi = new ReportFilterItem(rf);
                    rfi.Load(r);
                    rf.List.Add(rfi);

                }

            }

        }

        #endregion

        #region Criteria Class

        [Serializable()]
        public class Criteria
        {
            public Criteria()
            {
            }
        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || List.IsDirty;
            }
        }

    }


    [Serializable()]
    public class ReportFilterList : DataClassCollectionBase
    {

        public ReportFilter this[int reportFilterId]
        {
            get
            {
                foreach (ReportFilter r in List)
                {
                    if (r.Id == reportFilterId)
                        return r;
                }
                return null;
            }
        }

        public ReportFilter this[object index]
        {
            get
            {
                return (ReportFilter)List[Conversions.ToInteger(index)];
            }
        }

        internal void Add(ReportFilter filter)
        {
            List.Add(filter);
        }

        internal void Remove(ReportFilter filter)
        {
            List.Remove(filter);
        }

        internal void Update()
        {

            foreach (ReportFilter r in List)
                r.Update();

            if (!(DeletedItemsList == null))
            {
                foreach (ReportFilter a in DeletedItemsList)
                {
                    if (a.IsNew == false)
                        a.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (ReportFilter f in List)
                {
                    if (f.IsDirty)
                        return true;
                }

                return default;
            }
        }

    }


    [Serializable()]
    public class ReportFilter : DataClassBase
    {

        #region Private Members
        private int m_id = 0;
        private string m_name = "";
        private resultStatusType m_resultStatusType = resultStatusType.Pending;
        private transmitStatusType m_transmitStatusType = transmitStatusType.NotSet;
        private hasResultsType m_hasResults = hasResultsType.NotSet;
        private ReportFilterItemList m_list;

        // Friend m_testCodes As List(Of String)
        #endregion

        #region Constructor

        internal ReportFilter()
        {
            // m_testCodes = New List(Of String)
            m_list = new ReportFilterItemList();
            FlagClean();
        }

        #endregion

        #region Public Properties

        public ReportFilterItemList List
        {
            get
            {
                return m_list;
            }
        }

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_name ?? ""))
                {
                    m_name = value.Trim();
                    FlagDirty();
                }
            }
        }

        public resultStatusType ResultStatus
        {
            get
            {
                return m_resultStatusType;
            }
            set
            {
                if (value != m_resultStatusType)
                {
                    m_resultStatusType = value;
                    FlagDirty();
                }
            }
        }

        public transmitStatusType TransmitStatus
        {
            get
            {
                return m_transmitStatusType;
            }
            set
            {
                if (value != m_transmitStatusType)
                {
                    m_transmitStatusType = value;
                    FlagDirty();
                }
            }
        }

        public hasResultsType HasResults
        {
            get
            {
                return m_hasResults;
            }
            set
            {
                if (value != m_hasResults)
                {
                    m_hasResults = value;
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Public Methods

        public void AddFilterItem(string testCode)
        {

            var i = new ReportFilterItem(this, testCode);
            List.Add(i);

        }

        public void DeleteFilterItem(string testCode)
        {

            foreach (ReportFilterItem i in List)
            {
                if ((i.Code ?? "") == (testCode ?? ""))
                {
                    List.Remove(i);
                    break;
                }
            }

        }

        #endregion

        #region Data Functions

        internal void Update()
        {

            if (base.IsDirty)
            {

                var da = new DataWrapper(Configuration.ConnectionString);
                var paramList = new List<DbParameter>();

                paramList.Add((DbParameter)da.CreateParameter("@ReportFilterId", DbType.Int32, m_id, ParameterDirection.InputOutput));
                paramList.Add((DbParameter)da.CreateParameter("@FilterName", DbType.String, m_name));
                paramList.Add((DbParameter)da.CreateParameter("@ResultStatusType", DbType.Int32, m_resultStatusType));
                paramList.Add((DbParameter)da.CreateParameter("@TransmitStatusType", DbType.Int32, m_transmitStatusType));
                paramList.Add((DbParameter)da.CreateParameter("@HasResults", DbType.Int32, m_hasResults));

                DbParameter[] @params = paramList.ToArray();

                m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_ReportFilter_Save", @params)["@ReportFilterId"].Value);

                FlagClean();

            }

            m_list.Update();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@ReportFilterId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_ReportFilter_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["ReportFilterId"]);
            m_name = Conversions.ToString(row["FilterName"]);
            m_resultStatusType = (resultStatusType)Conversions.ToInteger(row["ResultStatusType"]);
            m_transmitStatusType = (transmitStatusType)Conversions.ToInteger(row["TransmitStatusType"]);
            m_hasResults = (hasResultsType)Conversions.ToInteger(row["HasResults"]);

        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || List.IsDirty;
            }
        }

    }

    [Serializable()]
    public class ReportFilterItemList : DataClassCollectionBase
    {

        public ReportFilterItem this[object index]
        {
            get
            {
                return (ReportFilterItem)List[Conversions.ToInteger(index)];
            }
        }

        internal void Add(ReportFilterItem filterItem)
        {
            List.Add(filterItem);
        }

        internal void Remove(ReportFilterItem filterItem)
        {
            List.Remove(filterItem);
        }

        internal void Update()
        {

            foreach (ReportFilterItem i in List)
            {
                if (i.IsDirty)
                    i.Update();
            }

            if (!(DeletedItemsList == null))
            {
                foreach (ReportFilterItem a in DeletedItemsList)
                {
                    if (a.IsNew == false)
                        a.Delete();
                }
                DeletedItemsList.Clear();
            }

        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (ReportFilterItem f in List)
                {
                    if (f.IsDirty)
                        return true;
                }

                return default;
            }
        }

    }

    [Serializable()]
    public class ReportFilterItem : DataClassBase
    {

        #region Private Members
        private ReportFilter m_parent;
        private int m_id = 0;
        private string m_code = "";
        #endregion

        #region Constructor

        internal ReportFilterItem(ReportFilter parent)
        {
            m_parent = parent;
            FlagChild();
        }

        internal ReportFilterItem(ReportFilter parent, string code)
        {
            m_parent = parent;
            m_code = code;
            FlagChild();
            FlagDirty();
        }

        #endregion

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string Code
        {
            get
            {
                return m_code;
            }
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["ReportFilterItemId"]);
            m_code = Conversions.ToString(row["TestCode"]);

            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@ReportFilterItemId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@ReportFilterId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@TestCode", DbType.String, m_code));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_ReportFilterItem_Save", @params)["@ReportFilterItemId"].Value);

            FlagClean();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@ReportFilterItemId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_ReportFilterItem_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

    }
}