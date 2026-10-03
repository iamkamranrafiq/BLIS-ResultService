using System;
using System.Collections.Generic;
using Bioreference.Common;
using Bioreference.Data.Audit;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class ReportAuditManager
    {

        private List<AuditInfo> m_list = new List<AuditInfo>();
        private string m_accessionNbr;
        private bool m_updatesOnly;
        private Report m_report;
        private Order m_order;
        private List<string> aipNames = new List<string> {
        "OrderingCodes", "ReportingHold", "CorrectedResultReason", "RefRange", "Units", "FlagValue", "ResultValue",
        "TransmitStatus", "ResultAnalyzedTechUser", "ResultReleasedUser", "InstrumentId", "IsPresumptiveHold",
        "SampleStatus", "PerformingFacility", "AccessioningFacility", "COCApprover", "IsCOCReviewed" };
        Dictionary<int,string> m_rreList = new Dictionary<int,string>();

        #region Public Properties

        public bool UpdatesOnly
        {
            get
            {
                return m_updatesOnly;
            }
            set
            {
                m_updatesOnly = value;
            }
        }


        #endregion

        #region Public Class AuditInfo

        [Serializable()]
        public class AuditInfo : IComparable
        {

            public AuditInfo(string accessionNbr, Data.Audit.auditActionType @type, DateTime eventDate, string userName, string propertyName = "", string fromValue = "", string toValue = "", string testCode = "", string testName = "")


            {

                AccessionNumber = accessionNbr;
                AuditType = type;
                TestCode = testCode;
                TestName = testName;
                PropertyName = propertyName;
                FromValue = fromValue;
                ToValue = toValue;
                UserName = userName;
                EventDate = eventDate;

            }

            public string AccessionNumber;
            public Data.Audit.auditActionType AuditType;
            public string TestCode;
            public string TestName;
            public string PropertyName;
            public string FromValue;
            public string ToValue;
            public string UserName;
            public DateTime EventDate;

            public int CompareTo(object obj)
            {

                if (!ReferenceEquals(obj.GetType(), typeof(AuditInfo)))
                    return 1;

                AuditInfo a = (AuditInfo)obj;
                if (EventDate == a.EventDate)
                {
                    return 0;
                }
                else if (EventDate < a.EventDate)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }

            }

        }

        #endregion

        public string FetchAsJsonString(string accessionNbr, bool updatesOnly, DateTime serviceDate)
        {

            var sb = new System.Text.StringBuilder();

            AuditInfo[] audits = Fetch(accessionNbr, updatesOnly, serviceDate,null);

            if (audits == null)
            {
                return "";
            }

            sb.Append("{\"Audits\": [");
            foreach (AuditInfo a in audits)
            {
                sb.Append(string.Format("{{\"AccessionNumber\": \"{0}\", ", a.AccessionNumber));
                sb.Append(string.Format("\"AuditType\": \"{0}\", ", a.AuditType.ToString()));
                sb.Append(string.Format("\"TestCode\": \"{0}\", ", a.TestCode));
                sb.Append(string.Format("\"TestName\": \"{0}\", ", a.TestName.Replace("\"", "\"\"")));
                sb.Append(string.Format("\"PropertyName\": \"{0}\", ", a.PropertyName));
                sb.Append(string.Format("\"FromValue\": \"{0}\", ", a.FromValue));
                sb.Append(string.Format("\"ToValue\": \"{0}\", ", a.ToValue));
                sb.Append(string.Format("\"UserName\": \"{0}\", ", a.UserName));
                sb.Append(string.Format("\"EventDate\": \"{0}\"}}, ", a.EventDate.ToString()));
            }
            string s;
            s = Conversions.ToString(Interaction.IIf(audits.Length > 0, string.Concat(sb.ToString().Substring(0, sb.ToString().Length - 2)), sb.ToString()));
            s = string.Concat(s, "]}");
            return s;

        }

        public AuditInfo[] Fetch(string accessionNbr, bool updatesOnly, DateTime serviceDate, Dictionary<int,string> m_rreTemplateList, long reportId = 0 )
        {

            m_accessionNbr = accessionNbr;
            m_updatesOnly = updatesOnly;

            Data.Audit.AuditItems auditItems;
            m_report = Report.Fetch(reportId, true);           

            if (m_report != null)
            {
                m_order = OrderManager.FetchOrder(m_report.OrderId, true);
                ProcessReport(m_report, m_rreTemplateList);
                ProcessOrderAudit(m_order);
                if (!m_updatesOnly)
                {
                    // auditItems = auditItems.Fetch(m_report.ID, m_report.ToString())
                    auditItems = Data.Audit.AuditItems.Fetch(m_report.ID.ToString(), m_report.ToString());

                    foreach (Data.Audit.AuditItem ai in auditItems.List)
                        m_list.Add(new AuditInfo(m_accessionNbr, auditActionType.View, ai.DateCreated, ai.UserName,"","",""));
                }

                m_list.Sort();

                return m_list.ToArray();
            }

            else
            {
                return null;
            }

        }

        private void ProcessReport(Report r, Dictionary<int, string> m_rreTemplateList)
        {

            foreach (Data.Audit.AuditItem ai in r.AuditItems.List)
            {
                if (ai.AuditId != 0L && ai.AuditActionType == auditActionType.CustomAction)
                {
                    string auditMsg = ai.AuditMessage;
                    string templateName;

                    // Include the templateName if the audit is for RRE added or removed
                    if (auditMsg.StartsWith("Added to RRE#") || auditMsg.StartsWith("Removed from RRE#"))
                    {
                        int rrID = int.Parse(auditMsg[(auditMsg.IndexOf("#") + 1)..].Trim());
                        int rrTemplateID = 0;

                        if (m_rreTemplateList.ContainsKey(rrID))
                        {
                            templateName = m_rreTemplateList[rrID];
                        }
                        else
                        {
                            rrTemplateID = RapidResult.Fetch(rrID).RapidResultTemplateId;

                            if (m_rreList.ContainsKey(rrTemplateID))
                            {
                                templateName = m_rreList[rrTemplateID];
                            }
                            else
                            {
                                m_rreList.Add(rrTemplateID, RapidResultTemplate.Fetch(rrTemplateID).Name);
                                templateName = m_rreList[rrTemplateID];
                            }

                            m_rreTemplateList.Add(rrID, templateName);
                        }

                        auditMsg = $"{auditMsg[..auditMsg.IndexOf("#")]} {templateName} {auditMsg[auditMsg.IndexOf("#")..]}";

                    }
                    m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, "", "", "", auditMsg));



                    foreach (Data.Audit.AuditItemProperty aip in ai.PropertyList)
                    {
                        if (aip.Name == "SpecimenComment")
                        {
                            m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, aip.Name, aip.PreviousValue, aip.CurrentValue));
                        }
                       
                    }
                }
            }

            foreach (ReportComment c in r.Comments.List)
                ProcessComment(c, "", "");

            foreach (ReportAnalytePanel p in r.AnalytePanels.List.ArchivedDeletedList)
                ProcessPanel(p);

            foreach (ReportAnalytePanel p in r.AnalytePanels.List)
                ProcessPanel(p);

            foreach (ReportAnalyte a in r.Analytes.List)
                ProcessAnalyte(a);


            foreach (ReportAnalyte a in r.Analytes.List.ArchivedDeletedList)
                ProcessAnalyte(a);
        }

        private void ProcessAnalyte(ReportAnalyte a)
        {

            foreach (Data.Audit.AuditItem ai in a.AuditItems.List)
            {
                if (ai.AuditId != 0L)
                {

                    if (!m_updatesOnly && ai.AuditActionType == Data.Audit.auditActionType.ObjectCreate && ai.PropertyList.Length == 0)
                    {
                        m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, "", "", "", a.Code, a.Analyte.Name));
                    }
                    else if (!m_updatesOnly && ai.AuditActionType == Data.Audit.auditActionType.ObjectDelete || ai.AuditActionType == Data.Audit.auditActionType.ObjectUnDelete)
                    {
                        m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, "", "", "", a.Code, a.Analyte.Name));
                    }
                    else if (ai.AuditActionType == Data.Audit.auditActionType.CustomAction && ai.AuditMessage.Contains("Double Entry"))
                    {
                        m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, "", "", ai.AuditMessage, a.Code, a.Analyte.Name));

                    }
                    else
                    {

                        foreach (Data.Audit.AuditItemProperty aip in ai.PropertyList)
                        {

                            if ((  aip.Name == "ResultValue" || aip.Name == "TransmitStatus" || aipNames.Contains(aip.Name) || (aip.Name == "ResultStatus" && IsHoldResultStatus(aip))) && (ai.AuditActionType == Data.Audit.auditActionType.ObjectCreate || (aip.CurrentValue ?? "") != (aip.PreviousValue ?? "")))
                            {

                                string pvalue = "";
                                string cvalue = "";
                                if (aip.Name == "TransmitStatus")
                                {
                                    pvalue = ((transmitStatusType)Conversions.ToInteger(aip.PreviousValue)).ToString();
                                    cvalue = ((transmitStatusType)Conversions.ToInteger(aip.CurrentValue)).ToString();
                                }
                                else if (aip.Name == "ResultStatus")
                                {
                                    pvalue = ((resultStatusType)Conversions.ToInteger(aip.PreviousValue)).ToString();
                                    cvalue = ((resultStatusType)Conversions.ToInteger(aip.CurrentValue)).ToString();
                                }
                                else
                                {
                                    pvalue = aip.PreviousValue;
                                    cvalue = aip.CurrentValue;
                                }
                                m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, aip.Name, pvalue, cvalue, a.Code, a.Analyte.Name));


                            }
                        }

                    }

                }
            }

            
            ProcessComments(a.Comments, a);

            foreach (ReportAlert ra in a.Alerts.List)
                ProcessAlert(ra, a.Code, a.Analyte.Name);

        }
        private void ProcessComments(ReportComments comments, ReportAnalyte reportAnalyte)
        {
            foreach (ReportComment c in comments.List)
            {
                this.ProcessComment(c, reportAnalyte.Code, reportAnalyte.Analyte.Name);
            }

            foreach (ReportComment c in comments.List.ArchivedDeletedList)
            {
                this.ProcessComment(c, reportAnalyte.Code, reportAnalyte.Analyte.Name);
            }
        }

        private void ProcessComment(ReportComment c, string testCode, string testName)
        {

            string propertyName = "Comment";

            foreach (Data.Audit.AuditItem ai in c.AuditItems.List)
            {
                if (ai.AuditId != 0L)
                {
                    if (ai.AuditActionType == Data.Audit.auditActionType.ObjectCreate || ai.AuditActionType == Data.Audit.auditActionType.ObjectDelete)
                    {
                        if (!m_updatesOnly)
                        {
                            m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, propertyName, "", c.Text, testCode, testName));
                        }
                    }
                    else
                    {
                        foreach (Data.Audit.AuditItemProperty aip in ai.PropertyList)
                        {
                            if (aip.Name == "Text" && (aip.CurrentValue ?? "") != (aip.PreviousValue ?? ""))
                            {

                                m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, propertyName, aip.PreviousValue, aip.CurrentValue, testCode, testName));
                            }
                        }
                    }
                }
            }

        }
        private void ProcessPanel(ReportAnalytePanel p)
        {

            foreach (Data.Audit.AuditItem ai in p.AuditItems.List)
            {
                if (ai.AuditId != 0L)
                {
                    if (!m_updatesOnly && ai.AuditActionType == Data.Audit.auditActionType.ObjectCreate)
                    {
                        m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, "", "", "", p.PanelCode, p.Panel.Name));
                    }
                    else if (!m_updatesOnly && ai.AuditActionType == Data.Audit.auditActionType.ObjectDelete || ai.AuditActionType == Data.Audit.auditActionType.ObjectUnDelete)
                    {
                        m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, "", "", "", p.PanelCode, p.Panel.Name));
                    }

                }
            }

            foreach (ReportComment c in p.Comments.List)
                ProcessComment(c, p.PanelCode, p.Panel.Name);

            foreach (ReportAnalyte a in p.Analytes.List)
                ProcessAnalyte(a);

        }
        private void ProcessAlert(ReportAlert a, string testCode, string testName)
        {

            foreach (Data.Audit.AuditItem ai in a.AuditItems.List)
            {
                if (ai.AuditId != 0L)
                {
                    if (!m_updatesOnly && ai.AuditActionType == Data.Audit.auditActionType.ObjectCreate)
                    {
                        m_list.Add(new AuditInfo(m_accessionNbr, ai.AuditActionType, ai.DateCreated, ai.UserName, "Alert", "", a.Alert.Code, testCode, testName));
                    }
                }
            }

        }

        private void ProcessOrderAudit(Order order)
        {
            if (order == null) return;

            // Order Audit Items
            foreach (AuditItem ai in order.AuditItems.List)
            {
                if (ai.AuditId != 0)
                {


                    foreach (AuditItemProperty aip in ai.PropertyList)
                    {

                        if (ai.AuditActionType == auditActionType.ObjectUpdate || !Equals(aip.CurrentValue, aip.PreviousValue))
                        {

                            m_list.Add(new AuditInfo((m_accessionNbr),
                           ai.AuditActionType,
                           ai.DateCreated,
                           ai.UserName,
                           aip.Name,
                           aip.PreviousValue,
                           aip.CurrentValue,
                           "Order"
                           ));

                        }

                    }
                }
            }

            // OrderPatient Audit Items
            foreach (AuditItem ai in order.Patient.AuditItems.List)
            {
                if (ai.AuditId != 0)
                {
                    foreach (AuditItemProperty aip in ai.PropertyList)
                    {
                        if (ai.AuditActionType == auditActionType.ObjectCreate || !Equals(aip.CurrentValue, aip.PreviousValue))
                        {
                            string pvalue = string.Empty;
                            string cvalue = string.Empty;

                            if (aip.Name == "Gender")
                            {

                                pvalue = Enum.TryParse<Bioreference.Common.Gender>(
                                           Convert.ToString(aip.PreviousValue), out var parsedPrev)
                                            ? parsedPrev.ToString() : Convert.ToString(aip.PreviousValue);

                                cvalue = Enum.TryParse<Bioreference.Common.Gender>(
                                            Convert.ToString(aip.CurrentValue), out var parsedCurr)
                                            ? parsedCurr.ToString() : Convert.ToString(aip.CurrentValue);
                            }
                            else if (aip.Name == "FirstName" || aip.Name == "LastName")
                            {
                                if (order.IsRestrictedAccession()) //TODO: CanAccessRestrictedAccount()
                                {
                                    pvalue = "[restricted]";
                                    cvalue = "[restricted]";
                                }
                                else
                                {
                                    pvalue = Convert.ToString(aip.PreviousValue);
                                    cvalue = Convert.ToString(aip.CurrentValue);
                                }
                            }
                            else
                            {
                                pvalue = Convert.ToString(aip.PreviousValue);
                                cvalue = Convert.ToString(aip.CurrentValue);
                            }


                            m_list.Add(new AuditInfo((m_accessionNbr),
                            ai.AuditActionType,
                             ai.DateCreated,
                             ai.UserName,
                             ai.AuditActionType == auditActionType.ObjectUpdate ? aip.Name : string.Empty,
                             aip.PreviousValue,
                             aip.CurrentValue,
                              "Demographics"
                             ));


                        }

                    }
                }
            }

            foreach (OrderComment o in order.OrderComments.List)
            {
                ProcessOrderComment(o);
            }

            foreach (OrderComment o in order.OrderComments.List.ArchivedDeletedList)
            {
                ProcessOrderComment(o);
            }

        }

        private void ProcessOrderComment(OrderComment comment)
        {
            foreach (AuditItem ai in comment.AuditItems.List)
            {
                if (ai.AuditId != 0)
                {

                    if (ai.AuditActionType == auditActionType.ObjectCreate || ai.AuditActionType == auditActionType.ObjectDelete)
                    {
                        m_list.Add(new AuditInfo((m_accessionNbr), ai.AuditActionType,
                        ai.DateCreated,
                        ai.UserName,
                        "",
                        "",
                        comment.Text,
                       "Accession Comment"));
                    }
                    else
                    {
                        foreach (AuditItemProperty aip in ai.PropertyList)
                        {

                            if (aip.Name == "Text" && !Equals(aip.CurrentValue, aip.PreviousValue))
                            {

                                m_list.Add(new AuditInfo((m_accessionNbr), ai.AuditActionType,
                                ai.DateCreated,
                                ai.UserName,
                                "",
                                aip.PreviousValue,
                                aip.CurrentValue,
                                 "Accession Comment"
                                ));

                                break; // Exit For
                            }

                        }
                    }
                }
            }
        }

        private bool IsHoldResultStatus(AuditItemProperty aip)
        {
            var current = (resultStatusType)Conversions.ToInteger(aip.CurrentValue);
            var previous = (resultStatusType)Conversions.ToInteger(aip.PreviousValue);

            return current == resultStatusType.DeltaHold ||
                   current == resultStatusType.OnHold ||
                   previous == resultStatusType.DeltaHold ||
                   previous == resultStatusType.OnHold;
        }


    }
}