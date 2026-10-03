using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Transactions;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class NoBillCode : AuditDataClassBase
    {

        public enum NoBillValidationType
        {
            Numeric = 1,
            Text = 2,
            TestExists = 3
        }

        #region Private Members

        private int _id = 0;
        private string _testCode;
        private string _thresholdLowValue;
        private string _thresholdHighValue;
        private bool _doNotReport;
        private string _performingFacilities;
        private NoBillValidationType _validationTypeId;
        private string _statusToSend;
        private bool _isComponent;
        private string _parentPanelCode;
        private bool _sendParent;
        private bool _sendTest;
        private string _panelStatusToSend;
        private List<string> _textRanges;
        private string _testExists;

        private bool _applyForAllTestCodes;

        #endregion

        #region Override Properties

        public override object IdentifierId
        {
            get
            {
                return _id;
            }
        }

        #endregion

        #region Constructor

        public NoBillCode()
        {
            _textRanges = new List<string>();
        }

        #endregion

        #region Public Properties

        public int NoBillId
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public string TestCode
        {
            get
            {
                return _testCode;
            }
            set
            {
                _testCode = value;
                FlagDirty();
            }
        }

        public string ThresholdLowValue
        {
            get
            {
                return _thresholdLowValue;
            }
            set
            {
                double d;
                value = value.Trim();
                if (string.IsNullOrEmpty(value) | double.TryParse(value, out d))
                {
                    _thresholdLowValue = value;
                    FlagDirty();
                }
                else
                {
                    throw new Exception("Value not numeric.");
                }
            }
        }

        public string ThresholdHighValue
        {
            get
            {
                return _thresholdHighValue;
            }
            set
            {
                double d;
                value = value.Trim();
                if (string.IsNullOrEmpty(value) | double.TryParse(value, out d))
                {
                    _thresholdHighValue = value;
                    FlagDirty();
                }
                else
                {
                    throw new Exception("Value not numeric.");
                }
            }
        }

        public bool DoNotReport
        {
            get
            {
                return _doNotReport;
            }
            set
            {
                _doNotReport = value;
                FlagDirty();
            }
        }

        public string PerformingFacilities
        {
            get
            {
                return _performingFacilities;
            }
            set
            {
                _performingFacilities = value;
                FlagDirty();
            }
        }

        public NoBillValidationType ValidationTypeId
        {
            get
            {
                return _validationTypeId;
            }
            set
            {
                _validationTypeId = value;
                FlagDirty();
            }
        }

        public string StatusToSend
        {
            get
            {
                return _statusToSend;
            }
            set
            {
                _statusToSend = value;
                FlagDirty();
            }
        }

        public List<string> TextRanges
        {
            get
            {
                return _textRanges;
            }
            set
            {
                _textRanges = value;
                FlagDirty();
            }
        }

        public bool IsComponent
        {
            get
            {
                return _isComponent;
            }
            set
            {
                _isComponent = value;
                FlagDirty();
            }
        }

        public string ParentPanelCode
        {
            get
            {
                return _parentPanelCode;
            }
            set
            {
                _parentPanelCode = value;
                FlagDirty();
            }
        }

        public bool SendParent
        {
            get
            {
                return _sendParent;
            }
            set
            {
                _sendParent = value;
                FlagDirty();
            }
        }

        public bool SendTest
        {
            get
            {
                return _sendTest;
            }
            set
            {
                _sendTest = value;
                FlagDirty();
            }
        }

        public string PanelStatusToSend
        {
            get
            {
                return _panelStatusToSend;
            }
            set
            {
                _panelStatusToSend = value;
                FlagDirty();
            }
        }

        public bool ApplyForAllTestCodes
        {
            get
            {
                return _applyForAllTestCodes;
            }
            set
            {
                _applyForAllTestCodes = value;

            }
        }

        public string TestExists
        {
            get
            {
                return _testExists;
            }
            set
            {
                _testExists = value;
                FlagDirty();
            }
        }

        #endregion

        #region Internal Criteria Class

        [Serializable()]
        internal class Criteria
        {
            public string TestCode { get; set; }
        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var @params = new List<DbParameter>();
            DataTable[] dt;
            try
            {
                @params.Add((DbParameter)da.CreateParameter("@TestCode", DbType.Int32, c.TestCode));
                dt = da.ExecuteProcedure("lis_NoBillCodes_Fetch", @params.ToArray());
                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0].Rows[0]);
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.ToString());
                throw;
            }

        }

        internal void Load(DataRow row)
        {

            _id = Conversions.ToInteger(row["NoBillId"]);
            _testCode = Conversions.ToString(row["TestCode"]).Trim();
            _thresholdLowValue = Conversions.ToString(row["ThresholdLowValue"]).Trim();
            _thresholdHighValue = Conversions.ToString(row["ThresholdHighValue"]).Trim();
            _doNotReport = Conversions.ToBoolean(row["DoNotReport"]);
            _performingFacilities = Conversions.ToString(row["PerformingFacilities"]).Trim();
            _validationTypeId = (NoBillValidationType)Conversions.ToInteger(row["ValidationTypeId"]);
            _statusToSend = Conversions.ToString(row["StatusToSend"]).Trim();
            _isComponent = Conversions.ToBoolean(row["IsComponent"]);
            _parentPanelCode = Conversions.ToString(row["ParentPanelCode"]).Trim();
            _sendParent = Conversions.ToBoolean(row["SendParent"]);
            _sendTest = Conversions.ToBoolean(row["SendTest"]);
            _panelStatusToSend = Conversions.ToString(row["PanelStatusToSend"]).Trim();
            _textRanges = new List<string>();
            _testExists = Conversions.ToString(row["TestExists"]).Trim();

            if (_validationTypeId == NoBillValidationType.Text)
            {
                var da = new DataWrapper(Configuration.ConnectionString);
                var @params = new List<DbParameter>();
                DataTable[] dt;
                var paramList = new List<DbParameter>();
                paramList.Add((DbParameter)da.CreateParameter("@NoBillId", DbType.Int32, _id));
                dt = da.ExecuteProcedure("[lis_NoBillCodeTextRanges_Fetch]", paramList.ToArray());
                if (dt[0].Rows.Count > 0)
                {
                    foreach (DataRow range in dt[0].Rows)
                        _textRanges.Add(Conversions.ToString(range["TextRange"]));
                }
            }

            FlagClean();

        }

        public void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            string userName = "";

            if (!(CurrentUser == null))
            {
                userName = CurrentUser.Name;
            }

            var options = new TransactionOptions();
            options.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
            options.Timeout = new TimeSpan(0, 2, 0);

            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {

                // Save master NoBillCode row
                var paramList = new List<DbParameter>();
                {
                    ref var withBlock = ref paramList;
                    withBlock.Add((DbParameter)da.CreateParameter("@NoBillId", DbType.Int32, _id, ParameterDirection.InputOutput));
                    withBlock.Add((DbParameter)da.CreateParameter("@TestCode", DbType.String, _testCode));
                    withBlock.Add((DbParameter)da.CreateParameter("@ThresholdLowValue", DbType.String, _thresholdLowValue));
                    withBlock.Add((DbParameter)da.CreateParameter("@ThresholdHighValue", DbType.String, _thresholdHighValue));
                    withBlock.Add((DbParameter)da.CreateParameter("@DoNotReport", DbType.Boolean, _doNotReport));
                    withBlock.Add((DbParameter)da.CreateParameter("@PerformingFacilities", DbType.String, _performingFacilities));
                    withBlock.Add((DbParameter)da.CreateParameter("@ValidationTypeId", DbType.Int32, _validationTypeId));
                    withBlock.Add((DbParameter)da.CreateParameter("@StatusToSend", DbType.String, _statusToSend));
                    withBlock.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, userName));
                    withBlock.Add((DbParameter)da.CreateParameter("@IsComponent", DbType.Boolean, _isComponent));
                    withBlock.Add((DbParameter)da.CreateParameter("@ParentPanelCode", DbType.String, _parentPanelCode));
                    withBlock.Add((DbParameter)da.CreateParameter("@SendParent", DbType.Boolean, _sendParent));
                    withBlock.Add((DbParameter)da.CreateParameter("@SendTest", DbType.Boolean, _sendTest));
                    withBlock.Add((DbParameter)da.CreateParameter("@PanelStatusToSend", DbType.String, _panelStatusToSend));
                    withBlock.Add((DbParameter)da.CreateParameter("@TestExists", DbType.String, _testExists));
                }
                _id = Conversions.ToInteger(da.ExecuteNonQuery("lis_NoBillCode_Save", paramList.ToArray())["@NoBillId"].Value);

                // Delete prior and save text range items
                if (_validationTypeId == NoBillValidationType.Text)
                {

                    paramList = new List<DbParameter>();
                    paramList.Add((DbParameter)da.CreateParameter("@NoBillId", DbType.Int32, _id));
                    paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, userName));
                    da.ExecuteNonQuery("lis_NoBillCodeTextRanges_Delete", paramList.ToArray());

                    foreach (string range in _textRanges)
                    {
                        paramList = new List<DbParameter>();
                        paramList.Add((DbParameter)da.CreateParameter("@NoBillCodeTextRangeId", DbType.Int32, 0, ParameterDirection.InputOutput));
                        paramList.Add((DbParameter)da.CreateParameter("@NoBillId", DbType.Int32, _id));
                        paramList.Add((DbParameter)da.CreateParameter("@TestCode", DbType.String, _testCode));
                        paramList.Add((DbParameter)da.CreateParameter("@TextRange", DbType.String, range));
                        paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, userName));
                        da.ExecuteNonQuery("[lis_NoBillCodeTextRange_Save]", paramList.ToArray());
                    }

                }

                FlagClean();

                scope.Complete();

            }

        }

        public void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@NoBillId", DbType.Int32, _id));
            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));

            DbParameter[] @params = paramList.ToArray();

            da.ExecuteNonQuery("lis_NoBillCode_Delete", @params);

            FlagDeleted();
            FlagClean();

        }

        #endregion


    }
}