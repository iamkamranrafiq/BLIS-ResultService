using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class ReportingDepartment : Data.DataClassBase
    {

        #region Private Members

        private string m_deptId;
        private string m_deptName;
        private string m_email;
        #endregion

        #region Properties

        public string DeptId
        {
            get
            {
                return m_deptId;
            }
        }

        public string DeptName
        {
            get
            {
                return m_deptName;
            }
        }

        public string Email
        {
            get
            {
                return m_email;

            }
        }

        #endregion

        #region Constructor

        internal ReportingDepartment()
        {
        }

        #endregion


        internal void Load(DataRow row)
        {

            m_deptId = Conversions.ToString(row["DeptId"]);
            m_deptName = Conversions.ToString(row["DeptName"]);
            m_email = Conversions.ToString(row["Email"]);

            FlagClean();

        }

    }
}