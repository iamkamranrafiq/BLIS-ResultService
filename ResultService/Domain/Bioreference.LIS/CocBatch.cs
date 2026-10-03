using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Bioreference.LIS.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CocBatch : AuditDataClassBase
    {

        private int m_id = 0;
        private string m_batchName = "";
        private string m_batchMM = "";
        private string m_batchDD = "";
        private string m_batchYYYY = "";
        private string m_startingAccessionNbr = "";
        private string m_endingAccessionNbr;
        private bool m_isClosed = false;

        private List<CocBatchAccession> m_list;
        private List<CocBatchAccession> m_deleteList;
        private DateTime m_dateCreated = DateTime.Parse("1900-01-01");
        private string m_createdBy = "";

        #region Constructor

        internal CocBatch()
        {
            m_list = new List<CocBatchAccession>();
            m_deleteList = new List<CocBatchAccession>();
        }
        internal CocBatch(int cocBatchId, string cocBatchName, string cocBatchMM, string cocBatchDD, string cocBatchYYYY, string startingAccessionNbr, string endingAccessionNbr)
        {
            m_id = cocBatchId;
            m_batchName = cocBatchName;
            m_batchMM = cocBatchMM;
            m_batchDD = cocBatchDD;
            m_batchYYYY = cocBatchYYYY;
            m_startingAccessionNbr = startingAccessionNbr;
            m_endingAccessionNbr = endingAccessionNbr;
            m_list = new List<CocBatchAccession>();
            m_deleteList = new List<CocBatchAccession>();
            FlagDirty();
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

        public CocBatchAccession[] List
        {
            get
            {
                return m_list.ToArray();
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

        public string BatchName
        {
            get
            {
                return m_batchName;
            }
        }

        public string batchMM
        {
            get
            {
                return m_batchMM;
            }
        }

        public string batchDD
        {
            get
            {
                return m_batchDD;
            }
        }

        public string batchYYYY
        {
            get
            {
                return m_batchYYYY;
            }
        }

        public string BatchStartingAccession
        {
            get
            {
                return m_startingAccessionNbr;
            }
        }

        public string BatchEndingAccession
        {
            get
            {
                return m_endingAccessionNbr;
            }
        }

        #endregion

        #region  Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();
            DataTable[] dt;

            try
            {

                if (!c.SearchType)
                {
                    paramList.Add((DbParameter)da.CreateParameter("@CocBatchId", DbType.Int32, c.CocBatchId));

                    DbParameter[] @params = paramList.ToArray();
                    dt = da.ExecuteProcedure("lis_CocBatch_Fetch", @params);
                }
                else
                {
                    paramList.Add((DbParameter)da.CreateParameter("@CocBatchId", DbType.Int32, c.CocBatchId));
                    paramList.Add((DbParameter)da.CreateParameter("@CocBatchName", DbType.String, c.CocBatchName));
                    paramList.Add((DbParameter)da.CreateParameter("@AccessionStart", DbType.String, c.StartingAccessionNumber));
                    paramList.Add((DbParameter)da.CreateParameter("@AccessionEnd", DbType.String, c.EndingAccessionNumber));

                    DbParameter[] @params = paramList.ToArray();
                    dt = da.ExecuteProcedure("lis_CocBatch_Find", @params);
                }

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0].Rows[0]);
                    if (dt.Length > 1)
                    {
                        LoadSpecimens(dt[1]);
                    }
                }

                FlagClean();
            }

            catch (Exception ex)
            {
                Trace.Write(ex.ToString());
                throw ex;
            }

        }

        protected override void DataFactory_Save()
        {
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@CocBatchId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@CocBatchName", DbType.String, m_batchName));
            paramList.Add((DbParameter)da.CreateParameter("@BatchMM", DbType.String, m_batchMM));
            paramList.Add((DbParameter)da.CreateParameter("@BatchDD", DbType.String, m_batchDD));
            paramList.Add((DbParameter)da.CreateParameter("@BatchYYYY", DbType.String, m_batchYYYY));
            paramList.Add((DbParameter)da.CreateParameter("@AccessionStart", DbType.String, m_startingAccessionNbr));
            paramList.Add((DbParameter)da.CreateParameter("@AccessionEnd", DbType.String, m_endingAccessionNbr));
            if (!(CurrentUser == null))
            {
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));
            }
            else
            {
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, ""));
            }

            DbParameter[] @params = paramList.ToArray();
            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_CocBatch_Save", @params)["@CocBatchId"].Value);

        }

        #endregion

        internal void Load(DataRow row)
        {
            m_id = Conversions.ToInteger(row["CocBatchId"]);
            m_batchName = Conversions.ToString(row["CocBatchName"]);
            m_batchMM = Conversions.ToString(row["batchMM"]);
            m_batchDD = Conversions.ToString(row["batchDD"]);
            m_batchYYYY = Conversions.ToString(row["batchYYYY"]);
            m_startingAccessionNbr = Conversions.ToString(row["AccessionStart"]);
            m_endingAccessionNbr = Conversions.ToString(row["AccessionEnd"]);
            m_isClosed = Conversions.ToBoolean(row["IsClosed"]);
        }

        internal void LoadSpecimens(DataTable dt)
        {
            m_list.Clear();

            CocBatchAccession ba;
            foreach (DataRow r in dt.Rows)
            {
                ba = new CocBatchAccession(this);
                ba.Load(r);
                m_list.Add(ba);
            }
        }

        #region Public Functions
        public static CocBatchCreateResponse CreateNew(string cocBatchMM, string cocBatchDD, string cocBatchYYYY, string startingAccessionNbr, string endingAccessionNbr)
        {
            // determine if
            // 1) startingAccessionNbr is less than ending.
            // 1) batch name does not already exist
            // 2) Accession sequence does not overlap with an existing batch

            try
            {

                var objCocBatchValidationResp = ValidateBatchInfo(cocBatchMM, cocBatchDD, cocBatchYYYY, startingAccessionNbr, endingAccessionNbr);
                if (objCocBatchValidationResp.CocBatchValidationStatus == CocBatchValidationStatus.Failure)
                {
                    return new CocBatchCreateResponse(null, (int)CocBatchCreateStatus.Failure, objCocBatchValidationResp.CocBatchValidationResponseMessage);
                }

                var baSearch = Fetch(objCocBatchValidationResp.BatchName, objCocBatchValidationResp.BeginAcc, objCocBatchValidationResp.EndAcc);

                if (baSearch is not null && baSearch.Id > 0)
                {
                    // if the BatchNames match,
                    if ((baSearch.BatchName.ToUpper() ?? "") == (objCocBatchValidationResp.BatchName.ToUpper() ?? ""))
                    {
                        return new CocBatchCreateResponse(null, (int)CocBatchCreateStatus.Failure, "Batch Name Already Exists.");
                    }
                    else
                    {
                        return new CocBatchCreateResponse(null, (int)CocBatchCreateStatus.Failure, "Accession Series Overlaps an Existing Batch : " + baSearch.BatchName);
                    }
                }

                // good.  We can create the batch
                var ba = new CocBatch(0, objCocBatchValidationResp.BatchName, objCocBatchValidationResp.BatchMM, objCocBatchValidationResp.BatchDD, objCocBatchValidationResp.BatchYYYY, objCocBatchValidationResp.BeginAcc, objCocBatchValidationResp.EndAcc);
                ba.Save();

                // fetch the complete object now.  The SAVE dose not fetch the complete object... IE the accessions.

                ba = Fetch(ba.Id);

                var objCocCreatResp = new CocBatchCreateResponse(ba, (int)CocBatchCreateStatus.Success);

                return objCocCreatResp;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private static CocBatchValidationResponse ValidateBatchInfo(string cocBatchMM, string cocBatchDD, string cocBatchYYYY, string startingAccessionNbr, string endingAccessionNbr)
        {
            string sResponseMessage = "";
            string sBatchName = "";
            string sBatchMM = "";
            string sBatchDD = "";
            string sBatchYYYY = "";
            string sBeginAcc = "";
            string sEndAcc = "";
            List<int> invalidAccessions = new List<int>();

            var objRetVal = new CocBatchValidationResponse();

            if (cocBatchMM.Trim().Length == 0 || cocBatchDD.Trim().Length == 0 || cocBatchYYYY.Trim().Length == 0)
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Batch MM DD YYYY Required.";
                return objRetVal;
            }

            sBatchMM = ValidateBatchName(cocBatchMM, 2);
            if (sBatchMM.Length == 0)
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Batch MM Invalid.";
                return objRetVal;
            }
            objRetVal.BatchMM = sBatchMM;

            sBatchDD = ValidateBatchName(cocBatchDD, 2);
            if (sBatchMM.Length == 0)
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Batch DD Invalid.";
                return objRetVal;
            }
            objRetVal.BatchDD = sBatchDD;

            sBatchYYYY = ValidateBatchName(cocBatchYYYY, 4);
            if (sBatchMM.Length == 0)
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Batch YYYY Invalid.";
                return objRetVal;
            }
            objRetVal.BatchYYYY = sBatchYYYY;

            sBatchName = sBatchMM + "/" + sBatchDD + "/" + sBatchYYYY;
            if (!ValidDate(sBatchName))
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Invalid Batch Name.";
                return objRetVal;
            }
            objRetVal.BatchName = sBatchName;

            sBeginAcc = ValidateAccession(startingAccessionNbr.Trim());
            if (sBeginAcc.Length == 0)
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Invalid Starting Accession Number.";
                return objRetVal;
            }

            sEndAcc = ValidateAccession(endingAccessionNbr.Trim());
            if (sEndAcc.Length == 0)
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Invalid Ending Accession Number.";
                return objRetVal;
            }

            int iStartAcc = int.Parse(sBeginAcc);
            int iEndAcc = int.Parse(sEndAcc);

            if (iStartAcc >= iEndAcc)
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Starting Accession Number Must Precede Ending Accession Number.";
                return objRetVal;
            }

            for(int i = iStartAcc;i<=iEndAcc;i++)
            {
                if(!OrderManager.OrderExists(i.ToString()))
                {
                    invalidAccessions.Add(i);
                }
            }

            if (invalidAccessions.Count > 0)
            {
                objRetVal.CocBatchValidationStatus = CocBatchValidationStatus.Failure;
                objRetVal.CocBatchValidationResponseMessage = "Following Accession Number(s) "+ string.Join(",",invalidAccessions)  +" are not existent.";
                return objRetVal;
            }

            objRetVal.BeginAcc = sBeginAcc;
            objRetVal.EndAcc = sEndAcc;

            return objRetVal;
        }

        private static string ValidateAccession(string accessionNbr)
        {
            int iAcc = 0;
            string sAcc = "";

            if (accessionNbr.Trim().Length == 0)
                return "";

            try
            {
                iAcc = int.Parse(accessionNbr.Trim());
            }
            catch (Exception ex)
            {
                return "";
            }

            sAcc = iAcc.ToString();

            if (sAcc.Trim().Length != 9 || sAcc.Trim().Substring(0, 2) != "19")
            {
                return "";
            }

            return sAcc;

        }

        private static string ValidateBatchName(string batchPart, int digitCount)
        {
            string rVal = "";
            int iValue = 0;
            string sFormat = "";

            try
            {
                iValue = int.Parse(batchPart.Trim());
                if (iValue == 0)
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                return "";
            }

            if (digitCount == 2)
            {
                sFormat = "00";
            }
            else
            {
                sFormat = "0000";
            }

            rVal = Strings.Format(iValue, sFormat);

            return rVal;
        }

        private static bool ValidDate(string sValue)
        {
            try
            {
                var sDate = Convert.ToDateTime(sValue, System.Globalization.CultureInfo.InvariantCulture);

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;

        }


        public static void Delete(int CocBatchId)
        {

            // DataFactory.Delete(New Criteria(rackWorksheetId))

        }

        public static CocBatch Fetch(int CocBatchId)
        {

            return (CocBatch)DataFactory.Fetch(new Criteria(CocBatchId));

        }

        public static CocBatch Fetch(string cocBatchName, string startingAccessionNbr, string endingAccessionNbr)
        {

            return (CocBatch)DataFactory.Fetch(new Criteria(cocBatchName, startingAccessionNbr, endingAccessionNbr));

        }
        public static CocBatch Fetch(int cocBatchId, string cocBatchName, string startingAccessionNbr, string endingAccessionNbr)
        {
            return (CocBatch)DataFactory.Fetch(new Criteria(cocBatchId, cocBatchName, startingAccessionNbr, endingAccessionNbr));
        }
        public static CocBatchAccessionEditResponse SetBatchStatus(int cocBatchId, bool isClosed, string userName)
        {
            try
            {
                var da = new DataWrapper(Configuration.ConnectionString);
                var paramList = new List<DbParameter>();

                paramList.Add((DbParameter)da.CreateParameter("@CocBatchId", DbType.Int32, cocBatchId));
                paramList.Add((DbParameter)da.CreateParameter("IsClosed", DbType.Boolean, isClosed));
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, userName));

                DbParameter[] @params = paramList.ToArray();
                da.ExecuteNonQuery("lis_CocBatch_SetStatus", @params);

                return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Success, "");
            }
            catch (Exception ex)
            {
                return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Failure, "Unable to set batch status batch. " + ex.Message);
            }

        }

        public CocBatchAccessionEditResponse RemoveAccession(CocBatchAccession batchAccession)
        {

            try
            {
                batchAccession.Delete();
                m_list.Remove(batchAccession);
                return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Success, "");
            }
            catch (Exception ex)
            {
                return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Failure, "Unable to delete Accession. " + ex.Message);
            }

        }

        // Public Sub RemoveSpecimen(ByVal specimenId As Integer)

        // For Each s As CocBatchAccession In m_list
        // If specimenId = s.ID Then
        // s.MarkForDelete()
        // Me.m_deleteList.Add(s)
        // End If
        // Next

        // End Sub

        public CocBatchAccessionEditResponse AddAccession(string accessionNbr)
        {

            foreach (CocBatchAccession s in m_list)
            {
                if ((s.AccessionNbr ?? "") == (accessionNbr.Trim() ?? ""))
                {
                    return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Failure, "Accession Already Exists");
                }
            }

            if (accessionNbr.Trim().Length == 0)
            {
                return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Failure, "Accession Number Missing.");
            }

            int iNewAccession;
            try
            {
                iNewAccession = int.Parse(accessionNbr.Trim());
            }
            catch (Exception ex)
            {
                return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Failure, "Invalid Accession Number.");
            }

            if (!OrderManager.OrderExists(accessionNbr))
            {
                return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Failure, "Accession Number is not existent.");
            }

            int iStartingAccession = int.Parse(m_startingAccessionNbr.Trim());
            int iEndAccession = int.Parse(m_endingAccessionNbr.Trim());

            if (iNewAccession >= iStartingAccession && iNewAccession <= iEndAccession)
            {
                var ba = AppendAccession(accessionNbr);
                if (ba is not null)
                {
                    return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Success, "Accession Added.");
                }
                else
                {
                    return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Failure, "Unable to add Accession.");
                }
            }
            else
            {
                return new CocBatchAccessionEditResponse((int)CocBatchAccessionEditStatus.Failure, "Accession Number outside of range.  Please expand range to include new Accession.");
            }

        }

        private CocBatchAccession AppendAccession(string accessionNbr)
        {
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();
            DataTable[] dt;

            paramList.Add((DbParameter)da.CreateParameter("@CocBatchId", DbType.Int32, m_id));
            paramList.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, accessionNbr));
            if (!(CurrentUser == null))
            {
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));
            }
            else
            {
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, ""));
            }

            DbParameter[] @params = paramList.ToArray();
            dt = da.ExecuteProcedure("lis_CocBatchAccession_Append", @params);

            CocBatchAccession ba = null;

            if (dt[0].Rows.Count > 0)
            {
                ba = new CocBatchAccession(this);
                ba.Load(dt[0].Rows[0]);
                m_list.Add(ba);
            }

            ba.FlagClean(true);

            return ba;

        }

        public static CocBatchCreateResponse ModifyBatchSetup(int cocBatchId, string cocBatchMM, string cocBatchDD, string cocBatchYYYY, string startingAccessionNbr, string endingAccessionNbr)
        {
            var ba = Fetch(cocBatchId);

            if (ba is null)
            {
                return new CocBatchCreateResponse(null, (int)CocBatchCreateStatus.Failure, "Unable to find batch.");
            }

            // nart here.  we modified the Save PROC to handle both create and modify.
            // think of how we want to do this routine.  We might want to make it a Shared one as well and do like the create but send the ID along. 
            // and then do a fetch on the new object.

            // if the incoming info is the same as what we have, then just return Fail
            if ((cocBatchMM ?? "") == (ba.batchMM ?? "") && (cocBatchDD ?? "") == (ba.batchDD ?? "") && (cocBatchYYYY ?? "") == (ba.batchYYYY ?? "") && (startingAccessionNbr ?? "") == (ba.BatchStartingAccession ?? "") && (endingAccessionNbr ?? "") == (ba.BatchEndingAccession ?? ""))
            {
                return new CocBatchCreateResponse(null, (int)CocBatchCreateStatus.Failure, "No Changes Detected.");
            }

            try
            {
                var objCocBatchValidationResp = ValidateBatchInfo(cocBatchMM, cocBatchDD, cocBatchYYYY, startingAccessionNbr, endingAccessionNbr);
                if (objCocBatchValidationResp.CocBatchValidationStatus == CocBatchValidationStatus.Failure)
                {
                    return new CocBatchCreateResponse(null, (int)CocBatchCreateStatus.Failure, objCocBatchValidationResp.CocBatchValidationResponseMessage);
                }

                var baSearch = Fetch(cocBatchId, objCocBatchValidationResp.BatchName, objCocBatchValidationResp.BeginAcc, objCocBatchValidationResp.EndAcc);

                if (baSearch is not null && baSearch.Id > 0)
                {
                    // if the BatchNames match,
                    if ((baSearch.BatchName.ToUpper() ?? "") == (objCocBatchValidationResp.BatchName.ToUpper() ?? ""))
                    {
                        return new CocBatchCreateResponse(null, (int)CocBatchCreateStatus.Failure, "Batch Already Exists.");
                    }
                    else
                    {
                        return new CocBatchCreateResponse(null, (int)CocBatchCreateStatus.Failure, "Accession Series Overlaps an Existing Batch : " + baSearch.BatchName);
                    }
                }

                // good.  We can create the batch
                var baNew = new CocBatch(cocBatchId, objCocBatchValidationResp.BatchName, objCocBatchValidationResp.BatchMM, objCocBatchValidationResp.BatchDD, objCocBatchValidationResp.BatchYYYY, objCocBatchValidationResp.BeginAcc, objCocBatchValidationResp.EndAcc);
                baNew.Save();

                // fetch the complete object now.  The SAVE dose not fetch the complete object... IE the accessions.

                baNew = Fetch(baNew.Id);

                var objCocCreatResp = new CocBatchCreateResponse(baNew, (int)CocBatchCreateStatus.Success);

                return objCocCreatResp;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region Criteria

        [Serializable()]
        internal class Criteria
        {
            private int m_cocBatchId = 0;
            private string m_cocBatchName = "";
            private string m_startingAccessionNbr = "";
            private string m_endingAccessionNbr = "";
            private bool m_searchType = false;

            public Criteria(int cocBatchId)
            {
                m_cocBatchId = cocBatchId;
            }

            public Criteria(string cocBatchName, string startingAccessionNbr, string endingAccessionNbr)
            {
                m_cocBatchName = cocBatchName;
                m_startingAccessionNbr = startingAccessionNbr;
                m_endingAccessionNbr = endingAccessionNbr;
                m_searchType = true;
            }

            public Criteria(int cocBatchId, string cocBatchName, string startingAccessionNbr, string endingAccessionNbr)
            {
                m_cocBatchId = cocBatchId;
                m_cocBatchName = cocBatchName;
                m_startingAccessionNbr = startingAccessionNbr;
                m_endingAccessionNbr = endingAccessionNbr;
                m_searchType = true;
            }

            public bool SearchType
            {
                get
                {
                    return m_searchType;
                }
            }

            public int CocBatchId
            {
                get
                {
                    return m_cocBatchId;
                }
            }

            public string CocBatchName
            {
                get
                {
                    return m_cocBatchName;
                }
            }

            public string StartingAccessionNumber
            {
                get
                {
                    return m_startingAccessionNbr;
                }
            }

            public string EndingAccessionNumber
            {
                get
                {
                    return m_endingAccessionNbr;
                }
            }
        }

        #endregion

    }


    [Serializable()]
    public class CocBatchValidationResponse
    {

        private CocBatchValidationStatus m_validationStatus = CocBatchValidationStatus.Success;
        private string m_validationResponseMessage = "";
        private string m_BatchName = "";
        private string m_BatchMM = "";
        private string m_BatchDD = "";
        private string m_BatchYYYY = "";
        private string m_BeginAcc = "";
        private string m_EndAcc = "";

        internal CocBatchValidationResponse()
        {
        }

        public CocBatchValidationStatus CocBatchValidationStatus
        {
            get
            {
                return m_validationStatus;
            }
            set
            {
                m_validationStatus = value;
            }
        }

        public string CocBatchValidationResponseMessage
        {
            get
            {
                return m_validationResponseMessage;
            }
            set
            {
                m_validationResponseMessage = value.NormalizeToWindows();
            }
        }

        public string BatchName
        {
            get
            {
                return m_BatchName;
            }
            set
            {
                m_BatchName = value;
            }
        }

        public string BatchMM
        {
            get
            {
                return m_BatchMM;
            }
            set
            {
                m_BatchMM = value;
            }
        }

        public string BatchDD
        {
            get
            {
                return m_BatchDD;
            }
            set
            {
                m_BatchDD = value;
            }
        }

        public string BatchYYYY
        {
            get
            {
                return m_BatchYYYY;
            }
            set
            {
                m_BatchYYYY = value;
            }
        }

        public string BeginAcc
        {
            get
            {
                return m_BeginAcc;
            }
            set
            {
                m_BeginAcc = value;
            }
        }

        public string EndAcc
        {
            get
            {
                return m_EndAcc;
            }
            set
            {
                m_EndAcc = value;
            }
        }
    }

    [Serializable()]
    public class CocBatchCreateResponse
    {

        private CocBatch m_cocBatch;
        private CocBatchCreateStatus m_createStatus = CocBatchCreateStatus.Success;
        private string m_createResponseMessage = "";

        internal CocBatchCreateResponse(CocBatch cocBAtchObject, int iCreateStatus, string responseMessage = "")
        {
            m_cocBatch = cocBAtchObject;
            m_createStatus = (CocBatchCreateStatus)iCreateStatus;
            m_createResponseMessage = responseMessage.NormalizeToWindows();
        }

        public CocBatch CocBatchInstance
        {
            get
            {
                return m_cocBatch;
            }
        }

        public CocBatchCreateStatus CocBatchCreateStatus
        {
            get
            {
                return m_createStatus;
            }
        }

        public string CocBatchCreateResponseMessage
        {
            get
            {
                return m_createResponseMessage;
            }
        }
    }

    [Serializable()]
    public class CocBatchAccessionEditResponse
    {

        private CocBatchAccessionEditStatus m_editResponseStatus = CocBatchAccessionEditStatus.Success;
        private string m_editResponseMessage = "";

        internal CocBatchAccessionEditResponse(int iResponseStatus, string responseMessage = "")
        {
            m_editResponseStatus = (CocBatchAccessionEditStatus)iResponseStatus;
            m_editResponseMessage = responseMessage.NormalizeToWindows();
        }

        public CocBatchAccessionEditStatus CocBatchAccessionEditStatus
        {
            get
            {
                return m_editResponseStatus;
            }
        }

        public string CocBatchEditResponseMessage
        {
            get
            {
                return m_editResponseMessage;
            }
        }
    }

    [Serializable()]
    public class CocBatchReport
    {

        private int m_cocBatchId = 0;
        private string m_cocBatchName = "";
        private string m_cocStartAccession = "";
        private string m_cocEndAccession = "";
        private DateTime m_dateCreated;
        private string m_createdby = "";
        private bool m_isClosed = false;

        private List<CocBatchReportItem> m_list;

        internal CocBatchReport(int cocBatchId, string cocBatchName, string cocStartAccession, string cocEndAccession, bool isClosed, DateTime dateCreated, string createdBy)
        {
            m_cocBatchId = cocBatchId;
            m_cocBatchName = cocBatchName;
            m_cocStartAccession = cocStartAccession;
            m_cocEndAccession = cocEndAccession;
            m_isClosed = isClosed;
            m_dateCreated = dateCreated;
            m_createdby = createdBy;

            m_list = new List<CocBatchReportItem>();
        }

        public int GetTotalReleased()
        {

            int released = 0;
            foreach (CocBatchReportItem i in m_list)
            {
                if (i.TransmitStatus != transmitStatusType.PendingRelease && i.TransmitStatus != transmitStatusType.NotSet)
                {
                    released += 1;
                }
            }

            return released;

        }

        public bool IsHasReleasedAccessions()
        {
            bool isReleased = false;
            foreach (CocBatchReportItem i in m_list)
            {
                if (i.TransmitStatus == transmitStatusType.Released && i.ResultStatus == resultStatusType.Final)
                {
                    isReleased = true;
                }
            }

            return isReleased;
        }

        public List<CocBatchReportItem> List
        {
            get
            {
                return m_list;
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
                return m_createdby;
            }
        }

        public string CocBatchName
        {
            get
            {
                return m_cocBatchName;
            }
        }

        public int CocBatchId
        {
            get
            {
                return m_cocBatchId;
            }
        }

        public string CocStartAccession
        {
            get
            {
                return m_cocStartAccession;
            }
        }

        public string CocEndAccession
        {
            get
            {
                return m_cocEndAccession;
            }
        }

        public bool IsClosed
        {
            get
            {
                return m_isClosed;
            }
        }
    }

    [Serializable()]
    public class CocBatchReportItem
    {

        private int m_cocBatchId = 0;
        private int m_cocBatchAccessionId = 0;
        private int m_reportId = 0;
        private string m_accessionNbr = "";
        private DateTime m_dateServiced;
        private string m_patientName = "";

        private resultStatusType m_resultStatus;
        private transmitStatusType m_transmitStatus;
        private int m_pendingCount = 0;
        private int m_alertCount = 0;
        private int m_rreCount = 0;

        internal CocBatchReportItem()
        {
        }

        public int CocBatchId
        {
            get
            {
                return m_cocBatchId;
            }
        }

        public int CocBatchAccessionId
        {
            get
            {
                return m_cocBatchAccessionId;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public DateTime DateServiced
        {
            get
            {
                return m_dateServiced;
            }
        }

        public string PatientName
        {
            get
            {
                return m_patientName;
            }
        }

        public resultStatusType ResultStatus
        {
            get
            {
                return m_resultStatus;
            }
        }

        public transmitStatusType TransmitStatus
        {
            get
            {
                return m_transmitStatus;
            }
        }

        public int PendingCount
        {
            get
            {
                return m_pendingCount;
            }
        }

        public int AlertCount
        {
            get
            {
                return m_alertCount;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
        }

        public int RRECount
        {
            get
            {
                return m_rreCount;
            }
        }

        internal void Load(DataRow row)
        {

            m_cocBatchId = Conversions.ToInteger(row["CocBatchId"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            m_cocBatchAccessionId = Conversions.ToInteger(row["CocBatchAccessionId"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);
            m_dateServiced = Conversions.ToDate(row["DateServiced"]);
            m_resultStatus = (resultStatusType)Conversions.ToInteger(row["ResultStatus"]);
            m_pendingCount = Conversions.ToInteger(row["PendingCount"]);
            m_alertCount = Conversions.ToInteger(row["AlertCount"]);
            m_transmitStatus = (transmitStatusType)Conversions.ToInteger(row["TransmitStatus"]);
            m_patientName = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(row["LastName"], ", "), row["FirstName"]));
            m_rreCount = Conversions.ToInteger(row["RRECount"]);

        }

    }
}