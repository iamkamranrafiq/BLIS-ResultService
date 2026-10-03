using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Xml;
using Bioreference.Common.Lab;
using Bioreference.Common.TestMaster;
using Bioreference.Data.Client;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace Bioreference.LIS
{

    [Serializable()]
    public class TestAnalyte : Analyte
    {

        #region Private Member
        protected long m_id = 0L;
        protected bool m_autoRelease = false;
        protected int m_precision = -1; // '-1 means that we are not using precision.
        protected string m_refLabOrderingCode = "";
        protected string m_altInboundTestCode = "";
        protected string m_altOutboundTestCode = "";
        protected string m_altOutboundTestDesc = "";
        private TestLookup m_refTest;

        // '''''''''''''' Modified to allow log ''''''''''''
        protected string m_original_ref_range;
        protected string m_original_units;
        // '''''''''''''''''''''''''''''''''''''''''''''''''
        protected string m_original_flag;
        protected string m_previous_ref_range;
        protected string m_previous_units;
        protected string m_previous_flag;
        protected bool m_isInterfaced;
        private bool m_unitsUpdated = false;
        private bool m_rangeUpdated = false;
        private bool m_flagUpdated = false;
        protected bool m_isFlagDeleted = false;
        protected bool _allowPreliminaryRelease = false;
        protected bool _allowInternalRefRanges = false;
        protected bool _isPOC = false;
        protected bool _isDoubleEntry = false;
        protected string m_deptShortName = string.Empty;
        protected List<TestInstrumentDTO> m_testInstruments = new List<TestInstrumentDTO>();
        private readonly ILog Log = LogManager.GetLogger<TestAnalyte>();

        // NOTE: m_parent exists in the base object and SHOULD NOT
        // be used in this object.
        #endregion

        #region Public Properties

        public long Id
        {
            get
            {
                return m_id;
            }
        }

        public bool AutoRelease
        {
            get
            {
                return m_autoRelease;
            }
        }

        public int Precision
        {
            get
            {
                return m_precision;
            }
        }

        public string ReferenceLabOrderingAnalyteCode
        {
            get
            {
                return Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(m_refLabOrderingCode), m_refLabCode, m_refLabOrderingCode));
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

        public string PreviousTestRangeValue
        {
            get
            {
                return m_previous_ref_range;
            }
        }

        public string PreviousUnitValue
        {
            get
            {
                return m_previous_units;
            }
        }

        public string PreviousFlagValue
        {
            get
            {
                return m_previous_flag;
            }
        }

        public string OriginalTestRangeValue
        {
            get
            {
                return m_original_ref_range;
            }
        }

        public string OriginalUnitValue
        {
            get
            {
                return m_original_units;
            }
        }

        public string OriginalFlagValue
        {
            get
            {
                return m_original_flag;
            }
        }

        public bool RangeUpdated
        {
            get
            {
                return m_rangeUpdated;
            }
            set
            {
                m_rangeUpdated = value;
            }
        }

        public bool UnitsUpdated
        {
            get
            {
                return m_unitsUpdated;
            }
            set
            {
                m_unitsUpdated = value;
            }
        }

        public bool FlagUpdated
        {
            get
            {
                return m_flagUpdated;
            }
            set
            {
                m_flagUpdated = value;
            }
        }

        public bool IsFlagDeleted
        {
            get
            {
                return m_isFlagDeleted;
            }
            set
            {
                m_isFlagDeleted = value;
                FlagDirty();
            }
        }

        public bool AllowPreliminaryRelease
        {
            get
            {
                return _allowPreliminaryRelease;
            }
        }

        public bool AllowInternalRefRanges
        {
            get
            {
                return _allowInternalRefRanges;
            }
        }

        public bool IsPOC
        {
            get
            {
                return _isPOC;
            }
        }

        public bool IsDoubleEntry
        {
            get
            {
                return _isDoubleEntry;
            }
        }

        public List<TestInstrumentDTO> TestInstruments
        {
            get
            {
                return m_testInstruments;
            }
        }

        public bool IsInterfaced
        {
            get
            {
                return m_isInterfaced;
            }
        }

        public string DepartmentShortname
        {
            get
            {
                return m_deptShortName;
            }
        }

        #endregion

        #region Constructor
        internal TestAnalyte()
        {
        }

        // Public Sub New(ByVal test As Bioreference.Common.TestMaster.TestInfo)
        // Me.Load(test)
        // End Sub

        public TestAnalyte(Common.TestMaster.Test test)
        {
            Load(test);
        }

        #endregion

        #region Public Functions

        public bool UpdateFlagValue(string value)
        {
            if ((value.Trim() ?? "") != (m_flagValue ?? "") && (value.Trim() ?? "") != (m_original_flag ?? ""))
            {
                m_flagValue = value;
                FlagDirty();
                return true;
            }
            return false;
        }
        public void UpdateAnalyteName(string value)
        {
            if ((value.Trim() ?? "") != (m_name ?? ""))
            {
                m_name = value;
                FlagDirty();
            }
        }
        public bool UpdateUnits(string value)
        {
            if ((value.Trim() ?? "") != (m_units ?? "") && (value.Trim() ?? "") != (m_original_units ?? ""))
            {
                m_units = value;
                FlagDirty();
                return true;
            }
            return false;
        }
        public bool UpdateReferenceRange(string value)
        {
            if ((value.Trim() ?? "") != (m_referenceRange ?? "") && (value.Trim() ?? "") != (m_original_ref_range ?? ""))
            {
                m_referenceRange = value; // assign value
                FlagDirty();
                return true;
            }
            return false;
        }

        #endregion

        #region Data Functions

        internal void Load(TestInfo test)
        {


            // ' m_flagValue = .FlagValue
            m_code = test.Code;
            m_name = test.Name;
            // 'm_resultType = .ResultType
            m_units = test.Units;
            m_category = test.Category;
            m_refLabId = test.ReferenceLabId;
            m_refLabCode = test.ReferenceLabeAnalyteCode;
            m_displayByDefault = test.IsRequired;
            m_isReportable = test.IsReportable;
            m_outboundChannelId = test.OutboundChannelId;
            m_allowUpdates = test.AllowUpdates;
            m_altInboundTestCode = test.AltInboundTestCode;
            m_altOutboundTestCode = test.AltOutboundTestCode;
            m_altOutboundTestDesc = test.AltOutBoundTestDescr;
            _allowPreliminaryRelease = test.AllowPreliminaryRelease;
            _allowInternalRefRanges = test.AllowInternalRefRanges;
            _isPOC = test.IsPOC;
            _isDoubleEntry = test.IsDoubleEntry;
            foreach (TestInstrument ti in test.Instruments)
            {

                var m = new TestInstrumentDTO();
                m.Deleted = ti.Deleted;
                m.Location = ti.Location;
                m.InstrumentCode = ti.InstrumentCode;
                m.Department = ti.Department;
                m.DI = ti.DI;
                m.VendorName = ti.VendorName;
                m.VendorId = ti.VendorId;
                m.TestInstrumentId = ti.TestInstrumentId;
                m.TestCode = ti.TestCode;
                m.InstrumentName = ti.InstrumentName;
                m.InstrumentId = ti.InstrumentId;
                m.Id = ti.Id;
                m_testInstruments.Add(m);

            }
            m_isInterfaced = test.IsInterfaced;

            // 'REMOVED - UNTIL TESTMASTER IS UPGRADED
            m_refLabOrderingCode = test.ReferenceLabOrderingAnalyteCode;

            if (test.IsCalculation)
                m_calc.SetCalculation(test.Calculation.Expression);

            m_isAgencyReportable = test.IsAgencyReportable;

            switch (test.ReportingType)
            {
                case Common.TestMaster.reportingType.Discrete:
                    {
                        m_reportingType = Common.Lab.reportingType.Discrete;
                        break;
                    }
                case Common.TestMaster.reportingType.Tabular_Text_Without_Headers:
                    {
                        m_reportingType = Common.Lab.reportingType.TabularTextWithOutHeaders;
                        break;
                    }
                case Common.TestMaster.reportingType.TabularText:
                    {
                        m_reportingType = Common.Lab.reportingType.TabularText;
                        break;
                    }
                case Common.TestMaster.reportingType.Terse:
                    {
                        m_reportingType = Common.Lab.reportingType.Terse;
                        break;
                    }
            }
            // m_reportingType = .ReportingType

            m_attachCommentToParent = test.AttachCommentToParent;
            m_autoRelease = test.AutoRelease;
            m_precision = test.Precision;
            m_deptShortName = test.DepartmentShortName;

            foreach (TestComment tc in test.Comments)
            {

                Common.Lab.Comment c = new DefaultComment("");
                c.Type = (commentType)tc.CommentType;
                c.Text = tc.CommentText;
                c.AssignedID = tc.AssignedId;
                c.IsRepeatable = tc.IsRepeatable;
                c.ExternalCommentCode = tc.ExternalCommentCode;
                c.ExternalCommentType = (int)tc.ExternalCommentType;
                m_comments.Add(c);

            }

            foreach (TestComplexResult complexResult in test.ComplexResults)
            {

                var r = new ComplexResultFlag(this, "");

                r.Name = complexResult.Name;
                r.IsDefault = complexResult.IsDefault;
                r.Gender = (genderFlagType)complexResult.Gender;
                r.UseAgeQualifier = complexResult.UseAgeQualifier;
                r.AgeFrom = complexResult.AgeFrom;
                r.AgeTo = complexResult.AgeTo;
                r.AgeType = Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(complexResult.AgeType), "Y", complexResult.AgeType));
                r.ReferenceRangeText = complexResult.ReferenceRangeText;
                r.UseRanges = complexResult.UseRanges;
                r.UseValues = complexResult.UseValues;

                r.DivisionCode = OrderManager.GetDivisionCode(complexResult.DivisionID);
                // TM needs to be deployed to PROD before this can be activated, replace line above
                // r.DivisionCode = IIf(Not String.IsNullOrEmpty(complexResult.DivisionCode), complexResult.DivisionCode, OrderManager.GetDivisionCode(complexResult.DivisionID))

                ComplexValue val;
                foreach (ComplexResultValue rv in complexResult.Values)
                {
                    val = r.Values.AddValue();
                    val.Flag = rv.Flag;
                    val.IsDropdown = Conversions.ToBoolean(rv.IsDropDown);
                    val.OrderIndex = rv.Sequence;
                    val.Value = rv.ValueText;
                    foreach (TestComment c in rv.Comments)
                        val.DefaultComments.Add(c.AssignedId, c.CommentText, (commentType)c.CommentType, c.IsRepeatable, c.ExternalCommentCode, (int)c.ExternalCommentType);
                }

                ComplexRange range;
                foreach (ComplexResultRange rr in complexResult.Ranges)
                {
                    range = r.Ranges.AddRange();
                    range.FlagValue = rr.Flag;
                    range.RangeFrom = rr.RangeFrom;
                    range.RangeTo = rr.RangeTo;
                    foreach (TestComment c in rr.Comments)
                        range.DefaultComments.Add(c.AssignedId, c.CommentText, (commentType)c.CommentType, c.IsRepeatable, c.ExternalCommentCode, (int)c.ExternalCommentType);
                }

                foreach (TestComment tc in complexResult.Comments)
                {

                    Common.Lab.Comment c = new DefaultComment("");
                    c.Type = (commentType)tc.CommentType;
                    c.Text = tc.CommentText;
                    c.AssignedID = tc.AssignedId;
                    c.IsRepeatable = tc.IsRepeatable;
                    c.ExternalCommentCode = tc.ExternalCommentCode;
                    c.ExternalCommentType = (int)tc.ExternalCommentType;
                    r.DefaultComments.Add(c);

                }

                m_resultFlagRanges.Add(r);

            }
            SortResultFlagRanges();

            foreach (TestMapping tm in test.TestMappings)
            {

                var m = new TestCodeMapping();
                m.OriginalTestCode = test.Code;
                m.AlternateTestCode = m.AlternateTestCode;
                m.Description = m.Description;

                m_testCodeMappingList.Add(m);

            }


            m_refTest = new TestLookup(m_code, m_refLabId, m_refLabCode, test.IsProfile, test.IsPanel);

            FlagDirty();

        }

        internal void Load(Common.TestMaster.Test test)
        {

            // ' m_flagValue = .FlagValue
            m_code = test.TestCode;
            m_name = test.ShortName;
            m_units = test.Units;
            m_category = test.SpecialtyDescr;
            m_refLabId = test.RefLab;
            m_refLabCode = test.RefLabTest; // .ReferenceLabeAnalyteCode
            m_displayByDefault = test.DisplayByDefault; // .IsRequired
            m_isReportable = test.IsReportable;
            m_outboundChannelId = test.OutboundChannelId;
            m_allowUpdates = test.AllowUpdates;
            m_altInboundTestCode = test.AlternateInboundTestcode; // .AltInboundTestCode
            m_altOutboundTestCode = test.AlternateOutboundTestcode; // .AltOutboundTestCode
            m_altOutboundTestDesc = test.AlternateOutboundDescription; // .AltOutBoundTestDescr
            _allowPreliminaryRelease = test.AllowPreliminary;
            _allowInternalRefRanges = test.AllowInternalRefRanges;
            _isPOC = test.IsPOC;
            _isDoubleEntry = test.IsDoubleEntry;
            // 'REMOVED - UNTIL TESTMASTER IS UPGRADED
            m_refLabOrderingCode = test.RefLabOrderCode; // .ReferenceLabOrderingAnalyteCode

            // 'IsCalculation is returning false even though there is a calc
            // 'If .IsCalculation Then m_calc.SetCalculation(.Calculation) '(.Calculation.Expression)
            m_calc.SetCalculation(test.Calculation);

            m_isAgencyReportable = test.IsAgencyReportable;

            switch (test.ReportingType)
            {
                case Common.TestMaster.reportingType.Discrete:
                    {
                        m_reportingType = Common.Lab.reportingType.Discrete;
                        break;
                    }
                case Common.TestMaster.reportingType.Tabular_Text_Without_Headers:
                    {
                        m_reportingType = Common.Lab.reportingType.TabularTextWithOutHeaders;
                        break;
                    }
                case Common.TestMaster.reportingType.TabularText:
                    {
                        m_reportingType = Common.Lab.reportingType.TabularText;
                        break;
                    }
                case Common.TestMaster.reportingType.Terse:
                    {
                        m_reportingType = Common.Lab.reportingType.Terse;
                        break;
                    }
            }

            m_attachCommentToParent = test.AttachCommentToParent;
            m_autoRelease = test.AutoRelease;
            m_precision = test.Precision;
            m_deptShortName = test.DepartmentShortName;

            foreach (TestComment tc in test.Comments)
            {

                Common.Lab.Comment c = new DefaultComment("");
                c.Type = (commentType)tc.CommentType;
                c.Text = tc.CommentText;
                c.AssignedID = tc.AssignedId;
                c.IsRepeatable = tc.IsRepeatable;
                c.ExternalCommentCode = tc.ExternalCommentCode;
                c.ExternalCommentType = (int)tc.ExternalCommentType;
                m_comments.Add(c);

            }

            foreach (TestComplexResult complexResult in test.ComplexResults)
            {

                var r = new ComplexResultFlag(this, "");

                r.Name = complexResult.Name;
                r.IsDefault = complexResult.IsDefault;
                r.Gender = (genderFlagType)complexResult.Gender;
                r.UseAgeQualifier = complexResult.UseAgeQualifier;
                r.AgeFrom = complexResult.AgeFrom;
                r.AgeTo = complexResult.AgeTo;
                r.AgeType = Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(complexResult.AgeType), "Y", complexResult.AgeType));
                r.ReferenceRangeText = complexResult.ReferenceRangeText;
                r.UseRanges = complexResult.UseRanges;
                r.UseValues = complexResult.UseValues;
                r.DivisionCode = OrderManager.GetDivisionCode(complexResult.DivisionID);

                ComplexValue val;
                foreach (ComplexResultValue rv in complexResult.Values)
                {
                    val = r.Values.AddValue();
                    val.Flag = rv.Flag;
                    val.IsDropdown = Conversions.ToBoolean(rv.IsDropDown);
                    val.OrderIndex = rv.Sequence;
                    val.Value = rv.ValueText;
                    foreach (TestComment c in rv.Comments)
                        val.DefaultComments.Add(c.AssignedId, c.CommentText, (commentType)c.CommentType, c.IsRepeatable, c.ExternalCommentCode, (int)c.ExternalCommentType);
                }

                ComplexRange range;
                foreach (ComplexResultRange rr in complexResult.Ranges)
                {
                    range = r.Ranges.AddRange();
                    range.FlagValue = rr.Flag;
                    range.RangeFrom = rr.RangeFrom;
                    range.RangeTo = rr.RangeTo;
                    foreach (TestComment c in rr.Comments)
                        range.DefaultComments.Add(c.AssignedId, c.CommentText, (commentType)c.CommentType, c.IsRepeatable, c.ExternalCommentCode, (int)c.ExternalCommentType);
                }

                foreach (TestComment tc in complexResult.Comments)
                {

                    Common.Lab.Comment c = new DefaultComment("");
                    c.Type = (commentType)tc.CommentType;
                    c.Text = tc.CommentText;
                    c.AssignedID = tc.AssignedId;
                    c.IsRepeatable = tc.IsRepeatable;
                    c.ExternalCommentCode = tc.ExternalCommentCode;
                    c.ExternalCommentType = (int)tc.ExternalCommentType;
                    r.DefaultComments.Add(c);

                }

                m_resultFlagRanges.Add(r);

            }
            SortResultFlagRanges();

            foreach (TestMapping tm in test.Mappings)
            {

                var m = new TestCodeMapping();
                m.OriginalTestCode = test.TestCode;
                m.AlternateTestCode = tm.AltCode;
                m.Description = m.Description;
                m_testCodeMappingList.Add(m);

            }

            // For Each c As TestComponent In .Components
            // m_refTest.ComponentCodes.Add(c.ComponentCode)
            // Next

            m_refTest = new TestLookup(m_code, m_refLabId, m_refLabCode, test.IsProfile, test.IsPanel);

            FlagDirty();

        }

        internal void Load(DataRow row)
        {




            m_id = Conversions.ToLong(row["RefAnalyteId"]);
            m_flagValue = Conversions.ToString(row["RefAnalyteFlagValue"]);
            m_code = Conversions.ToString(row["RefAnalyteCode"]);
            m_name = Conversions.ToString(row["RefAnalyteName"]);
            m_resultType = (resultType)Conversions.ToInteger(row["RefAnalyteResultType"]);
            m_units = Conversions.ToString(row["RefAnalyteUnits"]);
            m_category = row["RefAnalyteCategory"].ToString().Trim();
            m_referenceRange = Conversions.ToString(row["RefAnalyteRefRange"]);
            m_refLabId = Conversions.ToInteger(row["ReferenceLabId"]);
            m_refLabCode = Conversions.ToString(row["ReferenceLabAnalyteCode"]);
            m_displayByDefault = Conversions.ToBoolean(row["DisplayByDefault"]);
            m_isReportable = Conversions.ToBoolean(row["IsAnalyteReportable"]);
            SetCalculation(Conversions.ToString(row["Calculation"]));
            m_outboundChannelId = Conversions.ToInteger(row["OutboundChannelId"]);
            m_allowUpdates = Conversions.ToBoolean(row["AllowUpdates"]);
            m_isAgencyReportable = Conversions.ToBoolean(row["IsAnalyteAgencyReportable"]);
            m_reportingType = (Common.Lab.reportingType)Conversions.ToInteger(row["ReportingType"]);
            m_attachCommentToParent = Conversions.ToBoolean(row["AttachCommentToParent"]);
            m_autoRelease = Conversions.ToBoolean(row["AutoRelease"]);
            m_precision = Conversions.ToInteger(row["Precision"]);
            m_refLabOrderingCode = Conversions.ToString(row["RefLabOrderingAnalyteCode"]);
            m_altInboundTestCode = Conversions.ToString(row["AnalyteAltInboundTestCode"]);
            m_altOutboundTestCode = Conversions.ToString(row["AnalyteAltOutboundTestCode"]);
            m_altOutboundTestDesc = Conversions.ToString(row["AnalyteAltOutboundTestDesc"]);
            m_deptShortName = Conversions.ToString(row["DepartmentShortName"]);

            if (row.Table.Columns.Contains("AllowPreliminaryRelease"))
            {
                _allowPreliminaryRelease = Conversions.ToBoolean(row["AllowPreliminaryRelease"]);
            }

            if (row.Table.Columns.Contains("AllowInternalRefRanges"))
            {
                _allowInternalRefRanges = Conversions.ToBoolean(row["AllowInternalRefRanges"]);
            }

            // Load Originals
            // '''''''''''''' Modified to allow log '''''''''''
            m_original_ref_range = m_referenceRange;
            m_original_units = m_units;
            // ''''''''''''''''''''''''''''''''''''''''''''''''

            m_original_flag = m_flagValue;
            // Initialize previous from db values
            m_previous_ref_range = Conversions.ToString(row["RefAnalytePreviousRefRange"]);
            m_previous_units = Conversions.ToString(row["RefAnalytePreviousUnits"]);
            m_previous_flag = Conversions.ToString(row["RefAnalytePreviousFlag"]);
            if (Operators.ConditionalCompareObjectNotEqual(row["TestInstrumentsJson"], "", false))
            {
                m_testInstruments = JsonConvert.DeserializeObject<List<TestInstrumentDTO>>(Conversions.ToString(row["TestInstrumentsJson"]));
            }

            m_isInterfaced = Conversions.ToBoolean(row["IsInterfaced"]);

            if (((m_previous_ref_range ?? "") != (m_original_ref_range ?? "") && !string.IsNullOrEmpty(m_previous_ref_range)) | (string.IsNullOrEmpty(m_previous_ref_range) && !string.IsNullOrEmpty(m_referenceRange)))
            {

                m_rangeUpdated = true;
            }
            if ((m_previous_units ?? "") != (m_original_units ?? "") && !string.IsNullOrEmpty(m_previous_units))
            {
                m_unitsUpdated = true;
            }

            if ((m_previous_flag ?? "") != (m_original_flag ?? "") && !string.IsNullOrEmpty(m_previous_flag))
            {
                m_flagUpdated = true;
            }


            m_isFlagDeleted = Conversions.ToBoolean(row["IsFlagDeleted"]);

            System.IO.StringReader xmlstr;
            System.Xml.XPath.XPathDocument xmldoc;
            System.Xml.XPath.XPathNavigator xmlnav;

            // Criticals
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(row["CriticalsXml"], "", false)))
            {
                xmlstr = new System.IO.StringReader(Conversions.ToString(row["CriticalsXml"]));
                xmldoc = new System.Xml.XPath.XPathDocument(xmlstr);
                xmlnav = xmldoc.CreateNavigator();

                var xmlCriticals = xmlnav.Select("Criticals/Critical");
                if (xmlCriticals.Count > 0)
                {
                    while (xmlCriticals.MoveNext())
                        AddCritical(new Critical(xmlCriticals.Current.Value, (criticalType)Conversions.ToInteger(xmlCriticals.Current.GetAttribute("Type", ""))));
                }
            }


            // Results
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(row["ResultValuesXml"], "", false)))
            {
                Result r;
                xmlstr = new System.IO.StringReader(Conversions.ToString(row["ResultValuesXml"]));
                xmldoc = new System.Xml.XPath.XPathDocument(xmlstr);
                xmlnav = xmldoc.CreateNavigator();

                var xmlResults = xmlnav.Select("ResultValues/ResultValue");
                if (xmlResults.Count > 0)
                {
                    while (xmlResults.MoveNext())
                    {
                        r = new Result(xmlResults.Current.Value, xmlResults.Current.GetAttribute("FlagValue", ""), Conversions.ToInteger(xmlResults.Current.GetAttribute("Index", "")));
                        AddResult(r);
                    }
                }
            }


            // ComplexRanges
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
                        f = new ComplexResultFlag(this, xmlRanges.Current.OuterXml);
                        if (string.IsNullOrEmpty(f.DivisionCode))
                        {
                            f.DivisionCode = OrderManager.DefaultDivisionCodes[0];
                            f.FlagClean();
                        }

                        AddFlagRange(f);
                    }
                }
            }
            SortResultFlagRanges();


            // Comments
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(row["CommentsXml"], "", false)))
            {
                xmlstr = new System.IO.StringReader(Conversions.ToString(row["CommentsXml"]));
                xmldoc = new System.Xml.XPath.XPathDocument(xmlstr);
                xmlnav = xmldoc.CreateNavigator();

                Common.Lab.Comment c;
                var xmlComments = xmlnav.Select("Comments/Comment");
                if (xmlComments.Count > 0)
                {
                    while (xmlComments.MoveNext())
                    {
                        c = new DefaultComment(xmlComments.Current.OuterXml);
                        LoadComment((DefaultComment)c);
                    }
                }
            }

            // TestCodeMapping
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(row["TestCodeMappingsXml"], "", false)))
            {
                xmlstr = new System.IO.StringReader(Conversions.ToString(row["TestCodeMappingsXml"]));
                xmldoc = new System.Xml.XPath.XPathDocument(xmlstr);
                xmlnav = xmldoc.CreateNavigator();

                var xmlMappings = xmlnav.Select("Mappings/Mapping");
                if (xmlMappings.Count > 0)
                {
                    while (xmlMappings.MoveNext())
                        AddTestCodeMapping(xmlMappings.Current.GetAttribute("AltCode", ""));
                }


            }

            FlagClean();

        }

        internal virtual void Update()
        {
            UpdateCore();
        }

        internal void UpdateCore()
        {

            try
            {
                // **************************************************************
                var xmldoc = new XmlDocument();
                var root = xmldoc.CreateElement("Criticals");
                xmldoc.AppendChild(root);

                foreach (Critical c in Criticals)
                {
                    var eCritical = xmldoc.CreateElement("Critical");
                    eCritical.InnerText = c.Value;
                    var attrType = xmldoc.CreateAttribute("Type");
                    attrType.Value = ((int)c.Type).ToString();
                    eCritical.Attributes.Append(attrType);
                    root.AppendChild(eCritical);
                }

                string xmlCriticals = xmldoc.InnerXml;
                // *************************************************************

                xmldoc = new XmlDocument();
                root = xmldoc.CreateElement("ResultValues");
                xmldoc.AppendChild(root);

                foreach (Result r in ResultValues)
                {
                    var eResult = xmldoc.CreateElement("ResultValue");
                    eResult.InnerText = r.Value;
                    var attrFlag = xmldoc.CreateAttribute("FlagValue");
                    attrFlag.Value = r.FlagValue;
                    eResult.Attributes.Append(attrFlag);
                    var attrIdx = xmldoc.CreateAttribute("Index");
                    attrIdx.Value = r.OrderIndex.ToString();
                    eResult.Attributes.Append(attrIdx);
                    root.AppendChild(eResult);
                }

                string xmlResultValues = xmldoc.InnerXml;

                // *************************************************************

                xmldoc = new XmlDocument();
                root = xmldoc.CreateElement("ResultFlagRanges");
                xmldoc.AppendChild(root);

                foreach (ComplexResultFlag f in ResultFlagRanges)
                {
                    var eResult = xmldoc.CreateElement("FlagRange");
                    var attrFlag = xmldoc.CreateAttribute("RefRangeText");
                    attrFlag.Value = f.ReferenceRangeText;
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("Gender");
                    attrFlag.Value = ((int)f.Gender).ToString();
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("Name");
                    attrFlag.Value = f.Name;
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("UseAgeQualifier");
                    attrFlag.Value = Conversions.ToString(f.UseAgeQualifier);
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("AgeFrom");
                    attrFlag.Value = f.AgeFrom.ToString();
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("AgeTo");
                    attrFlag.Value = f.AgeTo.ToString();
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("AgeType");
                    attrFlag.Value = f.AgeType;
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("UseRanges");
                    attrFlag.Value = Conversions.ToString(f.UseRanges);
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("UseValues");
                    attrFlag.Value = Conversions.ToString(f.UseValues);
                    eResult.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("IsDefault");
                    attrFlag.Value = Conversions.ToString(f.IsDefault);
                    eResult.Attributes.Append(attrFlag);
                    // DivisionCode
                    attrFlag = xmldoc.CreateAttribute("DivisionCode");
                    attrFlag.Value = f.DivisionCode;
                    eResult.Attributes.Append(attrFlag);

                    // AG - Comments
                    eResult.AppendChild(xmldoc.ImportNode(f.DefaultComments.ToXmlNode(), true));

                    f.Ranges.AddToNode(xmldoc, eResult);
                    f.Values.AddToNode(xmldoc, eResult);

                    root.AppendChild(eResult);
                }

                string xmlFlagRanges = xmldoc.InnerXml;

                // *************************************************************



                // *************************************************************
                // TestCode Mappings
                xmldoc = new XmlDocument();
                root = xmldoc.CreateElement("Mappings");
                xmldoc.AppendChild(root);

                foreach (TestCodeMapping m in TestCodeAliasList)
                {
                    var eMapping = xmldoc.CreateElement("Mapping");
                    var attrFlag = xmldoc.CreateAttribute("AltCode");
                    attrFlag.Value = m.AlternateTestCode;
                    eMapping.Attributes.Append(attrFlag);
                    attrFlag = xmldoc.CreateAttribute("OrigCode");
                    attrFlag.Value = m.OriginalTestCode;
                    eMapping.Attributes.Append(attrFlag);

                    root.AppendChild(eMapping);
                }

                string xmlMappings = xmldoc.InnerXml;
                // **************************************************************

                var da = new DataWrapper(Configuration.ConnectionString);
                var paramList = new List<DbParameter>();

                paramList.Add((DbParameter)da.CreateParameter("@RefAnalyteId", DbType.Int32, m_id, ParameterDirection.InputOutput));
                paramList.Add((DbParameter)da.CreateParameter("@Code", DbType.String, m_code));
                paramList.Add((DbParameter)da.CreateParameter("@FlagValue", DbType.String, m_flagValue));
                paramList.Add((DbParameter)da.CreateParameter("@Name", DbType.String, m_name));
                paramList.Add((DbParameter)da.CreateParameter("@RefRange", DbType.String, m_referenceRange));
                paramList.Add((DbParameter)da.CreateParameter("@ResultType", DbType.Int32, m_resultType));
                paramList.Add((DbParameter)da.CreateParameter("@Units", DbType.String, m_units));
                paramList.Add((DbParameter)da.CreateParameter("@Category", DbType.String, m_category));
                paramList.Add((DbParameter)da.CreateParameter("@ReferenceLabID", DbType.Int32, m_refLabId));
                paramList.Add((DbParameter)da.CreateParameter("@ReferenceLabAnalyteCode", DbType.String, m_refLabCode));
                paramList.Add((DbParameter)da.CreateParameter("@DisplayByDefault", DbType.Boolean, m_displayByDefault));
                paramList.Add((DbParameter)da.CreateParameter("@IsReportable", DbType.Boolean, m_isReportable));
                paramList.Add((DbParameter)da.CreateParameter("@OutboundChannelId", DbType.Int32, m_outboundChannelId));
                paramList.Add((DbParameter)da.CreateParameter("@AllowUpdates", DbType.Boolean, m_allowUpdates));
                if (!(m_calc == null))
                    paramList.Add((DbParameter)da.CreateParameter("@Calculation", DbType.String, m_calc.Expression));

                paramList.Add((DbParameter)da.CreateParameter("@CriticalsXml", DbType.String, xmlCriticals));
                paramList.Add((DbParameter)da.CreateParameter("@ResultValuesXml", DbType.String, xmlResultValues));
                paramList.Add((DbParameter)da.CreateParameter("@CommentsXml", DbType.String, m_comments.ToXmlString()));
                paramList.Add((DbParameter)da.CreateParameter("@FlagRangesXml", DbType.String, xmlFlagRanges));
                paramList.Add((DbParameter)da.CreateParameter("@TestCodeMappingsXml", DbType.String, xmlMappings));
                paramList.Add((DbParameter)da.CreateParameter("@IsAgencyReportable", DbType.Boolean, m_isAgencyReportable));
                paramList.Add((DbParameter)da.CreateParameter("@ReportingType", DbType.Int32, m_reportingType));
                paramList.Add((DbParameter)da.CreateParameter("@AttachCommentToParent", DbType.Boolean, m_attachCommentToParent));
                paramList.Add((DbParameter)da.CreateParameter("@AutoRelease", DbType.Boolean, m_autoRelease));
                paramList.Add((DbParameter)da.CreateParameter("@Precision", DbType.Int32, m_precision));
                paramList.Add((DbParameter)da.CreateParameter("@ReferenceLabOrderingCode", DbType.String, m_refLabOrderingCode));

                paramList.Add((DbParameter)da.CreateParameter("@AltInboundTestCode", DbType.String, m_altInboundTestCode));
                paramList.Add((DbParameter)da.CreateParameter("@AltOutboundTestCode", DbType.String, m_altOutboundTestCode));
                paramList.Add((DbParameter)da.CreateParameter("@AltOutboundTestDesc", DbType.String, m_altOutboundTestDesc));

                paramList.Add((DbParameter)da.CreateParameter("@AllowPreliminaryRelease", DbType.Boolean, _allowPreliminaryRelease));
                paramList.Add((DbParameter)da.CreateParameter("@AllowInternalRefRanges", DbType.Boolean, _allowInternalRefRanges));

                if ((m_original_ref_range ?? "") != (ReferenceRange ?? ""))
                {
                    paramList.Add((DbParameter)da.CreateParameter("@PreviousRefRange", DbType.String, m_original_ref_range));
                }
                else
                {
                    paramList.Add((DbParameter)da.CreateParameter("@PreviousRefRange", DbType.String, m_previous_ref_range));
                }
                if ((m_original_units ?? "") != (Units ?? ""))
                {
                    paramList.Add((DbParameter)da.CreateParameter("@PreviousUnits", DbType.String, m_original_units));
                }
                else
                {
                    paramList.Add((DbParameter)da.CreateParameter("@PreviousUnits", DbType.String, m_previous_ref_range));
                }
                if ((m_original_flag ?? "") != (FlagValue ?? ""))
                {
                    paramList.Add((DbParameter)da.CreateParameter("@PreviousFlag", DbType.String, m_original_flag));
                }
                else
                {
                    paramList.Add((DbParameter)da.CreateParameter("@PreviousFlag", DbType.String, m_previous_flag));
                }

                paramList.Add((DbParameter)da.CreateParameter("@IsFlagDeleted", DbType.Boolean, m_isFlagDeleted));
                if (m_testInstruments is not null && m_testInstruments.Count > 0)
                {
                    paramList.Add((DbParameter)da.CreateParameter("@TestInstrumentsJson", DbType.String, JsonConvert.SerializeObject(m_testInstruments)));
                }
                else
                {
                    paramList.Add((DbParameter)da.CreateParameter("@TestInstrumentsJson", DbType.String, string.Empty));
                }
                paramList.Add((DbParameter)da.CreateParameter("@IsInterfaced", DbType.Boolean, m_isInterfaced));

                paramList.Add((DbParameter)da.CreateParameter("@DepartmentShortName", DbType.String, m_deptShortName));

                DbParameter[] @params = paramList.ToArray();

                Log.Debug("Ref Saving...");

                m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_TestAnalyte_Save", @params)["@RefAnalyteId"].Value);

                if (!(m_refTest == null))
                    m_refTest.Update();

                Log.Debug("Ref Saved");

                FlagClean();
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        protected override void DataFactory_Save()
        {
            Update();
        }

        #endregion

        internal void SetControlAnalyte(string name)
        {
            m_name = name;
        }

        internal void SetRefAnalyteId(long analyteId)
        {
            m_id = analyteId;
        }

    }
}