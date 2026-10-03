using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    // 
    // Created By: Victor Castro-Castillo
    // Date: 12/8/2014 10:08:11 AM
    // 
    [Serializable()]
    public class B2Version : AuditDataClassBase
    {

        #region  Private Members 

        private DateTime m_serverDateTime;

        #endregion

        #region  Public Properties 

        public DateTime ServerDateTime
        {
            get
            {
                return m_serverDateTime;
            }
        }

        #endregion

        #region  Overridden Properties 

        public override object IdentifierId
        {
            get
            {
                return 1;
            }
        }

        #endregion

        #region Constructor

        internal B2Version()
        {
            FlagDirty();
        }

        #endregion

        #region  Criteria Class 
        [Serializable()]
        internal class Criteria
        {
            private string m_PCName;
            private string m_DotNetVersion;
            private string m_OSVersion;
            private string m_PhysicalMemory;
            private string m_userName;

            public string PCName
            {
                get
                {
                    return m_PCName;
                }
            }

            public string UserName
            {
                get
                {
                    return m_userName;
                }
            }

            public string DotNetVersion
            {
                get
                {
                    return m_DotNetVersion;
                }
            }

            public string OSVersion
            {
                get
                {
                    return m_OSVersion;
                }
            }

            public string PhysicalMemory
            {
                get
                {
                    return m_PhysicalMemory;
                }
            }

            public Criteria(string pcName, string dotNetVersion, string osVersion, string physicalMemory, string userName)
            {
                m_PCName = pcName;
                m_DotNetVersion = dotNetVersion;
                m_OSVersion = osVersion;
                m_PhysicalMemory = physicalMemory;
                m_userName = userName;
            }

        }

        #endregion

        private static string GetDotNetVersion()
        {
            try
            {
                string s = @"SOFTWARE\\Microsoft\NET Framework Setup\\NDP\\v4\\Full";
                var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(s);
                uint release = uint.Parse(k.GetValue("Release").ToString());
                if (release >= 528040L)
                    return "4.8";
                if (release >= 461808L)
                    return "4.7.2";
                if (release >= 461308L)
                    return "4.7.1";
                if (release >= 460798L)
                    return "4.7";
                if (release >= 394802L)
                    return "4.6.2";
                if (release >= 394254L)
                    return "4.6.1";
                if (release >= 393295L)
                    return "4.6";
                if (release >= 379893L)
                    return "4.5.2";
                if (release >= 378675L)
                    return "4.5.1";
                if (release >= 378389L)
                    return "4.5";
                return "??";
            }
            catch (Exception ex)
            {
                return "??";
            }
        }

        #region Public Shared Methods

        public static B2Version Fetch(bool saveInfo, string userName)
        {

            string pcName = "";
            string dotNetVersion = "";
            string osVersion = "";
            //string physicalMemory = "";

            try
            {
                if (saveInfo)
                {
                    pcName = Environment.MachineName;
                    dotNetVersion = GetDotNetVersion();
                    osVersion = Environment.OSVersion.VersionString;
                    //physicalMemory = $"{My.MyProject.Computer.Info.TotalPhysicalMemory / 1073741824d:0.0} GB";
                }
            }
            catch (Exception ex)
            {
            }
            return (B2Version)DataFactory.Fetch(new Criteria(pcName, dotNetVersion, osVersion, "", userName));

        }

        #endregion

        #region  Overridden Methods 

        protected override void DataFactory_Fetch(object criteria)
        {
            try
            {
                Criteria c = (Criteria)criteria;
                var @params = new List<DbParameter>();
                DataTable[] dt = null;
                var dw = new DataWrapper(Configuration.ConnectionString);
                @params.Add((DbParameter)dw.CreateParameter("@PCName", DbType.String, c.PCName));
                @params.Add((DbParameter)dw.CreateParameter("@UserName", DbType.String, c.UserName));
                @params.Add((DbParameter)dw.CreateParameter("@DotNetVersion", DbType.String, c.DotNetVersion));
                @params.Add((DbParameter)dw.CreateParameter("@OSVersion", DbType.String, c.OSVersion));
                @params.Add((DbParameter)dw.CreateParameter("@PhysicalMemory", DbType.String, c.PhysicalMemory));
                dt = dw.ExecuteProcedure("lis_TrackClientInfo", @params.ToArray());
                if (dt is not null && dt.Length > 0 && dt[0].Rows.Count > 0)
                {
                    Load(dt[0].Rows[0]);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Load(DataRow row)
        {
            try
            {
                m_serverDateTime = Conversions.ToDate(row["ServerDateTime"]);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

    }
}