using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Transactions;
using Bioreference.Data;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RackWorksheet : AuditDataClassBase
    {

        #region Private members

        private int m_rackWorksheetTemplateId;
        private int m_id = 0;
        private List<RackWorksheetSpecimen> m_list;
        private List<RackWorksheetSpecimen> m_deleteList;
        private rackWorksheetType m_type;
        private bool m_isClosed;

        private DateTime m_dateCreated = DateTime.Parse("1900-01-01");
        private string m_createdBy = "";

        internal int m_currentSeq = 0;
        private string m_currentUser;

        #endregion

        #region Constructor

        internal RackWorksheet()
        {
            m_list = new List<RackWorksheetSpecimen>();
            m_deleteList = new List<RackWorksheetSpecimen>();
            m_currentUser = base.CurrentUser.Name;
        }

        internal RackWorksheet(RackWorksheetTemplate template)
        {
            m_list = new List<RackWorksheetSpecimen>();
            m_deleteList = new List<RackWorksheetSpecimen>();
            m_rackWorksheetTemplateId = template.Id;
            m_type = template.Type;
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

        public RackWorksheetSpecimen[] List
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

        public int RackWorksheetTemplateId
        {
            get
            {
                return m_rackWorksheetTemplateId;
            }
        }

        public rackWorksheetType RackWorksheetType
        {
            get
            {
                return m_type;
            }
        }

        public bool IsClosed
        {
            get
            {
                return m_isClosed;
            }
            set
            {
                if (value != m_isClosed)
                {
                    m_isClosed = value;
                    FlagDirty();
                }
            }
        }

        public new string CurrentUser
        {
            get
            {
                return m_currentUser;
            }

        }

        #endregion

        #region Public Functions

        public static RackWorksheet CreateNew(RackWorksheetTemplate template)
        {

            var ws = new RackWorksheet(template);
            return ws;

        }

        public static RackWorksheet CreateNew()
        {

            var ws = new RackWorksheet();
            return ws;

        }

        public static void Delete(int rackWorksheetId)
        {

            DataFactory.Delete(new Criteria(rackWorksheetId));

        }

        public static RackWorksheet Fetch(int rackWorksheetId)
        {

            return (RackWorksheet)DataFactory.Fetch(new Criteria(rackWorksheetId));

        }

        public static void AuditReleaseWorksheet(int rackWorksheetId, string user)
        {
            AuditManager.LogCustomObjectAction(rackWorksheetId.ToString(), "Bioreference.LIS.RackWorkSheet", string.Format("Release Worksheet #{0} by user {1}", rackWorksheetId, user));
        }

        public void RemoveSpecimen(RackWorksheetSpecimen specimen)
        {

            // Do not remove it from the list. we still want to be able to see it, until it is saved.
            // Me.m_list.Remove(specimen)
            specimen.MarkForDelete();
            m_deleteList.Add(specimen);

        }

        public void RemoveSpecimen(int specimenId)
        {

            foreach (RackWorksheetSpecimen s in m_list)
            {
                if (specimenId == s.ID)
                {
                    s.MarkForDelete();
                    m_deleteList.Add(s);
                }
            }

        }

        public AddAccessionStatusType AddSpecimen(string accessionNbr, string rackId, int rackPos)
        {
            return AddSpecimen(accessionNbr, rackId, rackPos, "0159");
        }

        public AddAccessionStatusType AddSpecimen(string accessionNbr, string rackId, int rackPos, string panelCode)
        {

            foreach (RackWorksheetSpecimen s in m_list)
            {
                if ((s.AccessionNbr ?? "") == (accessionNbr ?? ""))
                {
                    return AddAccessionStatusType.AlreadyExists;
                }
            }

            var wo = OrderManager.FetchOrderWorksheet(accessionNbr, RackWorksheetType, panelCode);
            if (wo == null) // Not OrderManager.OrderExists(accessionNbr) Then
            {

                return AddAccessionStatusType.TypeNotOnAccession;
            }

            else if (wo.Status != resultStatusType.Pending)
            {

                return AddAccessionStatusType.InvalidStatus;
            }

            else
            {

                var ws = RackWorksheets.Fetch(accessionNbr);

                foreach (RackWorksheetReport r in ws.List)
                {
                    if (r.RackWorksheetTemplateId == RackWorksheetTemplateId)
                    {
                        return AddAccessionStatusType.AlreadyExists;
                    }
                }

                m_currentSeq += 1;
                var s = new RackWorksheetSpecimen(this, rackId, rackPos, m_currentSeq, accessionNbr, wo.Has4kTest);

                m_list.Add(s);

                return AddAccessionStatusType.Success;

            }

        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["RackWorksheetId"]);
            m_rackWorksheetTemplateId = Conversions.ToInteger(row["RackWorksheetTemplateId"]);
            m_dateCreated = Conversions.ToDate(row["DateCreated"]);
            m_createdBy = Conversions.ToString(row["CreatedBy"]);
            m_type = (rackWorksheetType)Conversions.ToInteger(row["Type"]);
            m_isClosed = Conversions.ToBoolean(row["IsClosed"]);
        }

        private void LoadSpecimens(DataTable table)
        {

            m_list.Clear();

            RackWorksheetSpecimen sp;
            foreach (DataRow r in table.Rows)
            {
                sp = new RackWorksheetSpecimen(this);
                sp.Load(r);
                m_list.Add(sp);
                m_currentSeq = sp.Sequence;
            }


        }

        public bool WorksheetAssign(int rackWorksheetId, RackWorksheets m_worksheets)
        {
            bool IsAlreadyAssigned = false;
            var da = new DataWrapper(Configuration.ConnectionString);
            try
            {
                var @params = new DbParameter[3];

                @params[0] = (DbParameter)da.CreateParameter("@RackWorksheetId", DbType.Int32, rackWorksheetId);
                @params[1] = (DbParameter)da.CreateParameter("@UserName", DbType.String, base.CurrentUser.Name, ParameterDirection.InputOutput);
                @params[2] = (DbParameter)da.CreateParameter("@IsAlreadyAssigned", DbType.Boolean, 0, ParameterDirection.InputOutput);
                @params[1].Size = 50;

                var returnParams = da.ExecuteNonQuery("lis_RackWorksheet_Assign", @params);
                IsAlreadyAssigned = Conversions.ToBoolean(returnParams["@IsAlreadyAssigned"].Value);
                m_worksheets.AssignedUser = Conversions.ToString(returnParams["@UserName"].Value);
                AuditManager.LogCustomObjectAction(rackWorksheetId.ToString(), "Bioreference.LIS.RackWorkSheet", string.Format("Assigned Worksheet #{0} to user {1}", rackWorksheetId, base.CurrentUser.Name));
            }
            catch (Exception ex)
            {
                throw;
            }
            return IsAlreadyAssigned;
        }

        public void WorksheetUnAssign(int rackWorksheetId, string currentAssignedUser)
        {
            try
            {
                var da = new DataWrapper(Configuration.ConnectionString);
                var @params = new List<DbParameter>();

                @params.Add((DbParameter)da.CreateParameter("@RackWorksheetId", DbType.Int32, rackWorksheetId));

                DbParameter[] pList = @params.ToArray();
                da.ExecuteNonQuery("lis_RackWorksheet_Unassign", pList);
                AuditManager.LogCustomObjectAction(rackWorksheetId.ToString(), "Bioreference.LIS.RackWorkSheet", string.Format("Unassigned Worksheet #{0} from user {1}, by user {2}", rackWorksheetId, Interaction.IIf(string.IsNullOrEmpty(currentAssignedUser), "NONE", currentAssignedUser), base.CurrentUser.Name));
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@RackWorksheetId", DbType.Int32, c.RackWorksheetId);

            DataTable[] dt = da.ExecuteProcedure("lis_RackWorksheet_Fetch", @param);

            if (dt[0].Rows.Count > 0)
            {
                Load(dt[0].Rows[0]);
                LoadSpecimens(dt[1]);
            }

            FlagClean();

        }

        protected override void DataFactory_Delete(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@RackWorksheetId", DbType.Int32, c.RackWorksheetId);

            da.ExecuteNonQuery("lis_RackWorksheet_Delete", @param);

            FlagDeleted();
            FlagClean(true);


        }

        protected override void DataFactory_Save()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();

            @params.Add((DbParameter)da.CreateParameter("@RackWorksheetId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            @params.Add((DbParameter)da.CreateParameter("@RackWorksheetTemplateId", DbType.Int32, m_rackWorksheetTemplateId));
            @params.Add((DbParameter)da.CreateParameter("@DateCreated", DbType.DateTime, m_dateCreated, ParameterDirection.InputOutput));

            if (!(base.CurrentUser == null))
                @params.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, base.CurrentUser.Name));

            @params.Add((DbParameter)da.CreateParameter("@IsClosed", DbType.Boolean, m_isClosed));

            DbParameter[] pList = @params.ToArray();

            // 'We use the transaction scope when saving the Report. If any sql failures, all transaction should roll back.
            var options = new TransactionOptions();
            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
            options.Timeout = new TimeSpan(0, 2, 0);
            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {

                var returnParams = da.ExecuteNonQuery("lis_RackWorksheet_Save", pList);
                m_id = Conversions.ToInteger(returnParams["@RackWorksheetId"].Value);
                m_dateCreated = Conversions.ToDate(returnParams["@DateCreated"].Value);

                foreach (RackWorksheetSpecimen a in m_list)
                {
                    if (a.IsDirty)
                        a.Update();
                }
                foreach (RackWorksheetSpecimen a in m_deleteList)
                {
                    if (!a.IsNew)
                        a.Delete();
                }

                scope.Complete();

            }
            if (m_isClosed)
            {
                if (!(base.CurrentUser == null))
                    AuditManager.LogCustomObjectAction(m_id.ToString(), "Bioreference.LIS.RackWorkSheet", string.Format("Closed Worksheet #{0} by user {1}", m_id, base.CurrentUser.Name));
                else
                    AuditManager.LogCustomObjectAction(m_id.ToString(), "Bioreference.LIS.RackWorkSheet", string.Format("Closed Worksheet #{0} by user {1}", m_id, ""));
            }

            FlagClean(true);
        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                foreach (RackWorksheetSpecimen a in m_list)
                {
                    if (a.IsDirty)
                        return true;
                }

                return base.IsDirty || m_deleteList.Count > 0;
            }
        }

        [Serializable()]
        internal class Criteria
        {
            private int m_rackWorksheetId = 0; // Loads RackWorksheet with that id.


            public Criteria(int rackWorksheetId)
            {
                m_rackWorksheetId = rackWorksheetId;

            }

            public int RackWorksheetId
            {
                get
                {
                    return m_rackWorksheetId;
                }
            }

        }

    }

    [Serializable()]
    public class RackWorksheetReport
    {

        private int m_worksheetId = 0;
        private DateTime m_dateCreated;
        private string m_createdby = "";
        private int m_templateId = 0;
        private bool m_isClosed = false;
        private string m_assignedUser = "";

        private List<RackWorksheetReportItem> m_list;

        internal RackWorksheetReport(int worksheetId, DateTime dateCreated, string createdBy, int templateId)
        {
            m_worksheetId = worksheetId;
            m_dateCreated = dateCreated;
            m_createdby = createdBy;
            m_templateId = templateId;
            m_list = new List<RackWorksheetReportItem>();
        }

        internal RackWorksheetReport(int worksheetId, DateTime dateCreated, string createdBy, int templateId, bool isClosed, string AssignedUser)
        {
            m_worksheetId = worksheetId;
            m_dateCreated = dateCreated;
            m_createdby = createdBy;
            m_templateId = templateId;
            m_isClosed = isClosed;
            m_assignedUser = AssignedUser;
            m_list = new List<RackWorksheetReportItem>();
        }

        public int GetTotalReleased()
        {

            int released = 0;
            foreach (RackWorksheetReportItem i in m_list)
            {
                if (i.TransmitStatus >= transmitStatusType.Released && i.TransmitStatus != transmitStatusType.HeldForRerun)
                {
                    released += 1;
                }
            }

            return released;

        }

        public List<RackWorksheetReportItem> List
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

        public int RackWorksheetTemplateId
        {
            get
            {
                return m_templateId;
            }
        }

        public int RackWorksheetId
        {
            get
            {
                return m_worksheetId;
            }
        }

        public bool IsClosed
        {
            get
            {
                return m_isClosed;
            }
        }

        public string AssignedUser
        {
            get
            {
                return m_assignedUser;
            }
        }

    }

    [Serializable()]
    public class RackWorksheetReportItem
    {

        private string m_rackid = "";
        private int m_rackWorksheetId = 0;
        private string m_accessionNbr = "";
        private int m_sequence = 0;
        private int m_rackPosition = 0;
        private resultStatusType m_resultStatus;
        private transmitStatusType m_transmitStatus;
        private int m_pendingCount = 0;
        private int m_alertCount = 0;
        private rackWorksheetType m_rackworksheetType;
        private int m_reportId = 0;
        private int m_rackWorksheetTemplateId = 0;
        private int m_specimenId = 0;
        private bool m_has4ktest = false;

        internal RackWorksheetReportItem()
        {
        }

        public int RackWorksheetId
        {
            get
            {
                return m_rackWorksheetId;
            }
        }

        public int RackWorksheetTemplateId
        {
            get
            {
                return m_rackWorksheetTemplateId;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public int Sequence
        {
            get
            {
                return m_sequence;
            }
        }

        public int RackPosition
        {
            get
            {
                return m_rackPosition;
            }
        }

        public string RackId
        {
            get
            {
                return m_rackid;
            }
        }

        public rackWorksheetType RackWorksheetType
        {
            get
            {
                return m_rackworksheetType;
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

        public int SpecimenId
        {
            get
            {
                return m_specimenId;
            }
        }
        public bool Has4kTest
        {
            get
            {
                return m_has4ktest;
            }
        }
        internal void Load(DataRow row)
        {

            m_rackWorksheetId = Conversions.ToInteger(row["RackWorksheetId"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            m_sequence = Conversions.ToInteger(row["Sequence"]);
            m_rackPosition = Conversions.ToInteger(row["RackPosition"]);
            m_resultStatus = (resultStatusType)Conversions.ToInteger(row["ResultStatus"]);
            m_pendingCount = Conversions.ToInteger(row["PendingCount"]);
            m_alertCount = Conversions.ToInteger(row["AlertCount"]);
            m_rackworksheetType = (rackWorksheetType)Conversions.ToInteger(row["Type"]);
            m_rackid = Conversions.ToString(row["RackId"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);
            m_rackWorksheetTemplateId = Conversions.ToInteger(row["RackWorksheetTemplateId"]);
            m_transmitStatus = (transmitStatusType)Conversions.ToInteger(row["TransmitStatus"]);
            m_specimenId = Conversions.ToInteger(row["RackWorksheetSpecimenId"]);
            m_has4ktest = Conversions.ToBoolean(row["Has4kTest"]);

        }

    }
}