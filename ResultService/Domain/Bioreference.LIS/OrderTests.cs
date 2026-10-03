using System;
using System.Data;
using Bioreference.Common.Lab;
using Bioreference.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderTests : DataClassBase
    {

        private OrderTestList m_list;
        private Order m_parent;

        internal OrderTests(Order parent)
        {
            m_parent = parent;
            m_list = new OrderTestList(m_parent);
        }

        public OrderTestList List
        {
            get
            {
                return m_list;
            }
        }

        /// <summary>
    /// Returns true if any test have questions
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
        public bool HasTestAnswers()
        {

            foreach (OrderTest t in m_list)
            {
                if (t.AOEs.Count > 0)
                    return true;
            }

            return false;

        }

        internal void Load(DataTable table)
        {

            OrderTest ot = null;

            foreach (DataRow r in table.Rows)
            {
                ot = Find(Conversions.ToInteger(r["OrderTestId"]));
                if (ot == null)
                {
                    ot = new OrderTest(m_parent);
                    ot.Load(r);
                    m_list.Add(ot);
                }
                else
                {
                    ot.Load(r);
                }
            }

        }

        internal void Update()
        {

            m_list.Update();

        }

        public OrderTest Find(long ordertestID)
        {

            foreach (OrderTest t in m_list)
            {
                if (t.Id == ordertestID)
                    return t;
            }

            return null;

        }

        public OrderTest Find(string testCode, string orderedTestCode)
        {

            foreach (OrderTest t in m_list)
            {
                if ((t.TestCode ?? "") == (testCode ?? "") && (t.OrderedTestCode ?? "") == (orderedTestCode ?? ""))
                    return t;
            }

            return null;

        }

        public string FindOrderedTestName(string orderedTestCode)
        {

            foreach (OrderTest t in m_list)
            {
                if ((t.OrderedTestCode ?? "") == (orderedTestCode ?? ""))
                    return t.OrderedTestName;
            }

            return "";

        }

        /// <summary>
    /// Adds a new OrderTest to the list if that testcode does not already exist. If it does, returns the existing OrderTest. Use to forceAdd parameter to 
    /// force a new OrderTest regardless of list.
    /// </summary>
    /// <param name="testCode"></param>
    /// <param name="testName"></param>
    /// <param name="forceAdd"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public OrderTest AddTest(string testCode, string testName = "", bool forceAdd = false, string orderedTestCode = "", string orderedTestName = "", SPMStatusValue spmStatus = SPMStatusValue.None)
        {

            OrderTest ot = null;
            if (!forceAdd)
            {
                ot = Find(testCode, orderedTestCode);
            }

            if (ot == null)
            {
                ot = new OrderTest(m_parent, testCode, testName, orderedTestCode, orderedTestName, spmStatus);
                m_list.Add(ot);
            }

            return ot;

        }

        /// <summary>
    /// Adds a new OrderTest to the list if that testcode does not already exist. If it does, returns the existing OrderTest. Use to forceAdd parameter to 
    /// force a new OrderTest regardless of list.
    /// </summary>
    /// <param name="test"></param>
    /// <param name="forceAdd"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public OrderTest AddTest(Test test, bool forceAdd = false)
        {

            OrderTest ot = null;
            if (!forceAdd)
            {
                ot = Find(Conversions.ToInteger(test.TestCode));
            }

            if (ot == null)
            {
                ot = new OrderTest(m_parent, test);
                m_list.Add(ot);
            }

            return ot;

        }

        public void RemoveTest(string testCode)
        {

            foreach (OrderTest t in m_list)
            {

                if ((t.TestCode ?? "") == (testCode ?? ""))
                {
                    m_list.Remove(t);
                    break;
                }

            }

        }

        public override bool IsValid
        {
            get
            {
                return base.IsValid && List.IsValid;
            }
        }

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_list.IsDirty;
            }
        }

    }
}