using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Bioreference.Data;
using static Bioreference.LIS.OrderManager;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderReport : DataClassBase
    {

        #region Private Members

        private Order m_order = null;
        private Report m_report = null;
        private TestCodeStruct[] m_addTestCodes = null;
        private TestCodeStruct[] m_updateTestCodes = null;
        private TestCodeStruct[] m_changeTestCodes = null;
        private Status m_status;
        private bool m_onlyNewCodes = true;
        private bool m_addNewCodes = true;
        private bool m_isTNPupdate = false;
        private OrderSnapshot m_snapshot;
        private readonly ILog Log = LogManager.GetLogger<OrderReport>();
        private List<string> m_changeOrderedCodes = new List<string>();  // ordering codes of changed components

        #endregion

        #region Constructor

        public OrderReport(Order order, TestCodeStruct[] testCodes)
        {
            m_order = order;
            m_addTestCodes = testCodes;
            FlagDirty();
        }

        public OrderReport(Order order, TestCodeStruct[] testCodes, TestCodeStruct[] updateTestCodes)
        {

            m_order = order;
            m_addTestCodes = testCodes;
            m_updateTestCodes = updateTestCodes;
            FlagDirty();

        }
        public OrderReport(Order order, List<TestCodeStruct> addTestCodes, List<TestCodeStruct> updateTestCodes, List<TestCodeStruct> changeTestCodes)
        {

            m_order = order;
            m_addTestCodes = addTestCodes.ToArray();
            m_updateTestCodes = updateTestCodes.ToArray();
            m_changeTestCodes = changeTestCodes.ToArray();
            foreach (TestCodeStruct t in m_addTestCodes)
                Log.Debug($"TestCodeStruct Code={t.TestCode} Value={t.ResultValue} Status={t.SPMStatus}");
            FlagDirty();

        }

        public OrderReport(Order order, string[] testCodes)
        {

            m_order = order;
            var list = new List<TestCodeStruct>();
            foreach (string Item in testCodes)
                list.Add(new TestCodeStruct() { TestCode = Item.ToUpper(), OrderedTestCode = Item.ToUpper() });
            m_addTestCodes = list.ToArray();
            FlagDirty();

        }

        #endregion

        #region Public Properties

        public Order Order
        {
            get
            {
                return m_order;
            }
        }

        public Report Report
        {
            get
            {
                return m_report;
            }
        }

        public Status Status
        {
            get
            {
                return m_status;
            }
        }

        public OrderSnapshot Snapshot
        {
            get
            {
                return m_snapshot;
            }
            set
            {
                m_snapshot = value;
            }
        }

        #endregion

        private void SetToFollowFromReportHold(ReportAnalyteList list)
        {
            foreach (ReportAnalyte reportholdAnalyte in list)
            {
                if (reportholdAnalyte.TransmitStatus == transmitStatusType.PendingRelease && reportholdAnalyte.ToFollowSent == false)
                {
                    reportholdAnalyte.ToFollowSent = false;
                }
                if (reportholdAnalyte.ResultStatus == resultStatusType.Corrected)
                {
                    reportholdAnalyte.ResultStatus = resultStatusType.Final;
                }
            }
        }

        #region Data Functions

        protected override void DataFactory_Save()
        {

            try
            {
                var timer = new Stopwatch();
                long elapsedTime = timer.ElapsedMilliseconds;
                string resultsMsg = "";

                if (!(m_addTestCodes == null))
                {
                    foreach (TestCodeStruct i in m_addTestCodes)
                    {
                        Log.Debug($"AddTest code={i.TestCode} status={i.SPMStatus}");
                        m_order.Tests.AddTest(i.TestCode, "", false, i.OrderedTestCode, i.OrderedTestName, spmStatus: i.SPMStatus);
                    }
                }

                Log.Debug($"{m_order.AccessionNbr} Test Count: {m_addTestCodes.Length} Check Report Exists...");
                if (!ReportSearch.ReportExists(m_order.AccessionNbr, (int)m_order.ID))
                {
                    // If Not ReportSearch.ReportExists(m_order.AccessionNbr) Then

                    Log.Debug($"{m_order.AccessionNbr} Creating Report...");

                    // m_report = Report.CreateResults(m_order, m_onlyNewCodes)
                    timer.Start();
                    m_report = Report.CreateResults(m_order, m_addTestCodes);
                    if (m_snapshot is not null)
                    {
                        m_report.SpecimenSummary = m_snapshot.SpecimenSummary;
                    }
                    timer.Stop();
                    elapsedTime = timer.ElapsedMilliseconds;
                    timer.Reset();
                    Log.DebugFormat("Report CreateResults took {0} milliseconds", elapsedTime);              

                    if (m_report.AnalytePanels.List.Count > 0 || m_report.Analytes.List.Count > 0)
                    {
                        if (m_report.IsValid && m_order.IsValid)
                        {
                            // Save the report and the order

                            UpdateResultsFromTestCodeStruct();

                            // 'We use the transaction scope when saving the Order and Report. If any failures, then both are rolled back.
                            var options = new TransactionOptions();
                            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
                            options.Timeout = new TimeSpan(0, 2, 0);

                            if (ProcessSpecimenCodes())
                                m_report.FlagDirty();
                            if (ProcessInstrumentQueryQueue())
                                m_report.FlagDirty();
                            if (ProcessParentTestCodes())
                                m_report.FlagDirty();

                            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
                            {
                                // NA 12/17/2013 : Accession Level Comments need to be released even on Creation of Report.
                                // Can't wait until calling the Order.Save if report doesn't exist yet.
                                // if the report already exists, we let the Order.Save routing handle the check.
                                if (m_report.ID <= 0L && m_order.AreCommentsDirty)
                                {
                                    m_report.CheckAndFlagIfResend(true);
                                    m_report.FlagDirty();
                                }

                                timer.Start();
                                m_order = (Order)m_order.Save();
                                timer.Stop();
                                elapsedTime = timer.ElapsedMilliseconds;
                                timer.Reset();
                                Log.DebugFormat("Order save took {0} milliseconds", elapsedTime);
                                timer.Start();
                                m_report = (Report)m_report.Save();
                                timer.Stop();
                                elapsedTime = timer.ElapsedMilliseconds;
                                Log.DebugFormat("Report save took {0} milliseconds", elapsedTime);
                                timer.Reset();

                                scope.Complete();

                            }

                            Log.Debug($"{m_order.AccessionNbr} Transaction Scope Complete. Marking Unsolicited Processed...");

                            // Produce and Mark Processed all unsolicited results as processed.
                            Log.Debug($"{m_order.AccessionNbr} Check unsolicited...");
                            // Check to see if there are any unsolicited results for this order,
                            // and if so, apply them
                            // *****************************************************************
                            Log.Debug($"Fetch Unsolicited (OR) {Order.AccessionNbr}");
                            UnsolicitedMessage.FetchUnProcessed(Order.AccessionNbr);
                            Log.Debug($"Unsolicited Count {UnsolicitedMessage.List.Count}");

                            foreach (UnsolicitedMessage um in UnsolicitedMessage.List)
                            {
                                MessageProducerApiClient.Produce(MessageType.UnsolicitedMessage.ToString(), um.HL7Message, um.AccessionNbr);
                                um.MarkProcessed();
                                um.Save();
                            }
                            Log.Debug($"{m_order.AccessionNbr} Produce and Mark Processed - Unsolicited Completed.");
                            m_status = new Status(StatusType.Success, resultsMsg, m_report);
                        }
                        else
                        {
                            m_report.Rules.MergeList(m_order.Rules);
                            m_status = new Status(StatusType.Failure, m_report.Rules.ToString(), m_report);
                        }
                    }
                    else
                    {
                        // This error condition is returned if no Analytes or Panels are added to the Report based on the Order.
                        m_status = new Status(StatusType.Failure, string.Format("Report was not created. Unable to create report from OrderId #{0} ({1}). No valid tests were added. Make sure that tests are valid in TestMaster db.", m_order.ID, m_order.AccessionNbr), null);
                    }
                }
                else
                {
                    var newTests = new List<string>();
                    if (m_addNewCodes)
                    {
                        Log.Debug($"Fetch Report...{m_order.AccessionNbr} / {m_order.DateOfService}");
                        m_report = m_order.Report;
                        Log.Debug($"Fetch Completed...OrderId={m_report.OrderId}");
                        m_report.ReferencedOrder.SpecimenUpdated = m_order.SpecimenUpdated;

                        // process ReportHold updates
                        if (m_order.IsReportHoldDirty)
                        {
                            SetToFollowFromReportHold(m_report.Analytes.List);

                            foreach (ReportAnalytePanel reportholdAnalytePanel in m_report.AnalytePanels.List)
                                SetToFollowFromReportHold(reportholdAnalytePanel.Analytes.List);
                        }

                        // Process TNP and IsPresumptive updates  - Good order after TNP
                        if (!(m_changeTestCodes == null))
                        {

                            bool updateCalcAnalytes = false;

                            foreach (TestCodeStruct i in m_changeTestCodes)
                            {

                                bool isResultTNP = false;
                                bool isResultATP = false;
                                bool isResultTNPorATP = false;

                                if (!string.IsNullOrEmpty(i.ResultValue))
                                {
                                    isResultTNP = Configuration.LISSettings.GetList("SPMTNPTriggers").Contains(i.ResultValue.Trim(), new CompareText());
                                    isResultATP = Configuration.LISSettings.GetList("SPMATPTriggers").Contains(i.ResultValue.Trim(), new CompareText());
                                    isResultTNPorATP = isResultATP || isResultTNP;
                                }

                                // <PRE-ITBT-2765>
                                // Dim analyte As ReportAnalyte = m_report.FindAnalyte(i.TestCode, True)
                                // If Not IsNothing(analyte) AndAlso Not i.ResultValue.Trim().Equals("TNP") Then
                                // analyte.SetResultValue(i.ResultValue, False)
                                // analyte.ToFollowSent = False
                                // analyte.ResultStatus = resultStatusType.Preliminary
                                // analyte.Comments.DeleteAll()
                                // End If
                                // </PRE-ITBT-2765>

                                // <ITBT-2765>
                                bool analyteFound = false;
                                ReportAnalyte analyte = null;
                                // search for analyte (not in panel)
                                analyte = m_report.FindAnalyte(i.TestCode, false);
                                if (analyte is not null && !i.IsPresumptiveHold && analyte.IsPresumptiveHold)
                                {
                                    // process removal of IsPresumptiveHold
                                    analyteFound = true;
                                    analyte.IsPresumptiveHold = false;
                                    analyte.ToFollowSent = false;
                                }
                                else if (analyte is not null && analyte.ContainsOrderCode(i.OrderedTestCode))
                                {
                                    analyteFound = true;
                                    analyte.SetResultValue(i.ResultValue, false);
                                    analyte.ToFollowSent = false;
                                    analyte.ResultStatus = resultStatusType.Preliminary;
                                    analyte.Comments.DeleteAll();
                                    updateCalcAnalytes = true;
                                    if (!i.OrderedTestCode.IsNothingOrEmpty() && !m_changeOrderedCodes.Contains(i.OrderedTestCode))
                                    {
                                        m_changeOrderedCodes.Add(i.OrderedTestCode);
                                    }
                                    if (isResultTNPorATP)
                                    {
                                        analyte.SPMStatus = (SPMStatusValue)Conversions.ToInteger(Interaction.IIf(isResultTNP, SPMStatusValue.TNP, SPMStatusValue.ATP));
                                    }
                                    else if (analyte.SPMStatus == SPMStatusValue.ATP || analyte.SPMStatus == SPMStatusValue.TNP)
                                    {
                                        analyte.SPMStatus = SPMStatusValue.Reversed;
                                    }
                                    else
                                    {
                                        analyte.SPMStatus = SPMStatusValue.None;
                                    }
                                }
                                // search for analyte within panels only if not previously found.
                                if (!analyteFound)
                                {
                                    analyte = null;
                                    analyte = m_report.FindAnalyte(i.TestCode, true);
                                    if (analyte is not null && !i.IsPresumptiveHold && analyte.IsPresumptiveHold)
                                    {
                                        // process removal of IsPresumptiveHold
                                        analyteFound = true;
                                        analyte.IsPresumptiveHold = false;
                                        analyte.ToFollowSent = false;
                                    }
                                    else if (analyte is not null)
                                    {
                                        analyteFound = true;
                                        analyte.SetResultValue(i.ResultValue, false);
                                        analyte.ToFollowSent = false;
                                        analyte.ResultStatus = resultStatusType.Preliminary;
                                        analyte.Comments.DeleteAll();
                                        if (!i.OrderedTestCode.IsNothingOrEmpty() && !m_changeOrderedCodes.Contains(i.OrderedTestCode))
                                        {
                                            m_changeOrderedCodes.Add(i.OrderedTestCode);
                                        }
                                        updateCalcAnalytes = true;
                                        if (isResultTNPorATP)
                                        {
                                            analyte.SPMStatus = (SPMStatusValue)Conversions.ToInteger(Interaction.IIf(isResultTNP, SPMStatusValue.TNP, SPMStatusValue.ATP));
                                        }
                                        else if (analyte.SPMStatus == SPMStatusValue.ATP || analyte.SPMStatus == SPMStatusValue.TNP)
                                        {
                                            analyte.SPMStatus = SPMStatusValue.Reversed;
                                        }
                                        else
                                        {
                                            analyte.SPMStatus = SPMStatusValue.None;
                                        }
                                    }
                                }
                                // </ITBT-2765>
                                var analytePanel = m_report.FindAnalytePanel(i.TestCode);
                                if (analytePanel is not null && !i.IsPresumptiveHold && analytePanel.IsPresumptiveHold)
                                {
                                    // process removal of IsPresumptiveHold
                                    analytePanel.ToFollowSent = false;
                                    foreach (ReportAnalyte panelanalyte in analytePanel.Analytes.List)
                                    {
                                        if (panelanalyte is not null)
                                        {
                                            panelanalyte.IsPresumptiveHold = false;
                                            panelanalyte.ToFollowSent = false;
                                        }
                                    }
                                }
                                else if (analytePanel is not null)
                                {
                                    analytePanel.ToFollowSent = false;
                                    if (isResultTNPorATP)
                                    {
                                        analytePanel.SPMStatus = (SPMStatusValue)Conversions.ToInteger(Interaction.IIf(isResultTNP, SPMStatusValue.TNP, SPMStatusValue.ATP));
                                    }
                                    else if (analytePanel.SPMStatus == SPMStatusValue.ATP || analytePanel.SPMStatus == SPMStatusValue.TNP)
                                    {
                                        analytePanel.SPMStatus = SPMStatusValue.Reversed;
                                    }
                                    else
                                    {
                                        analytePanel.SPMStatus = SPMStatusValue.None;
                                    }
                                    if (!i.OrderedTestCode.IsNothingOrEmpty() && !m_changeOrderedCodes.Contains(i.OrderedTestCode))
                                    {
                                        m_changeOrderedCodes.Add(i.OrderedTestCode);
                                    }
                                    analytePanel.Comments.DeleteAll();
                                    foreach (ReportAnalyte panelanalyte in analytePanel.Analytes.List)
                                    {
                                        if (!(panelanalyte == null))
                                        {
                                            panelanalyte.SetResultValue(i.ResultValue, false);
                                            panelanalyte.ToFollowSent = false;
                                            panelanalyte.SPMStatus = analytePanel.SPMStatus;
                                            panelanalyte.ResultStatus = resultStatusType.Preliminary;
                                            panelanalyte.Comments.DeleteAll();
                                            if (!i.OrderedTestCode.IsNothingOrEmpty() && !m_changeOrderedCodes.Contains(i.OrderedTestCode))
                                            {
                                                m_changeOrderedCodes.Add(i.OrderedTestCode);
                                            }
                                            updateCalcAnalytes = true;
                                        }
                                    }
                                }
                            }

                            // good order update after TNP - updated all components result value to "" & status to Prelim, same applies for calculation
                            // system logic updated result value to "" for Calculation but resultstatus updated as corrected
                            // below logic updates calculation status to Prelim
                            if (m_report.HasCalcAnalytes && updateCalcAnalytes)
                            {
                                foreach (ReportAnalyte analyte in m_report.Analytes.List)
                                {
                                    if (analyte.Analyte.IsCalculation && string.IsNullOrEmpty(analyte.ResultValue) && analyte.ResultStatus == resultStatusType.Corrected)
                                    {
                                        analyte.ResultStatus = resultStatusType.Preliminary;
                                    }
                                }
                            }

                        }

                        // Update AOEs and components of ordered codes when another component of that ordered code was switched from TNP to a good order
                        if (!(m_updateTestCodes == null))
                        {
                            foreach (TestCodeStruct i in m_updateTestCodes)
                            {
                                if (!i.ResultValue.Equals(""))
                                {
                                    var analyte = m_report.FindAnalyte(i.TestCode, true);
                                    // AOE
                                    if (!(analyte == null) && analyte.CodeTypeId == 5)
                                    {
                                        // update the value result
                                        var currentTransmitStatus = analyte.TransmitStatus;
                                        // this will auto-release
                                        analyte.SetResultValue(i.ResultValue, false);
                                        // If CType(analyte.Analyte, RefAnalyte).AutoRelease Then 'If it is autorelease mark it as release.
                                        // analyte.MarkAsReleased()
                                        // End If
                                    }
                                    // Component of an ordered code where another component has been changed
                                    if (!(analyte == null) && !i.OrderedTestCode.IsNothingOrEmpty() && m_changeOrderedCodes.Contains(i.OrderedTestCode))
                                    {
                                        // Re-release this component
                                        analyte.MarkAsReleased();
                                    }
                                }
                            }
                        }

                        timer.Start();
                        var newTestsLock = new object();
                        Parallel.For(0, m_addTestCodes.Length, i =>
                        {
                            TestCodeStruct t = m_addTestCodes[i];
                            if (Report.AddByTestCodeAndOrderedCode(t.TestCode, t.OrderedTestCode, t.AccessioningFacility, t.PerformingFacility, t.SPMOrderTestId, spmStatus: t.SPMStatus))
                            {
                                lock (newTestsLock)
                                {
                                    newTests.Add(t.TestCode);
                                }
                                // Else
                                // Throw New Exception("Unable To add test.  Unable To retrieve test information from Test Master.  Test Code : " & t.TestCode)
                            }
                        });
                        timer.Stop();
                        elapsedTime = timer.ElapsedMilliseconds;
                        Log.InfoFormat(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedMs} ms",
                            "Tests",
                            "Update",
                            "Total time taken to add tests.",
                            elapsedTime); 
                        timer.Reset();

                        // we are qualifying the Report class here because there is another property in the OrderReport class
                        // named Report which point to m_report.  We want to call the Shared routine.
                        Report.CheckCalcPerfomingLocation(m_report);

                        if (m_report.IsValid && m_order.IsValid)
                        {

                            UpdateResultsFromTestCodeStruct();
                            UpdateResultsFromUpdatedTestCodeStruct();
                            UpdateTNPResultsFromTestCodeStruct();

                            // To re-release historical 6666(problem request) for TNP update
                            if (m_isTNPupdate.Equals(true))
                            {
                                var analyte = m_report.FindAnalyte("6666", true);
                                if (!(analyte == null) && analyte.TransmitStatus == transmitStatusType.StatusSentToVertex)
                                {
                                    analyte.MarkAsReleased();
                                    // analyte.ResultStatus = resultStatusType.Final
                                }
                            }

                            if (ProcessSpecimenCodes())
                                m_report.FlagDirty();
                            if (ProcessInstrumentQueryQueue())
                                m_report.FlagDirty();
                            if (ProcessParentTestCodes())
                                m_report.FlagDirty();

                            // 'We use the transaction scope when saving the Order and Report. If any failures, then both are rolled back.
                            var options = new TransactionOptions();
                            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
                            options.Timeout = new TimeSpan(0, 2, 0);

                            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
                            {
                                timer.Start();
                                m_order = (Order)m_order.Save();
                                timer.Stop();
                                elapsedTime = timer.ElapsedMilliseconds;
                                timer.Reset();
                                Log.DebugFormat("Order save took {0} milliseconds", elapsedTime);
                                timer.Start();
                                m_report = (Report)m_report.Save();
                                timer.Stop();
                                elapsedTime = timer.ElapsedMilliseconds;
                                Log.DebugFormat("Report save took {0} milliseconds", elapsedTime);
                                scope.Complete();

                            }

                            if (newTests.Count > 0)
                            {
                                resultsMsg = string.Concat(" New testCodes added: ", string.Join(",", newTests.ToArray()));
                            }
                            m_status = new Status(StatusType.Warning, string.Concat("Report already exists.", resultsMsg), m_report);
                        }
                        else
                        {
                            m_status = new Status(StatusType.Warning, string.Concat("Report already exists: ", m_report.Rules.ToString(), " - ", m_order.Rules.ToString()), null);
                        }
                    }
                    else
                    {
                        m_status = new Status(StatusType.Warning, "Report already exists.", null);
                    }
                }

                if (m_status == null)
                {
                    m_status = new Status(StatusType.Success, "", m_report);
                }
            }

            catch (Exception ex)
            {

                Log.Error(ex.Message, ex);
                Trace.Write(ex.ToString());
                m_status = new Status(StatusType.Failure, ex.Message, null);

            }

        }

        private bool ProcessInstrumentQueryQueue()
        {
            Log.Debug($"ProcessInstrumentQueryQueue accession '{m_report.AccessionNbr}'");
            bool retVal = false;
            var iq = InstrumentQueries.Fetch(m_report.AccessionNbr, true);
            if (iq is null)
            {
                Log.Debug("instrument query is nothing");
                return retVal;
            }
            Log.DebugFormat("iq.Count {0}", iq.List.Count);
            foreach (InstrumentQuery q in iq.List)
            {
                Log.DebugFormat("Accession {0} SentToETS {1} QueryTime {2} Analyte Count {3}", q.AccessionNbr, q.SentToETS, q.QueryTime, q.AnalyteList.Count);
                if (q.SentToETS > DateTime.Parse("1900-01-01"))
                {
                    if (q.AnalyteList.Count == 0)
                    {
                        Log.Debug("checking analytes by name");
                        foreach (ReportAnalyte a in m_report.Analytes.List)
                        {
                            Log.DebugFormat("checking analyte={0}", a.Code);
                            foreach (TestInstrumentDTO item in ((RefAnalyte)a.Analyte).TestInstruments)
                            {
                                Log.DebugFormat("checking code {0} name {1}", item.InstrumentCode, item.InstrumentName);
                                if ((q.InstrumentName.ToLower() ?? "") == (item.InstrumentName.ToLower() ?? ""))
                                {
                                    Log.DebugFormat("analyte found, InstrumentLoadTime={0}", q.QueryTime);
                                    a.InstrumentLoadTime = Conversions.ToDate(q.QueryTime);
                                    q.MarkProcessed();
                                    q.Save();
                                    retVal = true;
                                }
                            }
                        }
                        Log.Debug("looping through panels");
                        foreach (ReportAnalytePanel p in m_report.AnalytePanels.List)
                        {
                            Log.DebugFormat("panel {0}", p.PanelCode);
                            foreach (ReportAnalyte a in p.Analytes.List)
                            {
                                Log.DebugFormat("checking analyte={0}", a.Code);
                                foreach (TestInstrumentDTO item in ((RefAnalyte)a.Analyte).TestInstruments)
                                {
                                    Log.DebugFormat("checking code {0} name {1}", item.InstrumentCode, item.InstrumentName);
                                    if ((q.InstrumentName.ToLower() ?? "") == (item.InstrumentName.ToLower() ?? ""))
                                    {
                                        Log.DebugFormat("setting analyte {0} with {1}", a.Code, q.QueryTime);
                                        a.InstrumentLoadTime = Conversions.ToDate(q.QueryTime);
                                        q.MarkProcessed();
                                        q.Save();
                                        retVal = true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.Debug("checking by analyte list");
                        foreach (string code in q.AnalyteList)
                        {
                            ReportAnalyte analyte = m_report.FindAnalyte(code, true);
                            if (analyte == null)
                            {
                                ReportAnalytePanel panel = m_report.FindAnalytePanel(code);
                                if (panel != null)
                                {
                                    foreach (ReportAnalyte a in panel.Analytes.List)
                                    {
                                        if (string.IsNullOrEmpty(a.InstrumentId))
                                        {
                                            Log.DebugFormat("setting panel {0} analyte {1} with {2}", panel.PanelCode, a.Code, q.QueryTime);
                                            a.InstrumentLoadTime = Conversions.ToDate(q.QueryTime);
                                        }
                                        q.MarkProcessed();
                                        q.Save();
                                        retVal = true;
                                    }
                                }
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(analyte.InstrumentId))
                                {
                                    Log.DebugFormat("setting analyte {0} with {1}", analyte, q.QueryTime);
                                    analyte.InstrumentLoadTime = Conversions.ToDate(q.QueryTime);
                                }
                                q.MarkProcessed();
                                q.Save();
                                retVal = true;
                            }
                        }
                    }
                }
            }
            return retVal;
        }

        private bool ProcessSpecimenCodes()
        {
            bool retVal = false;
            if (m_snapshot is null || m_snapshot.SpecimenCodes.Count == 0)
                return retVal;
            Log.Debug($"ProcessSpecimenCodes Specimens '{Snapshot.SpecimenCodes.Count}'");
            var testCodes = new List<string>();
            foreach (OrderSnapshot.SpecimenCode specimen in m_snapshot.SpecimenCodes)
            {
                if (!testCodes.Contains(specimen.TestCode))
                    testCodes.Add(specimen.TestCode);
            }
            foreach (string testCode in testCodes)
            {
                var specimens = new List<string>();
                foreach (OrderSnapshot.SpecimenCode specimen in Snapshot.SpecimenCodes)
                {
                    if ((specimen.TestCode ?? "") == (testCode ?? ""))
                    {
                        if (!specimens.Contains(specimen.Specimen))
                            specimens.Add(specimen.Specimen);
                    }
                }
                string specimenList = string.Join(",", specimens);
                Log.DebugFormat($"testcode {testCode} specimens {specimenList}");
                m_report.SetAnalyteSpecimenCodes(testCode, specimenList);
                return retVal;
            }

            return default;
        }

        private bool ProcessParentTestCodes()
        {
            bool retVal = false;
            if (m_snapshot is null)
                return retVal;
            Log.Debug($"ProcessParentTestCodes ParentTestCodes '{m_snapshot.ParentTestCodes.Count}'");
            foreach (ReportAnalytePanel p in m_report.AnalytePanels.List)
            {
                Log.Debug($"Panel Code={p.PanelCode}");
                foreach (OrderSnapshot.ParentTestCode ptc in m_snapshot.ParentTestCodes)
                {
                    if ((p.PanelCode ?? "") == (ptc.TestCode ?? ""))
                    {
                        Log.Debug($"Found Panel:{p.PanelCode}, Parent:{ptc.ParentTestCodeField}");
                        p.ParentTestCode = ptc.ParentTestCodeField;
                        Log.Debug($"--> Panel:{p.PanelCode}, Parent:{p.ParentTestCode}");
                        foreach (ReportAnalyte a in p.Analytes.List)
                        {
                            Log.Debug($"--> Analyte={a.Code} Parent={p.PanelCode}");
                            a.ParentTestCode = p.PanelCode;
                            retVal = true;
                        }
                        break;
                    }
                }
            }
            foreach (ReportAnalyte a in m_report.Analytes.List)
            {
                foreach (OrderSnapshot.ParentTestCode ptc in m_snapshot.ParentTestCodes)
                {
                    if ((a.Code ?? "") == (ptc.TestCode ?? ""))
                    {
                        Log.Debug($"Found Analyte:{a.Code}, Parent:{ptc.ParentTestCodeField}");
                        a.ParentTestCode = ptc.ParentTestCodeField;
                        retVal = true;
                        Log.Debug($"--> Analyte:{a.Code}, Parent:{a.ParentTestCode}");
                        break;
                    }
                }
            }
            return retVal;
        }

        private void UpdateTNPResultsFromTestCodeStruct()
        {

            if (m_changeTestCodes is not null)
            {
                foreach (TestCodeStruct t in m_changeTestCodes)
                {
                    if (!string.IsNullOrEmpty(t.ResultValue) && t.ResultValue.Trim().Equals("TNP"))
                    {
                        m_isTNPupdate = true;
                        var presumptive = new Report.UpdateIsPresumptive() { ProcessUpdate = true, IsPresumptiveHold = t.IsPresumptiveHold };
                        m_report.UpdateResultAndFacility(t.TestCode, t.ResultValue, t.PerformingFacility, t.AccessioningFacility, t.IsOnHold, t.Cmnt, Configuration.ToFollowEnabled, t.HoldType, t.OrderedTestCode, presumptive: presumptive);
                        // If t.OrderedTestCode <> "" Then
                        // If Not m_report.ProfileTNP.Contains(t.OrderedTestCode) Then
                        // m_report.ProfileTNP.Add(t.OrderedTestCode)
                        // End If
                        // End If
                    }
                }
            }

        }

        private void UpdateResultsFromUpdatedTestCodeStruct()
        {

            if (m_updateTestCodes is not null)
            {
                foreach (TestCodeStruct t in m_updateTestCodes)
                {
                    var presumptive = new Report.UpdateIsPresumptive() { ProcessUpdate = true, IsPresumptiveHold = t.IsPresumptiveHold };
                    m_report.UpdateResultAndFacility(t.TestCode, t.ResultValue, t.PerformingFacility, t.AccessioningFacility, t.IsOnHold, t.Cmnt, Configuration.ToFollowEnabled, t.HoldType, t.OrderedTestCode, presumptive: presumptive);
                }
            }

        }

        private void UpdateResultsFromTestCodeStruct()
        {

            ReportAnalyte an = null;
            foreach (TestCodeStruct t in m_addTestCodes)
            {
                var presumptive = new Report.UpdateIsPresumptive() { ProcessUpdate = true, IsPresumptiveHold = t.IsPresumptiveHold };
                // If Not String.IsNullOrEmpty(t.ResultValue) Then
                // NA: 1/2/2014: Send in the comment
                // <PRE-ITBT-2765>m_report.UpdateResultAndFacility(t.TestCode, t.ResultValue, t.PerformingFacility, t.AccessioningFacility, t.IsOnHold, t.Cmnt, My.Settings.ToFollowEnabled, t.HoldType)
                m_report.UpdateResultAndFacility(t.TestCode, t.ResultValue, t.PerformingFacility, t.AccessioningFacility, t.IsOnHold, t.Cmnt, Configuration.ToFollowEnabled, t.HoldType, t.OrderedTestCode, presumptive);   // <ITBT-2765>
                                                                                                                                                                                                                                             // End If
            }

        }

        #endregion

        [Serializable()]
        public class TestCodeStruct
        {
            public string TestCode = "";
            public string OrderedTestCode = "";
            public string ResultValue = "";
            public string OrderedTestName = "";
            public string PerformingFacility = "";
            public string AccessioningFacility = "";
            public bool IsOnHold = false;
            public string Cmnt = "";
            public long SPMOrderTestId = 0;
            public HoldType HoldType = HoldType.None;
            public bool IsPresumptiveHold = false;
            public SPMStatusValue SPMStatus;
        }

    }
}