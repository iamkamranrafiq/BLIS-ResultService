using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class TestDetail : DataClassReadOnlyBase
    {

        #region Private Members

        private int m_id = 0;
        private string m_testCode = "";
        private bool m_isPanel = false;
        private bool m_isProfile = false;
        private TestAnalyte m_refAnalyte;
        private TestAnalytePanel m_refAnalytePanel;
        private List<TestAnalyte> m_listAnalyte = new List<TestAnalyte>();
        private List<TestAnalytePanel> m_listPanel = new List<TestAnalytePanel>();

        #endregion

        public int GetReferenceLabId()
        {
            if (IsPanel && !(AnalytePanel == null))
            {
                return AnalytePanel.ReferenceLabId;
            }
            else if (!(Analyte == null))
            {
                return Analyte.ReferenceLabId;
            }
            return 0;
        }

        public string GetReferenceLabAnalyteCode()
        {
            if (IsPanel && !(AnalytePanel == null))
            {
                return AnalytePanel.ReferenceLabCode;
            }
            else if (!(Analyte == null))
            {
                return Analyte.ReferenceLabeAnalyteCode;
            }
            return 0.ToString();
        }

        public string GetReferenceLabOrderingAnalyteCode()
        {
            if (IsPanel && !(AnalytePanel == null))
            {
                return AnalytePanel.RefLabOrderingCode;
            }
            else if (!(Analyte == null))
            {
                return Analyte.ReferenceLabOrderingAnalyteCode;
            }
            return 0.ToString();
        }

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string TestCode
        {
            get
            {
                return m_testCode;
            }
        }

        public bool IsPanel
        {
            get
            {
                return m_isPanel;
            }
        }

        public bool IsProfile
        {
            get
            {
                return m_isProfile;
            }
        }

        public TestAnalyte Analyte
        {
            get
            {
                return m_refAnalyte;
            }
        }

        public TestAnalytePanel AnalytePanel
        {
            get
            {
                return m_refAnalytePanel;
            }
        }

        public List<TestAnalytePanel> AnalytePanelList
        {
            get
            {
                return m_listPanel;
            }
        }

        public List<TestAnalyte> AnalyteList
        {
            get
            {
                return m_listAnalyte;
            }
        }

        #endregion

        public static TestDetail Fetch(string testCode, int refLabId)
        {

            TestDetail rt = (TestDetail)DataFactory.Fetch(new Criteria(testCode, refLabId));

            if (!string.IsNullOrEmpty(rt.TestCode))
            {
                return rt;
            }
            else
            {
                return null;
            }

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DataTable[] dt;

            var @params = new DbParameter[2];
            @params[0] = (DbParameter)da.CreateParameter("@TestCode", DbType.String, c.TestCode);
            @params[1] = (DbParameter)da.CreateParameter("@RefLabId", DbType.String, c.RefLabId);
            dt = da.ExecuteProcedure("lis_TestDetail_Fetch", @params);

            if (dt[0].Rows.Count > 0)
            {
                Load(dt);
            }

        }

        private void Load(DataTable[] tables)
        {

            var r = tables[0].Rows[0];

            m_testCode = Conversions.ToString(r["ParentCode"]);
            m_isProfile = Conversions.ToBoolean(r["IsProfile"]);
            m_isPanel = Conversions.ToBoolean(r["IsPanel"]);

            if (m_isProfile)
            {

                foreach (DataRow row in tables[0].Rows)
                {
                    var td = Fetch(Conversions.ToString(row["TestCode"]), 0);
                    m_listAnalyte.AddRange(td.AnalyteList);
                    m_listPanel.AddRange(td.AnalytePanelList);
                }
            }

            else if (m_isPanel)
            {
                m_refAnalytePanel = new TestAnalytePanel();
                m_refAnalytePanel.Load(r);
                m_refAnalytePanel.LoadComponents(tables[1]);

                m_listPanel.Add(m_refAnalytePanel);
            }
            else
            {
                m_refAnalyte = new TestAnalyte();
                m_refAnalyte.Load(r);

                m_listAnalyte.Add(m_refAnalyte);
            }

        }



        [Serializable()]
        internal class Criteria
        {
            private string m_testCode;
            private int m_refLabId;

            public Criteria(string testCode, int refLabId)
            {
                m_testCode = testCode;
                m_refLabId = refLabId;
            }
            public string TestCode
            {
                get
                {
                    return m_testCode;
                }
            }
            public int RefLabId
            {
                get
                {
                    return m_refLabId;
                }
            }
        }

    }
}