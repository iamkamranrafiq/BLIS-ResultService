using System.Data;
using Bioreference.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class Controllable : DataClassBase
    {

        #region Private Members

        private string m_Id;
        private string m_Name;

        #endregion

        #region Properties

        public string Id
        {
            get
            {
                return m_Id;
            }
        }
        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        #endregion

        #region Constructor

        internal Controllable()
        {
        }

        #endregion


        internal void Load(DataRow row)
        {

            m_Id = Conversions.ToString(row["Id"]);

            m_Name = Conversions.ToString(row["Name"]);

            FlagClean();

        }

    }
}