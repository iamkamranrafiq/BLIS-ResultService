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
    public class DiffPadTemplateCell : DataClassBase
    {

        #region Private Members

        private int m_id = 0;
        private DiffPadTemplate m_parent = null;
        private DiffPadCell m_cell = null;
        private string m_keyCode = "";
        private bool m_alwaysDisplay = false;

        #endregion

        #region Constructors

        internal DiffPadTemplateCell(DiffPadTemplate parent, Analyte analyte)
        {
            m_parent = parent;
            m_cell = new DiffPadCell(analyte);
            FlagDirty();
        }

        internal DiffPadTemplateCell(DiffPadTemplate parent, DiffPadCell cell)
        {
            m_parent = parent;
            m_cell = cell;
            FlagDirty();
        }

        internal DiffPadTemplateCell(DiffPadTemplate parent)
        {
            m_parent = parent;
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

        public DiffPadCell Cell
        {
            get
            {
                return m_cell;
            }
        }

        public DiffPadTemplate ParentTemplate
        {
            get
            {
                return m_parent;
            }
        }

        public string KeyCode
        {
            get
            {
                return m_keyCode;
            }
            set
            {
                if ((m_keyCode ?? "") != (value.Trim() ?? ""))
                {
                    m_keyCode = value;
                    FlagDirty();
                }
            }
        }

        [Obsolete("Use DisplayByDefault field from Analyte/RefAnalyte object.")]
        public bool AlwaysDisplay
        {
            get
            {
                return m_alwaysDisplay;
            }
            set
            {
                if (value != m_alwaysDisplay)
                {
                    m_alwaysDisplay = value;
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Data Functions

        /// <summary>
    /// If DiffPadCell is passed in as Nothing, then a DiffPadCell is created and the DataRow is passed to its Load function.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="cell"></param>
    /// <remarks></remarks>
        internal void Load(DataRow row, DiffPadCell cell)
        {

            if (cell == null)
            {
                var c = new DiffPadCell();
                c.Load(row);
                m_cell = c;
            }
            else
            {
                m_cell = cell;
            }

            m_id = Conversions.ToInteger(row["DiffPadTemplateCellId"]);
            m_keyCode = Conversions.ToString(row["KeyCode"]);
            m_alwaysDisplay = Conversions.ToBoolean(row["AlwaysDisplay"]);

            FlagClean();

        }

        internal void Update()
        {

            if (m_cell.IsNew)
            {
                m_cell.Update();
            }

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@DiffPadCellId", DbType.Int32, m_cell.Id));
            paramList.Add((DbParameter)da.CreateParameter("@DiffPadTemplateId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@KeyCode", DbType.String, m_keyCode));
            paramList.Add((DbParameter)da.CreateParameter("@AlwaysDisplay", DbType.Boolean, m_alwaysDisplay));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_DiffPadTemplateCell_Save", @params)["@Id"].Value);

            FlagClean();


        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_DiffPadTemplateCell_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

    }
}