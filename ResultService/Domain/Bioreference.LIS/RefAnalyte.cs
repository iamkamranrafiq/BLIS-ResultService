using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Xml;
using Bioreference.Common.Lab;
using Bioreference.Data.Client;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RefAnalyte : TestAnalyte
    {

        #region Private Member
        private ReportAnalyte m_analyteParent;
        private static readonly ILog Log = LogManager.GetLogger<RefAnalyte>();

        // NOTE: m_parent exists in the base object and SHOULD NOT
        // be used in this object.

        #endregion

        #region Constructor
        internal RefAnalyte(ReportAnalyte parent) : base()
        {
            m_analyteParent = parent;
        }
        #endregion

        #region Data Functions

        internal void Load(TestAnalyte analyte)
        {
            m_id = analyte.Id;
            m_flagValue = analyte.FlagValue;
            m_code = analyte.Code;
            m_name = analyte.Name;
            m_resultType = analyte.ResultType;
            m_units = analyte.Units;
            m_category = analyte.Category;
            m_referenceRange = analyte.ReferenceRange;
            m_refLabId = analyte.ReferenceLabId;
            m_refLabCode = analyte.ReferenceLabeAnalyteCode;
            m_displayByDefault = analyte.IsRequired;
            m_isReportable = analyte.IsReportable;
            m_calc.SetCalculation(analyte.Calculation.Expression);
            m_outboundChannelId = analyte.OutboundChannelId;
            m_allowUpdates = analyte.AllowUpdates;

            m_isAgencyReportable = analyte.IsAgencyReportable;
            m_reportingType = analyte.ReportingType;
            m_attachCommentToParent = analyte.AttachCommentToParent;
            _allowPreliminaryRelease = analyte.AllowPreliminaryRelease;
            _allowInternalRefRanges = analyte.AllowInternalRefRanges;
            _isPOC = analyte.IsPOC;
            _isDoubleEntry = analyte.IsDoubleEntry;
            m_testInstruments = analyte.TestInstruments;
            m_isInterfaced = analyte.IsInterfaced;
            m_deptShortName = DepartmentShortname;
            foreach (Critical c in analyte.Criticals)
                m_criticals.Add(c);

            foreach (Result r in analyte.ResultValues)
                m_resultValues.Add(r);

            // AG - Comments
            foreach (Common.Lab.Comment c in analyte.CommentsSelection)
                m_comments.Add(c);

            foreach (ComplexResultFlag r in analyte.ResultFlagRanges)
                m_resultFlagRanges.Add(r);


            m_testCodeMappingList = analyte.TestCodeAliasList;

        }

        internal override void Update()
        {

            try
            {

                var da = new DataWrapper(Configuration.ConnectionString);
                var paramList = new List<DbParameter>();

                paramList.Add((DbParameter)da.CreateParameter("@RefAnalyteId", DbType.Int64, m_id));
                paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, m_analyteParent.ID));

                DbParameter[] @params = paramList.ToArray();

                Log.Debug("Ref Saving....");


                m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_RefReportAnalyte_Save", @params)["@RefAnalyteId"].Value);

                Log.Debug("Ref Saved");

                FlagClean();
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        /// <summary>
    /// This is the older update method where RefAnalyte and Mapping (Ref_Report_Analyte) are updated in the same proc
    /// We currenly are using this when AddFromTestMaster is set to true
    /// </summary>
    /// <remarks></remarks>
        internal void UpdateCombined(ref DataRow row)
        {

            try
            {
                // **************************************************************
                var xmldoc = new XmlDocument();
                xmldoc.PreserveWhitespace = true;
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
                xmldoc.PreserveWhitespace = true;
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
                xmldoc.PreserveWhitespace = true;
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
                xmldoc.PreserveWhitespace = true;
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

                row["RefAnalyteId"] = m_id;
                row["ReportAnalyteId"] = m_analyteParent.ID;
                row["Code"] = m_code;
                row["FlagValue"] = m_flagValue;
                row["Name"] = m_name;
                row["RefRange"] = m_referenceRange;
                row["ResultType"] = m_resultType;
                row["Units"] = m_units;
                row["Category"] = m_category;
                row["ReferenceLabID"] = m_refLabId;
                row["ReferenceLabAnalyteCode"] = m_refLabCode;
                row["DisplayByDefault"] = m_displayByDefault;
                row["IsReportable"] = m_isReportable;
                row["OutboundChannelId"] = m_outboundChannelId;
                row["AllowUpdates"] = m_allowUpdates;
                if (!(m_calc == null))
                    row["Calculation"] = m_calc.Expression;

                row["CriticalsXml"] = xmlCriticals;
                row["ResultValuesXml"] = xmlResultValues;
                row["CommentsXml"] = m_comments.ToXmlString();
                row["FlagRangesXml"] = xmlFlagRanges;
                row["TestCodeMappingsXml"] = xmlMappings;
                row["IsAgencyReportable"] = m_isAgencyReportable;
                row["ReportingType"] = m_reportingType;
                row["AttachCommentToParent"] = m_attachCommentToParent;
                row["AutoRelease"] = m_autoRelease;
                row["Precision"] = m_precision;
                row["ReferenceLabOrderingCode"] = m_refLabOrderingCode;

                row["AltInboundTestCode"] = m_altInboundTestCode;
                row["AltOutboundTestCode"] = m_altOutboundTestCode;
                row["AltOutboundTestDesc"] = m_altOutboundTestDesc;

                if ((m_original_ref_range ?? "") != (ReferenceRange ?? ""))
                {
                    row["PreviousRefRange"] = m_original_ref_range;
                }
                else
                {
                    row["PreviousRefRange"] = m_previous_ref_range;
                }
                if ((m_original_units ?? "") != (Units ?? ""))
                {
                    row["PreviousUnits"] = m_original_units;
                }
                else
                {
                    row["PreviousUnits"] = m_previous_units;
                }
                if ((m_original_flag ?? "") != (FlagValue ?? ""))
                {
                    row["PreviousFlag"] = m_original_flag;
                }
                else
                {
                    row["PreviousFlag"] = m_previous_flag;
                }


                row["IsFlagDeleted"] = m_isFlagDeleted;
                row["AllowPreliminaryRelease"] = _allowPreliminaryRelease;
                row["AllowInternalRefRanges"] = _allowInternalRefRanges;
                row["IsPOC"] = _isPOC;
                row["IsDoubleEntry"] = _isDoubleEntry;
                if (m_testInstruments is not null && m_testInstruments.Count > 0)
                {
                    row["TestInstruments"] = JsonConvert.SerializeObject(m_testInstruments);
                }
                else
                {
                    row["TestInstruments"] = "";
                }
                row["IsInterfaced"] = m_isInterfaced;

                // Dim params() As DbParameter = paramList.ToArray()

                // Log.Debug("Ref Saving....")

                // m_id = da.ExecuteNonQuery("lis_RefAnalyte_Save", params).Item("@RefAnalyteId").Value

                // Log.Debug("Ref Saved")

                FlagClean();
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

                // need to re-throw this error.  this is part of an existing transaction. The caller needs to know.
                throw;

            }

        }

        internal void UpdateCombined()
        {

            try
            {
                // **************************************************************
                var xmldoc = new XmlDocument();
                xmldoc.PreserveWhitespace = true;
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
                xmldoc.PreserveWhitespace = true;
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
                xmldoc.PreserveWhitespace = true;
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
                xmldoc.PreserveWhitespace = true;
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


                paramList.Add((DbParameter)da.CreateParameter("@RefAnalyteId", DbType.Int64, m_id, ParameterDirection.InputOutput));
                paramList.Add((DbParameter)da.CreateParameter("@ReportAnalyteId", DbType.Int64, m_analyteParent.ID));
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
                    paramList.Add((DbParameter)da.CreateParameter("@PreviousUnits", DbType.String, m_previous_units));
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
                paramList.Add((DbParameter)da.CreateParameter("@AllowPreliminaryRelease", DbType.Boolean, _allowPreliminaryRelease));
                paramList.Add((DbParameter)da.CreateParameter("@AllowInternalRefRanges", DbType.Boolean, _allowInternalRefRanges));
                paramList.Add((DbParameter)da.CreateParameter("@IsPOC", DbType.Boolean, _isPOC));
                paramList.Add((DbParameter)da.CreateParameter("@IsDoubleEntry", DbType.Boolean, _isDoubleEntry));
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

                Log.Debug("Ref Saving....");

                m_id = Conversions.ToLong(da.ExecuteNonQuery("lis_RefAnalyte_Save", @params)["@RefAnalyteId"].Value);

                Log.Debug("Ref Saved");

                FlagClean();
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

                // need to re-throw this error.  this is part of an existing transaction. The caller needs to know.
                throw;

            }

        }


        // '''''''''''''' Modified to allow log '''''''''''
        public List<PropertyGroup> GetAudits()
        {
            var lst = new List<PropertyGroup>();

            if ((m_original_ref_range ?? "") != (m_referenceRange ?? ""))
            {
                var pRange = new PropertyGroup();
                pRange.CurrentValue = m_referenceRange;
                pRange.IsPriority = false;
                pRange.Name = "RefRange";
                pRange.PreviousValue = Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(m_original_ref_range), "", m_original_ref_range));
                pRange.PropertyType = "System.String";
                lst.Add(pRange);
                Log.Debug($"RefAnalyte:GetAudits:RefRange: original referenceRange '{m_original_ref_range}' - current referenceRange '{m_referenceRange}'");
            }
            if ((m_original_units ?? "") != (m_units ?? ""))
            {
                var pUnits = new PropertyGroup();
                pUnits.CurrentValue = m_units;
                pUnits.IsPriority = false;
                pUnits.Name = "Units";
                pUnits.PreviousValue = Conversions.ToString(Interaction.IIf(string.IsNullOrEmpty(m_original_units), "", m_original_units));
                pUnits.PropertyType = "System.String";
                lst.Add(pUnits);
                Log.Debug($"RefAnalyte:GetAudits:Units: original Units '{m_original_units}' - current units '{m_units}'");
            }
            return lst;
        }
        // '''''''''''''''''''''''''''''''''''''''''''''''

        // '''''''''''''' Modified to allow log '''''''''''
        internal void ResetOriginalValues()
        {
            m_original_ref_range = m_referenceRange;
            m_original_units = m_units;
        }

        // ''''''''''''''''''''''''''''''''''''''''''''''''

        #endregion

    }


    // '''''''''''''' Modified to allow log '''''''''''
    public class PropertyGroup
    {
        public string Name;
        public string PreviousValue;
        public string CurrentValue;
        public string PropertyType;
        public bool IsPriority;
    }
}
// ''''''''''''''''''''''''''''''''''''''''''''''''