using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Common.Client;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderManager : DataClassBase
    {

        public enum StatusType
        {
            Success = 0,
            Warning = 1,
            Failure = 2
        }


        #region Private Members

        internal bool m_orderExists = false;
        internal OrderWorksheetInfo m_orderWorksheetInfo = null;
        internal List<string> m_OpenRREList = new List<string>();
        internal List<string> m_clinicalTrialsReportIds = new List<string>();
        private const int MAXCOMMENTLINELENGTH = 78;
        private static Report m_report = null;
        private static readonly ILog Log = LogManager.GetLogger<OrderManager>();

        #endregion

        #region Shared Functions

        public static bool OrderExists(string accessionNbr)
        {
            return ((OrderManager)DataFactory.Fetch(new Criteria(accessionNbr))).m_orderExists;
        }

        public static bool OrderExists(string accessionNbr, DateTime dateOfService, long spmOrderId)
        {
            return ((OrderManager)DataFactory.Fetch(new Criteria(accessionNbr, dateOfService.ToShortDateString(), spmOrderId))).m_orderExists;
        }

        public static OrderWorksheetInfo FetchOrderWorksheet(string accessionNbr, rackWorksheetType @type)
        {

            return FetchOrderWorksheet(accessionNbr, type, "0159");

        }

        public static OrderWorksheetInfo FetchOrderWorksheet(string accessionNbr, rackWorksheetType @type, string panelCodes)
        {

            return ((OrderManager)DataFactory.Fetch(new Criteria(accessionNbr, type, panelCodes))).m_orderWorksheetInfo;

        }

        // Public Shared Function FetchOpenRRECount(ByVal rapidResultId As Integer, ByVal accessionNbr As String, ByVal rapidResultTempateId As Integer) As Integer
        // Return CType(DataFactory.Fetch(New Criteria(rapidResultId, accessionNbr, rapidResultTempateId)), OrderManager).m_OpenRRECount
        // End Function

        public static List<string> FetchOpenRREList(int rapidResultId, string accessionNbr, int rapidResultTempateId)
        {
            return ((OrderManager)DataFactory.Fetch(new Criteria(rapidResultId, accessionNbr, rapidResultTempateId))).m_OpenRREList;
        }

        public static List<string> FetchClinicalTrailsReportIDs(DateTime startDate, DateTime endDate, string clientIds, string testCodes)
        {
            return ((OrderManager)DataFactory.Fetch(new Criteria(startDate, endDate, clientIds, testCodes))).m_clinicalTrialsReportIds;
        }

        public static DemographicUpdateStatus UpdateDemographics(string accessionNbr, string dateOfBirth, Common.Gender gender)
        {

            var releaseTests = new List<string>();
            bool updatedGender = default, updatedDob = default;
            var o = Order.Fetch(accessionNbr, false);

            if (o == null)
            {
                return new DemographicUpdateStatus();
            }
            if (!(gender == null) && o.Patient.Gender != gender)
            {
                o.Patient.Gender = gender;
                updatedGender = true;
            }
            if (!(dateOfBirth == null) && (o.Patient.DateOfBirth ?? "") != (dateOfBirth ?? ""))
            {
                o.Patient.DateOfBirth = dateOfBirth;
                updatedDob = true;
            }

            if (updatedDob || updatedGender)
            {
                o.Save();

                var r = Report.Fetch(accessionNbr);

                foreach (ReportAnalyte a in r.Analytes.List)
                {
                    if (AnalyteNeedsRelease(a, updatedDob, updatedGender, gender))
                    {
                        releaseTests.Add(a.Code);
                        a.MarkAsReleased();
                    }

                }
                foreach (ReportAnalytePanel p in r.AnalytePanels.List)
                {
                    foreach (ReportAnalyte a in p.Analytes.List)
                    {
                        if (AnalyteNeedsRelease(a, updatedDob, updatedGender, gender))
                        {
                            releaseTests.Add(p.PanelCode);
                            p.MarkAsReleased();
                            break;
                        }
                    }
                }

                if (releaseTests.Count > 0)
                {
                    r.Save();
                    return new DemographicUpdateStatus(releaseTests.ToArray());
                }

            }

            return new DemographicUpdateStatus();

        }

        private static bool AnalyteNeedsRelease(ReportAnalyte a, bool updatedDob, bool updatedGender, Common.Gender gender)
        {
            // in certain instances we do not want to re-release based on demo updates.
            // I.e. TOX.
            if (Configuration.LISSettings.GetList("BypassCatOnDemoUpdate").Contains(a.Analyte.Category.ToUpper()))
            {
                return false;
            }

            if (a.HasBeenReleased() && a.Analyte.ResultFlagRanges.Length > 0)
            {
                foreach (ComplexResultFlag c in a.Analyte.ResultFlagRanges)
                {
                    if (updatedDob && c.UseAgeQualifier)
                    {
                        return Conversions.ToBoolean(a.Code);
                        continue;
                    }
                    if (updatedGender && c.Gender != genderFlagType.Both && (int)c.Gender != (int)gender)
                    {
                        return Conversions.ToBoolean(a.Code);
                    }
                }

            }

            return default;

        }


        public static Statuses ReleaseResults(string accessionNbr, DateTime dateOfService, string testCode)
        {

            return ReleaseResults(accessionNbr, dateOfService, new string[] { testCode });

        }

        public static Statuses ReleaseResults(string accessionNbr, DateTime dateOfService, string[] testCodes)
        {

            var s = new Statuses();
            ReportAnalyte analyte;
            ReportAnalytePanel panel;

            if (accessionNbr.Trim().Equals(""))
            {
                s.List.Add(new Status(StatusType.Failure, "Accession Number cannot be blank.", null));
                return s;
            }

            var r = FetchReport(accessionNbr, dateOfService);
            if (r == null)
            {
                s.List.Add(new Status(StatusType.Failure, string.Concat("Accession #", accessionNbr, " does not exist."), null));
                return s;
            }
            else if (!r.DateServiced.ToShortDateString().Equals(dateOfService.ToShortDateString()))
            {
                s.List.Add(new Status(StatusType.Failure, string.Concat("Accession was found with service date of '", r.DateServiced.ToShortDateString(), "', not supplied date of '", dateOfService.ToShortDateString(), "'"), null));
                return s;
            }
            else
            {

                if (r.IsCOC)
                {
                    // For COC, we don't want to user MarkAsReleased.  We want to reset the transmit status back to Released and save.
                    // Because MarkAsReleased will wipe out the Approver information.

                    foreach (string t in testCodes)
                    {

                        try
                        {
                            analyte = r.FindAnalyte(t, true);
                            if (!(analyte == null))
                            {
                                analyte.SetTransmitStatus(transmitStatusType.Released);
                                // analyte.MarkAsReleased()
                                analyte.ReRelease(true);
                                s.List.Add(new Status(StatusType.Success, string.Concat("Test code '", t, "' was successfully released."), null));
                            }
                            else
                            {
                                panel = r.FindAnalytePanel(t);
                                if (!(panel == null))
                                {
                                    // panel.MarkAsReleased()
                                    panel.ReRelease(true);
                                    s.List.Add(new Status(StatusType.Success, string.Concat("Test code '", t, "' was successfully released."), null));
                                }
                                else
                                {
                                    s.List.Add(new Status(StatusType.Failure, string.Concat("Test code '", t, "' was not found."), null));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            s.List.Add(new Status(StatusType.Failure, string.Concat("Error releasing '", t, "': ", ex.Message), null));
                        }

                    }
                }
                else
                {
                    foreach (string t in testCodes)
                    {

                        try
                        {
                            analyte = r.FindAnalyte(t, true);
                            if (!(analyte == null))
                            {
                                analyte.MarkAsReleased();
                                s.List.Add(new Status(StatusType.Success, string.Concat("Test code '", t, "' was successfully released."), null));
                            }
                            else
                            {
                                panel = r.FindAnalytePanel(t);
                                if (!(panel == null))
                                {
                                    panel.MarkAsReleased();
                                    s.List.Add(new Status(StatusType.Success, string.Concat("Test code '", t, "' was successfully released."), null));
                                }
                                else
                                {
                                    s.List.Add(new Status(StatusType.Failure, string.Concat("Test code '", t, "' was not found."), null));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            s.List.Add(new Status(StatusType.Failure, string.Concat("Error releasing '", t, "': ", ex.Message), null));
                        }

                    }
                }

                try
                {
                    r.Save();
                }
                catch (Exception ex)
                {
                    s.List.Add(new Status(StatusType.Failure, string.Concat("Error saving Accession #", accessionNbr, " : ", ex.Message), null));
                }


            }

            return s;

        }

        #endregion

        #region OrderWorksheetInfo Class
        [Serializable()]
        public class OrderWorksheetInfo
        {
            private string m_code;
            private resultStatusType m_status;
            private bool m_has4ktest;
            public OrderWorksheetInfo(string code, resultStatusType status, bool has4ktest)
            {
                m_code = code;
                m_status = status;
                m_has4ktest = has4ktest;
            }
            public string Code
            {
                get
                {
                    return m_code;
                }
            }
            public resultStatusType Status
            {
                get
                {
                    return m_status;
                }
            }
            public bool Has4kTest
            {
                get
                {
                    return m_has4ktest;
                }
            }
        }
        #endregion

        #region Criteria Class
        [Serializable()]
        public class Criteria
        {
            private string m_accessionNbr = "";
            private string m_dateServiced = "";
            private int m_spmOrderId = 0;
            private rackWorksheetType m_worksheetType;
            private string m_panelCodes = "";
            private int m_rapidResultsTemplateId;
            private int m_rapidResultId;
            private bool m_searchRRE = false;
            public DateTime m_startDate;
            public DateTime m_endDate;
            public string m_clientIds = "";
            public string m_testCodes = "";

            public Criteria(string accessionNbr, rackWorksheetType @type = rackWorksheetType.NotSet, string panelCodes = "0159")
            {
                m_accessionNbr = accessionNbr;
                m_worksheetType = type;
                m_panelCodes = panelCodes;
                m_searchRRE = false;
            }
            public Criteria(string accessionNbr, string dateServiced)
            {
                m_worksheetType = rackWorksheetType.NotSet;
                m_accessionNbr = accessionNbr;
                m_dateServiced = dateServiced;
            }
            public Criteria(string accessionNbr, string dateServiced, long spmOrderId)
            {
                m_worksheetType = rackWorksheetType.NotSet;
                m_accessionNbr = accessionNbr;
                m_dateServiced = dateServiced;
                m_spmOrderId = (int)spmOrderId;
            }
            public Criteria(int rapidResId, string accessionNbr, int rapidResTemplateId)
            {
                m_searchRRE = true;
                m_accessionNbr = accessionNbr;
                m_rapidResultId = rapidResId;
                m_rapidResultsTemplateId = rapidResTemplateId;
            }
            public Criteria(int spmOrderId)
            {
                m_worksheetType = rackWorksheetType.NotSet;
                m_spmOrderId = spmOrderId;
            }

            public Criteria(DateTime startDate, DateTime endDate, string clientIDs, string testCodes)
            {

                m_startDate = startDate;
                m_endDate = endDate;
                m_clientIds = clientIDs;
                m_testCodes = testCodes;
            }

            public string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }
            public string DateServiced
            {
                get
                {
                    return m_dateServiced;
                }
            }
            public int SPMOrderId
            {
                get
                {
                    return m_spmOrderId;
                }
            }
            public rackWorksheetType WorksheetType
            {
                get
                {
                    return m_worksheetType;
                }
            }
            public string PanelCodes
            {
                get
                {
                    return m_panelCodes;
                }
            }
            public int RapidResultId
            {
                get
                {
                    return m_rapidResultId;
                }
            }
            public int RapidResultTemplateId
            {
                get
                {
                    return m_rapidResultsTemplateId;
                }
            }
            public bool SearchRRE
            {
                get
                {
                    return m_searchRRE;
                }
            }

            public DateTime StartDate
            {
                get
                {
                    return m_startDate;
                }
            }

            public DateTime EndDate
            {
                get
                {
                    return m_endDate;
                }
            }

            public string ClientIDs
            {
                get
                {
                    return m_clientIds;
                }
            }

            public string TestCodes
            {
                get
                {
                    return m_testCodes;
                }
            }
        }
        #endregion

        #region Data Function

        protected override void DataFactory_Fetch(object criteria)
        {
            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            DataTable[] dt;
            try
            {

                if (c.SearchRRE)
                {
                    var @params = new DbParameter[3];
                    @params[0] = (DbParameter)da.CreateParameter("@AccessionNbrList", DbType.String, c.AccessionNbr);
                    @params[1] = (DbParameter)da.CreateParameter("@RapidResultId", DbType.Int32, c.RapidResultId);
                    @params[2] = (DbParameter)da.CreateParameter("@RapidResultTemplateId", DbType.String, c.RapidResultTemplateId);
                    dt = da.ExecuteProcedure("lis_RapidResultsOpen_Fetch", @params);
                    m_OpenRREList = new List<string>();
                    foreach (DataRow dr in dt[0].Rows)
                        m_OpenRREList.Add(Conversions.ToString(dr["AccessionNbr"]));
                }
                else if (!string.IsNullOrEmpty(c.ClientIDs))
                {
                    var @params = new DbParameter[4];
                    @params[0] = (DbParameter)da.CreateParameter("@TestCode", DbType.String, c.TestCodes);
                    @params[1] = (DbParameter)da.CreateParameter("@FromResultDate", DbType.DateTime, c.StartDate);
                    @params[2] = (DbParameter)da.CreateParameter("@ToResultDate", DbType.DateTime, c.EndDate);
                    @params[3] = (DbParameter)da.CreateParameter("@ClientID", DbType.String, c.ClientIDs);

                    dt = da.ExecuteProcedure("lis_Reports_ClinicalTrial_Fetch", @params);
                    m_clinicalTrialsReportIds = new List<string>();
                    foreach (DataRow dr in dt[0].Rows)
                        m_clinicalTrialsReportIds.Add(Conversions.ToString(dr["ReportId"]));
                }
                else if (c.WorksheetType == rackWorksheetType.NotSet)
                {
                    var @params = new List<DbParameter>();
                    @params.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr));
                    if (!string.IsNullOrEmpty(c.DateServiced))
                    {
                        @params.Add((DbParameter)da.CreateParameter("@DateServiced", DbType.DateTime, c.DateServiced));
                    }
                    @params.Add((DbParameter)da.CreateParameter("@SPMOrderId", DbType.Int64, c.SPMOrderId));
                    Log.DebugFormat("OrderExists @AccessionNbr='{0}'; @DateServiced='{1}'; @SPMOrderId='{2}'", c.AccessionNbr, c.DateServiced, c.SPMOrderId);
                    dt = da.ExecuteProcedure("lis_OrderExists_Fetch", @params.ToArray());
                    Log.DebugFormat("OrderExists RowCount={0}", dt[0].Rows.Count);
                    if (dt[0].Rows.Count > 0)
                    {
                        m_orderExists = true;
                    }
                }
                else
                {
                    var @params = new DbParameter[3];
                    @params[0] = (DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr);
                    @params[1] = (DbParameter)da.CreateParameter("@WorksheetType", DbType.Int32, c.WorksheetType);
                    @params[2] = (DbParameter)da.CreateParameter("@PanelCode", DbType.String, c.PanelCodes);
                    dt = da.ExecuteProcedure("lis_OrderWorksheet_Fetch", @params);
                    if (dt[0].Rows.Count > 0)
                    {
                        m_orderWorksheetInfo = new OrderWorksheetInfo(dt[0].Rows[0]["Code"].ToString(), (resultStatusType)Conversions.ToInteger(dt[0].Rows[0]["ResultStatus"]), Conversions.ToBoolean(dt[0].Rows[0]["Has4kTest"]));
                    }
                }
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                throw;
            }

        }

        #endregion

        #region Public Shared Functions

        public static Order FetchOrder(long orderID)
        {

            var o = Order.Fetch(orderID, false);
            return (Order)Interaction.IIf(o.ID == 0L, null, o);

        }

        public static Order FetchOrder(string accessionNbr)
        {

            var o = Order.Fetch(accessionNbr, false);
            return (Order)Interaction.IIf(o.ID == 0L, null, o);

        }

        public static Order FetchOrder(string accessionNbr, DateTime dateOfService)
        {

            var o = Order.Fetch(accessionNbr, dateOfService, false);
            return (Order)Interaction.IIf(o.ID == 0L, null, o);

        }

        public static Order FetchOrder(string accessionNbr, bool loadAudit)
        {

            var o = Order.Fetch(accessionNbr, loadAudit);
            return (Order)Interaction.IIf(o.ID == 0L, null, o);

        }

        public static Order FetchOrder(long orderID, bool loadAudit)
        {

            var o = Order.Fetch(orderID, loadAudit);
            return (Order)Interaction.IIf(o.ID == 0L, null, o);

        }

        public static Order CreateOrder(string accountNbr)
        {

            var order = Order.Create(accountNbr);
            if (order.ID == 0L)
            {
                return null;
            }
            else
            {
                return order;
            }

        }

        public static Order CreateOrder(Account account)
        {

            var order = Order.Create(account);
            if (order.ID == 0L)
            {
                return null;
            }
            else
            {
                return order;
            }

        }

        /// <summary>
    /// Returns a Status object with the Order as the ReturnValue. If an existing Order exists with the specified accession number,
    /// will return that Order if returnExisting = True
    /// </summary>
    /// <param name="accountNbr"></param>
    /// <param name="accessionNbr"></param>
    /// <param name="returnExisting"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static Status CreateOrder(string accountNbr, string accessionNbr, bool returnExisting = false)
        {

            return CreateOrder(accountNbr, accessionNbr, "", Conversions.ToInteger(returnExisting));

        }

        public static Status CreateOrder(string accountNbr, string accessionNbr, string dateOfService, int spmOrderId, bool returnExisting = false)
        {
            try
            {
                Status s = null;
                Order o;
                string msg = "";
                DateTime dos;

                Log.DebugFormat($"Create/Retrieve Order - Account:'{0}' Accession:'{1}' DOS:'{2}' SPMOrderId:'{3}' ReturnExisting:'{4}'", accountNbr, accessionNbr, dateOfService, spmOrderId, returnExisting);

                // get some initial validation out of the way

                if (!DateTime.TryParse(dateOfService, out dos) || dos == DateTime.Parse("1900-01-01"))
                {
                    Log.InfoFormat("Failed Invalid DOS");
                    msg = "Unable to create an Order with an invalid DOS.";
                    return new Status(StatusType.Failure, msg, null);
                }
                if (string.IsNullOrEmpty(accountNbr.Trim()))
                {
                    Log.InfoFormat("Failed. Blank AccountNbr.");
                    msg = "Unable to create an Order with a blank AccountNbr.";
                    return new Status(StatusType.Failure, msg, null);
                }
                if (string.IsNullOrEmpty(accessionNbr.Trim()))
                {
                    Log.InfoFormat("Failed. Blank accession number.");
                    msg = "Unable to create an Order with a blank AccessionNbr.";
                    return new Status(StatusType.Failure, msg, null);
                }

                // if the accession number was never used before, create it.
                Log.InfoFormat("Search by AccessionNbr.");
                if (!OrderExists(accessionNbr))
                {
                    Log.Debug("AccessionNbr not found. Creating new.");
                    return NewOrder(accountNbr, accessionNbr);
                }

                // search for existing order
                if (DateTime.TryParse(dateOfService, out dos))
                {
                    Log.Debug("Search by AccessionNbr and DOS");
                    if (OrderExists(accessionNbr, dos, spmOrderId) & returnExisting)
                    {
                        Log.Debug("Accession Exists");
                        o = Order.Fetch(accessionNbr, dos, spmOrderId, false);
                        if (o is not null)
                        {
                            Log.InfoFormat("Accession found, OrderId:'{0}'. Returning.", o.ID);
                            msg = $"An order with accession #{accessionNbr} already exists.";
                            return new Status(StatusType.Warning, msg, o);
                        }
                        Log.Debug("Accession not fetched.");
                    }
                }

                // create new order if enough time has passed
                if (OrderExists(accessionNbr))
                {
                    o = Order.Fetch(accessionNbr, false);
                    if (!string.IsNullOrEmpty(dateOfService) && o.DateOfService != DateTime.Parse("1900-01-01") && (o.DateOfService.ToShortDateString() ?? "") != (DateTime.Parse(dateOfService).ToShortDateString() ?? ""))
                    {
                        var newDOS = Convert.ToDateTime(dateOfService);
                        if (o.DateOfService.AddDays(Configuration.LISSettings.GetInt("DuplicateAccessionTimeSpan")) < newDOS)
                        {
                            var order = Order.Create(accountNbr);
                            order.AccessionNbr = accessionNbr;
                            order = (Order)order.Save();
                            s = new Status(StatusType.Success, "", order);
                        }
                        else
                        {
                            s = new Status(StatusType.Failure, string.Format("An order with accession #{0} already exists with DOS of {1}. New DOS failed: {2}", accessionNbr, o.DateOfService.ToShortDateString(), dateOfService), null);
                        }
                        return s;
                    }
                    if (returnExisting)
                    {
                        s = new Status(StatusType.Warning, string.Format("An order with accession #{0} already exists.", accessionNbr), o);
                    }
                    else
                    {
                        s = new Status(StatusType.Failure, string.Format("An order with accession #{0} already exists.", accessionNbr), null);
                    }
                }
                else
                {
                    var order = Order.Create(accountNbr);
                    order.AccessionNbr = accessionNbr;
                    order = (Order)order.Save();
                    s = new Status(StatusType.Success, "", order);
                }
                return s;
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new Status(StatusType.Failure, $"Error creating new order with accession #{accessionNbr}.", null);
            }

        }

        private static DateTime BaseDate(DateTime d)
        {
            return new DateTime(d.Year, d.Month, d.Day);
        }

        private static DateTime BaseDate(string s)
        {
            DateTime d;
            if (!DateTime.TryParse(s, out d))
                d = DateTime.Parse("1900-01-01");
            return new DateTime(d.Year, d.Month, d.Day);
        }

        // Public Shared Function CreateOrder(ByVal accountNbr As String, ByVal accessionNbr As String, ByVal dateOfService As String, Optional ByVal returnExisting As Boolean = False) As OrderManager.Status

        // If accountNbr.Trim() = "" Then
        // Return New Status(StatusType.Failure, "Unable to create an Order with a blank accountNbr.", Nothing)
        // End If

        // If accessionNbr.Trim() = "" Then
        // Return New Status(StatusType.Failure, "Unable to create an Order with a blank accessionNbr.", Nothing)
        // End If

        // Dim s As Status = Nothing
        // Dim o As Order = Nothing
        // 'Check if exists

        // 'SharedFunctions.DebugWrite(String.Concat(accessionNbr, " Check order exists..."), "OrderManager")
        // If OrderManager.OrderExists(accessionNbr) Then

        // 'SharedFunctions.DebugWrite(String.Concat(accessionNbr, "Order Exists - Fetching order..."), "OrderManager")
        // o = Order.Fetch(accessionNbr, False)
        // If Not String.IsNullOrEmpty(dateOfService) AndAlso o.DateOfService <> SharedFunctions.NoDate AndAlso o.DateOfService.ToShortDateString() <> DateTime.Parse(dateOfService).ToShortDateString() Then
        // 'NA 10/6 permit duplicate Accession if the DOS is Greater than default timespan
        // Dim newDOS As Date = Convert.ToDateTime(dateOfService)
        // If o.DateOfService.AddDays(Configuration.Settings.DuplicateAccessionTimeSpan) < newDOS Then
        // 'create new order
        // 'SharedFunctions.DebugWrite(String.Concat(accessionNbr, "New Order - Creating..."), "OrderManager")
        // Dim order As Order = Order.Create(accountNbr)
        // order.AccessionNbr = accessionNbr

        // 'AG 4/9/09 - We must save the order to guarantee the accession isn't used again.
        // order = order.Save()
        // s = New Status(StatusType.Success, "", order)
        // Else
        // '
        // s = New Status(StatusType.Failure, String.Format("An order with accession #{0} already exists with DOS of {1}. New DOS failed: {2}", accessionNbr, o.DateOfService.ToShortDateString(), dateOfService), Nothing)
        // End If
        // 's = New Status(StatusType.Failure, String.Format("An order with accession #{0} already exists with DOS of {1}. New DOS failed: {2}", accessionNbr, o.DateOfService.ToShortDateString(), dateOfService), Nothing)
        // Return s
        // End If

        // If returnExisting Then
        // s = New Status(StatusType.Warning, String.Format("An order with accession #{0} already exists.", accessionNbr), o)
        // Else
        // s = New Status(StatusType.Failure, String.Format("An order with accession #{0} already exists.", accessionNbr), Nothing)
        // End If
        // Else
        // 'SharedFunctions.DebugWrite(String.Concat(accessionNbr, "New Order - Creating..."), "OrderManager")
        // Dim order As Order = Order.Create(accountNbr)
        // order.AccessionNbr = accessionNbr

        // 'AG 4/9/09 - We must save the order to guarantee the accession isn't used again.
        // order = order.Save()
        // s = New Status(StatusType.Success, "", order)
        // End If

        // Return s

        // End Function

        private static Status NewOrder(string accountNbr, string accessionNbr)
        {
            var order = Order.Create(accountNbr);
            order.AccessionNbr = accessionNbr;
            order = (Order)order.Save();
            return new Status(StatusType.Success, "", order);
        }

        public static Report FetchReport(string accessionNbr)
        {
            // SharedFunctions.DebugWrite("Report fetch...", "OrderManager.FetchReport")
            var oRes = Report.Fetch(accessionNbr);
            // SharedFunctions.DebugWrite("Report fetch complete...", "OrderManager.FetchReport")
            return (Report)Interaction.IIf(oRes.ID == 0L, null, oRes);

        }

        public static Report FetchReport(string accessionNbr, DateTime dateServiced)
        {
            var r = Report.Fetch(accessionNbr, dateServiced);
            return (Report)Interaction.IIf(r.ID == 0L, null, r);
        }

        public static Report FetchReport(string accessionNbr, DateTime dateServiced, Order referenceOrder)
        {
            var r = Report.Fetch(accessionNbr, dateServiced, referenceOrder);
            return (Report)Interaction.IIf(r.ID == 0L, null, r);
        }

        public static Report FetchReportWithState(string accessionNbr)
        {
            // SharedFunctions.DebugWrite("Report fetch...", "OrderManager.FetchReport")
            m_report = Report.Fetch(accessionNbr);
            // SharedFunctions.DebugWrite("Report fetch complete...", "OrderManager.FetchReport")
            return (Report)Interaction.IIf(m_report.ID == 0L, null, m_report);

        }

        public static Report FetchReport(int reportID)
        {

            var oRes = Report.Fetch(reportID);
            return (Report)Interaction.IIf(oRes.ID == 0L, null, oRes);
            return null;

        }

        public static Reports FetchReports(transmitStatusType status, int outboundChannelId = 0)
        {

            return Reports.Fetch(0, status, outboundChannelId);

        }


        /// <summary>
    /// Creates the Report for the given order and attempts to save it.
    /// If Results already exists, Status will report as an error and return existing Report.
    /// If addNewCodes = True, existing Report will return with any new OrderTests that have been added to the report.
    /// </summary>
    /// <param name="order"></param>
    /// <param name="addNewCodes">If a Report exists for the existing order, this specifies that any new OrderTests added
    /// to the Order will be created in the Report.</param>
    /// <param name="onlyNewCodes">If a Report does not already exist, only create OrderTests that were added to the 
    /// Order after the Order was created or fetched.</param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static Status CreateReport(Order order, bool addNewCodes = false, bool onlyNewCodes = true)
        {

            try
            {

                string resultsMsg = "";

                if (order.IsDirty || order.IsNew)
                {
                    return new Status(StatusType.Failure, "Cannot create Report for an Order that is Dirty or is New.", null);
                }

                Report r = null;
                // r = OrderManager.FetchReport(order.AccessionNbr)

                // SharedFunctions.DebugWrite(String.Concat(order.AccessionNbr, " Check Report Exists..."), "Order Manager")
                // If Not ReportSearch.ReportExists(order.AccessionNbr) Then
                if (!ReportSearch.ReportExists(order.AccessionNbr, (int)order.ID))
                {
                    Log.Debug("Fetch Unsolicited (OM)");
                    UnsolicitedMessage.FetchUnProcessed(order.AccessionNbr);
                    Log.Debug($"Unsolicited Count {UnsolicitedMessage.List.Count}");

                    foreach (UnsolicitedMessage um in UnsolicitedMessage.List)
                    {
                        //For now just mark as processed and move on.
                        //string messageFile = $"{order.AccessionNbr}-{Guid.NewGuid()}.hl7";
                        //string messagePath = Path.Combine(Configuration.LISSettings.GetString("UnsolicitedMessageDropOff"), messageFile);
                        //File.WriteAllText(messagePath, um.HL7Message);
                        //Log.Debug($"File Created {messagePath}");
                        MessageProducerApiClient.Produce(MessageType.UnsolicitedMessage.ToString(), um.HL7Message, um.AccessionNbr);
                        um.MarkProcessed();
                        um.Save();
                    }
                    return new Status(StatusType.Success, resultsMsg, r);

                }
                else
                {
                    var newTests = new List<string>();
                    if (addNewCodes)
                    {
                        r = FetchReport(order.AccessionNbr, order.DateOfService);
                        foreach (OrderTest t in order.Tests.List)
                        {
                            // Make sure that only adding codes (if onlyNewCodes = True) that have just been added to the order
                            // and not those that have been pulled from the database.
                            if (onlyNewCodes && t.m_loadedFromDatabase == false || onlyNewCodes == false)
                            {
                                if (r.FindAnalytePanel(t.TestCode) == null && r.FindAnalyte(t.TestCode, true) == null)
                                {
                                    r.AddByTestCodeAndOrderedCode(t.TestCode, t.OrderedTestCode, spmStatus: t.SPMStatus);
                                    newTests.Add(t.TestCode);
                                }
                            }
                        }

                        Report.CheckCalcPerfomingLocation(r);

                        if (r.IsValid)
                        {
                            r = (Report)r.Save();
                            if (newTests.Count > 0)
                            {
                                resultsMsg = string.Concat(" New testCodes added: ", string.Join(",", newTests.ToArray()));
                            }
                            return new Status(StatusType.Warning, string.Concat("Report already exists.", resultsMsg), r);
                        }
                        else
                        {
                            return new Status(StatusType.Warning, string.Concat("Report already exists: ", r.Rules.ToString()), null);
                        }
                    }
                    else
                    {
                        return new Status(StatusType.Success, "Report already exists.", null);
                    }
                }
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Trace.Write(ex.ToString());
                return new Status(StatusType.Failure, ex.Message, null);

            }

        }

        /// <summary>
    /// Used to set one analyte result value. Attempts to save data.
    /// </summary>
    /// <param name="accessionNbr"></param>
    /// <param name="panelCode"></param>
    /// <param name="analyteCode"></param>
    /// <param name="resultValue"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        [Obsolete("This method is outdated and should not be used.")]
        public static Status UpdateResult(string accessionNbr, string panelCode, string analyteCode, string resultValue, string[] resultFlagCodes = null)
        {

            return null;
            // Dim results As Report = OrderManager.FetchReport(accessionNbr)
            // Dim panel As ReportAnalytePanel = Nothing
            // Dim analyte As ReportAnalyte = Nothing

            // If Not IsNothing(results) Then
            // If panelCode <> "" Then
            // panel = results.AnalytePanels.Find(panelCode)
            // If Not IsNothing(panel) Then
            // analyte = panel.Analytes.FindFirst(analyteCode)
            // Else
            // Return New Status(StatusType.Failure, String.Format("Panel does not exist with code {0} for Accession Number {1}.", panelCode, accessionNbr), Nothing)
            // End If
            // Else
            // analyte = results.Analytes.FindFirst(analyteCode)
            // End If

            // If Not IsNothing(analyte) Then

            // ''If the analyte has a result, do not reset it! For now.
            // '*******************************************************
            // If analyte.ResultValue = "" Then
            // analyte.ResultValue = resultValue

            // Dim alert As Alert
            // If Not IsNothing(resultFlagCodes) Then
            // If Not IsNothing(resultFlagCodes) Then
            // 'Fetch Error Flags from TestMaster
            // Dim alerts As Alerts = alerts.Fetch()
            // For Each code As String In resultFlagCodes
            // alert = alerts.List.Find(code)
            // If Not IsNothing(alert) Then
            // 'results.Flags.Add(flag)
            // analyte.Alerts.Add(alert)
            // Else
            // Return New Status(StatusType.Failure, String.Format("Unable to find Flag with code {0} for {1}.", code, analyteCode), Nothing)
            // End If
            // Next
            // End If
            // End If
            // End If

            // Else
            // Return New Status(StatusType.Failure, String.Format("Analyte does not exist with code {0}.", analyteCode), Nothing)
            // End If

            // If results.IsValid Then
            // results.Save()
            // Return New Status(StatusType.Success, "", analyte)
            // Else
            // Return New Status(StatusType.Failure, String.Format("Unable to save: {0}.", results.Rules.ToString()), results)
            // End If

            // Else
            // Return New Status(StatusType.Failure, String.Format("Results does not exist for Accession Number {0}.", accessionNbr), Nothing)
            // End If

        }

        private static void ClearAnalyte(ReportAnalyte analyte)
        {

            // If SharedFunctions.IsFinalOrCorrected(analyte.ResultStatus) Then
            // Throw New Exception(String.Concat("Code ", analyte.Code, " cannot be reset with a status of ", analyte.ResultStatus.ToString(), "."))
            // End If

            analyte.ResultValue = "";
            analyte.ResultDate = DateTime.Parse("1900-01-01");
            analyte.Instrument = "";
            analyte.InstrumentId = "";
            analyte.InstrumentAlt1 = "";
            analyte.InstrumentAlt2 = "";
            analyte.InstrumentAlt3 = "";
            analyte.SpecimenRackId = "";
            analyte.SpecimenRackPosition = "";
            analyte.SpecimenRackSequence = "";
            analyte.SpecimenAlt1 = "";
            analyte.SpecimenAlt2 = "";
            analyte.SetFlaggedValue("");
            analyte.ResultStatus = resultStatusType.Pending;

            analyte.Comments.DeleteAll();
            analyte.Alerts.DeleteAll();

        }



        /// <summary>
        /// Used to update Report with Results and save Report. If Report does not exist,
        /// UnsolicitedResults are created and saved. 
        /// </summary>
        /// <param name="updatedResults"></param>
        /// <param name="dynamicAdd">Dynamically add tests if they do not exist on the Report.</param>
        /// <param name="autoRelease">Automatically mark the results as released when updating results.</param>
        /// <returns>Returns a Report if exists, if not returns UnsolictedResults.</returns>
        /// <remarks></remarks>     

        public static Statuses UpdateResults(Results updatedResults, bool dynamicAdd = false, bool autoRelease = false, string hl7Message = null)

        {

            try
            {
                Log.DebugFormat("Enter UpdateResults: Accession='{0}', dynamicAdd={1}, autoRelease={2}, hl7MessagePresent={3}, resultCount={4}",
                    updatedResults?.AccessionNumber ?? "", dynamicAdd, autoRelease, !string.IsNullOrEmpty(hl7Message), updatedResults?.List?.Count ?? 0);

                var statuses = new Statuses();
                Report resultReport;

                Log.DebugFormat("Fetching Report ... m_report is null? {0}", m_report == null);
                if (m_report is null || (m_report.AccessionNbr ?? "") != (updatedResults.AccessionNumber ?? ""))
                {
                    Log.DebugFormat("Cached m_report not used or does not match accession. Fetching fresh Report for Accession '{0}'", updatedResults?.AccessionNumber ?? "");
                    resultReport = FetchReport(updatedResults.AccessionNumber);
                    m_report = null;
                    Log.DebugFormat("FetchReport returned: {0} (ID={1})", resultReport == null ? "null" : "Report", resultReport == null ? 0 : resultReport.ID);
                }
                else
                {
                    Log.DebugFormat("Using cached m_report for Accession '{0}'", updatedResults?.AccessionNumber ?? "");
                    resultReport = m_report;
                    m_report = null;
                }

                Log.DebugFormat("Completed Fetching Report ... ");
                var clearedCodes = new List<string>();
                var o = Order.Fetch(updatedResults.AccessionNumber, false);
                Log.DebugFormat("Fetched Order: Accession='{0}' OrderID={1}", updatedResults?.AccessionNumber ?? "", o?.ID ?? 0L);

                if (!(resultReport == null) && SharedFunctions.AccessionAgeInRange(resultReport.DateServiced))
                {
                    Log.DebugFormat("Processing results for Report Accession='{0}', DateServiced='{1}'", resultReport.AccessionNbr, resultReport.DateServiced);
                    foreach (Result r in updatedResults.List)
                    {
                        try
                        {
                            Log.DebugFormat("Processing Result: AnalyteCode='{0}', PanelCode='{1}', RefLabId={2}, Value='{3}', ResultDate='{4}', ForceUpdate={5}, ResetAll={6}, IsPrelim={7}, AutoReleaseResult={8}, CommentsCount={9}",
                                r?.AnalyteCode ?? "", r?.PanelCode ?? "", r?.ReferenceLabId ?? 0, r?.ResultValue ?? "", r?.ResultDate.ToString() ?? "", r?.ForceUpdate ?? false, r?.ResetAll ?? false, r?.IsPreliminaryRelease ?? false, r?.AutoReleaseResult ?? false, r?.Comments == null ? 0 : r.Comments.Length);

                            ReportAnalyte analyte = null;
                            ReportAnalytePanel panel = null;

                            if (r.AnalyteCode.Equals("SPCMT") && !(r.Comments == null) && !string.IsNullOrEmpty(r.Comments[0]))
                            {
                                Trace.Write(string.Concat(r.AnalyteCode, "-", r.Comments[0]));
                                Log.DebugFormat("Updating SpecimenComment for Accession '{0}' -> '{1}'", resultReport.AccessionNbr, r.Comments[0]);
                                resultReport.SpecimenComment = r.Comments[0];
                            }

                            else if (!string.IsNullOrEmpty(r.PanelCode))
                            {
                                Log.DebugFormat("PanelCode provided. Searching for panel '{0}' RefLabId={1}", r.PanelCode, r.ReferenceLabId);
                                panel = resultReport.AnalytePanels.Find(r.PanelCode, r.ReferenceLabId);
                                Log.DebugFormat("Panel find result: {0}", panel == null ? "not found" : $"found PanelCode={panel?.PanelCode}");

                                if (!(panel == null))
                                {
                                    Log.DebugFormat("Panel '{0}' exists. Panel.Status={1}", panel.PanelCode, panel.GetStatus());
                                    if (r.IsPreliminaryRelease && SharedFunctions.IsFinalOrCorrected(panel.GetStatus()))
                                    {
                                        var msg = string.Concat("Panel ", panel.PanelCode, "(" + r.AnalyteCode, ") cannot be updated with a prelimary result since it's status is ", panel.GetStatus(), ".");
                                        Log.Error(msg);
                                        throw new Exception(msg);
                                    }

                                    if (r.ResetAll && clearedCodes.Contains(panel.PanelCode) == false)
                                    {
                                        Log.DebugFormat("ResetAll true for panel '{0}'. Clearing panel comments and analytes.", panel.PanelCode);
                                        panel.Comments.DeleteAll();
                                        foreach (ReportAnalyte a in panel.Analytes.List)
                                            ClearAnalyte(a);
                                        clearedCodes.Add(panel.PanelCode);
                                    }

                                    if (string.IsNullOrEmpty(r.AnalyteCode))
                                    {
                                        Log.DebugFormat("Result is OBR placeholder (no analyte code). Adding comments to panel '{0}' if provided.", panel.PanelCode);
                                        if (!(r.Comments == null))
                                        {
                                            foreach (string c in r.Comments)
                                            {
                                                string formatted = SharedFunctions.FormatCommentText(c, MAXCOMMENTLINELENGTH);
                                                if (panel.Comments.FindByText(formatted) == null)
                                                {
                                                    Log.DebugFormat("Adding panel comment: '{0}'", formatted);
                                                    panel.Comments.AddComment(formatted);
                                                }
                                            }
                                        }
                                        continue;
                                    }

                                    analyte = panel.Analytes.FindFirst(r.AnalyteCode, r.ReferenceLabId);
                                    Log.DebugFormat("Panel.Analytes.FindFirst for Code='{0}' returned: {1}", r.AnalyteCode, analyte == null ? "null" : "found");

                                    if (!(analyte == null))
                                    {
                                        Log.DebugFormat("Calling panel.CheckProcessing() for panel '{0}'", panel.PanelCode);
                                        panel.CheckProcessing();
                                        Log.DebugFormat("Calling UpdateAnalyte for analyte '{0}' in panel '{1}'", analyte.Code, panel.PanelCode);
                                        UpdateAnalyte(analyte, r, statuses, resultReport, autoRelease);
                                        Log.DebugFormat("Completed UpdateAnalyte for analyte '{0}'. Current statuses count={1}", analyte.Code, statuses.List.Count);
                                    }
                                    else if (dynamicAdd)
                                    {
                                        Log.DebugFormat("DynamicAdd enabled and analyte '{0}' not found in panel '{1}'. Attempting dynamic add.", r.AnalyteCode, panel.PanelCode);
                                        if (r.ReferenceLabId == 0)
                                        {
                                            var ti = TestMasterWrapper.Fetch(r.AnalyteCode, order: ref o);
                                            Log.DebugFormat("TestMasterWrapper.Fetch('{0}') returned {1}", r.AnalyteCode, ti == null ? "null" : "TestInfo");
                                            if (!(ti == null))
                                            {
                                                var ra = panel.Analytes.Add(ti, "", (SPMStatusValue)Conversions.ToInteger(false));
                                                Log.DebugFormat("Added analyte to panel '{0}' via TestMaster. New analyte code (Report)='{1}'", panel.PanelCode, ra.Code);
                                                UpdateAnalyte(ra, r, statuses, resultReport, autoRelease);
                                            }
                                            else
                                            {
                                                var m = string.Format("Analyte does not exist with code {0} in TestMaster for dynamic addition. Accession # {1}.", r.AnalyteCode, updatedResults.AccessionNumber);
                                                Log.Warn(m);
                                                statuses.List.Add(new Status(StatusType.Failure, m, null));
                                            }
                                        }
                                        else
                                        {
                                            var ti = TestMasterWrapper.Fetch(r.AnalyteCode.TrimStart('0'), r.ReferenceLabId, ref o);
                                            Log.DebugFormat("TestMasterWrapper.Fetch('{0}', refLab={1}) returned {2}", r.AnalyteCode.TrimStart('0'), r.ReferenceLabId, ti == null ? "null" : "TestInfo");
                                            if (!(ti == null))
                                            {
                                                var ra = panel.Analytes.Add(ti, "", (SPMStatusValue)Conversions.ToInteger(false));
                                                Log.DebugFormat("Added analyte to panel '{0}' for RefLab {1}. New analyte code (Report)='{2}'", panel.PanelCode, r.ReferenceLabId, ra.Code);
                                                UpdateAnalyte(ra, r, statuses, resultReport, autoRelease);
                                            }
                                            else
                                            {
                                                var m = string.Format("Analyte does not exist with code {0} for RefLab {2} in TestMaster for dynamic addition. Accession # {1}.", r.AnalyteCode, updatedResults.AccessionNumber, r.ReferenceLabId);
                                                Log.Warn(m);
                                                statuses.List.Add(new Status(StatusType.Failure, m, null));
                                            }
                                        }
                                    }
                                    else if (r.ReferenceLabId == 0)
                                    {
                                        var m = string.Format("Unable to update analyte, does not exist in panel:{4}Accession#: {3}{4}Code: {0}{4}PanelCode: {2}{4}Value: {5}{4}RefLabId: {1}", r.AnalyteCode, r.ReferenceLabId, r.PanelCode, updatedResults.AccessionNumber, Constants.vbCrLf, r.ResultValue);
                                        Log.Warn(m);
                                        statuses.List.Add(new Status(StatusType.Warning, m, null));
                                    }
                                    else
                                    {
                                        var m = string.Format("Unable to update analyte, does not exist in panel:{4}Accession#: {3}{4}Code: {0}{4}PanelCode: {2}{4}Value: {5}{4}RefLabId: {1}", r.AnalyteCode, r.ReferenceLabId, r.PanelCode, updatedResults.AccessionNumber, Constants.vbCrLf, r.ResultValue);
                                        Log.Error(m);
                                        statuses.List.Add(new Status(StatusType.Failure, m, null));
                                    }
                                }

                                else if (dynamicAdd == false && r.ReferenceLabId != 0)
                                {
                                    Log.DebugFormat("Panel not found; dynamicAdd disabled; attempt to find analyte across report for RefLabId={0}", r.ReferenceLabId);
                                    analyte = resultReport.FindAnalyte(r.AnalyteCode, false, r.ReferenceLabId);
                                    Log.DebugFormat("resultReport.FindAnalyte returned: {0}", analyte == null ? "null" : $"found analyte {analyte.Code}");
                                    if (!(analyte == null))
                                    {
                                        foreach (Result r2 in updatedResults.List)
                                        {
                                            if ((r2.PanelCode ?? "") == (r.PanelCode ?? "") && (r2.AnalyteCode ?? "") != (r.AnalyteCode ?? ""))
                                            {
                                                var msg = string.Concat("Invalid: Message contains multiple results for analyte ", analyte.Analyte.Name, " - ", analyte.Code, ". Result will not be updated.");
                                                Log.Error(msg);
                                                throw new Exception(msg);
                                            }
                                        }

                                        if (r.IsPreliminaryRelease && SharedFunctions.IsFinalOrCorrected(analyte.ResultStatus))
                                        {
                                            var msg = string.Concat("Analyte ", analyte.Code, " cannot be updated with a prelimary result since it's status is ", analyte.ResultStatus.ToString(), ".");
                                            Log.Error(msg);
                                            throw new Exception(msg);
                                        }

                                        if (r.ResetAll)
                                        {
                                            Log.DebugFormat("ResetAll true for analyte '{0}' - clearing analyte.", analyte.Code);
                                            ClearAnalyte(analyte);
                                        }

                                        if (!string.IsNullOrEmpty(r.ResultValue))
                                        {
                                            Log.DebugFormat("Updating found analyte '{0}' with value '{1}'", analyte.Code, r.ResultValue);
                                            UpdateAnalyte(analyte, r, statuses, resultReport, autoRelease);
                                            Log.DebugFormat("Updated analyte '{0}'.", analyte.Code);
                                        }
                                    }
                                    else
                                    {
                                        if (r.LabReportStatus.ToUpper() == "I")
                                        {
                                            analyte = resultReport.FindAnalyte(r.PanelCode, false, r.ReferenceLabId);
                                            Log.DebugFormat("LabReportStatus='I' - attempted to find analyte by PanelCode '{0}': {1}", r.PanelCode, analyte == null ? "null" : $"found {analyte.Code}");
                                        }
                                        if (analyte is null)
                                        {
                                            var m = string.Format("OBR ordering code {3} does not exist (analyte {0}) for RefLab {2}. Accession # {1}.", r.AnalyteCode, updatedResults.AccessionNumber, r.ReferenceLabId, r.PanelCode);
                                            Log.Error(m);
                                            statuses.List.Add(new Status(StatusType.Failure, m, null));
                                        }
                                    }
                                }

                                else if (dynamicAdd)
                                {
                                    Log.DebugFormat("Panel not found and dynamicAdd enabled for PanelCode='{0}' RefLabId={1}. Attempting to dynamic-add panel and analyte.", r.PanelCode, r.ReferenceLabId);
                                    ReportAnalytePanel rp;

                                    if (r.ReferenceLabId == 0)
                                    {
                                        var ti = TestMasterWrapper.Fetch(r.PanelCode, order: ref o);
                                        Log.DebugFormat("TestMasterWrapper.Fetch(panel='{0}') returned {1}", r.PanelCode, ti == null ? "null" : "TestInfo");
                                        if (!(ti == null) && ti.IsPanel)
                                        {
                                            rp = resultReport.AnalytePanels.Add(ti, r.PanelCode);
                                            Log.DebugFormat("Added panel '{0}' to report dynamically.", rp.PanelCode);
                                            ti = TestMasterWrapper.Fetch(r.AnalyteCode, order: ref o);

                                            if (!(ti == null))
                                            {
                                                var ra = rp.Analytes.Add(ti, r.AnalyteCode, (SPMStatusValue)Conversions.ToInteger(false));
                                                UpdateAnalyte(ra, r, statuses, resultReport, autoRelease);
                                            }
                                            else
                                            {
                                                var m = string.Format("Analyte does not exist with code {0} in TestMaster for dynamic addition. Accession # {1}.", r.AnalyteCode, updatedResults.AccessionNumber);
                                                Log.Warn(m);
                                                statuses.List.Add(new Status(StatusType.Failure, m, null));
                                            }
                                        }
                                        else
                                        {
                                            var m = string.Format("Analyte Panel does not exist with code {0} in TestMaster for dynamic addition. Accession # {1}.", r.PanelCode, updatedResults.AccessionNumber);
                                            Log.Warn(m);
                                            statuses.List.Add(new Status(StatusType.Failure, m, null));
                                        }
                                    }
                                    else
                                    {
                                        var ti = TestMasterWrapper.Fetch(r.PanelCode.TrimStart('0'), r.ReferenceLabId, ref o);
                                        Log.DebugFormat("TestMasterWrapper.Fetch(panel='{0}', refLab={1}) returned {2}", r.PanelCode.TrimStart('0'), r.ReferenceLabId, ti == null ? "null" : "TestInfo");
                                        if (!(ti == null) && ti.IsPanel)
                                        {
                                            rp = resultReport.AnalytePanels.Add(ti, r.PanelCode);
                                            Log.DebugFormat("Added panel '{0}' for RefLab {1} to report dynamically.", rp.PanelCode, r.ReferenceLabId);
                                            ti = TestMasterWrapper.Fetch(r.AnalyteCode.TrimStart('0'), r.ReferenceLabId, ref o);

                                            if (!(ti == null))
                                            {
                                                var ra = rp.Analytes.Add(ti, r.AnalyteCode, (SPMStatusValue)Conversions.ToInteger(false));
                                                UpdateAnalyte(ra, r, statuses, resultReport, autoRelease);
                                            }
                                            else
                                            {
                                                var m = string.Format("Analyte does not exist with code {0} for RefLab {2} in TestMaster for dynamic addition. Accession # {1}.", r.AnalyteCode, updatedResults.AccessionNumber, r.ReferenceLabId);
                                                Log.Warn(m);
                                                statuses.List.Add(new Status(StatusType.Failure, m, null));
                                            }
                                        }
                                        else
                                        {
                                            var m = string.Format("Analyte Panel does not exist with code {0} for RefLab {2} in TestMaster for dynamic addition. Accession # {1}.", r.PanelCode, updatedResults.AccessionNumber, r.ReferenceLabId);
                                            Log.Warn(m);
                                            statuses.List.Add(new Status(StatusType.Failure, m, null));
                                        }
                                    }
                                }

                                else
                                {
                                    var m = string.Format("Panel does not exist with code {0} for Accession Number {1}.", r.PanelCode, updatedResults.AccessionNumber);
                                    Log.Warn(m);
                                    statuses.List.Add(new Status(StatusType.Warning, m, null));
                                }
                            }

                            else
                            {
                                Log.DebugFormat("No PanelCode provided. Searching panels and analytes across report for AnalyteCode='{0}' RefLabId={1}", r.AnalyteCode, r.ReferenceLabId);
                                bool analyteFound = false;

                                if (dynamicAdd == false)
                                {
                                    int cnt = resultReport.AnalytePanels.List.Count - 1;
                                    for (int i = 0, loopTo = cnt; i <= loopTo; i++)
                                    {
                                        var p = resultReport.AnalytePanels.List[i];
                                        analyte = p.Analytes.FindFirst(r.AnalyteCode, r.ReferenceLabId);
                                        if (!(analyte == null))
                                        {
                                            analyteFound = true;
                                            Log.DebugFormat("Found analyte '{0}' in panel '{1}' (index {2}). Checking processing.", r.AnalyteCode, p.PanelCode, i);
                                            p.CheckProcessing();

                                            if (r.IsPreliminaryRelease && (analyte.ResultStatus == resultStatusType.Final || analyte.ResultStatus == resultStatusType.Corrected))
                                            {
                                                var msg = string.Concat("Analyte ", analyte.Code, " cannot be updated with a prelimary result since it's status is ", analyte.ResultStatus.ToString(), ".");
                                                Log.Error(msg);
                                                throw new Exception(msg);
                                            }

                                            if (r.ResetAll)
                                            {
                                                Log.DebugFormat("ResetAll true for analyte '{0}' in panel '{1}' - clearing.", analyte.Code, p.PanelCode);
                                                ClearAnalyte(analyte);
                                            }

                                            if (r.CanDeleteComment())
                                            {
                                                string commentString;
                                                foreach (ReportComment c in p.Comments.List)
                                                {
                                                    commentString = c.Text.Replace(Environment.NewLine, " ").Trim();
                                                    if ((commentString ?? "") == (Configuration.AppSettings.GetString("Bioreference.LIS:PreliminaryCommentToRemove")))
                                                    {
                                                        Log.DebugFormat("Deleting preliminary comment '{0}' from panel '{1}'", commentString, p.PanelCode);
                                                        p.DeleteComment(c, false);
                                                        break;
                                                    }
                                                }
                                            }
                                            UpdateAnalyte(analyte, r, statuses, resultReport, autoRelease);
                                            Log.DebugFormat("Updated analyte '{0}' in panel '{1}' via panel iteration.", analyte.Code, p.PanelCode);
                                        }
                                    }
                                }

                                int foundCount = 0;
                                foreach (ReportAnalyte a in resultReport.Analytes.Find(r.AnalyteCode, r.ReferenceLabId))
                                {
                                    foundCount++;
                                    analyteFound = true;
                                    if (r.ResetAll)
                                    {
                                        Log.DebugFormat("ResetAll true for analyte '{0}' (non-panel). Clearing.", a.Code);
                                        ClearAnalyte(a);
                                    }

                                    UpdateAnalyte(a, r, statuses, resultReport, autoRelease);
                                    Log.DebugFormat("Updated analyte '{0}' (non-panel).", a.Code);
                                }
                                Log.DebugFormat("Non-panel analyte find returned {0} items.", foundCount);

                                if (dynamicAdd && analyteFound == false)
                                {
                                    Log.Error("Dynamic Add requested for non-panel analyte but dynamic add is disabled. Throwing.");
                                    throw new Exception("Dynamic Add has been disabled");
                                }
                                else if (analyteFound == false)
                                {
                                    var m = string.Format("Unable to update analyte, does not exist:{3}Accession#: {2}{3}Code: {0}{3}Value: {4}{3}RefLabId: {1}", r.AnalyteCode, r.ReferenceLabId, updatedResults.AccessionNumber, Constants.vbCrLf, r.ResultValue);
                                    Log.Warn(m);
                                    statuses.List.Add(new Status(StatusType.Warning, m, null));
                                }
                            }

                            // Log status summary after processing this result
                            int failureCount = 0, warnCount = 0, successCount = 0;
                            foreach (Status st in statuses.List)
                            {
                                if (st.StatusType == StatusType.Failure) failureCount++;
                                else if (st.StatusType == StatusType.Warning) warnCount++;
                                else if (st.StatusType == StatusType.Success) successCount++;
                            }
                            Log.DebugFormat("After processing result Analyte='{0}': statuses => Success={1}, Warning={2}, Failure={3}, Total={4}",
                                r?.AnalyteCode ?? "", successCount, warnCount, failureCount, statuses.List.Count);
                        }
                        catch (Exception exItem)
                        {
                            Log.ErrorFormat("Error processing individual result (Analyte='{0}', Panel='{1}'): {2}", r?.AnalyteCode ?? "", r?.PanelCode ?? "", exItem.ToString());
                            statuses.List.Add(new Status(StatusType.Failure, $"Error processing result {r?.AnalyteCode ?? r?.PanelCode ?? "(unknown)"}: {exItem.Message}", null));
                        }
                    }
                }

                else
                {
                    Log.DebugFormat("Report is null or accession out of age range for Accession '{0}'. Handling unsolicited.", updatedResults?.AccessionNumber ?? "");
                    DateTime expireDate = DateTime.Now.AddDays(Configuration.LISSettings.GetInt("UnsolicitedMessageExpirationDays"));
                    if (!string.IsNullOrEmpty(hl7Message))
                    {
                        
                        UnsolicitedMessage um = new UnsolicitedMessage(updatedResults.AccessionNumber, hl7Message, expireDate);
                        Log.DebugFormat("Created UnsolicitedMessage for Accession '{0}'. IsValid={1}", um.AccessionNbr, um.IsValid);
                        if (um.IsValid)
                        {
                            um.Save();
                            Log.InfoFormat("UnsolicitedMessage saved for Accession '{0}'. Message expire='{1}'", um.AccessionNbr, expireDate);
                            statuses.List.Add(new Status(StatusType.Success, $"Unsolicited message saved for {um.AccessionNbr}", um));
                        }
                        else
                        {
                            Log.WarnFormat("Unable to save UnsolicitedMessage for Accession '{0}' - Rules: {1}", um.AccessionNbr, um.Rules);
                            statuses.List.Add(new Status(StatusType.Failure, $"Unable to save unsolicited message for {um.AccessionNbr} - {um.Rules}", um));
                        }
                    }
                    else
                    {
                        Log.WarnFormat("Missing HL7 Message for Accession '{0}' - unsolicited cannot be saved.", updatedResults?.AccessionNumber ?? "");
                        statuses.List.Add(new Status(StatusType.Warning, $"Missing HL7 Message for {updatedResults.AccessionNumber}", null));
                    }

                    var ur = new UnsolicitedResults();
                    foreach (Result r in updatedResults.List)
                    {
                        Log.DebugFormat("Adding unsolicited result for Accession '{0}' Analyte '{1}' Message expire='{2}", updatedResults.AccessionNumber, r?.AnalyteCode ?? "", expireDate);
                        ur.AddResult(updatedResults.AccessionNumber, r, expireDate);
                    } 

                    Log.DebugFormat("UnsolicitedResults built. Count={0}. IsValid={1}", ur.List.Count, ur.IsValid);
                    if (ur.IsValid)
                    {
                        ur.Save();
                        Log.InfoFormat("UnsolicitedResults saved. Count={0}", ur.List.Count);
                        statuses.List.Add(new Status(StatusType.Warning, string.Format("Unsolicited results saved: Count {0}.", ur.List.Count), ur));
                    }
                    else
                    {
                        Log.ErrorFormat("Unable to save unsolicited results. Rules: {0}", ur.Rules);
                        statuses.List.Add(new Status(StatusType.Failure, string.Format("Unable to save unsolicited results: {0}.", ur.Rules.ToString()), null));
                    }

                    return statuses;
                }

                if (!statuses.IsError)
                {
                    Log.DebugFormat("No fatal errors detected in statuses. Preparing to validate and save Report for Accession '{0}'", resultReport.AccessionNbr);
                    Report.CheckCalcPerfomingLocation(resultReport);
                    Log.DebugFormat("Called Report.CheckCalcPerfomingLocation for Accession '{0}'", resultReport.AccessionNbr);

                    if (resultReport.IsValid)
                    {
                        Log.DebugFormat("Report is valid. Saving Report Accession='{0}', ReportID={1}", resultReport.AccessionNbr, resultReport.ID);
                        resultReport.Save();
                        Log.InfoFormat("Report saved successfully. Accession='{0}', ReportID={1}", resultReport.AccessionNbr, resultReport.ID);
                        statuses.AuditList.AddRange(resultReport.GetFormattedAuditItems);
                        Log.DebugFormat("Added {0} audit items to statuses.AuditList", resultReport.GetFormattedAuditItems?.Count ?? 0);
                        statuses.List.Add(new Status(StatusType.Success, "", resultReport));
                    }
                    else
                    {
                        Log.ErrorFormat("Report validation failed for Accession '{0}'. Rules: {1}", resultReport.AccessionNbr, resultReport.Rules);
                        statuses.List.Add(new Status(StatusType.Failure, string.Format("Unable to save: {0}.", resultReport.Rules.ToString()), resultReport));
                    }
                }
                else
                {
                    Log.ErrorFormat("Not saving Report for Accession '{0}' because statuses.IsError == true", resultReport?.AccessionNbr ?? "(none)");
                }

                Log.DebugFormat("Exit UpdateResults for Accession='{0}'. Total statuses={1}", updatedResults?.AccessionNumber ?? "", statuses.List.Count);
                return statuses;
            }

            catch (Exception ex)
            {
                Log.ErrorFormat("Unhandled exception in UpdateResults for Accession '{0}': {1}", (object)(updatedResults?.AccessionNumber ?? ""), ex.ToString());
                Trace.Write(ex.ToString());
                throw;
            }

        }

        [Obsolete("No longer valid with new TestMaster.")]
        public static Analyte GetReferenceLabAnalyte(string referenceLabCode, string alternateTestCode, int referenceLabId, string analyteName, bool addNew)
        {

            ResultCodes rc;
            int code = 0;

            // AG - DO NOT DYNAMICALLY ADD - NEEDS TO BE IN TESTMASTER
            // First check to see if this analyte was already dynamically added.
            rc = ResultCodes.Fetch(referenceLabCode.TrimStart('0'), referenceLabId);
            if (rc.Analytes.List.Count == 1)
            {
                // If this is a match, return it.
                // If rc.Analytes.List(0).ReferenceLabeAnalyteCode = referenceLabCode Then
                return rc.Analytes.List[0];
            }
            // If no match, then check for another code.
            // ElseIf Integer.TryParse(alternateTestCode, code) Then
            // Return GetReferenceLabAnalyte(referenceLabCode, code + 1, referenceLabId, analyteName, addNew)
            // End If
            // ElseIf addNew Then
            // 'If it doesn't exist, we add it.
            // 'Save to TestMaster
            // Dim a As Analyte = New Analyte()
            // a.Code = alternateTestCode
            // a.ReferenceLabId = referenceLabId
            // a.ReferenceLabeAnalyteCode = referenceLabCode
            // a.Name = analyteName
            // a.Save()
            // Return a
            else
            {
                throw new Exception(string.Concat("ReferenceLab Code not found or duplicate detected: RefLab- ", referenceLabId, " Code- ", referenceLabCode));
            }

            return null;

        }

        [Obsolete("No longer valid with new TestMaster.")]
        public static AnalytePanel GetReferenceLabAnalytePanel(string referenceLabPanelCode, string alternateTestPanelCode, int referenceLabId, string panelName, bool addNew)
        {

            ResultCodes rc;
            int code = 0;

            // First check to see if this analyte was already dynamically added.
            rc = ResultCodes.Fetch(referenceLabPanelCode.TrimStart('0'), referenceLabId);
            if (rc.Panels.List.Count > 0)
            {
                return rc.Panels.List[0];
            }
            else if (addNew)
            {
                // If it doesn't exist, we add it.
                // Save to TestMaster

                // 'See if exists in testmaster with altcode, regardless of ReferenceLab, if not try another.
                // ************************************************************
                bool exists = true;
                while (exists)
                {
                    rc = ResultCodes.Fetch(alternateTestPanelCode, true);
                    if (rc.Panels.List.Count == 0 && rc.Analytes.List.Count == 0)
                    {
                        exists = false;
                    }
                    else
                    {
                        alternateTestPanelCode = (alternateTestPanelCode + 1d).ToString();
                    }
                }
                // ************************************************************

                var pl = new AnalytePanel();
                pl.PanelCode = alternateTestPanelCode;
                pl.ReferenceLabId = referenceLabId;
                pl.ReferenceLabCode = referenceLabPanelCode;
                pl.Name = panelName;
                pl = (AnalytePanel)pl.Save();
                return pl;
            }
            else
            {
                throw new Exception(string.Concat("ReferenceLab Code not found: ", referenceLabPanelCode));
            }

            return null;

        }



        // '*******************************************************************
        // 'Needed since test is only returning the DivisionId without the code
        private static Dictionary<int, string> m_divisions;
        private static object m_syncLock = new object();
        private static DateTime m_divisionLastCheck;

        public static string[] DefaultDivisionCodes
        {
            get
            {
                return Configuration.LISSettings.GetList("DefaultDivisionCode").ToArray();
            }
        }

        public static Dictionary<int, string> Divisions
        {
            get
            {
                bool timeExpired = m_divisionLastCheck.AddMinutes(10d) < DateTime.Now;

                if (m_divisions == null || timeExpired)
                {

                    lock (m_syncLock)
                    {
                        if (!(m_divisions == null) && timeExpired == false)
                            return m_divisions;

                        var divs = Common.TestMaster.Divisions.Fetch();
                        m_divisions = new Dictionary<int, string>();
                        foreach (Common.TestMaster.Division d in divs.List)
                        {
                            if (!m_divisions.ContainsKey(d.DivisionID))
                                m_divisions.Add(d.DivisionID, d.DivisionCode);
                        }
                        m_divisionLastCheck = DateTime.Now;

                    }
                }

                return m_divisions;

            }
        }

        public static string GetDivisionCode(int divisionId, bool returnDefault = true)
        {

            string dk = "";
            if (Divisions.ContainsKey(divisionId))
                dk = Divisions[divisionId];
            if (!string.IsNullOrEmpty(dk))
            {
                return dk;
            }

            if (divisionId != 0 || !returnDefault)
            {
                throw new Exception(string.Format("Error getting division code for id {0}: Id not found.", divisionId));
            }
            else
            {
                return DefaultDivisionCodes[0];
            }

        }



        // '*******************************************************************

        #endregion

        #region Private Shared Functions

        private static void UpdateAnalyte(ReportAnalyte analyte, Result r, Statuses statuses, Report rpt, bool autoRelease = false)
        {

            Log.DebugFormat("Enter UpdateAnalyte: Analyte='{0}', PanelParentType='{1}', ResultValue='{2}', ForceUpdate={3}, AllowUpdates={4}, AutoReleaseParam={5}",
                analyte?.Code ?? "(null)",
                analyte?.Parent?.GetType().Name ?? "(null)",
                r?.ResultValue ?? "(null)",
                r?.ForceUpdate ?? false,
                analyte?.Analyte?.AllowUpdates ?? false,
                autoRelease);

            // If Not IsNothing(analyte) Then

            // 'If the analyte has a result, do not reset it! Skip everything, for now, unless
            // 'ForceUpdate is set.
            // 'AG - 12/08/2008 - AllowUpdates setting added to allow updates by TestCode
            // *******************************************************
            if (string.IsNullOrEmpty(analyte.ResultValue) || r.ForceUpdate || analyte.Analyte.AllowUpdates == true)
            {

                Log.DebugFormat("Proceeding to update analyte '{0}'. Current ResultValue='{1}'", analyte.Code, analyte.ResultValue);

                analyte.InstrumentId = r.InstrumentId;
                analyte.InstrumentAlt1 = r.InstrumentAlt1;
                analyte.InstrumentAlt2 = r.InstrumentAlt2;
                analyte.InstrumentAlt3 = r.InstrumentAlt3;
                analyte.SpecimenRackId = r.SpecimenRackId;
                analyte.SpecimenRackPosition = r.SpecimenRackPosition;
                analyte.SpecimenRackSequence = r.SpecimenRackSequence;
                analyte.SpecimenAlt1 = r.SpecimenAlt1;
                analyte.SpecimenAlt2 = r.SpecimenAlt2;
                analyte.ResultAnalyzedTechUser = r.TechUser;
                analyte.ResultReleasedUser = r.ReleaseUser;
                analyte.RefLabPerformingFacilityId = r.RefLabPerformingFacilityId;
                if (!string.IsNullOrEmpty(r.PerformingFacility))
                {
                    analyte.SetPerformingFacility(r.PerformingFacility);
                    Log.DebugFormat("Set PerformingFacility='{0}' for analyte '{1}'", r.PerformingFacility, analyte.Code);
                }

                // Report Alerts
                // *******************************************************
                if (!(r.ResultAlertCodes == null))
                {
                    Log.DebugFormat("Processing {0} ResultAlertCodes for analyte '{1}'", r.ResultAlertCodes.Length, analyte.Code);
                    // Fetch Error Flags from TestMaster
                    var alerts = Alerts.Fetch();
                    Alert alert;
                    if (alerts != null && alerts.List.Count > 0)
                    {
                        if (!(r.ResultAlertCodes == null))
                        {
                            foreach (string code in r.ResultAlertCodes)
                            {
                                alert = (Alert)alerts.List.Find(code);
                                if (!(alert == null))
                                {
                                    analyte.Alerts.Add(alert);
                                    Log.DebugFormat("Added Alert code '{0}' to analyte '{1}'", code, analyte.Code);
                                }
                                else
                                {
                                    var msg = string.Format("Unable to find Alert with code {0} for {1}.", code, r.AnalyteCode);
                                    Log.Warn(msg);
                                    statuses.List.Add(new Status(StatusType.Failure, msg, null));
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.ErrorFormat("Unable to retrieve Alert Codes list while updating analyte '{0}'", analyte.Code);
                        statuses.List.Add(new Status(StatusType.Failure, "Unable to retreive Alert Codes list.", null));
                    }
                }
                // *******************************************************

                // Reference Analytes can only be updated if the Analyte is a sendout.
                // *******************************************************
                if (analyte.Analyte.IsSendOut && analyte.Analyte.ReferenceLabId > 0) // r.ReferenceLabId > 0 Then
                {

                    // Removed this - not used for anything - causing new RefAnalyte to be inserted everytime Flag changes
                    // If r.FlagValue.Trim() <> "" Then analyte.UpdateFlagValue(r.FlagValue)
                    if (!string.IsNullOrEmpty(r.AnalyteName.Trim()))
                    {
                        analyte.UpdateAnalyteName(r.AnalyteName);
                        Log.DebugFormat("Updated AnalyteName for analyte '{0}' to '{1}'", analyte.Code, r.AnalyteName);
                    }
                    if (!string.IsNullOrEmpty(r.ReferenceRange.Trim()))
                    {
                        analyte.UpdateReferenceRange(r.ReferenceRange);
                        Log.DebugFormat("Updated ReferenceRange for analyte '{0}' to '{1}'", analyte.Code, r.ReferenceRange);
                    }
                    if (!string.IsNullOrEmpty(r.Units.Trim()))
                    {
                        analyte.UpdateUnits(r.Units);
                        Log.DebugFormat("Updated Units for analyte '{0}' to '{1}'", analyte.Code, r.Units);
                    }
                }
                // *******************************************************

                // 'Set this after the alerts are added - result value can trigger an alert check.
                analyte.ResultValue = r.ResultValue;
                Log.Debug($"Setting Analyte {analyte.Code} to {r.ResultValue}");
                analyte.ResultDate = r.ResultDate;
                analyte.Instrument = r.Instrument;
                Log.DebugFormat("Set ResultDate='{0}', Instrument='{1}' for analyte '{2}'", r.ResultDate, r.Instrument, analyte.Code);

                // AG - 12/3/08 - Removed "if" condition for Results Review
                // AG - 1/28/09 - Added "if" condition back.  Caused problems evaluating flags on inbound channel.
                if (!string.IsNullOrEmpty(r.FlagValue))
                {
                    analyte.SetFlaggedValue(r.FlagValue);
                    Log.DebugFormat("Set FlagValue='{0}' for analyte '{1}'", r.FlagValue, analyte.Code);
                }

                // Comments
                // *******************************************************
                if (!(r.Comments == null))
                {
                    foreach (string c in r.Comments)
                    {
                        analyte.Comments.AddComment(c);
                        Log.DebugFormat("Added comment to analyte '{0}': '{1}'", analyte.Code, c);
                    }
                }
                // *******************************************************






                // AG-Added 8/20/2008 for automatically releasing results.
                // AG-Added 3/16/2009 check for individually set autoreleasing.
                // AG-Added 9/30/2009 Check for AutoRelease - is part of RefAnalyte and not base Common.Lab.Analyte class.
                // AG-Added 9/13/2011 Don't autorelease if has alert codes
                // Dim blockAutoRelease As Boolean = False
                // If My.Settings.DisableAutoReleaseWithAlerts Then ''check to see if AutoReleasing is disabled for results with alerts
                // If analyte.Parent.GetType() Is GetType(ReportAnalytePanel) Then
                // If CType(analyte.Parent, ReportAnalytePanel).HasAlerts() Then
                // blockAutoRelease = True
                // End If
                // ElseIf analyte.Alerts.List.Count > 0 Then
                // blockAutoRelease = True
                // End If
                // End If

                // 'Result could have been released already above when setting result, internally if autorelease flag exists on RefAnalyte
                Log.Debug($"Analyte {analyte.Code} autoRelease={autoRelease} r.AutoReleaseResult={r.AutoReleaseResult} CanAutoRelease={analyte.CanAutoRelease()} ResultStatus={analyte.ResultStatus}");
                if (Conversions.ToBoolean((autoRelease || r.AutoReleaseResult || ((RefAnalyte)analyte.Analyte).AutoRelease) && analyte.CanAutoRelease() && analyte.ResultStatus != resultStatusType.DeltaHold))
                {

                    Log.DebugFormat("AutoRelease conditions met for analyte '{0}'. IsPrelim={1}, ParentType='{2}'", analyte.Code, r.IsPreliminaryRelease, analyte.Parent?.GetType().Name ?? "(null)");

                    // 'For Preliminary release - should get indicator from Result object
                    if (r.IsPreliminaryRelease && ReferenceEquals(analyte.Parent.GetType(), typeof(ReportAnalytePanel)))
                    {

                        ReportAnalytePanel p = (ReportAnalytePanel)analyte.Parent;
                        if (((RefAnalytePanel)p.Panel).AllowPreliminaryRelease)
                        {
                            if (r.PrelimReleaseTestCodes.Count > 0)
                            {
                                Log.DebugFormat("Calling MarkAsPreliminaryReleasedOnCondition on panel '{0}' with {1} conditions", p.PanelCode, r.PrelimReleaseTestCodes.Count);
                                p.MarkAsPreliminaryReleasedOnCondition(r.PrelimReleaseTestCodes);
                            }
                            else
                            {
                                Log.DebugFormat("Calling MarkAsPreliminaryReleased on panel '{0}'", p.PanelCode);
                                p.MarkAsPreliminaryReleased();
                            }
                            Log.InfoFormat("Panel '{0}' preliminary released (analyte '{1}').", p.PanelCode, analyte.Code);
                        }
                        else
                        {
                            Log.DebugFormat("Panel '{0}' does not allow preliminary release; skipping preliminary release for analyte '{1}'", p.PanelCode, analyte.Code);
                        }
                    }

                    // analyte.MarkAsReleased()
                    // AG - 9/8/09 Remove this - if analyte is part of a panel, it is done within the analyte.
                    // AG - 9/23/09 Added back - needs this because of calcs and each ResultValue update resets all analyte Transmitstatus
                    else if (ReferenceEquals(analyte.Parent.GetType(), typeof(Report)))
                    {
                        if (r.IsPreliminaryRelease)
                        {
                            if (r.PrelimReleaseTestCodes.Count > 0)
                            {
                                if (r.PrelimReleaseTestCodes.Contains(analyte.Code))
                                {
                                    analyte.MarkAsPreliminaryReleased();
                                    Log.InfoFormat("Analyte '{0}' preliminary released (direct on Report).", analyte.Code);
                                }
                            }
                            else
                            {
                                analyte.MarkAsPreliminaryReleased();
                                Log.InfoFormat("Analyte '{0}' preliminary released (direct on Report).", analyte.Code);
                            }
                        }
                        else
                        {
                            analyte.MarkAsReleased();
                            Log.InfoFormat("Analyte '{0}' released (direct on Report).", analyte.Code);
                        }
                    }

                    else // For analyte that is part of a panel. We need to make sure all the components of the panel are
                    {
                        // ready to be released.
                        ReportAnalytePanel p = (ReportAnalytePanel)analyte.Parent;
                        // if 4K panel do not do this.
                        // If rpt.Is4KPanel(p.PanelCode) = False Then
                        Log.Debug($"Panel {p.PanelCode} HoldReleaseForDeltaHold={p.HoldReleaseForDeltaHold()}");
                        if (p.GetStatus() != resultStatusType.Pending && !p.HoldReleaseForDeltaHold())
                        {
                            Log.DebugFormat("Calling MarkAsReleased on panel '{0}' for analyte '{1}'", p.PanelCode, analyte.Code);
                            p.MarkAsReleased();
                            Log.InfoFormat("Panel '{0}' released due to analyte update for analyte '{1}'", p.PanelCode, analyte.Code);
                        }
                        else
                        {
                            Log.DebugFormat("Panel '{0}' not ready to release (Status={1} HoldReleaseForDeltaHold={2})", p.PanelCode, p.GetStatus(), p.HoldReleaseForDeltaHold());
                        }
                        // End If

                    }

                }
                else
                {
                    Log.DebugFormat("AutoRelease not performed for analyte '{0}'. Conditions: AutoReleaseParam={1}, r.AutoReleaseResult={2}, RefAnalyte.AutoRelease={3}, CanAutoRelease={4}, ResultStatus={5}",
                        analyte.Code,
                        autoRelease,
                        r.AutoReleaseResult,
                        ((RefAnalyte)analyte.Analyte).AutoRelease,
                        analyte.CanAutoRelease(),
                        analyte.ResultStatus);
                }
                // ' 'Reference Analytes can only be updated if the Analyte is a sendout.
                // '*******************************************************
                // If analyte.Analyte.IsSendOut AndAlso analyte.Analyte.ReferenceLabId > 0 Then ' r.ReferenceLabId > 0 Then

                // 'Removed this - not used for anything - causing new RefAnalyte to be inserted everytime Flag changes
                // 'If r.FlagValue.Trim() <> "" Then analyte.UpdateFlagValue(r.FlagValue)
                // If r.AnalyteName.Trim() <> "" Then analyte.UpdateAnalyteName(r.AnalyteName)
                // If r.ReferenceRange.Trim() <> "" Then analyte.UpdateReferenceRange(r.ReferenceRange)
                // If r.Units.Trim() <> "" Then analyte.UpdateUnits(r.Units)
                // End If
                // '*******************************************************


                Log.DebugFormat("Exit UpdateAnalyte (updated) for analyte '{0}'. ResultValue='{1}', ResultStatus='{2}'", analyte.Code, analyte.ResultValue, analyte.ResultStatus);

            }
            else
            {
                Log.DebugFormat("Skipping update for analyte '{0}'. Existing ResultValue='{1}', ForceUpdate={2}, AllowUpdates={3}", analyte.Code, analyte.ResultValue, r.ForceUpdate, analyte.Analyte.AllowUpdates);
            }

        }

        #endregion

        #region UpdateResults Class

        [Serializable()]
        public class Results
        {

            private string m_accessionNbr = "";
            private List<Result> m_results;

            public Results(string accessionNbr)
            {
                m_accessionNbr = accessionNbr;
                m_results = new List<Result>();
            }

            public string AccessionNumber
            {
                get
                {
                    return m_accessionNbr;
                }
            }

            public List<Result> List
            {
                get
                {
                    return m_results;
                }
            }

        }

        [Serializable()]
        public class Result
        {

            private string m_analyteCode = "";
            private string m_panelCode = "";
            private string m_resultValue = "";
            private string[] m_resultAlertCodes = null;
            private DateTime m_resultDate;
            private bool m_forceUpdate = false;
            private string[] m_comments = null;
            private bool m_resetAll = false;

            // Instrument info
            private string m_instrument = "";
            private string m_instrumentId = 0.ToString();
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
            private string m_panelName = "";

            private string m_analyteCodeAlt = "";
            private string m_panelCodeAlt = "";

            // 'Settings per Result
            private bool m_autoReleaseResult = false;
            private bool m_isPreliminaryRelease = false;

            private string m_techUser = "";
            private string m_releaseUser = "";

            private string m_performingFacility = "";
            private bool m_deleteComment = false;
            private string m_labReportStatus = string.Empty;
            private List<string> m_prelimReleaseTestCodes = new List<string>();
            private int m_RefLabPerformingFacilityId = 0;

            public Result(string analyteCode, string resultValue, string[] resultFlagCodes = null)
            {
                m_analyteCode = analyteCode;
                m_resultValue = resultValue;
                m_resultAlertCodes = resultFlagCodes;
            }

            public Result(string panelCode, string analyteCode, string resultValue, string[] resultFlagCodes = null)
            {
                m_panelCode = panelCode;
                m_analyteCode = analyteCode;
                m_resultValue = resultValue;
                m_resultAlertCodes = resultFlagCodes;
            }

            public string PerformingFacility
            {
                get
                {
                    return m_performingFacility;
                }
                set
                {
                    m_performingFacility = value;
                }
            }

            public int RefLabPerformingFacilityId
            {
                get
                {
                    return m_RefLabPerformingFacilityId;
                }
                set
                {
                    m_RefLabPerformingFacilityId = value;
                }
            }

            public string LabReportStatus
            {
                get
                {
                    return m_labReportStatus;
                }
                set
                {
                    m_labReportStatus = value;
                }
            }

            public bool ResetAll
            {
                get
                {
                    return m_resetAll;
                }
                set
                {
                    m_resetAll = value;
                }
            }

            public string PanelCode
            {
                get
                {
                    return m_panelCode;
                }
                set
                {
                    m_panelCode = value;
                }
            }

            public string PanelCodeAlternate
            {
                get
                {
                    return m_panelCodeAlt;
                }
                set
                {
                    m_panelCodeAlt = value;
                }
            }

            public string PanelName
            {
                get
                {
                    return m_panelName;
                }
                set
                {
                    m_panelName = value;
                }
            }

            public string AnalyteCode
            {
                get
                {
                    return m_analyteCode;
                }
                set
                {
                    m_analyteCode = value;
                }
            }

            public string AnalyteCodeAlternate
            {
                get
                {
                    return m_analyteCodeAlt;
                }
                set
                {
                    m_analyteCodeAlt = value;
                }
            }

            public string ResultValue
            {
                get
                {
                    return m_resultValue;
                }
                set
                {
                    m_resultValue = value;
                }
            }

            public DateTime ResultDate
            {
                get
                {
                    return m_resultDate;
                }
                set
                {
                    m_resultDate = value;
                }
            }

            public bool ForceUpdate
            {
                get
                {
                    return m_forceUpdate;
                }
                set
                {
                    m_forceUpdate = value;
                }
            }

            [Obsolete("ResultAlertCodes should be used instead. Retained for backwards compatibility")]
            public string[] ResultFlagCodes
            {
                get
                {
                    return m_resultAlertCodes;
                }
                set
                {
                    m_resultAlertCodes = value;
                }
            }

            public string[] ResultAlertCodes
            {
                get
                {
                    return m_resultAlertCodes;
                }
                set
                {
                    m_resultAlertCodes = value;
                }
            }

            public string[] Comments
            {
                get
                {
                    return m_comments;
                }
                set
                {
                    m_comments = value;
                }
            }

            public string Instrument
            {
                get
                {
                    return m_instrument;
                }
                set
                {
                    m_instrument = value;
                }
            }

            public string InstrumentId
            {
                get
                {
                    return m_instrumentId;
                }
                set
                {
                    m_instrumentId = value;
                }
            }

            public string InstrumentAlt1
            {
                get
                {
                    return m_instrumentAlt1;
                }
                set
                {
                    m_instrumentAlt1 = value;
                }
            }

            public string InstrumentAlt2
            {
                get
                {
                    return m_instrumentAlt2;
                }
                set
                {
                    m_instrumentAlt2 = value;
                }
            }

            public string InstrumentAlt3
            {
                get
                {
                    return m_instrumentAlt3;
                }
                set
                {
                    m_instrumentAlt3 = value;
                }
            }

            public string SpecimenRackId
            {
                get
                {
                    return m_specimenRackID;
                }
                set
                {
                    m_specimenRackID = value;
                }
            }

            public string SpecimenRackPosition
            {
                get
                {
                    return m_specimenRackPos;
                }
                set
                {
                    m_specimenRackPos = value;
                }
            }

            public string SpecimenRackSequence
            {
                get
                {
                    return m_specimenRackSeq;
                }
                set
                {
                    m_specimenRackSeq = value;
                }
            }

            public string SpecimenAlt1
            {
                get
                {
                    return m_specimenAlt1;
                }
                set
                {
                    m_specimenAlt1 = value;
                }
            }

            public string SpecimenAlt2
            {
                get
                {
                    return m_specimenAlt2;
                }
                set
                {
                    m_specimenAlt2 = value;
                }
            }


            public bool AutoReleaseResult
            {
                get
                {
                    return m_autoReleaseResult;
                }
                set
                {
                    m_autoReleaseResult = value;
                }
            }

            [Obsolete("Temporarily here - channel is using this property")]
            public bool AllowPreliminaryRelease
            {
                get
                {
                    return m_isPreliminaryRelease;
                }
                set
                {
                    m_isPreliminaryRelease = value;
                }
            }


            public bool IsPreliminaryRelease
            {
                get
                {
                    return m_isPreliminaryRelease;
                }
                set
                {
                    m_isPreliminaryRelease = value;
                }
            }

            public List<string> PrelimReleaseTestCodes
            {
                get
                {
                    return m_prelimReleaseTestCodes;
                }
                set
                {
                    m_prelimReleaseTestCodes = value;
                }
            }

            // For Reference Analyte
            // *****************************************************
            public int ReferenceLabId
            {
                get
                {
                    return m_refLabId;
                }
                set
                {
                    m_refLabId = value;
                }
            }
            public string FlagValue
            {
                get
                {
                    return m_flagValue;
                }
                set
                {
                    m_flagValue = value;
                }
            }

            public string Units
            {
                get
                {
                    return m_units;
                }
                set
                {
                    m_units = value;
                }
            }

            public string ReferenceRange
            {
                get
                {
                    return m_refRange;
                }
                set
                {
                    m_refRange = value;
                }
            }

            public string AnalyteName
            {
                get
                {
                    return m_name;
                }
                set
                {
                    m_name = value;
                }
            }

            public string TechUser
            {
                get
                {
                    return m_techUser;
                }
                set
                {
                    m_techUser = value;
                }
            }

            public string ReleaseUser
            {
                get
                {
                    return m_releaseUser;
                }
                set
                {
                    m_releaseUser = value;
                }
            }
            // *****************************************************
            public void SetDeleteComment(bool value)
            {
                m_deleteComment = value;
            }

            public bool CanDeleteComment()
            {
                return m_deleteComment;
            }
        }

        #endregion

        #region Status Classes

        [Serializable()]
        public class Statuses
        {

            private List<Status> m_list;
            private List<string> m_auditItemList;

            public Statuses()
            {
                m_list = new List<Status>();
                m_auditItemList = new List<string>();
            }

            /// <summary>
        /// Returns true if any of the Status objects have StatusType.Failure
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
            public bool IsError
            {
                get
                {
                    foreach (Status s in m_list)
                    {
                        if (s.StatusType == StatusType.Failure)
                            return true;
                    }
                    return false;
                }
            }

            public StatusType StatusType
            {
                get
                {
                    int i = 0;
                    foreach (Status s in m_list)
                    {
                        if ((int)s.StatusType > i)
                            i = (int)s.StatusType;
                    }
                    return (StatusType)i;
                }
            }

            public List<Status> List
            {
                get
                {
                    return m_list;
                }
            }

            public List<string> AuditList
            {
                get
                {
                    return m_auditItemList;
                }
            }

            public string ErrorsAsString()
            {

                return StatusAsString(StatusType.Failure);

            }

            public string WarningsAsString()
            {

                return StatusAsString(StatusType.Warning);

            }

            public string SuccessesAsString()
            {

                return StatusAsString(StatusType.Success);

            }

            public string StatusAsString(StatusType @type)
            {

                var sb = new System.Text.StringBuilder();

                foreach (Status i in m_list)
                {
                    if (i.StatusType == type)
                    {
                        sb.Append(string.Concat(i.Message, Constants.vbCrLf));
                    }
                }

                return sb.ToString();

            }

        }

        [Serializable()]
        public class Status
        {

            private StatusType m_type = StatusType.Success;
            private string m_msg = "";
            private object m_obj = null;

            internal Status(StatusType @type, string msg, object obj)
            {
                m_type = type;
                m_msg = msg?.NormalizeToWindows() ?? string.Empty;
                m_obj = obj;
            }

            public StatusType StatusType
            {
                get
                {
                    return m_type;
                }
            }
            public string Message
            {
                get
                {
                    return m_msg;
                }
            }
            public object ReturnValue
            {
                get
                {
                    return m_obj;
                }
            }

        }

        #endregion

        #region DemographicUpdateStatus Class

        public class DemographicUpdateStatus
        {
            private string[] m_reRelease = new string[] { };
            public DemographicUpdateStatus()
            {
            }
            public DemographicUpdateStatus(string[] reReleaseTests)
            {
                m_reRelease = reReleaseTests;
            }
            public string[] ReReleasedTests
            {
                get
                {
                    return m_reRelease;
                }
            }
            public bool ResultsReleased
            {
                get
                {
                    return m_reRelease.Length > 0;
                }
            }

        }

        #endregion

    }
}