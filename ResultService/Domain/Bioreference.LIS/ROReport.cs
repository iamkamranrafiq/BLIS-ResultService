using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Common;
using Bioreference.Common.Lab;
using Bioreference.Common.TestMaster;
using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    /// <summary>
/// ReadOnly versions of the Report, ReportAnalytePanel, ReportAnalyte, ReportComment
/// Used for outbound results.
/// </summary>
/// <remarks></remarks>

    [Serializable()]
    public class ROReport : DataClassReadOnlyBase
    {

        #region Private Members

        private int m_orderId = 0;
        private string m_accountNbr = "";
        private DateTime m_dateServiced = DateTime.Parse("1900-01-01");
        private string m_accessionNbr = "";
        private DateTime m_dateCollected = DateTime.Parse("1900-01-01");
        private string m_orderComment = "";
        private int m_patientId = 0;
        private string m_patientLastName = "";
        private string m_patientFirstName = "";
        private string m_patientMiddleName = "";
        private string m_patientDateOfBirth = "";
        private Gender m_patientGender = Gender.Unknown;
        private string m_patientStreetLine1 = "";
        private string m_patientStreetLine2 = "";
        private string m_patientCity = "";
        private string m_patientState = "";
        private string m_patientZipCode = "";
        private string m_patientHomePhoneNumber = "";
        private string m_patientWorkPhoneNumber = "";
        private string m_patientSocialSecurityNum = "";
        private int m_patientAgeNbr = 0;
        private string m_patientAgeType = "";

        private string m_physicianLastName = "";

        private List<ROPanel> m_panels = new List<ROPanel>();
        private List<ROAnalyte> m_analytes = new List<ROAnalyte>();
        private List<ROComment> m_comments = new List<ROComment>();

        #endregion

        #region Public Properties

        public string OrderComment
        {
            get
            {
                return m_orderComment;
            }
        }
        public ROPanel[] Panels
        {
            get
            {
                return m_panels.ToArray();
            }
        }
        public ROAnalyte[] Analytes
        {
            get
            {
                return m_analytes.ToArray();
            }
        }
        public ROComment[] Comments
        {
            get
            {
                return m_comments.ToArray();
            }
        }
        public int OrderId
        {
            get
            {
                return m_orderId;
            }
        }
        public string AccountNumber
        {
            get
            {
                return m_accountNbr;
            }
        }
        public DateTime DateServiced
        {
            get
            {
                return m_dateServiced;
            }
        }
        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }
        public DateTime DateCollected
        {
            get
            {
                return m_dateCollected;
            }
        }

        public int PatientId
        {
            get
            {
                return m_patientId;
            }
        }
        public string PatientLastName
        {
            get
            {
                return m_patientLastName;
            }
        }
        public string PatientFirstName
        {
            get
            {
                return m_patientFirstName;
            }
        }
        public string PatientMiddleName
        {
            get
            {
                return m_patientMiddleName;
            }
        }
        public int PatientAgeNbr
        {
            get
            {
                return m_patientAgeNbr;
            }
        }
        public string PatientAgeType
        {
            get
            {
                return m_patientAgeType;
            }
        }
        public string PatientDOB
        {
            get
            {
                return m_patientDateOfBirth;
            }
        }
        public Gender PatientGender
        {
            get
            {
                return m_patientGender;
            }
        }
        public string PatientStreetLine1
        {
            get
            {
                return m_patientStreetLine1;
            }
        }
        public string PatientStreetLine2
        {
            get
            {
                return m_patientStreetLine2;
            }
        }
        public string PatientCity
        {
            get
            {
                return m_patientCity;
            }
        }
        public string PatientState
        {
            get
            {
                return m_patientState;
            }
        }
        public string PatientZipCode
        {
            get
            {
                return m_patientZipCode;
            }
        }
        public string PatientHomePhoneNumber
        {
            get
            {
                return m_patientHomePhoneNumber;
            }
        }
        public string PatientWorkPhoneNumber
        {
            get
            {
                return m_patientWorkPhoneNumber;
            }
        }
        public string PatientSocialSecurityNum
        {
            get
            {
                return m_patientSocialSecurityNum;
            }
        }
        public string PhysicianLastName
        {
            get
            {
                return m_physicianLastName;
            }
        }

        #endregion

        #region Data Functions

        public static ROReport Fetch(string accessionNbr)
        {

            return (ROReport)DataFactory.Fetch(new Criteria(accessionNbr));

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DataTable[] dt;

            try
            {

                DbParameter @param;
                @param = (DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr);
                dt = da.ExecuteProcedure("lis_ROReport_Fetch", @param);

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt);
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        internal void Load(DataTable[] datatables)
        {

            {
                var withBlock = datatables[0].Rows[0];
                m_orderId = Conversions.ToInteger(withBlock["OrderId"]);
                m_accountNbr = Conversions.ToString(withBlock["AccountNumber"]);
                m_dateServiced = Conversions.ToDate(withBlock["DateServiced"]);
                m_accessionNbr = Conversions.ToString(withBlock["AccessionNbr"]);
                m_dateCollected = Conversions.ToDate(withBlock["DateCollected"]);
                m_patientId = Conversions.ToInteger(withBlock["PatientId"]);
                m_patientLastName = Conversions.ToString(withBlock["PatientLastName"]);
                m_patientFirstName = Conversions.ToString(withBlock["PatientFirstName"]);
                m_patientMiddleName = Conversions.ToString(withBlock["PatientMiddleName"]);
                m_patientDateOfBirth = Conversions.ToString(withBlock["PatientDOB"]);
                m_patientGender = (Gender)Conversions.ToInteger(withBlock["PatientGender"]);
                m_patientStreetLine1 = Conversions.ToString(withBlock["PatientStreetLine1"]);
                m_patientStreetLine2 = Conversions.ToString(withBlock["PatientStreetLine2"]);
                m_patientCity = Conversions.ToString(withBlock["PatientCity"]);
                m_patientState = Conversions.ToString(withBlock["PatientState"]);
                m_patientZipCode = Conversions.ToString(withBlock["PatientZipCode"]);
                m_patientHomePhoneNumber = Conversions.ToString(withBlock["PatientHomePhoneNumber"]);
                m_patientWorkPhoneNumber = Conversions.ToString(withBlock["PatientWorkPhoneNumber"]);
                m_patientSocialSecurityNum = Conversions.ToString(withBlock["PatientSSN"]);
                m_physicianLastName = Conversions.ToString(withBlock["PhysicianLastName"]);
                m_orderComment = Conversions.ToString(withBlock["OrderComment"]).NormalizeToWindows();
                m_patientAgeNbr = Conversions.ToInteger(withBlock["PatientAgeNbr"]);
                m_patientAgeType = Conversions.ToString(withBlock["PatientAgeType"]);
            }

            // Load up all the ReportAnalytePanels and ReportAnalytes
            int apID = 0; // AnalytePanelID
            ROPanel oap = null;
            ROAnalyte oa = null;
            {
                ref var withBlock1 = ref datatables[1];
                foreach (DataRow r in withBlock1.Rows)
                {
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["ReportAnalytePanelID"], 0, false)))
                    {
                        if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["ReportAnalytePanelID"], apID, false)))
                        {
                            oap = new ROPanel(this);
                            oap.Load(r);
                            m_panels.Add(oap);
                            apID = Conversions.ToInteger(r["ReportAnalytePanelID"]);
                        }
                        if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["ReportAnalyteId"], 0, false)))
                        {
                            oa = new ROAnalyte(oap);
                            oa.Load(r);
                            oap.m_analytes.Add(oa);
                        }
                    }
                    else
                    {
                        oa = new ROAnalyte(this);
                        oa.Load(r);
                        m_analytes.Add(oa);
                    }
                }
            }

            // Load up the Order Comments
            var ptype = orderParentType.Unknown;
            ROComment oc = null;
            {
                ref var withBlock2 = ref datatables[2];
                foreach (DataRow r in withBlock2.Rows)
                {
                    ptype = (orderParentType)Conversions.ToInteger(r["ParentType"]);
                    switch (ptype)
                    {
                        case orderParentType.Report:
                            {
                                oc = new ROComment();
                                oc.Load(r);
                                m_comments.Add(oc);
                                break;
                            }
                        case orderParentType.ReportAnalytePanel:
                            {
                                oc = new ROComment();
                                oc.Load(r);
                                FindPanel(Conversions.ToInteger(r["ReportAnalytePanelId"])).m_comments.Add(oc);
                                break;
                            }
                        case orderParentType.ReportAnalyte:
                            {
                                oc = new ROComment();
                                oc.Load(r);
                                ROAnalyte ra;
                                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(r["ReportAnalytePanelId"], 0, false)))
                                {
                                    ra = FindAnalyte(Conversions.ToLong(r["ReportAnalyteId"]));
                                }
                                else
                                {
                                    ra = FindPanel(Conversions.ToInteger(r["ReportAnalytePanelId"])).FindAnalyte(Conversions.ToLong(r["ReportAnalyteId"]));
                                }
                                ra.m_comments.Add(oc);
                                break;
                            }
                    }

                }
            }

        }

        internal ROAnalyte FindAnalyte(long analyteId)
        {
            foreach (ROAnalyte a in m_analytes)
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(a.AnalyteId, analyteId, false)))
                    return a;
            }
            return null;
        }

        internal ROPanel FindPanel(int panelId)
        {
            foreach (ROPanel p in m_panels)
            {
                if (p.PanelId == panelId)
                    return p;
            }
            return null;
        }

        #endregion

        #region Criteria Class

        [Serializable()]
        public class Criteria
        {

            private string m_accessionNbr = "";

            public Criteria(string accessionNbr)
            {
                m_accessionNbr = accessionNbr;
            }

            public string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }

        }

        #endregion

    }

    [Serializable()]
    public class ROPanel
    {

        internal ROPanel(object parent)
        {
            m_parent = parent;
        }

        #region Private Members

        internal object m_parent;

        private int m_panelId = 0;
        private transmitStatusType m_transmitStatus = transmitStatusType.None;
        private bool m_isReportable = true;
        private string m_panelCode = "";
        private string m_orderingPanelCode = "";
        private string m_panelName = "";
        private string m_category = "";
        internal List<ROAnalyte> m_analytes = new List<ROAnalyte>();
        internal List<ROComment> m_comments = new List<ROComment>();

        #endregion

        #region Public Properties

        public int PanelId
        {
            get
            {
                return m_panelId;
            }
        }
        public transmitStatusType TransmitStatus
        {
            get
            {
                return m_transmitStatus;
            }
        }
        public bool IsReportable
        {
            get
            {
                return m_isReportable;
            }
        }
        public string PanelCode
        {
            get
            {
                return m_panelCode;
            }
        }
        public string OrderingPanelCode
        {
            get
            {
                return m_orderingPanelCode;
            }
        }
        public string PanelName
        {
            get
            {
                return m_panelName;
            }
        }
        public string Category
        {
            get
            {
                return m_category;
            }
        }
        public ROAnalyte[] Analytes
        {
            get
            {
                return m_analytes.ToArray();
            }
        }
        public ROComment[] Comments
        {
            get
            {
                return m_comments.ToArray();
            }
        }
        #endregion

        #region Public Functions

        public DateTime GetLatestResultDate()
        {
            return default;
        }

        public resultStatusType GetStatus()
        {
            return default;
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {
            m_panelId = Conversions.ToInteger(row["ReportAnalytePanelId"]);
            m_transmitStatus = (transmitStatusType)Conversions.ToInteger(row["PanelTransmitStatus"]);
            m_isReportable = Conversions.ToBoolean(row["PanelIsReportable"]);
            m_panelCode = Conversions.ToString(row["PanelCode"]);
            m_orderingPanelCode = Conversions.ToString(row["OrderingPanelCode"]);
            m_panelName = Conversions.ToString(row["PanelName"]);
            m_category = Conversions.ToString(row["PanelCategory"]);
        }

        internal ROAnalyte FindAnalyte(long analyteId)
        {
            foreach (ROAnalyte a in m_analytes)
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(a.AnalyteId, analyteId, false)))
                    return a;
            }
            return null;
        }

        #endregion

    }

    [Serializable()]
    public class ROAnalyte
    {

        internal ROAnalyte(object parent)
        {
            m_parent = parent;
        }

        #region Private Members

        internal object m_parent;

        private long m_analyteId = 0L;
        private transmitStatusType m_transmitStatus = transmitStatusType.None;
        private bool m_isReportable = false;
        private string m_analyteCode = "";
        private string m_orderingAnalyteCode = "";
        private string m_analyteName = "";
        private DateTime m_resultDate;
        private string m_category = "";
        private resultStatusType m_resultStatus = resultStatusType.Pending;
        private Common.Lab.reportingType m_reportingType = Common.Lab.reportingType.Discrete;
        private string m_resultValue = "";
        private DateTime m_releaseDate;
        private resultType m_resultType = resultType.StringFormat;
        private string m_units = "";
        private int m_referenceLabId = 0;
        private string m_flagValue = "";
        private string m_referenceRange = "";

        internal List<ROComment> m_comments = new List<ROComment>();

        internal List<ComplexResultFlag> m_resultFlagRanges = new List<ComplexResultFlag>();

        #endregion

        #region Public Properties

        public resultStatusType ResultStatus
        {
            get
            {
                return m_resultStatus;
            }
        }
        public string Category
        {
            get
            {
                return m_category;
            }
        }
        public string FlagValue
        {
            get
            {
                return m_flagValue;
            }
        }
        public ROComment[] Comments
        {
            get
            {
                return m_comments.ToArray();
            }
        }

        public object AnalyteId
        {
            get
            {
                return m_analyteId;
            }
        }
        public transmitStatusType TransmitStatus
        {
            get
            {
                return m_transmitStatus;
            }
        }
        public bool IsReportable
        {
            get
            {
                return m_isReportable;
            }
        }
        public string AnalyteCode
        {
            get
            {
                return m_analyteCode;
            }
        }
        public string OrderingAnalyteCode
        {
            get
            {
                return m_orderingAnalyteCode;
            }
        }
        public string AnalyteName
        {
            get
            {
                return m_analyteName;
            }
        }
        public DateTime ResultDate
        {
            get
            {
                return m_resultDate;
            }
        }
        public Common.Lab.reportingType ReportingType
        {
            get
            {
                return m_reportingType;
            }
        }
        public string ResultValue
        {
            get
            {
                return m_resultValue;
            }
        }
        public DateTime ReleaseDate
        {
            get
            {
                return m_releaseDate;
            }
        }
        public resultType ResultType
        {
            get
            {
                return m_resultType;
            }
        }
        public string Units
        {
            get
            {
                return m_units;
            }
        }
        public int ReferenceLabId
        {
            get
            {
                return m_referenceLabId;
            }
        }
        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {
            m_analyteId = Conversions.ToLong(row["ReportAnalyteId"]);
            m_transmitStatus = (transmitStatusType)Conversions.ToInteger(row["AnalyteTransmitStatus"]);
            m_isReportable = Conversions.ToBoolean(row["AnalyteIsReportable"]);
            m_analyteCode = Conversions.ToString(row["AnalyteCode"]);
            m_orderingAnalyteCode = Conversions.ToString(row["OrderingAnalyteCode"]);
            m_analyteName = Conversions.ToString(row["AnalyteName"]);
            m_resultDate = Conversions.ToDate(row["ResultDate"]);
            m_category = Conversions.ToString(row["AnalyteCategory"]);
            m_resultStatus = (resultStatusType)Conversions.ToInteger(row["ResultStatus"]);
            m_reportingType = (Common.Lab.reportingType)Conversions.ToInteger(row["ReportingType"]);
            m_resultValue = Conversions.ToString(row["ResultValue"]);
            m_releaseDate = Conversions.ToDate(row["AnalyteReleaseDate"]);
            m_resultType = (resultType)Conversions.ToInteger(row["AnalyteResultType"]);
            m_units = Conversions.ToString(row["AnalyteUnits"]);
            m_referenceLabId = Conversions.ToInteger(row["ReferenceLabId"]);
            m_flagValue = Conversions.ToString(row["FlagValue"]);
            m_referenceRange = Conversions.ToString(row["ReferenceRange"]);

            // ComplexRanges
            System.IO.StringReader xmlstr = null;
            System.Xml.XPath.XPathDocument xmldoc = null;
            System.Xml.XPath.XPathNavigator xmlnav = null;
            if (!string.IsNullOrEmpty(row["FlagRangesXml"].ToString().Trim()))
            {
                ComplexResultFlag f;
                xmlstr = new System.IO.StringReader(Conversions.ToString(row["FlagRangesXml"]));
                xmldoc = new System.Xml.XPath.XPathDocument(xmlstr);
                xmlnav = xmldoc.CreateNavigator();

                var xmlRanges = xmlnav.Select("ResultFlagRanges/FlagRange");
                if (xmlRanges.Count > 0)
                {
                    while (xmlRanges.MoveNext())
                    {
                        f = new ComplexResultFlag(null, xmlRanges.Current.OuterXml);
                        m_resultFlagRanges.Add(f);
                    }
                }

            }
        }

        #endregion

        #region Public Functions

        /// <summary>
    /// Returns ReferenceRange Text based on complex ranges. If not found, uses default on reference Analyte.
    /// If ReferenceLabId is not 0, returns Range on analyte.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
        public string GetReferenceRange()
        {

            if (m_referenceLabId != 0)
                return m_referenceRange;

            ROReport r = null;
            if (ReferenceEquals(m_parent.GetType(), typeof(ROReport)))
            {
                r = (ROReport)m_parent;
            }
            else if (ReferenceEquals(m_parent.GetType(), typeof(ROPanel)))
            {
                r = (ROReport)((ROPanel)m_parent).m_parent;
            }

            if (!(r == null))
            {
                var f = SharedFunctions.GetMatchingResultFlag(m_resultFlagRanges.ToArray(), r.PatientGender, r.PatientDOB, r.PatientAgeNbr, r.PatientAgeType, r.DateServiced);

                if (!(f == null))
                {
                    return f.ReferenceRangeText;
                }
                else
                {
                    // If a match isn't found we return the Default ReferenceRange
                    foreach (ComplexResultFlag rf in m_resultFlagRanges)
                    {
                        if (rf.IsDefault)
                            return rf.ReferenceRangeText;
                    }
                }
            }

            // If Default not found, return Reference Range on analyte.
            return m_referenceRange;

        }

        #endregion

    }

    [Serializable()]
    public class ROComment
    {

        #region Private Members

        private int m_commentId = 0;
        private string m_text = "";
        private string m_externalCommentCode = "";
        private ExternalCommentType m_externalCommentType = ExternalCommentType.Comment;

        #endregion

        #region Public Properties

        public int CommentId
        {
            get
            {
                return m_commentId;
            }
        }
        public string Text
        {
            get
            {
                return m_text;
            }
        }
        public string ExtCommentCode
        {
            get
            {
                return m_externalCommentCode;
            }
        }
        public ExternalCommentType ExtCommentType
        {
            get
            {
                return m_externalCommentType;
            }
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {
            m_commentId = Conversions.ToInteger(row["ReportCommentId"]);
            m_text = Conversions.ToString(row["CommentText"]).NormalizeToWindows();
            m_externalCommentCode = Conversions.ToString(row["ExtCommentCode"]);
            m_externalCommentType = (ExternalCommentType)Conversions.ToInteger(row["ExtCommentType"]);
        }

        #endregion

    }
}