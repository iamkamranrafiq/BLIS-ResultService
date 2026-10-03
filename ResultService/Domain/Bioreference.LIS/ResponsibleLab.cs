using System.Data;
using Bioreference.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class ResponsibleLab : DataClassBase
    {

        #region Private Members

        private string m_responsibleLabId;
        private string m_responsibleLabName;

        #endregion

        #region Properties

        public string ResponsibleLabId
        {
            get
            {
                return m_responsibleLabId;
            }
        }
        public string ResponsibleLabName
        {
            get
            {
                return m_responsibleLabName;
            }
        }

        #endregion

        #region Constructor

        internal ResponsibleLab()
        {
        }

        #endregion


        internal void Load(DataRow row)
        {

            m_responsibleLabId = Conversions.ToString(row["ResponsibleLabId"]);

            m_responsibleLabName = Conversions.ToString(row["ResponsibleLabName"]);

            FlagClean();

        }
    }
}