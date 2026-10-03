using System;
using System.Collections;
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
    public class CriticalReport : DataClassBase
    {

        #region Private Members

        private long m_objectId = 0L;
        private string m_acctNbr = "";
        private string m_accessionNbr = "";

        // Private m_analyte As RefAnalyte = Nothing
        // Private m_analytes As List(Of RefAnalyte) = New List(Of RefAnalyte)
        private Hashtable m_resultValues = null;
        private string m_resultValue = "";
        private string m_name = "";

        private string m_isPanel = "";
        private string m_testCode = "";

        private criticalType m_critical = criticalType.NotEvaluated;
        private string m_patientName = "";
        private DateTime m_dateOfService;
        private List<AnalyteInfo> m_analyteInfo = new List<AnalyteInfo>();

        #endregion

        #region AnalyteInfo Class
        [Serializable()]
        public class AnalyteInfo
        {

            private string m_code = "";
            private string m_name = "";
            private string m_value = "";
            internal AnalyteInfo(string code, string name, string value)
            {
                m_code = code;
                m_name = name;
                m_value = value;
            }

            public string TestCode
            {
                get
                {
                    return m_code;
                }
            }

            public string Name
            {
                get
                {
                    return m_name;
                }
            }

            public string Value
            {
                get
                {
                    return m_value;
                }
            }
        }
        #endregion

        #region Constructor

        internal CriticalReport()
        {
            // m_analyte = New RefAnalyte(Nothing)
        }

        #endregion

        #region Public Properties

        public string PatientName
        {
            get
            {
                return m_patientName;
            }
        }

        public DateTime DateOfService
        {
            get
            {
                return m_dateOfService;
            }
        }

        public string AccountNumber
        {
            get
            {
                return m_acctNbr;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public string PanelCode
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
                return Conversions.ToBoolean(m_isPanel);
            }
        }

        public string TestName
        {
            get
            {
                return m_name;
            }
        }

        public string TestCode
        {
            get
            {
                return m_testCode;
            }
        }

        public string ResultValue
        {
            get
            {
                return m_resultValue;
            }
        }

        public Hashtable ResultValues
        {
            get
            {
                return m_resultValues;
            }
        }

        public AnalyteInfo[] Analytes
        {
            get
            {
                return m_analyteInfo.ToArray();
            }
        }

        public criticalType Critical
        {
            get
            {
                return m_critical;
            }
            set
            {
                if (m_critical != value)
                {
                    m_critical = value;
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Data Functions

        internal void AddAnalyte(string testCode, string value, string name = "")
        {

            // Me.m_analytes.Add(analyte)
            if (!m_resultValues.Contains(testCode))
            {
                m_resultValues.Add(testCode, value);
                m_analyteInfo.Add(new AnalyteInfo(testCode, name, value));
            }

        }

        internal void Load(DataRow row)
        {


            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(row["ReportAnalytePanelId"], 0, false)))
            {
                m_objectId = Conversions.ToLong(row["ReportAnalyteId"]);
                m_resultValue = Conversions.ToString(row["ResultValue"]);
                m_testCode = row["AnalyteCode"].ToString().PadLeft(4, '0'); // Needs to be at least 4 char long.
                m_isPanel = Conversions.ToString(false);
            }
            // m_analyte = New RefAnalyte(Nothing)
            // m_analyte.Load(row)
            else
            {
                m_objectId = Conversions.ToLong(row["ReportAnalytePanelId"]);
                m_testCode = row["PanelCode"].ToString().PadLeft(4, '0'); // Needs to be at least 4 char long.
                m_isPanel = Conversions.ToString(true);
                m_resultValues = new Hashtable();
            }

            m_acctNbr = Conversions.ToString(row["AccountNumber"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            // m_testCode = .Item("PanelCode")
            // m_isPanel = IIf(m_testCode = "", False, True)
            m_critical = (criticalType)Conversions.ToInteger(row["CriticalType"]);
            m_patientName = Conversions.ToString(row["PatientName"]);
            m_dateOfService = Conversions.ToDate(row["DateServiced"]);
            // m_analyte.Load(row)
            m_name = Conversions.ToString(row["TestName"]);

            FlagClean();

        }

        internal void Update()
        {
            InternalUpdate();
        }

        protected override void DataFactory_Save()
        {
            InternalUpdate();
        }

        private void InternalUpdate()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@Id", DbType.Int32, m_objectId));
            paramList.Add((DbParameter)da.CreateParameter("@IsPanel", DbType.Boolean, m_isPanel));
            paramList.Add((DbParameter)da.CreateParameter("@CriticalType", DbType.Int32, m_critical));

            DbParameter[] @params = paramList.ToArray();
            da.ExecuteNonQuery("lis_CriticalReport_Save", @params);

            FlagClean();

        }

        #endregion

    }
}