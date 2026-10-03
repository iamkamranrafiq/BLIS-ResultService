using System;
using System.Data;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ExternalCalcLabQueues : DataClassBase
    {

        #region  Private Members 

        private ExternalCalcLabQueueList m_list;

        #endregion

        #region  Public Properties 

        public ExternalCalcLabQueueList List
        {
            get
            {
                return m_list;
            }
        }

        #endregion

        #region  Constructor 

        private ExternalCalcLabQueues()
        {
            m_list = new ExternalCalcLabQueueList();
        }

        #endregion

        #region  Criteria Class 
        [Serializable()]
        internal class Criteria
        {

        }
        #endregion

        #region  Public Methods 
        // retrieves all ExternalCalcLabs
        public static ExternalCalcLabQueues Fetch()
        {
            ExternalCalcLabQueues objExternalCalcLabs = (ExternalCalcLabQueues)DataFactory.Fetch(new Criteria());
            return objExternalCalcLabs;
        }

        #endregion


        #region  Overridden Methods 

        protected override void DataFactory_Fetch(object criteria)
        {

            throw new Exception("not implemented");
            // Dim dw As DataWrapper = New DataWrapper(My.MySettings.Default.ConnString)
            // Dim params As List(Of DbParameter) = New List(Of DbParameter)
            // Dim crit As Criteria = CType(criteria, Criteria)

            // params.Add(dw.CreateParameter("@ParamName", DbType.String, crit.Property))

            // Dim dt() As DataTable = dw.ExecuteProcedure("common_ExternalCalcLab_FetchAll", params.ToArray())
            // If dt(0).Rows.Count > 0 Then
            // Me.Load(dt)
            // End If
        }

        #endregion

        #region  Private Methods 

        private void Load(DataTable[] dt)
        {
            ExternalCalcLabQueue objExternalCalcLabQueue = null;
            {
                ref var withBlock = ref dt[0];
                foreach (DataRow dr in withBlock.Rows)
                {
                    objExternalCalcLabQueue = new ExternalCalcLabQueue();
                    objExternalCalcLabQueue.Load(dr);
                    m_list.Add(objExternalCalcLabQueue);
                }
            }
        }

        #endregion


    }
}