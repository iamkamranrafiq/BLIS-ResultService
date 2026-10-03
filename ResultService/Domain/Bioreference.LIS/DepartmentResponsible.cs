using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{
    public class DepartmentResponsible : Data.DataClassBase
    {

        #region Private Members

        private string m_deptId;
        private string m_deptName;

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

        #endregion

        #region Constructor

        internal DepartmentResponsible()
        {
        }

        #endregion


        internal void Load(DataRow row)
        {

            m_deptId = Conversions.ToString(row["DeptId"]);

            m_deptName = Conversions.ToString(row["DeptName"]);

            FlagClean();

        }

    }
}