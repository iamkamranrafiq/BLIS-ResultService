using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Xml;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadResult : DataClassBase
    {


        #region Private Members

        private int m_id = 0;
        private string m_accessionNbr = "";
        private int m_templateId = 0;
        private string m_templateName = "";
        private DateTime m_dateCreated = DateTime.Parse("1900-01-01");
        private string m_createdBy = "";
        private DiffPadResultValueList m_listValue;
        private int m_reportId;

        #endregion

        #region Constructor

        internal DiffPadResult(DiffPadTemplate template)
        {
            m_templateId = template.Id;
            m_templateName = template.Name;
            m_listValue = new DiffPadResultValueList();
            FlagDirty();
        }
        internal DiffPadResult()
        {
        }

        #endregion

        public static DiffPadResult CreateNew(DiffPadTemplate template)
        {

            var r = new DiffPadResult(template);
            return r;

        }

        public DiffPadResultValue AddValue(string code, string value)
        {

            foreach (DiffPadResultValue v in m_listValue)
            {
                if ((v.Code ?? "") == (code ?? ""))
                {
                    v.Value = value;
                    return v;
                }
            }

            var rv = new DiffPadResultValue(this);
            rv.Code = code;
            rv.Value = value;
            m_listValue.Add(rv);

            return rv;

        }


        #region Public Properties

        public DiffPadResultValueList ValueList
        {
            get
            {
                return m_listValue;
            }
        }

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
            set
            {
                if ((value.Trim() ?? "") != (m_accessionNbr ?? ""))
                {
                    m_accessionNbr = value.Trim();
                    FlagDirty();
                }
            }
        }

        public DateTime CreatedDate
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

        public int TemplateId
        {
            get
            {
                return m_templateId;
            }
        }

        public string TemplateName
        {
            get
            {
                return m_templateName;
            }
        }

        public int ReportId
        {
            get
            {
                return m_reportId;
            }
            set
            {
                m_reportId = value;
            }
        }

        #endregion

        #region Data Functions


        protected override void DataFactory_Save()
        {

            // ********************************************
            var xmldoc = new XmlDocument();
            var root = xmldoc.CreateElement("Values");
            xmldoc.AppendChild(root);

            foreach (DiffPadResultValue r in m_listValue)
            {
                var eResult = xmldoc.CreateElement("Value");
                eResult.InnerText = r.Value;
                var attrFlag = xmldoc.CreateAttribute("Code");
                attrFlag.Value = r.Code;
                eResult.Attributes.Append(attrFlag);
                root.AppendChild(eResult);
            }

            string xmlResultValues = xmldoc.InnerXml;
            // *********************************************

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@Id", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, AccessionNbr));
            paramList.Add((DbParameter)da.CreateParameter("@TemplateId", DbType.Int32, m_templateId));
            paramList.Add((DbParameter)da.CreateParameter("@DateCreated", DbType.DateTime, m_dateCreated, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@ValuesXml", DbType.String, xmlResultValues));

            if (!(CurrentUser == null))
                paramList.Add((DbParameter)da.CreateParameter("@UserName", DbType.String, CurrentUser.Name));
            paramList.Add((DbParameter)da.CreateParameter("@ReportId", DbType.String, ReportId));

            DbParameter[] @params = paramList.ToArray();

            var pc = da.ExecuteNonQuery("lis_DiffPadResult_Save", @params);
            m_id = Conversions.ToInteger(pc["@Id"].Value);
            m_dateCreated = Conversions.ToDate(pc["@DateCreated"].Value);

            FlagClean();

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["DiffPadResultId"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);
            m_templateId = Conversions.ToInteger(row["TemplateId"]);
            m_templateName = Conversions.ToString(row["TemplateName"]);
            m_dateCreated = Conversions.ToDate(row["DateCreated"]);
            m_createdBy = Conversions.ToString(row["CreatedBy"]);
            m_reportId = Conversions.ToInteger(row["ReportId"]);

            FlagClean();

        }


        #endregion

    }
}