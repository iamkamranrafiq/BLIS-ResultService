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
    public class RackWorksheetSpecimen : AuditDataClassBase
    {

        #region Private Members

        private RackWorksheet m_parent;
        private int m_id = 0;
        private int m_sequence = 0;
        private int m_rackPosition = 0;
        private string m_rackId = "";
        private string m_accessionNbr = "";
        private bool m_has4kTest = false;

        private bool m_isMarkForDelete = false;

        #endregion

        #region Constructor

        internal RackWorksheetSpecimen(RackWorksheet parent)
        {
            m_parent = parent;
            FlagDirty();
            FlagChild();
        }

        internal RackWorksheetSpecimen(RackWorksheet parent, string rackid, int rackPosition, int sequence, string accessionNbr, bool has4kTest)
        {
            m_parent = parent;
            m_rackId = rackid;
            m_rackPosition = rackPosition;
            m_sequence = sequence;
            m_accessionNbr = accessionNbr;
            m_has4kTest = has4kTest;
            FlagDirty();
            FlagChild();
        }

        #endregion

        public void MarkForDelete()
        {
            m_isMarkForDelete = true;
        }

        #region Public Properties

        /// <summary>
    /// Determines if the specimen is flagged for deletion.
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public bool IsMarkForDelete
        {
            get
            {
                return m_isMarkForDelete;
            }
        }

        public int ID
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
                return m_rackId;
            }
        }

        public string AccessionNbr
        {
            get
            {
                return m_accessionNbr;
            }
        }
        public bool Has4kTest
        {
            get
            {
                return m_has4kTest;
            }
        }
        #endregion

        #region Data Functions

        internal void Update()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@WorksheetSpecimenId", DbType.Int32, m_id, ParameterDirection.InputOutput));
            paramList.Add((DbParameter)da.CreateParameter("@WorksheetId", DbType.Int32, m_parent.Id));
            paramList.Add((DbParameter)da.CreateParameter("@Sequence", DbType.Int32, m_sequence));
            paramList.Add((DbParameter)da.CreateParameter("@RackPosition", DbType.Int32, m_rackPosition));
            paramList.Add((DbParameter)da.CreateParameter("@RackId", DbType.String, m_rackId));

            paramList.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, m_accessionNbr.Trim()));

            DbParameter[] @params = paramList.ToArray();

            m_id = Conversions.ToInteger(da.ExecuteNonQuery("lis_RackWorksheetSpecimen_Save", @params)["@WorksheetSpecimenId"].Value);
            AuditManager.LogCustomObjectAction(m_parent.Id.ToString(), "Bioreference.LIS.RackWorksheet", $"Accession {m_accessionNbr} added to position {m_rackPosition}, Sequence {m_sequence}");

            FlagClean(true);

        }

        internal void Delete()
        {

            var da = new DataWrapper(Configuration.ConnectionString);
            var @param = new DbParameter[1];

            @param[0] = (DbParameter)da.CreateParameter("@WorksheetSpecimenId", DbType.Int32, m_id);

            da.ExecuteNonQuery("lis_RackWorksheetSpecimen_Delete", @param);
            AuditManager.LogCustomObjectAction(m_parent.Id.ToString(), "Bioreference.LIS.RackWorksheet", $"Specimen {m_id} removed.");

            FlagDeleted();
            FlagClean(true);

        }

        internal void Load(DataRow row)
        {

            m_id = Conversions.ToInteger(row["RackWorksheetSpecimenId"]);
            m_sequence = Conversions.ToInteger(row["Sequence"]);
            m_rackPosition = Conversions.ToInteger(row["RackPosition"]);
            m_rackId = Conversions.ToString(row["RackId"]);
            m_accessionNbr = Conversions.ToString(row["AccessionNbr"]);

            FlagClean();

        }

        #endregion

    }
}