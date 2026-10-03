using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class VertexReleasedAnalytes : DataClassBase
    {

        #region Private Members

        private List<VertexReleasedAnalyte> m_vtxReleasedAnalyteList = new List<VertexReleasedAnalyte>();

        #endregion

        public List<VertexReleasedAnalyte> VertexReleasedAnalyteList
        {
            get
            {
                return m_vtxReleasedAnalyteList;
            }
        }

        #region Public Methods
        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            try
            {
                DataTable[] dt = da.ExecuteProcedure("vtx_ReleasedData_Fetch");

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0]);
                }
            }

            catch (Exception ex)
            {

                Trace.Write(ex.ToString());

            }

        }

        public static VertexReleasedAnalytes Fetch()
        {

            return (VertexReleasedAnalytes)DataFactory.Fetch(new Criteria());

        }

        public static void Delete(string sAccession, DateTime dtDOS)
        {
            var da = new DataWrapper(Configuration.ConnectionString);
            var paramList = new List<DbParameter>();

            paramList.Add((DbParameter)da.CreateParameter("@AccessionNbr", DbType.String, sAccession));
            paramList.Add((DbParameter)da.CreateParameter("@DateOfService", DbType.Date, dtDOS));

            DbParameter[] @params = paramList.ToArray();
            da.ExecuteNonQuery("vtx_ReleasedData_Delete", @params);

        }

        #endregion

        #region Private Methods

        internal VertexReleasedAnalytes()
        {
        }

        private void Load(DataTable table)
        {

            VertexReleasedAnalyte p;
            foreach (DataRow r in table.Rows)
            {
                p = new VertexReleasedAnalyte();
                p.Load(r);
                m_vtxReleasedAnalyteList.Add(p);
            }
        }
        #endregion

        [Serializable()]
        internal class Criteria
        {
            // Private m_accessionNbr As String = ""
            // Private m_dateOfService As Date = SharedFunctions.nodate

            public Criteria()
            {
            }

            // Sub New(ByVal sAccession As String, ByVal dtDOS As Date)
            // m_accessionNbr = sAccession
            // m_dateOfService = dtDOS
            // End Sub

            // ReadOnly Property AccessionNumber As String
            // Get
            // Return m_accessionNbr
            // End Get
            // End Property

            // ReadOnly Property DateOfService As Date
            // Get
            // Return m_dateOfService
            // End Get
            // End Property
        }
    }
}