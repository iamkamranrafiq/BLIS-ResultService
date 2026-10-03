using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bioreference.Data.Audit;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RapidResultTemplateAnalyte : AuditDataClassBase, IComparable
    {

        #region Private Members

        private int m_id = 0;
        private string m_analyteCode = "";
        private string m_analyteName = "";
        private RapidResultTemplate m_parent;
        private int m_sortOrder = 0;
        private string m_defaultValue = "";
        private bool m_isReference = false;

        #endregion

        #region Constructor

        internal RapidResultTemplateAnalyte(RapidResultTemplate parent)
        {
            m_parent = parent;
            FlagChild();
        }

        internal RapidResultTemplateAnalyte(RapidResultTemplate parent, string analyteCode, string analyteName)
        {
            m_parent = parent;
            m_analyteCode = analyteCode;
            m_analyteName = analyteName;
            FlagChild();
            FlagDirty();
        }

        #endregion

        #region Public Properties

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        [Audit("IsReference")]
        public bool IsReference
        {
            get
            {
                return m_isReference;
            }
            set
            {
                if (m_isReference != value)
                {
                    m_isReference = value;
                    FlagDirty();
                }
            }
        }

        public string AnalyteCode
        {
            get
            {
                return m_analyteCode;
            }
        }

        [Audit("AnalyteName")]
        public string AnalyteName
        {
            get
            {
                return m_analyteName;
            }
            set
            {
                if ((m_analyteName.Trim() ?? "") != (value ?? ""))
                {
                    m_analyteName = value;
                    FlagDirty();
                }
            }
        }

        [Audit("SortOrder")]
        public int SortOrder
        {
            get
            {
                return m_sortOrder;
            }
            set
            {
                if (m_sortOrder != value)
                {
                    m_sortOrder = value;
                    FlagDirty();
                }
            }
        }

        [Audit("DefaultValue")]
        public string DefaultValue
        {
            get
            {
                return m_defaultValue;
            }
            set
            {
                if ((m_defaultValue ?? "") != (value.Trim() ?? ""))
                {
                    m_defaultValue = value.Trim();
                    FlagDirty();
                }
            }
        }

        #endregion

        #region Data Functions

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["RapidResultTemplateAnalyteId"]);
            m_analyteCode = Conversions.ToString(row["AnalyteCode"]);
            m_analyteName = Conversions.ToString(row["AnalyteName"]);
            m_sortOrder = Conversions.ToInteger(row["SortOrder"]);
            m_defaultValue = Conversions.ToString(row["DefaultValue"]);
            m_isReference = Conversions.ToBoolean(row["IsReference"]);

            FlagClean();

        }

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@AnalyteId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@AnalyteCode", DbType.String, m_analyteCode));
            paramList.Add((DbParameter)da.CreateParameter("@SortOrder", DbType.Int32, m_sortOrder));
            paramList.Add((DbParameter)da.CreateParameter("@AnalyteName", DbType.String, m_analyteName));
            paramList.Add((DbParameter)da.CreateParameter("@DefaultValue", DbType.String, m_defaultValue));
            paramList.Add((DbParameter)da.CreateParameter("@IsReference", DbType.Boolean, m_isReference));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_RapidResultTemplateAnalyte_Save", @params)["@AnalyteId"].Value);

            FlagClean(true);

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@AnalyteId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_RapidResultTemplateAnalyte_Delete", @param);

            FlagDeleted();
            FlagClean();

        }

        public int CompareTo(object obj)
        {

            if (!ReferenceEquals(obj.GetType(), typeof(RapidResultTemplateAnalyte)))
            {
                throw new ArgumentException();
            }

            RapidResultTemplateAnalyte a = (RapidResultTemplateAnalyte)obj;
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

        #endregion

    }
}