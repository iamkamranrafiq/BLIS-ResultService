using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadCell : DataClassBase
    {


        #region Private Members

        private int m_id = 0;
        private string m_cellName = "";
        private string m_code = ""; // Used to map to analyte code
        private string m_absoluteTestCode = ""; // 'Used when DiffPadCellType = Absolute

        private DiffPadCellType m_type = DiffPadCellType.Count;
        private DiffPadCellValueList m_list = null;

        #endregion

        #region Constructor

        internal DiffPadCell(Analyte analyte)
        {
            m_list = new DiffPadCellValueList();

            m_cellName = analyte.Name;
            m_code = analyte.Code;

            FlagChild();
            FlagDirty();
        }

        internal DiffPadCell()
        {
            m_list = new DiffPadCellValueList();
            FlagChild();
            FlagDirty();
        }

        #endregion

        #region Public Functions

        public DiffPadCellValue AddValue(string value, int orderIndex)
        {

            var v = new DiffPadCellValue(this);
            v.Value = value;
            v.OrderIndex = orderIndex;
            m_list.Add(v);

            return v;

        }

        public void RemoveValue(DiffPadCellValue cellValue)
        {

            m_list.Remove(cellValue);

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

        public string AbsoluteTestCode
        {
            get
            {
                return m_absoluteTestCode;
            }
            set
            {
                if ((m_absoluteTestCode ?? "") != (value.Trim() ?? ""))
                {
                    m_absoluteTestCode = value;
                    FlagDirty();
                }
            }
        }

        public string CellName
        {
            get
            {
                return m_cellName;
            }
            set
            {
                if ((m_cellName ?? "") != (value.Trim() ?? ""))
                {
                    m_cellName = value;
                    FlagDirty();
                }
            }
        }

        public string Code
        {
            get
            {
                return m_code;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_code ?? ""))
                {
                    m_code = value.Trim();
                    FlagDirty();
                }
            }
        }

        public DiffPadCellType Type
        {
            get
            {
                return m_type;
            }
            set
            {
                if (m_type != value)
                {
                    m_type = value;
                    FlagDirty();
                }
            }
        }

        public DiffPadCellValueList ValueList
        {
            get
            {
                return m_list;
            }
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["DiffPadCellId"]);
            m_cellName = Conversions.ToString(row["CellName"]);
            m_code = Conversions.ToString(row["Code"]);
            m_type = (DiffPadCellType)Conversions.ToInteger(row["Type"]);
            m_absoluteTestCode = Conversions.ToString(row["AbsoluteTestCode"]);

            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@DiffPadCellId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@CellName", DbType.String, m_cellName));
            paramList.Add((DbParameter)da.CreateParameter("@Code", DbType.String, m_code));
            paramList.Add((DbParameter)da.CreateParameter("@Type", DbType.Int32, m_type));
            paramList.Add((DbParameter)da.CreateParameter("@AbsoluteTestcode", DbType.String, m_absoluteTestCode));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_DiffPadCell_Save", @params)["@DiffPadCellId"].Value);

            m_list.Update();

            FlagClean();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@DiffPadCellId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_DiffPadCell_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_list.IsDirty;
            }
        }

    }

    [Serializable()]
    public class DiffPadCellValueList : DataClassCollectionBase
    {

        internal void Add(DiffPadCellValue value)
        {
            List.Add(value);
        }

        internal void Remove(DiffPadCellValue value)
        {
            List.Remove(value);
        }

        public DiffPadCellValue Find(int orderIndex)
        {

            foreach (DiffPadCellValue d in List)
            {
                if (d.OrderIndex == orderIndex)
                {
                    return d;
                }
            }

            return null;

        }

        /// <summary>
    /// Finds the next value, starting with the orderIndex passed in
    /// </summary>
    /// <param name="orderIndex"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public DiffPadCellValue FindNext(int orderIndex)
        {

            int nextOrder = 0;
            foreach (DiffPadCellValue d in List)
            {
                if (d.OrderIndex == orderIndex)
                {
                    return d;
                }
                if (d.OrderIndex > orderIndex && d.OrderIndex < nextOrder)
                {
                    nextOrder = d.OrderIndex;
                }
                if (nextOrder == 0 && d.OrderIndex > orderIndex)
                    nextOrder = d.OrderIndex;
            }

            return Find(nextOrder);

        }

        /// <summary>
    /// Finds the next value, starting with the orderIndex passed in
    /// </summary>
    /// <param name="orderIndex"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public DiffPadCellValue FindPrevious(int orderIndex)
        {

            int nextOrder = orderIndex;
            foreach (DiffPadCellValue d in List)
            {
                if (d.OrderIndex == orderIndex)
                {
                    return d;
                }
                if (d.OrderIndex < orderIndex && d.OrderIndex > nextOrder)
                {
                    nextOrder = d.OrderIndex;
                }
            }

            return Find(nextOrder);

        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (DiffPadCellValue f in List)
                {
                    if (f.IsDirty)
                        return true;
                }

                return base.IsDirty;

            }
        }

        public DiffPadCellValue this[int index]
        {
            get
            {
                return (DiffPadCellValue)List[index];
            }
        }

        internal void Update()
        {

            foreach (DiffPadCellValue f in List)
            {
                if (f.IsDirty)
                {
                    f.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (DiffPadCellValue f in DeletedItemsList)
                {
                    if (f.IsNew == false)
                        f.Delete();
                }
                DeletedItemsList.Clear();
            }

        }


    }

    [Serializable()]
    public class DiffPadCellValue : DataClassBase
    {

        #region Private Members
        private int m_id = 0;
        private int m_orderIndex = 0;
        private string m_value = "";
        private DiffPadCell m_parent = null;
        #endregion

        #region Constructor

        internal DiffPadCellValue(DiffPadCell parent)
        {
            m_parent = parent;
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

        public int OrderIndex
        {
            get
            {
                return m_orderIndex;
            }
            set
            {
                if (value != m_orderIndex)
                {
                    m_orderIndex = value;
                    FlagDirty();
                }
            }
        }

        public string Value
        {
            get
            {
                return m_value;
            }
            set
            {
                if ((m_value ?? "") != (value.Trim() ?? ""))
                {
                    m_value = value.Trim();
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@Value", DbType.String, m_value));
            paramList.Add((DbParameter)da.CreateParameter("@DiffPadCellId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@OrderIndex", DbType.Int32, m_orderIndex));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_DiffPadCellValue_Save", @params)["@Id"].Value);

            FlagClean();

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_DiffPadCellValue_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["DiffPadCellValueId"]);
            m_value = Conversions.ToString(row["Value"]);
            m_orderIndex = Conversions.ToInteger(row["OrderIndex"]);

            FlagClean();

        }

        #endregion

    }
}