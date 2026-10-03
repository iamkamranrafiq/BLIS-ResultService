using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadTemplate : DataClassBase
    {


        #region Private Members

        private int m_id = 0;
        private string m_name = "";
        private string m_panelCode = "";
        private int m_totalCount = 0;
        private DiffPadCellTemplateList m_list = null;
        private string m_totalTestCode = "";

        #endregion

        #region Constructors

        internal DiffPadTemplate()
        {
            m_list = new DiffPadCellTemplateList();
        }

        #endregion

        public void Delete()
        {

            DataFactory.Delete(new Criteria(m_id));

        }

        public static DiffPadTemplate CreateNew()
        {

            var t = new DiffPadTemplate();
            return t;

        }

        public void DeleteCell(DiffPadTemplateCell cell)
        {

            List.Remove(cell);

        }

        public DiffPadTemplateCell AddCell(DiffPadCell cell)
        {

            var c = new DiffPadTemplateCell(this, cell);
            List.Add(c);
            return c;

        }

        public DiffPadTemplateCell AddCell(Analyte analyte)
        {

            var c = new DiffPadTemplateCell(this, analyte);
            List.Add(c);
            return c;

        }

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string PanelCode
        {
            get
            {
                return m_panelCode;
            }
            set
            {
                if ((m_panelCode ?? "") != (value.Trim() ?? ""))
                {
                    m_panelCode = value.Trim();
                    FlagDirty();
                }
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
                if ((m_name ?? "") != (value.Trim() ?? ""))
                {
                    m_name = value.Trim();
                    FlagDirty();
                }
            }
        }

        public int TotalCount
        {
            get
            {
                return m_totalCount;
            }
            set
            {
                if (m_totalCount != value)
                {
                    m_totalCount = value;
                    FlagDirty();
                }
            }
        }

        public string TotalTestCode
        {
            get
            {
                return m_totalTestCode;
            }
            set
            {
                if ((m_totalTestCode ?? "") != (value ?? ""))
                {
                    m_totalTestCode = value;
                    FlagDirty();
                }
            }
        }

        public DiffPadCellTemplateList List
        {
            get
            {
                return m_list;
            }
        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DbParameter @param;

            try
            {
                @param = (DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId);

                DataTable[] dt = da.ExecuteProcedure("lis_DiffPadTemplate_Fetch", @param);

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

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["DiffPadTemplateId"]);
            m_name = Conversions.ToString(row["TemplateName"]);
            m_totalCount = Conversions.ToInteger(row["TotalCount"]);
            m_panelCode = Conversions.ToString(row["PanelCode"]);
            m_totalTestCode = Conversions.ToString(row["TotalTestCode"]);

            FlagClean();

        }

        private void Load(DataTable[] tables)
        {

            {
                var withBlock = tables[0].Rows[0];
                m_id = Conversions.ToInteger(withBlock["DiffLabTemplateId"]);
                m_name = Conversions.ToString(withBlock["TemplateName"]);
                m_totalCount = Conversions.ToInteger(withBlock["TotalCount"]);
                m_panelCode = Conversions.ToString(withBlock["PanelCode"]);
                m_totalTestCode = Conversions.ToString(withBlock["TotalTestCode"]);
            }

            DiffPadTemplateCell c;
            foreach (DataRow r in tables[1].Rows)
            {
                c = new DiffPadTemplateCell(this);
                c.Load(r, null);
                m_list.Add(c);
            }

        }

        protected override void DataFactory_Save()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@DiffPadTemplateId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@TemplateName", DbType.String, m_name));
            paramList.Add((DbParameter)da.CreateParameter("@TotalCount", DbType.Int32, m_totalCount));
            paramList.Add((DbParameter)da.CreateParameter("@PanelCode", DbType.String, m_panelCode));
            paramList.Add((DbParameter)da.CreateParameter("@TotalTestCode", DbType.String, m_totalTestCode));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_DiffPadTemplate_Save", @params)["@DiffPadTemplateId"].Value);

            m_list.Update();

            FlagClean();

        }

        protected override void DataFactory_Delete(object criteria)
        {

            Criteria oCriteria = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            var @param = new DbParameter[1];
            @param[0] = (DbParameter)da.CreateParameter("@DiffPadTemplateId", DbType.Int32, oCriteria.TemplateId);

            da.ExecuteNonQuery("lis_DiffPadTemplate_Delete", @param);

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

        #region Inner Criteria
        [Serializable()]
        internal class Criteria
        {
            private int m_templateId = 0;
            public Criteria(int templateId)
            {
                m_templateId = templateId;
            }

            public object TemplateId
            {
                get
                {
                    return m_templateId;
                }
            }
        }
        #endregion

    }
}