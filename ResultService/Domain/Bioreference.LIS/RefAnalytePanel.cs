using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RefAnalytePanel : TestAnalytePanel
    {

        private ReportAnalytePanel m_parent;

        internal RefAnalytePanel(ReportAnalytePanel parent) : base()
        {
            m_parent = parent;
        }

        // Friend Overloads Sub Load(ByVal analytePanel As AnalytePanel)

        // With analytePanel
        // m_code = .PanelCode
        // m_name = .Name
        // m_category = .Category
        // m_reflabCode = .ReferenceLabCode
        // m_reflabId = .ReferenceLabId
        // m_isReportable = .IsReportable
        // m_outboundchannelid = .OutboundChannelId
        // m_isAgencyReportable = .IsAgencyReportable
        // End With

        // End Sub

        internal override void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@RefAnalytePanelId", DbType.Int32, m_id));
            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalytePanelId", DbType.Int64, m_parent.Id));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_RefReportAnalytePanel_Save", @params)["@RefAnalytePanelId"].Value);

        }

        /// <summary>
    /// This is the older update method where RefAnalytePanel and Mapping (Ref_Report_AnalytePanel) are updated in the same proc
    /// We currenly are using this when AddFromTestMaster is set to true
    /// </summary>
    /// <remarks></remarks>
        internal void UpdateCombined()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();


            paramList.Add((DbParameter)da.CreateParameter("@ReportAnalytePanelId", DbType.Int64, m_parent.Id));
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

            da.ExecuteNonQuery("lis_RefAnalytePanel_Save", @params);

        }


    }
}