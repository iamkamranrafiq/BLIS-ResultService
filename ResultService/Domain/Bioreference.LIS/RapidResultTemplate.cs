using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Transactions;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RapidResultTemplate : AuditDataClassBase
    {


        #region Private members

        private int m_id = 0;
        private string m_name = "";
        private List<RapidResultTemplateAnalyte> m_list;
        private List<RapidResultTemplateAnalyte> m_deleteList;
        private bool m_requiresVerification = false;
        private bool m_hideResult = false;
        private bool m_confirmDeactivate = false;
        private bool m_printPathologistSheet = false;


        private RapidResultTemplateControlList m_controls;

        private bool m_printLandscape = true;

        #endregion

        #region Constructor

        internal RapidResultTemplate()
        {
            m_controls = new RapidResultTemplateControlList(this);
            m_list = new List<RapidResultTemplateAnalyte>();
            m_deleteList = new List<RapidResultTemplateAnalyte>();
        }

        #endregion

        #region Public Properties

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        [Audit("PrintLandscape")]
        public bool PrintLandscape
        {
            get
            {
                return m_printLandscape;
            }
            set
            {
                if (m_printLandscape != value)
                {
                    m_printLandscape = value;
                    FlagDirty();
                }
            }
        }

        [Audit("RequiresVerification")]
        public bool RequiresVerification
        {
            get
            {
                return m_requiresVerification;
            }
            set
            {
                if (m_requiresVerification != value)
                {
                    m_requiresVerification = value;
                    FlagDirty();
                }
            }
        }

        [Audit("PrintPathologistSheet")]
        public bool PrintPathologistSheet
        {
            get
            {
                return m_printPathologistSheet;
            }
            set
            {
                if (m_printPathologistSheet != value)
                {
                    m_printPathologistSheet = value;
                    FlagDirty();
                }
            }
        }


        [Audit("Name")]
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

        [Audit("HideResult")]
        public bool HideResult
        {
            get
            {
                return m_hideResult;
            }
            set
            {
                if (m_hideResult != value)
                {
                    m_hideResult = value;
                    FlagDirty();
                }
            }
        }

        [Audit("ConfirmDeactivate")]
        public bool ConfirmDeactivate
        {
            get
            {
                return m_confirmDeactivate;
            }
            set
            {
                if (m_confirmDeactivate != value)
                {
                    m_confirmDeactivate = value;
                    FlagDirty();
                }
            }
        }

        public RapidResultTemplateAnalyte[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        public RapidResultTemplateControlList ControlList
        {
            get
            {
                return m_controls;
            }
        }

        #endregion

        #region Public Methods

        public RapidResultTemplateControl AddControlName(string name)
        {
            if (m_controls.Find(name) == null)
            {
                var c = new RapidResultTemplateControl(this);
                c.Name = name;
                m_controls.Add(c);
                return c;
            }
            return null;
        }

        public void RemoveControlName(string name)
        {
            m_controls.Remove(name);
        }



        public static RapidResultTemplate CreateNew()
        {
            return new RapidResultTemplate();
        }

        public static void Delete(int rapidResultTemplateId)
        {

            DataFactory.Delete(new Criteria(rapidResultTemplateId));

        }

        public static RapidResultTemplate Fetch(int rapidResultTemplateId)
        {

            return (RapidResultTemplate)DataFactory.Fetch(new Criteria(rapidResultTemplateId));

        }

        public void AddAnalyte(string analyteCode, string analyteName)
        {

            var a = new RapidResultTemplateAnalyte(this, analyteCode, analyteName);
            m_list.Add(a);

        }

        public void AddAnalyte(string analyteCode)
        {

            var a = new RapidResultTemplateAnalyte(this, analyteCode, analyteCode);
            m_list.Add(a);

        }

        public void RemoveAnalyte(string analyteCode)
        {

            foreach (RapidResultTemplateAnalyte a in m_list)
            {
                if ((a.AnalyteCode ?? "") == (analyteCode ?? ""))
                {
                    m_deleteList.Add(a);
                    m_list.Remove(a);
                    break;
                }
            }

        }

        public string GetAnalyteName(string code)
        {

            foreach (RapidResultTemplateAnalyte a in m_list)
            {
                if ((a.AnalyteCode ?? "") == (code ?? ""))
                    return a.AnalyteName;
            }
            return "";

        }

        public RapidResultTemplateAnalyte Find(string testCode)
        {

            foreach (RapidResultTemplateAnalyte a in List)
            {
                if ((a.AnalyteCode ?? "") == (testCode ?? ""))
                {
                    return a;
                }
            }
            return null;

        }

        #endregion

        #region Data Functions

        internal void Load(DataTable[] dt)
        {

            {
                var withBlock = dt[0].Rows[0];
                m_id = Conversions.ToInteger(withBlock["RapidResultTemplateId"]);
                m_name = Conversions.ToString(withBlock["Name"]);
                m_requiresVerification = Conversions.ToBoolean(withBlock["RequiresVerification"]);
                m_hideResult = Conversions.ToBoolean(withBlock["HideResult"]);
                m_printLandscape = Conversions.ToBoolean(withBlock["PrintLandscape"]);
                m_confirmDeactivate = Conversions.ToBoolean(withBlock["ConfirmDeactivate"]);
                m_printPathologistSheet = Conversions.ToBoolean(withBlock["PrintPathologistSheet"]);
            }

            RapidResultTemplateAnalyte ta;
            foreach (DataRow r in dt[1].Rows)
            {
                ta = new RapidResultTemplateAnalyte(this);
                ta.Load(r);
                m_list.Add(ta);
            }

            m_list.Sort();

            RapidResultTemplateControl c;
            foreach (DataRow r in dt[2].Rows)
            {
                c = new RapidResultTemplateControl(this);
                c.Load(r);
                m_controls.Add(c);
            }

            FlagClean();

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId);

            DataTable[] dt = da.ExecuteProcedure("lis_RapidResultTemplate_Fetch", @param);

            if (dt[0].Rows.Count > 0)
            {
                Load(dt);
            }

            FlagClean();

        }

        protected override void DataFactory_Save()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[7];

            @param[0] = (DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, m_id, ParameterDirection.InputOutput);
            @param[1] = (DbParameter)da.CreateParameter("@Name", DbType.String, m_name);
            @param[2] = (DbParameter)da.CreateParameter("@RequiresVerification", DbType.Boolean, m_requiresVerification);
            @param[3] = (DbParameter)da.CreateParameter("@HideResult", DbType.Boolean, m_hideResult);
            @param[4] = (DbParameter)da.CreateParameter("@PrintLandscape", DbType.Boolean, m_printLandscape);
            @param[5] = (DbParameter)da.CreateParameter("@ConfirmDeactivate", DbType.Boolean, m_confirmDeactivate);
            @param[6] = (DbParameter)da.CreateParameter("@PrintPathologistSheet", DbType.Boolean, m_printPathologistSheet);


            // 'We use the transaction scope when saving the Report. If any sql failures, all transaction should roll back.
            var options = new TransactionOptions();
            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
            options.Timeout = new TimeSpan(0, 2, 0);
            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {

                m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_RapidResultTemplate_Save", @param)["@TemplateId"].Value);

                foreach (RapidResultTemplateAnalyte a in m_list)
                {
                    if (a.IsDirty)
                        a.Update();
                }
                foreach (RapidResultTemplateAnalyte a in m_deleteList)
                {
                    if (!a.IsNew)
                        a.Delete();
                }

                ControlList.Update();

                scope.Complete();

            }

            FlagClean(true);

        }

        protected override void DataFactory_Delete(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId);

            da.ExecuteNonQuery("lis_RapidResultTemplate_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                foreach (RapidResultTemplateAnalyte a in m_list)
                {
                    if (a.IsDirty)
                        return true;
                }

                return base.IsDirty || ControlList.IsDirty || m_deleteList.Count > 0;
            }
        }

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


    }
}