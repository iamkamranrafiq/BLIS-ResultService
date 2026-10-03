using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.Data.Security;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{


    [Serializable()]
    public class RackWorksheetTemplates : DataClassReadOnlyBase, ISecurityParent
    {

        #region Private Members

        private List<RackWorksheetTemplate> m_list;

        #endregion

        #region Constructor

        public RackWorksheetTemplates()
        {
            m_list = new List<RackWorksheetTemplate>();
        }

        #endregion

        #region Public Properties

        public RackWorksheetTemplate[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        #endregion

        #region Public Functions

        public static RackWorksheetTemplate Create()
        {

            var t = new RackWorksheetTemplate();
            return t;

        }


        public static RackWorksheetTemplates Fetch()
        {

            return (RackWorksheetTemplates)DataFactory.Fetch(new Criteria());

        }

        public RackWorksheetTemplate Find(int templateId)
        {

            foreach (RackWorksheetTemplate i in m_list)
            {
                if (i.Id == templateId)
                    return i;
            }
            return null;

        }

        public RackWorksheetTemplateAnalyte FindAnalyte(string analyteCode)
        {
            RackWorksheetTemplateAnalyte a;
            foreach (RackWorksheetTemplate t in m_list)
            {
                a = t.Find(analyteCode);
                if (!(a == null))
                    return a;
            }
            return null;
        }

        #endregion

        #region Security Implementation

        public ISecurityRole[] FetchRoles()
        {

            var t = Fetch();
            if (!(t == null))
            {
                return t.m_list.ToArray();
            }
            else
            {
                return null;
            }

        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            try
            {
                DataTable[] dt = da.ExecuteProcedure("lis_RackWorksheetTemplates_Fetch");

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0]);
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        private void Load(DataTable dt)
        {

            RackWorksheetTemplate ws = null;
            RackWorksheetTemplateAnalyte wsa;

            // For Each r As DataRow In dt.Rows
            // ws = New RackWorksheetTemplate()
            // ws.Load(r)
            // m_list.Add(ws)
            // Next

            int templateId = 0;
            DataTable t = null;
            foreach (DataRow r in dt.Rows)
            {

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["RackWorksheetTemplateId"], templateId, false)))
                {
                    ws = new RackWorksheetTemplate();
                    ws.Load(r);
                    m_list.Add(ws);
                    templateId = Conversions.ToInteger(r["RackWorksheetTemplateId"]);
                }

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["RackWorksheetTemplateAnalyteId"], 0, false)))
                {
                    wsa = new RackWorksheetTemplateAnalyte(ws);
                    wsa.Load(r);
                    ws.Analytes.Add(wsa);
                }

            }

        }

        #endregion

        [Serializable()]
        internal class Criteria
        {
            public Criteria()
            {
            }
        }

    }
}