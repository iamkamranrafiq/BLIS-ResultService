using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RapidResultTemplates : DataClassReadOnlyBase
    {

        private List<RapidResultTemplateInfo> m_list;

        private RapidResultTemplates()
        {
            m_list = new List<RapidResultTemplateInfo>();
        }

        public RapidResultTemplateInfo[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }


        public static RapidResultTemplates Fetch()
        {

            return (RapidResultTemplates)DataFactory.Fetch(new Criteria());

        }

        public RapidResultTemplateInfo Find(int templateId)
        {

            foreach (RapidResultTemplateInfo i in m_list)
            {
                if (i.Id == templateId)
                    return i;
            }
            return null;

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            try
            {
                DataTable[] dt = da.ExecuteProcedure("lis_RapidResultTemplates_Fetch");

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

            RapidResultTemplateInfo ws;
            foreach (DataRow r in dt.Rows)
            {
                ws = new RapidResultTemplateInfo(Conversions.ToInteger(r["RapidResultTemplateId"]), Conversions.ToString(r["Name"]), Conversions.ToBoolean(r["RequiresVerification"]), Conversions.ToBoolean(r["HideResult"]), Conversions.ToBoolean(r["PrintLandscape"]), Conversions.ToBoolean(r["ConfirmDeactivate"]), Conversions.ToBoolean(r["PrintPathologistSheet"]));
                m_list.Add(ws);
            }

        }

        [Serializable()]
        internal class Criteria
        {
            public Criteria()
            {
            }
        }

    }


    [Serializable()]
    public class RapidResultTemplateInfo
    {

        private int m_id = 0;
        private string m_name = "";
        private bool m_requiresVerification = false;
        private bool m_hideResult = false;
        private bool m_printLandscape = false;
        private bool m_confirmDeactivate = false;
        private bool m_printPathologistSheet = false;

        internal RapidResultTemplateInfo(int id, string name, bool requiresVerification, bool hideResult, bool printLandscape, bool confirmDeactivate, bool printPathologistSheet)
        {
            m_id = id;
            m_name = name;
            m_requiresVerification = requiresVerification;
            m_hideResult = hideResult;
            m_printLandscape = printLandscape;
            m_confirmDeactivate = confirmDeactivate;
            m_printPathologistSheet = printPathologistSheet;
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
        }

        public bool RequiresVerification
        {
            get
            {
                return m_requiresVerification;
            }
        }

        public bool PrintLandscape
        {
            get
            {
                return m_printLandscape;
            }
        }

        public bool HideResult
        {
            get
            {
                return m_hideResult;
            }
        }

        public bool ConfirmDeactivate
        {
            get
            {
                return m_confirmDeactivate;
            }
        }

        public bool PrintPathologistSheet
        {
            get
            {
                return m_printPathologistSheet;
            }
        }

    }
}