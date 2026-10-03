using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.Data.Security;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RackWorksheetTemplate : DataClassBase, ISecurityRole
    {


        #region Private members

        private int m_id = 0;
        private string m_name = "";
        private int m_specimensPerRack = 0;
        private rackWorksheetType m_worksheetType = rackWorksheetType.NotSet;
        private bool m_hasAssignedFunctionality;
        private bool m_isAutoGenerate;
        private List<RackWorksheetTemplateAnalyte> m_analytes;
        private List<RackWorksheetTemplateAnalyte> m_deleteAnalytes;

        #endregion

        #region Constructor

        internal RackWorksheetTemplate()
        {
            m_analytes = new List<RackWorksheetTemplateAnalyte>();
            m_deleteAnalytes = new List<RackWorksheetTemplateAnalyte>();
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

        public int SpecimensPerRack
        {
            get
            {
                return m_specimensPerRack;
            }
            set
            {
                if (m_specimensPerRack != value)
                {
                    m_specimensPerRack = value;
                    FlagDirty();
                }
            }
        }

        public bool HasAssignedFunctionality
        {
            get
            {
                return m_hasAssignedFunctionality;
            }
        }

        public bool IsAutoGenerate
        {
            get
            {
                return m_isAutoGenerate;
            }
        }

        public rackWorksheetType Type
        {
            get
            {
                return m_worksheetType;
            }
            set
            {
                if (m_worksheetType != value)
                {
                    m_worksheetType = value;
                    FlagDirty();
                }
            }
        }

        public List<RackWorksheetTemplateAnalyte> Analytes
        {
            get
            {
                return m_analytes;
            }
        }

        #endregion

        #region Public Methods

        public RackWorksheetTemplateAnalyte AddAnalyte(string analyteCode)
        {

            return AddAnalyte(analyteCode, "");

        }

        public RackWorksheetTemplateAnalyte AddAnalyte(string analyteCode, string panelCode)
        {

            // Return nothing if already exists.
            foreach (RackWorksheetTemplateAnalyte an in m_analytes)
            {
                if ((an.AnalyteCode ?? "") == (analyteCode ?? "") && (an.PanelCode ?? "") == (panelCode ?? ""))
                    return null;
            }

            var a = new RackWorksheetTemplateAnalyte(this, analyteCode, panelCode);
            m_analytes.Add(a);

            return a;

        }

        public void RemoveAnalyte(string analyteCode)
        {
            RemoveAnalyte(analyteCode, "");
        }

        public void RemoveAnalyte(string analyteCode, string panelCode)
        {

            foreach (RackWorksheetTemplateAnalyte a in m_analytes)
            {
                if ((a.AnalyteCode ?? "") == (analyteCode ?? "") & (a.PanelCode ?? "") == (panelCode ?? ""))
                {
                    m_deleteAnalytes.Add(a);
                    m_analytes.Remove(a);
                    break;
                }
            }

        }

        public RackWorksheetTemplateAnalyte Find(string analyteCode)
        {

            return Find(analyteCode, "");

        }

        public RackWorksheetTemplateAnalyte Find(string analyteCode, string panelCode)
        {

            foreach (RackWorksheetTemplateAnalyte a in m_analytes)
            {
                if ((a.AnalyteCode ?? "") == (analyteCode ?? "") & (a.PanelCode ?? "") == (panelCode ?? ""))
                {
                    return a;
                }
            }

            return null;

        }

        public static RackWorksheetTemplate CreateNew()
        {
            return new RackWorksheetTemplate();
        }

        public static void Delete(int rackWorksheetTemplateId)
        {

            DataFactory.Delete(new Criteria(rackWorksheetTemplateId));

        }

        public static RackWorksheetTemplate Fetch(int rackWorksheetTemplateId)
        {

            return (RackWorksheetTemplate)DataFactory.Fetch(new Criteria(rackWorksheetTemplateId));

        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["RackWorksheetTemplateId"]);
            m_name = Conversions.ToString(row["Name"]);
            m_specimensPerRack = Conversions.ToInteger(row["SpecimensPerRack"]);
            m_worksheetType = (rackWorksheetType)Conversions.ToInteger(row["RackWorksheetType"]);
            m_hasAssignedFunctionality = Conversions.ToBoolean(row["HasAssignedFunctionality"]);
            m_isAutoGenerate = Conversions.ToBoolean(row["IsAutoGenerate"]);

            FlagClean();

        }
       
        protected override void DataFactory_Fetch(object criteria)
        {
            Criteria c = (Criteria)criteria;
            RackWorksheetTemplateAnalyte wsa;
            DataWrapper da = new DataWrapper(Configuration.ConnectionString);

            DbParameter[] param = new DbParameter[1];
            param[0] = da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId);

            DataTable[] dt = da.ExecuteProcedure("lis_RackWorksheetTemplates_Fetch", param);
            int templateId = 0;
            if (dt[0].Rows.Count > 0)
            {
                foreach (DataRow r in dt[0].Rows)
                {
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["RackWorksheetTemplateId"], templateId, false)))
                    {
                        this.Load(r);
                        templateId = Conversions.ToInteger(r["RackWorksheetTemplateId"]);
                    }
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["RackWorksheetTemplateAnalyteId"], 0, false)))
                    {
                        wsa = new RackWorksheetTemplateAnalyte(this);
                        wsa.Load(r);
                        this.Analytes.Add(wsa);
                    }

                }
                
            }

            this.FlagClean();
        }

        protected override void DataFactory_Save()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[4];

            @param[0] = (DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, m_id, ParameterDirection.InputOutput);
            @param[1] = (DbParameter)da.CreateParameter("@Name", DbType.String, m_name);
            @param[2] = (DbParameter)da.CreateParameter("@SpecimensPerRack", DbType.Int32, m_specimensPerRack);
            @param[3] = (DbParameter)da.CreateParameter("@WorksheetType", DbType.Int32, m_worksheetType);

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_RackWorksheetTemplate_Save", @param)["@TemplateId"].Value);


            foreach (RackWorksheetTemplateAnalyte a in m_analytes)
            {
                if (a.IsDirty)
                    a.Update();
            }
            foreach (RackWorksheetTemplateAnalyte a in m_deleteAnalytes)
            {
                if (!a.IsNew)
                    a.Delete();
            }

            FlagClean();

        }

        protected override void DataFactory_Delete(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId);

            da.ExecuteNonQuery("lis_RackWorksheetTemplate_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

        #region Security Implmentation

        public string RoleKey
        {
            get
            {
                return string.Concat(GetType().ToString(), "_", m_id);
            }
        }

        public string RoleName
        {
            get
            {
                return m_name;
            }
        }

        #endregion

        #region Public Overrides

        public override bool IsDirty
        {
            get
            {
                if (m_deleteAnalytes.Count > 0)
                    return true;

                foreach (RackWorksheetTemplateAnalyte a in m_analytes)
                    return true;
                return base.IsDirty;
            }
        }

        #endregion

        #region Friend Criteria Class

        [Serializable()]
        internal class Criteria
        {
            private int m_templateId = 0;
            public Criteria(int templateId)
            {
                m_templateId = templateId;
            }
            public int TemplateId
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