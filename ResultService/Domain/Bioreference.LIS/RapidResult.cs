using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Transactions;
//using B24K.Shared.Entities;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RapidResult : AuditDataClassBase
    {

        #region Private members

        private int m_RapidResultTemplateId;
        private int m_id = 0;
        private List<RapidResultAnalyte> m_list;
        private List<RapidResultAnalyte> m_deleteList;
        private List<string> m_accessionList;
        private bool m_isClosed = false;
        private bool m_isCompleted = false;
        private DateTime m_dateCreated = DateTime.Parse("1900-01-01");
        private string m_createdBy = "";
        private DateTime m_dateUpdated = DateTime.Parse("1900-01-01");
        private string m_updatedBy = "";

        private List<AnalyteInfo> m_analyteList;

        private string m_duplicateAccessions; // comma separated Accession list
        private bool m_duplicateAccessionValidationSuccessful = false;

        #endregion

        #region Constructor

        private RapidResult()
        {
            m_list = new List<RapidResultAnalyte>();
            m_analyteList = new List<AnalyteInfo>();
            m_deleteList = new List<RapidResultAnalyte>();
            m_accessionList = new List<string>();
        }

        internal RapidResult(int templateId)
        {
            m_list = new List<RapidResultAnalyte>();
            m_analyteList = new List<AnalyteInfo>();
            m_deleteList = new List<RapidResultAnalyte>();
            m_accessionList = new List<string>();
            m_RapidResultTemplateId = templateId;
        }

        #endregion

        #region Public Properties

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public RapidResultAnalyte[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        public AnalyteInfo[] Analytes
        {
            get
            {
                return m_analyteList.ToArray();
            }
        }

        public DateTime DateCreated
        {
            get
            {
                return m_dateCreated;
            }
        }

        public string CreatedBy
        {
            get
            {
                return m_createdBy;
            }
        }

        public DateTime DateUpdated
        {
            get
            {
                return m_dateUpdated;
            }
        }

        public string UpdatedBy
        {
            get
            {
                return m_updatedBy;
            }
        }

        public int RapidResultTemplateId
        {
            get
            {
                return m_RapidResultTemplateId;
            }
        }

        [Audit("IsCompleted")]
        public object IsCompleted
        {
            get
            {
                return m_isCompleted;
            }
        }

        [Audit("IsClosed")]
        public bool IsClosed
        {
            get
            {
                return m_isClosed;
            }
        }

        public bool IsDuplicateAccessionValidationSuccessfull
        {
            get
            {
                return m_duplicateAccessionValidationSuccessful;
            }
        }
        #endregion

        #region Public Functions

        public static void Delete(int RapidResultId)
        {

            DataFactory.Delete(new Criteria(RapidResultId, 0, ""));

        }

        public static RapidResult Fetch(int RapidResultId)
        {

            return (RapidResult)DataFactory.Fetch(new Criteria(RapidResultId, 0, ""));

        }

        /// <summary>
    /// Return -1 if RapidResult does not have the specified analyteCode
    /// </summary>
    /// <param name="analyteCode"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public int GetResultCount(string analyteCode, bool onlyBlankResults)
        {
            var accessionCounted = new List<string>();
            int c = -1;
            foreach (RapidResultAnalyte a in m_list)
            {
                if ((a.AnalyteCode.TrimStart(new char[] { '0' }) ?? "") == (analyteCode.TrimStart(new char[] { '0' }) ?? ""))
                {
                    if (c == -1)
                        c = 0;

                    if (accessionCounted.Contains(a.AccessionNbr))
                        continue; // taskid: ITBT-2239; this will eliminate duplicate analyte.

                    if (string.IsNullOrEmpty(a.ResultValue) && onlyBlankResults || !onlyBlankResults)
                    {
                        c += 1;
                        accessionCounted.Add(a.AccessionNbr);
                    }
                }
            }

            return c;

        }

        public int GetPendingResultCount(string analyteCode, bool onlyPreliminary = false)
        {
            var accessionCounted = new List<string>();
            int c = -1;
            foreach (RapidResultAnalyte a in m_list)
            {
                if ((a.AnalyteCode.TrimStart(new char[] { '0' }) ?? "") == (analyteCode.TrimStart(new char[] { '0' }) ?? ""))
                {
                    if (c == -1)
                        c = 0;

                    if (accessionCounted.Contains(a.AccessionNbr))
                        continue; // taskid: ITBT-2239; this will eliminate duplicate analyte.

                    if (!onlyPreliminary && (string.IsNullOrEmpty(a.ResultValue) || a.TransmitStatus == transmitStatusType.HeldForRerun) || onlyPreliminary && !string.IsNullOrEmpty(a.ResultValue) && a.TransmitStatus == transmitStatusType.PendingRelease)
                    {
                        c += 1;
                        accessionCounted.Add(a.AccessionNbr);
                    }
                }
            }

            return c;

        }

        public void DeactivateOrder(string accessionNbr)
        {

            foreach (RapidResultAnalyte a in List)
            {
                if ((a.AccessionNbr ?? "") == (accessionNbr ?? ""))
                {
                    a.IsDeactivated = true;
                }
            }


        }

        /// <summary>
    /// Need to pass in row number in order to remove Controls.
    /// </summary>
    /// <param name="accessionNbr"></param>
    /// <param name="rowNumber"></param>
    /// <remarks></remarks>
        public void RemoveOrder(string accessionNbr, int rowNumber = 0)
        {

            foreach (RapidResultAnalyte a in List)
            {
                if ((a.AccessionNbr ?? "") == (accessionNbr ?? "") && (rowNumber == 0 || a.RowNumber == rowNumber))
                {
                    m_list.Remove(a);
                    m_deleteList.Add(a);
                    m_accessionList.Remove(accessionNbr);
                    RemoveOrder(accessionNbr, rowNumber); // need to call again since list has changed.
                    break;
                }
            }

        }

        public AddAccessionStatusType AddOrder(string accessionNbr, bool isControl = false)
        {

            if (m_accessionList.Contains(accessionNbr))
            {
                return AddAccessionStatusType.AlreadyExists;
            }

            RapidResult ws;
            RapidResultAnalyte wa;
            int l = FetchRapidResultReports().Length + 1;

            if (isControl)
            {

                // 'Blank RapidResultAnalytes
                ws = (RapidResult)DataFactory.Fetch(new Criteria(0, RapidResultTemplateId, accessionNbr, true));

                if (Conversions.ToBoolean(ws.List.Length))
                {

                    foreach (RapidResultAnalyte a in ws.List)
                    {

                        wa = new RapidResultAnalyte(this, accessionNbr, isControl, l);
                        wa.Load(a);
                        m_list.Add(wa);

                        if (AnalyteExists(a.AnalyteCode) == false)
                        {
                            m_analyteList.Add(new AnalyteInfo(a.SortOrder, a.Analyte, a.AnalyteName));
                        }

                    }

                    m_analyteList.Sort();

                    return AddAccessionStatusType.Success;
                }
                else
                {
                    return AddAccessionStatusType.NoMatchingAnalytes;
                }
            }

            else
            {
                // validate that the same Accession is not on a different worksheet and not close out.
                // NA 3/18/2014 : This was adding a second to 3 seconds of additional time during scanning.
                // commenting out to determine if this is the case.
                // Dim duplicateAccList As List(Of String) = OrderManager.FetchOpenRREList(m_id, accessionNbr, m_RapidResultTemplateId)
                // If duplicateAccList.Count > 0 Then
                // Return AddAccessionStatusType.NoMatchingAnalytes
                // End If

                // We need to fetch Analytes by accession and add to the current RapidResult
                ws = (RapidResult)DataFactory.Fetch(new Criteria(0, m_RapidResultTemplateId, accessionNbr));

                if (Conversions.ToBoolean(ws.List.Length))
                {
                    foreach (RapidResultAnalyte a in ws.List)
                    {
                        wa = new RapidResultAnalyte(this, 1);
                        wa.Load(a);
                        m_list.Add(wa);

                        if (AnalyteExists(a.AnalyteCode) == false)
                        {
                            m_analyteList.Add(new AnalyteInfo(a.SortOrder, a.Analyte, a.AnalyteName));
                        }

                    }
                    m_accessionList.Add(accessionNbr);

                    m_analyteList.Sort();

                    return AddAccessionStatusType.Success;
                }
                else
                {
                    return AddAccessionStatusType.NoMatchingAnalytes;
                }

            }

        }

        public static RapidResult CreateRapidResult(int RapidResultTemplateId)
        {

            return (RapidResult)DataFactory.Fetch(new Criteria(0, RapidResultTemplateId, ""));
            // Dim ws As RapidResult = New RapidResult(RapidResultTemplateId)
            // Return ws

        }

        /// <summary>
    /// Returns RapidResultAnalytes categorized by AccessionNbr(Report).
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
        public RapidResultReport[] FetchRapidResultReports(bool onlyPreliminary = false)
        {

            var wrs = new List<RapidResultReport>();

            foreach (RapidResultAnalyte a in m_list)
            {

                if (!onlyPreliminary || onlyPreliminary && !string.IsNullOrEmpty(a.ResultValue))
                {

                    RapidResultReport wr = null;
                    foreach (RapidResultReport r in wrs)
                    {
                        if (a.ReportId != 0 && r.ReportId == a.ReportId || a.ReportId == 0 && (a.AccessionNbr ?? "") == (r.AccessionNbr ?? "") && a.RowNumber == r.RowNumber)
                        {
                            wr = r;
                            break;
                        }
                    }
                    if (wr == null)
                    {
                        wr = new RapidResultReport(this, a.ReportId, a.AccessionNbr, Conversions.ToInteger(a.AccountStatusLevel), a.RowNumber);
                        wrs.Add(wr);
                    }
                    wr.Add(a);

                }

            }

            return wrs.ToArray();

        }

        /// <summary>
    /// Loads the Report for each RapidResultReport and sets the value accordingly and then saves the Report.
    /// </summary>
    /// <param name="markReleased">Set to true to release all results for analytes with a ResultValue and a status of PendingResults.</param>
    /// <remarks></remarks>
        public void Process(bool markReleased)
        {

            // get a recent copy of the worksheet to see if any of the analytes have been deleted
            var objWsheet = Fetch(Id);
            if (objWsheet is not null)
            {
                foreach (RapidResultReport w in FetchRapidResultReports())
                {
                    if (w.NeedToPushResults && w.ReportId != 0)
                    {
                        foreach (RapidResultAnalyte x in objWsheet.List)
                        {
                            if (x.ReportId == w.ReportId)
                            {
                                if (x.IsDeactivated)
                                {
                                    // error message
                                    throw new Exception("Accession : " + w.AccessionNbr + " has been removed from this worksheet by another user and cannot be modified.  please reload worksheet to get latest data.");
                                }
                                break;
                            }
                        }
                        w.Process(markReleased, false);
                    }
                }

                if (markReleased)
                {
                    m_isCompleted = true;
                    FlagDirty();
                    Save();
                }
            }
            else
            {
                throw new Exception("This worksheet has been deleted by another user and cannot be modified.");
            }

        }


        public void ClearResults()
        {

            foreach (RapidResultReport w in FetchRapidResultReports())
            {
                if (w.ReportId != 0)
                {
                    AuditManager.LogCustomObjectAction(w.ReportId.ToString(), "Bioreference.LIS.Report", string.Concat("Removed from RRE# ", Id));
                    w.ClearResults();
                }
            }

        }
        /// <summary>
    /// Closes the RapidResult
    /// </summary>
    /// <remarks></remarks>
        public void Close()
        {

            m_isClosed = true;
            FlagDirty();

        }

        public bool AnalyteExists(string analyteCode)
        {

            foreach (AnalyteInfo a in m_analyteList)
            {
                if ((a.Code.TrimStart(new char[] { '0' }) ?? "") == (analyteCode.TrimStart(new char[] { '0' }) ?? ""))
                    return true;
            }
            return false;
        }

        #endregion

        #region Data Functions

        internal void LoadReportHistory(DataTable dt)
        {

            ReportAnalyteHistory rh;

            foreach (DataRow r in dt.Rows)
            {
                rh = new ReportAnalyteHistory();
                rh.Load(r);
                foreach (RapidResultAnalyte rra in m_list)
                {
                    if (rh.ReportAnalyteId == rra.ReportAnalyteId)
                    {
                        rra.AddHistory(rh);
                        break;
                    }
                }
            }

        }

        internal void Load(DataTable dt, bool fetchForControl = false)
        {

            if (dt.Rows.Count > 0)
            {
                m_id = Conversions.ToInteger(dt.Rows[0]["RapidResultId"]);
                m_RapidResultTemplateId = Conversions.ToInteger(dt.Rows[0]["RapidResultTemplateId"]);
                m_dateCreated = Conversions.ToDate(dt.Rows[0]["DateCreated"]);
                m_createdBy = Conversions.ToString(dt.Rows[0]["CreatedBy"]);
                m_isClosed = Conversions.ToBoolean(dt.Rows[0]["IsClosed"]);
                m_dateUpdated = Conversions.ToDate(dt.Rows[0]["DateUpdated"]);
                m_updatedBy = Conversions.ToString(dt.Rows[0]["UpdatedBy"]);

                RapidResultAnalyte wa;
                foreach (DataRow r in dt.Rows)
                {

                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["AnalyteCode"], "", false)) && AnalyteExists(Conversions.ToString(r["AnalyteCode"])) == false)
                    {
                        var a = new RefAnalyte(null);
                        a.Load(r);
                        m_analyteList.Add(new AnalyteInfo(Conversions.ToInteger(r["SortOrder"]), a, Conversions.ToString(r["AnalyteName"])));
                    }

                    // 'fetchForControl for when using AddOrder to add a control.
                    if (fetchForControl || Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["ReportAnalyteId"], 0, false)) || Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["RapidResultAnalyteId"], 0, false)))
                    {
                        wa = new RapidResultAnalyte(this, 0);
                        wa.Load(r);
                        m_list.Add(wa);
                    }

                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["ReportAnalyteId"], 0, false)) && !m_accessionList.Contains(Conversions.ToString(r["AccessionNbr"])))
                    {
                        m_accessionList.Add(Conversions.ToString(r["AccessionNbr"]));
                    }
                }

                // 'For storing controls
                // Dim control As RefAnalyte = New RefAnalyte(Nothing)
                // control.SetControlAnalyte("Control")
                // m_analyteList.Add(New AnalyteInfo(-1, control, "Control"))
                // '*********************

                m_analyteList.Sort();

            }

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new List<DbParameter>();
            DataTable[] dt;

            if (!c.AccessionNbr.Equals("") && !c.FetchForControl)
            {

                @param.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId));
                @param.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr));
                dt = da.ExecuteProcedure("lis_RapidResultAdd_Fetch", @param.ToArray());
            }

            else if (!c.TemplateId.Equals(0))
            {

                @param.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, c.TemplateId));
                @param.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, c.AccessionNbr));
                dt = da.ExecuteProcedure("lis_RapidResultBlank_Fetch", @param.ToArray());
            }

            else
            {

                @param.Add((DbParameter)da.CreateParameter("@RapidResultId", DbType.Int32, c.RapidResultId));
                dt = da.ExecuteProcedure("lis_RapidResult_Fetch", @param.ToArray());

            }

            Load(dt[0], c.FetchForControl);

            // Load Report History
            if (dt.Length == 2)
            {
                LoadReportHistory(dt[1]);
            }

            // If this is an existing RapidResult.
            if (m_id > 0)
            {
                FlagClean();
            }

        }

        protected override void DataFactory_Delete(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[2];

            @param[0] = (DbParameter)da.CreateParameter("@RapidResultId", DbType.Int32, c.RapidResultId);

            if (!(CurrentUser == null))
            {
                @param[1] = (DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name);
            }
            else
            {
                @param[1] = (DbParameter)da.CreateParameter("@UserName", DbType.String, "");
            }

            da.ExecuteNonQuery("lis_RapidResult_Delete", @param);

            FlagDeleted();
            FlagClean(true);

        }

        private bool ValidateForDuplicateAccession()
        {
            // m_id
            // m_RapidResultTemplateId
            // AccesionNbr List

            m_duplicateAccessions = "";
            m_duplicateAccessionValidationSuccessful = true;

            var lstACC = new List<string>();

            foreach (RapidResultAnalyte a in m_list)
            {
                if (a.IsDirty && a.IsNew && !a.IsControl && !lstACC.Contains(a.AccessionNbr.Trim()))
                {
                    lstACC.Add(a.AccessionNbr.Trim());
                }
            }

            // split the Accessions into 500 counts in case there are more than 500 accessions.
            // we will verify 500 at a time.

            var lstAccListItems = SharedFunctions.SplitStringList(lstACC, 500);

            // each item now has a comma separated list of Accessions.

            foreach (string AccListItem in lstAccListItems)
            {
                // check db for duplicate.
                var duplicateAccList = OrderManager.FetchOpenRREList(m_id, AccListItem, m_RapidResultTemplateId);
                if (duplicateAccList.Count > 0)
                {
                    m_duplicateAccessionValidationSuccessful = false;
                    string[] arWorkList = duplicateAccList.ToArray();
                    m_duplicateAccessions = string.Join(",", arWorkList);
                }
            }

            return m_duplicateAccessionValidationSuccessful;
        }

        protected override void DataFactory_Save()
        {

            // get the distinct list of the rapidresultanalyte Accessions that are new and are not isControl.
            // then loop through them checking to see if the Accession is part of an existing active
            // we should do this as one call with a string of multiple accessions.

            if (!ValidateForDuplicateAccession())
            {
                throw new Exception("The following Accessions exist in other open RREs. " + m_duplicateAccessions);
            }

            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();

            @params.Add((DbParameter)da.CreateParameter("@RapidResultId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            @params.Add((DbParameter)da.CreateParameter("@RapidResultTemplateId", DbType.Int32, m_RapidResultTemplateId));
            @params.Add((DbParameter)da.CreateParameter("@DateCreated", DbType.DateTime, m_dateCreated, ParameterDirection.InputOutput));
            @params.Add((DbParameter)da.CreateParameter("@IsClosed", DbType.Boolean, m_isClosed));
            @params.Add((DbParameter)da.CreateParameter("@IsCompleted", DbType.Boolean, m_isCompleted));

            if (!(CurrentUser == null))
            {
                if (m_id == 0)
                    m_createdBy = CurrentUser.Name;
                @params.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));
            }


            DbParameter[] pList = @params.ToArray();

            // 'We use the transaction scope when saving the Report. If any sql failures, all transaction should roll back.
            var options = new TransactionOptions();
            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
            options.Timeout = new TimeSpan(0, 2, 0);
            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {

                var returnParams = da.ExecuteNonQuery("lis_RapidResult_Save", pList);
                m_id = Conversions.ToInteger(returnParams["@RapidResultId"].Value);
                m_dateCreated = Conversions.ToDate(returnParams["@DateCreated"].Value);

                foreach (RapidResultAnalyte a in m_list)
                {
                    if (a.IsDirty)
                    {
                        if (a.IsNew && !m_addAccession.Contains(a.ReportId))
                        {
                            var rri = new RapidResultItem()
                            {
                                ReportId = a.ReportId,
                                AccessionNbr = a.AccessionNbr,
                                AccessionIdentifier = a.AccessionIdentifier
                            };
                            m_addAccession.Add(a.ReportId, rri);
                        }
                        a.Update();
                    }
                }
                foreach (RapidResultAnalyte a in m_deleteList)
                {
                    if (!a.IsNew)
                    {
                        if (!m_removeAccession.Contains(a.ReportId))
                        {
                            var rri = new RapidResultItem()
                            {
                                ReportId = a.ReportId,
                                AccessionNbr = a.AccessionNbr,
                                AccessionIdentifier = a.AccessionIdentifier
                            };
                            m_removeAccession.Add(a.ReportId, rri);
                        }
                        a.Delete();
                    }
                }
                m_deleteList.Clear();

                scope.Complete();

            }

            string userName = Configuration.AppSettings.GetString("Bioreference.LIS:UserName");
            foreach (int Item in m_addAccession.Keys)
            {
                RapidResultItem rri = (RapidResultItem)m_addAccession[Item];
                AuditManager.LogCustomObjectAction(Item.ToString(), "Bioreference.LIS.Report", string.Concat("Added to RRE# ", Id), userName, rri.AccessionIdentifier, parentIdentifierType.AccessionNbr_ServiceDate);
            }

            foreach (int Item in m_removeAccession.Keys)
            {
                RapidResultItem rri = (RapidResultItem)m_removeAccession[Item];
                AuditManager.LogCustomObjectAction(Item.ToString(), "Bioreference.LIS.Report", string.Concat("Removed from RRE# ", Id), userName, rri.AccessionIdentifier, parentIdentifierType.AccessionNbr_ServiceDate);
            }

            FlagClean(true);
        }


        private Hashtable m_addAccession = new Hashtable();
        private Hashtable m_removeAccession = new Hashtable();

        #endregion

        public override bool IsDirty
        {
            get
            {
                foreach (RapidResultAnalyte a in m_list)
                {
                    if (a.IsDirty)
                        return true;
                }

                return base.IsDirty || m_deleteList.Count > 0;
            }
        }

        private class RapidResultItem
        {
            public int ReportId;
            public string AccessionNbr;
            public string AccessionIdentifier;
        }

        [Serializable()]
        internal class Criteria
        {
            private int m_RapidResultId = 0; // Loads RapidResult with that id.
            private int m_templateId = 0; // Creates a new RapidResult based on the RapidResultTemplateId
            private string m_accessionNbr = "";

            private bool m_fetchForControl = false;

            public Criteria(int RapidResultId, int templateId, string accessionNbr, bool fetchForControl = false)
            {
                m_RapidResultId = RapidResultId;
                m_templateId = templateId;
                m_accessionNbr = accessionNbr;
                m_fetchForControl = fetchForControl;
            }

            public int RapidResultId
            {
                get
                {
                    return m_RapidResultId;
                }
            }

            public int TemplateId
            {
                get
                {
                    return m_templateId;
                }
            }

            public string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }

            public bool FetchForControl
            {
                get
                {
                    return m_fetchForControl;
                }
            }

        }

        [Serializable()]
        public class RapidResultReport
        {

            private RapidResult m_parent = null;
            private string m_accessionNbr = "";
            private int m_reportId = 0;
            private List<RapidResultAnalyte> m_list;
            private int m_accountStatusLevel = 0;
            private object m_rowNumber;

            private Report m_report = null; // Used to store Report when processing


            internal RapidResultReport(RapidResult parent, int reportId, string accessionNbr, int accountStatusLevel, int RowNumber = 0)
            {
                m_parent = parent;
                m_accessionNbr = accessionNbr;
                m_reportId = reportId;
                m_accountStatusLevel = accountStatusLevel;
                m_list = new List<RapidResultAnalyte>();
                m_rowNumber = RowNumber;
            }

            internal void Add(RapidResultAnalyte analyte)
            {
                m_list.Add(analyte);
            }

            public RapidResult Parent
            {
                get
                {
                    return m_parent;
                }
            }

            public int RowNumber
            {
                get
                {
                    return Conversions.ToInteger(m_rowNumber);
                }
            }

            public string AccessionNbr
            {
                get
                {
                    return m_accessionNbr;
                }
            }

            public int AccountStatusLevel
            {
                get
                {
                    return m_accountStatusLevel;
                }
            }

            public int ReportId
            {
                get
                {
                    return m_reportId;
                }
            }

            public List<RapidResultAnalyte> List
            {
                get
                {
                    return m_list;
                }
            }

            public bool NeedToPushResults
            {
                get
                {
                    foreach (RapidResultAnalyte r in m_list)
                    {
                        if (r.m_needToPushResult)
                            return true;
                    }
                    return false;
                }
            }

            /// <summary>
        /// Loads the Report for the RapidResultReport and sets the value accordingly and then saves the Report.
        /// </summary>
        /// <param name="markReleased">Set to true to release results for analytes with a ResultValue and a status of PendingResults.</param>
        /// <remarks></remarks>
            public RapidResultReport Process(bool markReleased)
            {

                Process(markReleased, false);

                return this;

            }

            public RapidResultReport Process(string analyteCode, transmitStatusType transmitStatus, bool saveReport = true)
            {

                ReportAnalyte ra;
                if (m_report == null)
                {
                    m_report = OrderManager.FetchReport(ReportId);
                }

                foreach (RapidResultAnalyte a in List)
                {
                    if (!a.IsReference)   // no changes to a reference analyte
                    {
                        if ((a.Analyte.Code ?? "") == (analyteCode ?? "") && !string.IsNullOrEmpty(a.ResultValue.Trim()))
                        {

                            // First search invidual analytes
                            ra = m_report.FindAnalyte(a.AnalyteCode, false);
                            if (!(ra == null) && !ra.HasBeenReleased())
                            {
                                ra.SetResultValue(a.ResultValue, true); // ra.ResultValue = a.ResultValue
                                if (ra.TransmitStatus == transmitStatusType.PendingRelease)
                                {
                                    if (transmitStatus == transmitStatusType.Released)
                                    {
                                        ra.MarkAsReleased();
                                        a.m_transmitStatus = transmitStatusType.Released; // Done for internal processing.
                                    }
                                    else if (transmitStatus == transmitStatusType.HeldForRerun)
                                    {
                                        ra.MarkAsHeldForRerun();
                                        a.m_transmitStatus = transmitStatusType.HeldForRerun; // Done for internal processing.
                                    }
                                }
                                else if (ra.TransmitStatus == transmitStatusType.HeldForRerun)
                                {
                                    if (transmitStatus == transmitStatusType.PendingRelease)
                                    {
                                        ra.MarkAsPendingReleased();
                                        a.m_transmitStatus = transmitStatusType.PendingRelease;
                                    }
                                }
                            }

                            // then Loop through all the panels
                            foreach (ReportAnalytePanel p in m_report.AnalytePanels.List)
                            {
                                ra = p.Analytes.FindFirst(a.AnalyteCode);
                                if (!(ra == null) && !ra.HasBeenReleased())
                                {
                                    if (ra.TransmitStatus == transmitStatusType.PendingRelease)
                                    {
                                        if (transmitStatus == transmitStatusType.Released)
                                        {
                                            ra.MarkAsReleased();
                                            a.m_transmitStatus = transmitStatusType.Released; // Done for internal processing.
                                        }
                                        else if (transmitStatus == transmitStatusType.HeldForRerun)
                                        {
                                            ra.MarkAsHeldForRerun();
                                            a.m_transmitStatus = transmitStatusType.HeldForRerun; // Done for internal processing.
                                        }
                                    }
                                    else if (ra.TransmitStatus == transmitStatusType.HeldForRerun)
                                    {
                                        if (transmitStatus == transmitStatusType.PendingRelease)
                                        {
                                            ra.MarkAsPendingReleased();
                                            a.m_transmitStatus = transmitStatusType.PendingRelease;
                                        }
                                    }
                                }
                            }

                        }
                    }
                }

                if (saveReport)
                {
                    SaveReport();
                }

                return this;

            }

            public void SaveReport()
            {
                if (!(m_report == null))
                {
                    if (m_report.IsValid)
                    {
                        m_report = (Report)m_report.Save();
                    }
                }
            }

            internal void ClearResults()
            {
                Report r;
                ReportAnalyte ra;

                r = OrderManager.FetchReport(ReportId);

                foreach (RapidResultAnalyte a in List)
                {
                    if (!a.IsReference)
                    {
                        ra = r.FindAnalyte(a.ReportAnalyteId);
                        if (!(ra == null) && !((int)ra.ResultStatus > 1))       // Don't permit changes to an already released analyte
                        {
                            ra.SetResultValue("", true, a.PerformingFacility);
                        }
                    }
                }

                if (r.IsValid)
                {
                    r = (Report)r.Save();
                }
            }
            internal RapidResultReport Process(bool markReleased, bool checkParent)
            {

                Report r;
                ReportAnalyte ra;

                r = OrderManager.FetchReport(ReportId);

                foreach (RapidResultAnalyte a in List)
                {
                    // If a.ResultValue.Trim() <> "" Then

                    if (!a.IsReference)
                    {
                        ra = r.FindAnalyte(a.ReportAnalyteId); // 'a.AnalyteCode, True)
                        if (!(ra == null) && !ra.HasBeenPreviouslyReleased())      // Don't permit changes to an already released analyte
                        {
                            ra.SetResultValue(a.ResultValue, true, a.PerformingFacility); // ra.ResultValue = a.ResultValue

                            if (!string.IsNullOrEmpty(a.CommentsToAdd))
                            {
                                ra.Comments.AddComment(a.CommentsToAdd);
                            }

                            if (!string.IsNullOrEmpty(ra.ResultValue) && markReleased && ra.TransmitStatus == transmitStatusType.PendingRelease)
                            {
                                ra.MarkAsReleased();
                                a.m_transmitStatus = transmitStatusType.Released; // Done for internal processing
                            }

                        }
                    }
                    // End If
                }

                if (r.IsValid)
                {
                    r = (Report)r.Save();
                }

                // *************************************************
                if (markReleased && checkParent)
                {
                    UpdateParent();
                }
                // *************************************************

                return this;

            }

            /// <summary>
        /// Since each can be processed individually, we have to check ALL rapidresult reports
        /// and mark the RapidResult IsComplete if all are released.
        /// </summary>
        /// <remarks></remarks>
            public void UpdateParent()
            {
                bool released = true;
                foreach (RapidResultReport report in Parent.FetchRapidResultReports())
                {
                    foreach (RapidResultAnalyte analyte in report.List)
                    {
                        if (analyte.TransmitStatus != transmitStatusType.Released)
                        {
                            released = false;
                            break;
                        }
                    }
                    if (released == false)
                        break;
                }

                if (released)
                {
                    Parent.m_isCompleted = true;
                    Parent.Save();
                }

            }

        }

        [Serializable()]
        public class AnalyteInfo : IComparable
        {

            private string m_name = "";
            private string m_code = "";
            private int m_sortOrder = 0;

            private RefAnalyte m_analyte = null;

            public AnalyteInfo(int sortOrder, RefAnalyte analyte, string shortName)
            {
                // m_name = analyte.Name
                m_name = shortName;
                m_code = analyte.Code;
                m_sortOrder = sortOrder;
                m_analyte = analyte;
            }

            public RefAnalyte Analyte
            {
                get
                {
                    return m_analyte;
                }
            }

            public int SortOrder
            {
                get
                {
                    return m_sortOrder;
                }
            }

            public string Name
            {
                get
                {
                    return m_name;
                }
            }

            public string Code
            {
                get
                {
                    return m_code;
                }
            }

            public int CompareTo(object obj)
            {

                if (!ReferenceEquals(obj.GetType(), typeof(AnalyteInfo)))
                {
                    throw new ArgumentException();
                }

                AnalyteInfo a = (AnalyteInfo)obj;
                if (a.SortOrder < m_sortOrder)
                {
                    return 1;
                }
                else if (a.SortOrder == m_sortOrder)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }

            }

            ~AnalyteInfo()
            {
            }
        }



    }
}