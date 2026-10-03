using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class COCApprover : DataClassBase
    {

        #region Private Members

        private int m_cocApproverId = 0;
        private string m_cocApproverLoginId = "";
        private string m_cocApproverFirstName = "";
        private string m_cocApproverMiddleName = "";
        private string m_cocApproverLastName = "";
        private string m_cocApproverEmpNbr = "";
        private bool m_isActive = true;

        #endregion

        #region Constructor
        internal COCApprover()
        {
        }
        #endregion

        #region Public Properties
        public int CocApproverId
        {
            get
            {
                return m_cocApproverId;
            }
        }

        public string CocApproverLoginId
        {
            get
            {
                return m_cocApproverLoginId;
            }
            set
            {
                if ((m_cocApproverLoginId ?? "") != (value ?? ""))
                {
                    m_cocApproverLoginId = value;
                    FlagDirty();
                }
            }
        }

        public string CocApproverFirstName
        {
            get
            {
                return m_cocApproverFirstName;
            }
            set
            {
                if ((m_cocApproverFirstName ?? "") != (value ?? ""))
                {
                    m_cocApproverFirstName = value;
                    FlagDirty();
                }
            }
        }

        public string CocApproverMiddleName
        {
            get
            {
                return m_cocApproverMiddleName;
            }
            set
            {
                if ((m_cocApproverMiddleName ?? "") != (value ?? ""))
                {
                    m_cocApproverMiddleName = value;
                    FlagDirty();
                }
            }
        }

        public string CocApproverLastName
        {
            get
            {
                return m_cocApproverLastName;
            }
            set
            {
                if ((m_cocApproverLastName ?? "") != (value ?? ""))
                {
                    m_cocApproverLastName = value;
                    FlagDirty();
                }
            }
        }

        public string CocApproverEmpNbr
        {
            get
            {
                return m_cocApproverEmpNbr;
            }
            set
            {
                if ((m_cocApproverEmpNbr ?? "") != (value ?? ""))
                {
                    m_cocApproverEmpNbr = value;
                    FlagDirty();
                }
            }
        }
        public bool IsActive
        {
            get
            {
                return m_isActive;
            }
        }
        #endregion

        #region Data Functions
        public void Delete(string userName)
        {
            DataFactory.Delete(new Criteria(userName, m_cocApproverId));
        }

        public void Update(string userName)
        {
            if (base.IsDirty)
            {
                var da = new DataWrapper(Configuration.ConnectionString);
                var paramList = new List<DbParameter>();


                paramList.Add((DbParameter)da.CreateParameter("@CocApproverId", DbType.Int32, m_cocApproverId, ParameterDirection.InputOutput));
                paramList.Add((DbParameter)da.CreateParameter("@CocApproverLoginId", DbType.String, m_cocApproverLoginId));
                paramList.Add((DbParameter)da.CreateParameter("@CocApproverFirstName", DbType.String, m_cocApproverFirstName));
                paramList.Add((DbParameter)da.CreateParameter("@CocApproverMiddleName", DbType.String, m_cocApproverMiddleName));
                paramList.Add((DbParameter)da.CreateParameter("@CocApproverLastName", DbType.String, m_cocApproverLastName));
                paramList.Add((DbParameter)da.CreateParameter("@CocApproverEmpNbr", DbType.String, m_cocApproverEmpNbr));
                paramList.Add((DbParameter)da.CreateParameter("@IsActive", DbType.Boolean, m_isActive));
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, userName));

                DbParameter[] @params = paramList.ToArray();

                m_cocApproverId = Conversions.ToInteger(da.ExecuteNonQuery("lis_CocApprover_Save", @params)["@CocApproverId"].Value);

                FlagClean();
            }
        }

        #endregion

        internal void Load(DataRow row)
        {

            m_cocApproverId = Conversions.ToInteger(row["CocApproverId"]);
            m_cocApproverLoginId = Conversions.ToString(row["CocApproverLoginId"]);
            m_cocApproverFirstName = Conversions.ToString(row["CocApproverFirstName"]);
            m_cocApproverMiddleName = Conversions.ToString(row["CocApproverMiddleName"]);
            m_cocApproverLastName = Conversions.ToString(row["CocApproverLastName"]);
            m_cocApproverEmpNbr = Conversions.ToString(row["CocApproverEmpNbr"]);
            m_isActive = Conversions.ToBoolean(row["IsActive"]);

            FlagClean();

        }

        // Protected Overrides Sub DataFactory_Save()

        // If MyBase.IsDirty Then
        // Dim da As DataWrapper = New DataWrapper(Configuration.ConnectionString)
        // Dim paramList As List(Of DbParameter) = New List(Of DbParameter)

        // With paramList

        // .Add(da.CreateParameter("@CocApproverId", DbType.Int32, m_cocApproverId, ParameterDirection.InputOutput))
        // .Add(da.CreateParameter("@CocApproverLoginId", DbType.String, m_cocApproverLoginId))
        // .Add(da.CreateParameter("@CocApproverFirstName", DbType.String, m_cocApproverFirstName))
        // .Add(da.CreateParameter("@CocApproverMiddleName", DbType.String, m_cocApproverMiddleName))
        // .Add(da.CreateParameter("@CocApproverLastName", DbType.String, m_cocApproverLastName))
        // .Add(da.CreateParameter("@CocApproverEmpNbr", DbType.String, m_cocApproverEmpNbr))
        // .Add(da.CreateParameter("@IsActive", DbType.Boolean, m_isActive))

        // If Not IsNothing(MyBase.CurrentUser) Then .Add(da.CreateParameter("@UserName", DbType.String, MyBase.CurrentUser.Name))
        // End With

        // Dim params() As DbParameter = paramList.ToArray()

        // m_cocApproverId = da.ExecuteNonQuery("lis_CocApprover_Save", params).Item("@CocApproverId").Value

        // Me.FlagClean()
        // End If
        // End Sub

        protected override void DataFactory_Delete(object criteria)
        {
            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@CocApproverId", DbType.Int32, c.CcocApproverId));
            paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, c.UserName));

            DbParameter[] @params = paramList.ToArray();
            da.ExecuteNonQuery("lis_CocApprover_Delete", @params);

        }

        #region Internal Criteria Class
        [Serializable()]
        internal class Criteria
        {

            private int m_cocApproverId = 0;
            private string m_updatedBy = "";

            internal Criteria(string userName, int cocApproverId)
            {
                m_updatedBy = userName;
                m_cocApproverId = cocApproverId;
            }

            internal int CcocApproverId
            {
                get
                {
                    return m_cocApproverId;
                }
            }
            internal string UserName
            {
                get
                {
                    return m_updatedBy;
                }
            }
        }
        #endregion

    }
}