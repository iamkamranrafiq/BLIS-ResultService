using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Xml;
using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class UnsolicitedResult : DataClassBase
    {

        #region Constructors

        public UnsolicitedResult(string accessionNbr, OrderManager.Result orderManagerResult, DateTime resultsExpireDate)
        {
            m_accessionNbr = accessionNbr;
            m_resultsExpireDate = resultsExpireDate;
            Load(orderManagerResult);
            FlagDirty();
        }

        internal UnsolicitedResult()
        {
        }

        internal UnsolicitedResult(string accessionNbr, DateTime resultsExpireDate)
        {
            m_accessionNbr = accessionNbr;
            m_resultsExpireDate = resultsExpireDate;
            FlagDirty();
        }

        #endregion

        #region Private Members

        private int m_id = 0;
        private string m_accessionNbr = "";
        private string m_analyteCode = "";
        private string m_panelCode = "";
        private string m_resultValue = "";
        private DateTime m_resultDate;
        private List<string> m_resultAlertCodes = new List<string>();
        private List<string> m_comments = new List<string>();
        private bool m_isProcessed = false; // 'Indicates if result was set to the ReportAnalyte.
        private DateTime m_sentToETS = DateTime.Parse("1900-01-01");
        private DateTime m_resultsExpireDate = DateTime.Parse("1900-01-01");

        // Instrument info
        private string m_instrument = "";
        private string m_instrumentId = "";
        private string m_instrumentAlt1 = "";
        private string m_instrumentAlt2 = "";
        private string m_instrumentAlt3 = "";

        // Specimen info
        private string m_specimenRackID = "";
        private string m_specimenRackPos = "";
        private string m_specimenRackSeq = "";
        private string m_specimenAlt1 = "";
        private string m_specimenAlt2 = "";

        // 'For reference analyte
        private string m_flagValue = "";
        private string m_units = "";
        private string m_refRange = "";
        private string m_name = "";
        private int m_refLabId = 0;

        #endregion

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public string AnalyteCode
        {
            get
            {
                return m_analyteCode;
            }
        }

        public string PanelCode
        {
            get
            {
                return m_panelCode;
            }
        }

        public string ResultValue
        {
            get
            {
                return m_resultValue;
            }
        }

        public DateTime ResultDate
        {
            get
            {
                return m_resultDate;
            }
        }

        public string[] ResultAlertCodes
        {
            get
            {
                return m_resultAlertCodes.ToArray();
            }
        }

        public string[] Comments
        {
            get
            {
                return m_comments.ToArray();
            }
        }

        public bool IsProcessed
        {
            get
            {
                return m_isProcessed;
            }
        }

        public string Instrument
        {
            get
            {
                return m_instrument;
            }
        }

        public string InstrumentId
        {
            get
            {
                return m_instrumentId;
            }
        }

        public string InstrumentAlt1
        {
            get
            {
                return m_instrumentAlt1;
            }
        }

        public string InstrumentAlt2
        {
            get
            {
                return m_instrumentAlt2;
            }
        }

        public string InstrumentAlt3
        {
            get
            {
                return m_instrumentAlt3;
            }
        }

        public string SpecimenRackId
        {
            get
            {
                return m_specimenRackID;
            }
        }

        public string SpecimenRackPosition
        {
            get
            {
                return m_specimenRackPos;
            }
        }

        public string SpecimenRackSeqeunce
        {
            get
            {
                return m_specimenRackSeq;
            }
        }

        public string SpecimenAlt1
        {
            get
            {
                return m_specimenAlt1;
            }
        }

        public string SpecimenAlt2
        {
            get
            {
                return m_specimenAlt2;
            }
        }

        public string FlagValue
        {
            get
            {
                return m_flagValue;
            }
        }

        public string Units
        {
            get
            {
                return m_units;
            }
        }

        public string RefRange
        {
            get
            {
                return m_refRange;
            }
        }

        public string AnalyteName
        {
            get
            {
                return m_name;
            }
        }

        public int RefLabId
        {
            get
            {
                return m_refLabId;
            }
        }

        public DateTime SentToETS
        {
            get
            {
                return m_sentToETS;
            }
        }

        #endregion

        #region Public Functions

        public void MarkProcessed()
        {

            if (m_isProcessed == false)
            {
                m_isProcessed = true;
                FlagDirty();
            }

        }

        public DateTime ResultsExpireDate
        {
            get
            {
                return m_resultsExpireDate;
            }
        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Save()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            // Comments
            var xmldoc = new XmlDocument();
            var root = xmldoc.CreateElement("Comments");
            xmldoc.AppendChild(root);
            foreach (string s in m_comments)
            {
                var eComment = xmldoc.CreateElement("Comment");
                eComment.InnerText = s;
                root.AppendChild(eComment);
            }
            string xmlComments = xmldoc.InnerXml;

            // AlertCodes
            xmldoc = new XmlDocument();
            root = xmldoc.CreateElement("Alerts");
            xmldoc.AppendChild(root);
            foreach (string s in m_resultAlertCodes)
            {
                var eComment = xmldoc.CreateElement("Alert");
                eComment.InnerText = s;
                root.AppendChild(eComment);
            }
            string xmlAlerts = xmldoc.InnerXml;

            paramList.Add((DbParameter)da.CreateParameter("@UnsolicitedResultId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, m_accessionNbr));
            paramList.Add((DbParameter)da.CreateParameter("@ResultValue", DbType.String, m_resultValue));
            paramList.Add((DbParameter)da.CreateParameter("@PanelCode", DbType.String, m_panelCode));
            paramList.Add((DbParameter)da.CreateParameter("@AnalyteCode", DbType.String, m_analyteCode));
            if (m_resultDate > DateTime.Parse("1900-01-01"))
                paramList.Add((DbParameter)da.CreateParameter("@ResultDate", DbType.DateTime, m_resultDate));
            paramList.Add((DbParameter)da.CreateParameter("@FlagValue", DbType.String, m_flagValue));

            paramList.Add((DbParameter)da.CreateParameter("@CommentsXml", DbType.String, xmlComments));
            paramList.Add((DbParameter)da.CreateParameter("@AlertsXml", DbType.String, xmlAlerts));

            paramList.Add((DbParameter)da.CreateParameter("@Instrument", DbType.String, m_instrument));
            paramList.Add((DbParameter)da.CreateParameter("@InstrumentId", DbType.String, m_instrumentId));
            paramList.Add((DbParameter)da.CreateParameter("@InstrumentAlt1", DbType.String, m_instrumentAlt1));
            paramList.Add((DbParameter)da.CreateParameter("@InstrumentAlt2", DbType.String, m_instrumentAlt2));
            paramList.Add((DbParameter)da.CreateParameter("@InstrumentAlt3", DbType.String, m_instrumentAlt3));

            paramList.Add((DbParameter)da.CreateParameter("@SpecimenRackId", DbType.String, m_specimenRackID));
            paramList.Add((DbParameter)da.CreateParameter("@SpecimenRackPosition", DbType.String, m_specimenRackPos));
            paramList.Add((DbParameter)da.CreateParameter("@SpecimenRackSequence", DbType.String, m_specimenRackSeq));
            paramList.Add((DbParameter)da.CreateParameter("@SpecimenAlt1", DbType.String, m_specimenAlt1));
            paramList.Add((DbParameter)da.CreateParameter("@SpecimenAlt2", DbType.String, m_specimenAlt2));
            paramList.Add((DbParameter)da.CreateParameter("@IsProcessed", DbType.Boolean, m_isProcessed));

            paramList.Add((DbParameter)da.CreateParameter("@Units", DbType.String, m_units));
            paramList.Add((DbParameter)da.CreateParameter("@RefRange", DbType.String, m_refRange));
            paramList.Add((DbParameter)da.CreateParameter("@AnalyteName", DbType.String, m_name));
            paramList.Add((DbParameter)da.CreateParameter("@RefLabId", DbType.Int32, m_refLabId));
            paramList.Add((DbParameter)da.CreateParameter("@SentToETS", DbType.DateTime, m_sentToETS));
            paramList.Add((DbParameter)da.CreateParameter("@ResultsExpireDate", DbType.DateTime, m_resultsExpireDate));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_UnsolicitedResult_Save", @params)["@UnsolicitedResultId"].Value);

            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["UnsolicitedResultId"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            m_analyteCode = Conversions.ToString(row["AnalyteCode"]);
            m_panelCode = Conversions.ToString(row["PanelCode"]);
            m_resultValue = Conversions.ToString(row["ResultValue"]);
            m_resultDate = Conversions.ToDate(row["ResultDate"]);

            // Instrument info
            m_instrument = Conversions.ToString(row["Instrument"]);
            m_instrumentId = Conversions.ToString(row["InstrumentId"]);
            m_instrumentAlt1 = Conversions.ToString(row["InstrumentAlt1"]);
            m_instrumentAlt2 = Conversions.ToString(row["InstrumentAlt2"]);
            m_instrumentAlt3 = Conversions.ToString(row["InstrumentAlt3"]);

            // Specimen info
            m_specimenRackID = Conversions.ToString(row["SpecimenRackId"]);
            m_specimenRackPos = Conversions.ToString(row["SpecimenRackPosition"]);
            m_specimenRackSeq = Conversions.ToString(row["SpecimenRackSequence"]);
            m_specimenAlt1 = Conversions.ToString(row["SpecimenAlt1"]);
            m_specimenAlt2 = Conversions.ToString(row["SpecimenAlt2"]);

            // 'For reference analyte
            m_flagValue = Conversions.ToString(row["FlagValue"]);
            m_units = Conversions.ToString(row["Units"]);
            m_refRange = Conversions.ToString(row["RefRange"]);
            m_name = Conversions.ToString(row["AnalyteName"]);
            m_refLabId = Conversions.ToInteger(row["RefLabId"]);

            // 'For Sending to ETS
            m_sentToETS = Conversions.ToDate(row["SentToETS"]);
            m_resultsExpireDate = Conversions.ToDate(row["ResultsExpireDate"]);

            // Comments
            var xmlstr = new System.IO.StringReader(Conversions.ToString(row["CommentsXml"]));
            var xmldoc = new System.Xml.XPath.XPathDocument(xmlstr);
            var xmlnav = xmldoc.CreateNavigator();

            var xmlComments = xmlnav.Select("Comments/Comment");
            if (xmlComments.Count > 0)
            {
                while (xmlComments.MoveNext())
                    m_comments.Add(xmlComments.Current.Value.NormalizeToWindows());
            }

            // Alert codes
            xmlstr = new System.IO.StringReader(Conversions.ToString(row["AlertsXml"]));
            xmldoc = new System.Xml.XPath.XPathDocument(xmlstr);
            xmlnav = xmldoc.CreateNavigator();

            var xmlAlerts = xmlnav.Select("Alerts/Alert");
            if (xmlAlerts.Count > 0)
            {
                while (xmlAlerts.MoveNext())
                    m_resultAlertCodes.Add(xmlAlerts.Current.Value);

            }

            FlagClean();

        }

        private void Load(OrderManager.Result result)
        {

            m_analyteCode = result.AnalyteCode;
            m_panelCode = result.PanelCode;
            m_resultValue = result.ResultValue;
            m_resultDate = result.ResultDate;

            // Instrument info
            m_instrument = result.Instrument;
            m_instrumentId = result.InstrumentId;
            m_instrumentAlt1 = result.InstrumentAlt1;
            m_instrumentAlt2 = result.InstrumentAlt2;
            m_instrumentAlt3 = result.InstrumentAlt3;

            // Specimen info
            m_specimenRackID = result.SpecimenRackId;
            m_specimenRackPos = result.SpecimenRackPosition;
            m_specimenRackSeq = result.SpecimenRackSequence;
            m_specimenAlt1 = result.SpecimenAlt1;
            m_specimenAlt2 = result.SpecimenAlt2;

            // 'For reference analyte
            m_flagValue = result.FlagValue;
            m_units = result.Units;
            m_refRange = result.ReferenceRange;
            m_name = result.AnalyteName;
            m_refLabId = result.ReferenceLabId;

            if (!(result.Comments == null))
            {
                foreach (string s in result.Comments)
                    m_comments.Add(s.NormalizeToWindows());
            }

            if (!(result.ResultAlertCodes == null))
            {
                foreach (string s in result.ResultAlertCodes)
                    m_resultAlertCodes.Add(s);

            }

        }

        #endregion

    }
}