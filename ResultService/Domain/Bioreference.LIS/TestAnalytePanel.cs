using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Common.Lab;
using Bioreference.Common.TestMaster;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    /// <summary>
/// Saves to ReferenceAnalytePanel
/// </summary>
/// <remarks></remarks>
    [Serializable()]
    public class TestAnalytePanel : AnalytePanel
    {

        protected long m_id = 0L;
        protected string m_refLabOrderingCode = "";
        protected bool m_allowPreliminaryRelease = false;
        protected string m_altInboundTestCode;
        protected string m_altOutboundTestCode;
        protected string m_altOutboundTestDesc;
        protected string m_deptShortName;

        private TestLookup m_refTest;

        public TestAnalytePanel(TestInfo test)
        {
            Load(test);
            FlagDirty();
        }

        public TestAnalytePanel(Common.TestMaster.Test test)
        {
            Load(test);
            FlagDirty();
        }

        internal TestAnalytePanel() : base()
        {
        }

        public long Id
        {
            get
            {
                return m_id;
            }
        }

        public string RefLabOrderingCode
        {
            get
            {
                return m_refLabOrderingCode;
            }
        }

        public bool AllowPreliminaryRelease
        {
            get
            {
                return m_allowPreliminaryRelease;
            }
        }

        public string AltInboundTestCode
        {
            get
            {
                return m_altInboundTestCode;
            }
        }
        public string AltOutboundTestCode
        {
            get
            {
                return m_altOutboundTestCode;
            }
        }
        public string AltOutboundDescription
        {
            get
            {
                return m_altOutboundTestDesc;
            }
        }

        public string DepartmentShortName
        {
            get
            {
                return m_deptShortName;
            }
        }

        internal void Load(TestAnalytePanel analytePanel)
        {

            m_id = analytePanel.Id;
            m_code = analytePanel.PanelCode;
            m_name = analytePanel.Name;
            m_category = analytePanel.Category;
            m_reflabCode = analytePanel.ReferenceLabCode;
            m_reflabId = analytePanel.ReferenceLabId;
            m_isReportable = analytePanel.IsReportable;
            m_outboundchannelid = analytePanel.OutboundChannelId;
            m_isAgencyReportable = analytePanel.IsAgencyReportable;

        }

        internal void Load(TestInfo testInfo)
        {

            m_code = testInfo.Code;
            m_name = testInfo.Name;
            m_category = testInfo.Category;
            m_reflabCode = testInfo.ReferenceLabeAnalyteCode;
            m_reflabId = testInfo.ReferenceLabId;
            m_isReportable = testInfo.IsReportable;
            m_outboundchannelid = testInfo.OutboundChannelId;
            m_isAgencyReportable = testInfo.IsAgencyReportable;

            m_altInboundTestCode = testInfo.AltInboundTestCode;
            m_altOutboundTestCode = testInfo.AltOutboundTestCode;
            m_altOutboundTestDesc = testInfo.AltOutBoundTestDescr;

            // 'REMOVED - UNTIL TESTMASTER IS UPGRADED
            m_refLabOrderingCode = testInfo.ReferenceLabOrderingAnalyteCode;
            m_allowPreliminaryRelease = testInfo.AllowPreliminaryRelease;
            m_deptShortName = testInfo.DepartmentShortName;

            FlagDirty();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToLong(row["RefAnalytePanelId"]);
            m_code = Conversions.ToString(row["RefPanelCode"]);
            m_name = Conversions.ToString(row["RefPanelName"]);
            m_category = row["RefPanelCategory"].ToString().Trim();
            m_reflabCode = row["ReferenceLabPanelCode"].ToString().Trim();
            m_reflabId = Conversions.ToInteger(row["PanelReferenceLabId"]);
            m_isReportable = Conversions.ToBoolean(row["IsPanelReportable"]);
            m_outboundchannelid = Conversions.ToInteger(row["OutboundChannelId"]);
            m_isAgencyReportable = Conversions.ToBoolean(row["IsPanelAgencyReportable"]);
            m_refLabOrderingCode = row["RefLabOrderingPanelCode"].ToString().Trim();

            m_altInboundTestCode = Conversions.ToString(row["PanelAltInboundTestCode"]);
            m_altOutboundTestCode = Conversions.ToString(row["PanelAltOutboundTestCode"]);
            m_altOutboundTestDesc = Conversions.ToString(row["PanelAltOutboundTestDesc"]);

            if (row.Table.Columns.Contains("AllowPreliminaryRelease"))
            {
                m_allowPreliminaryRelease = Conversions.ToBoolean(row["AllowPreliminaryRelease"]);

            }

        }

        public void LoadComponents(DataTable table)
        {

            TestAnalyte ra;
            foreach (DataRow r in table.Rows)
            {
                ra = new TestAnalyte();
                ra.Load(r);
                Analytes.Add(ra);
            }

        }

        internal void Load(Common.TestMaster.Test test)
        {

            m_code = test.TestCode;
            m_name = test.ShortName;
            m_category = test.SpecialtyDescr;
            m_reflabCode = test.RefLabTest;
            m_reflabId = test.RefLab;
            m_isReportable = test.IsReportable;
            m_outboundchannelid = test.OutboundChannelId;
            m_isAgencyReportable = test.IsAgencyReportable;

            m_altInboundTestCode = test.AlternateInboundTestcode;
            m_altOutboundTestCode = test.AlternateOutboundTestcode;
            m_altOutboundTestDesc = test.AlternateOutboundDescription;

            // 'REMOVED - UNTIL TESTMASTER IS UPGRADED
            m_refLabOrderingCode = test.RefLabOrderCode;
            m_allowPreliminaryRelease = test.AllowPreliminary;

            m_refTest = new TestLookup(m_code, m_reflabId, m_reflabCode, test.IsProfile, test.IsPanel);
            foreach (TestComponent c in test.Components)
                m_refTest.ComponentCodes.Add(new TestLookupComponent(m_refTest, c.ComponentCode, c.AlternateOutboundTestCode, c.AlternateOutboundDescription, c.AlternateInboundTestCode));

            FlagDirty();

        }

        internal virtual void Update()
        {
            UpdateCore();
        }

        internal void UpdateCore()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@RefAnalytePanelId", DbType.Int32, m_id, ParameterDirection.Output));
            paramList.Add((DbParameter)da.CreateParameter("@Code", DbType.String, m_code));
            paramList.Add((DbParameter)da.CreateParameter("@Name", DbType.String, m_name));
            paramList.Add((DbParameter)da.CreateParameter("@Category", DbType.String, m_category));
            paramList.Add((DbParameter)da.CreateParameter("@RefLabId", DbType.Int32, m_reflabId));
            paramList.Add((DbParameter)da.CreateParameter("@RefLabCode", DbType.String, m_reflabCode));
            paramList.Add((DbParameter)da.CreateParameter("@IsReportable", DbType.Boolean, m_isReportable));
            paramList.Add((DbParameter)da.CreateParameter("@OutboundChannelId", DbType.Int32, m_outboundchannelid));
            paramList.Add((DbParameter)da.CreateParameter("@IsAgencyReportable", DbType.Boolean, m_isAgencyReportable));
            paramList.Add((DbParameter)da.CreateParameter("@ReferenceLabOrderingCode", DbType.String, m_refLabOrderingCode));
            paramList.Add((DbParameter)da.CreateParameter("@AllowPreliminaryRelease", DbType.Boolean, m_allowPreliminaryRelease));

            paramList.Add((DbParameter)da.CreateParameter("@AltInboundTestCode", DbType.String, m_altInboundTestCode));
            paramList.Add((DbParameter)da.CreateParameter("@AltOutboundTestCode", DbType.String, m_altOutboundTestCode));
            paramList.Add((DbParameter)da.CreateParameter("@AltOutboundTestDesc", DbType.String, m_altOutboundTestDesc));

            paramList.Add((DbParameter)da.CreateParameter("@DepartmentShortName", DbType.String, m_deptShortName));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_TestAnalytePanel_Save", @params)["@RefAnalytePanelId"].Value);
            if (!(m_refTest == null))
                m_refTest.Update();


        }

        protected override void DataFactory_Save()
        {
            Update();
        }

    }
}