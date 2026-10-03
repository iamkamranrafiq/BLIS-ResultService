using System;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CorrectedResultComments : DataClassBase
    {


        private CorrectedResultCommentList m_list = null;

        /// <summary>
    /// initialize list in Sub New
    /// </summary>
        private CorrectedResultComments()
        {
            m_list = new CorrectedResultCommentList(this);
        }

        public static CorrectedResultComments Fetch()
        {

            return (CorrectedResultComments)DataFactory.Fetch(new Criteria());

        }


        public CorrectedResultComment AddComment(string testCode, bool isForPanel, string text)
        {

            if (m_list.Find(testCode, isForPanel) == null)
            {
                var c = new CorrectedResultComment(testCode);
                c.Text = text;
                m_list.Add(c);
                return c;
            }
            return null;

        }

        public void DeleteComment(string testCode)
        {

            foreach (CorrectedResultComment c in m_list)
            {
                if ((c.TestCode ?? "") == (testCode ?? ""))
                {
                    m_list.Remove(c);
                    break;
                }
            }

        }

        #region Data Functions

        /// 
    /// <param name="criteria"></param>
        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            try
            {
                DataTable[] dt = da.ExecuteProcedure("lis_CorrectedResultComments_Fetch");

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

        protected override void DataFactory_Save()
        {

            m_list.Update();

        }

        /// 
    /// <param name="table"></param>
        private void Load(DataTable table)
        {

            CorrectedResultComment c = null;
            foreach (DataRow r in table.Rows)
            {
                c = new CorrectedResultComment();
                c.Load(r);
                m_list.Add(c);
            }

        }

        #endregion

        /// <summary>
    /// Returns true if Base IsDirty or List IsDirty
    /// </summary>
        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_list.IsDirty;
            }
        }

        public CorrectedResultCommentList List
        {
            get
            {
                return m_list;
            }
        }

        #region Criteria Class

        [Serializable()]
        private class Criteria
        {
            public Criteria()
            {
            }
        }

        #endregion

    }
} // Comments