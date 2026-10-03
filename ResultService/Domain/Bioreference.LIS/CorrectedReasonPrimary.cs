using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{
    public class CorrectedReasonPrimary : Data.DataClassBase
    {

        #region Private Members

        private int m_primaryReasonId;
        private string m_primaryReasonName;

        #endregion

        #region Properties

        public int PrimaryReasonId
        {
            get
            {
                return m_primaryReasonId;
            }
        }

        public string PrimaryReasonName
        {
            get
            {
                return m_primaryReasonName;
            }
        }

        #endregion

        #region Constructor
        internal CorrectedReasonPrimary()
        {
        }
        #endregion


        internal void Load(DataRow row)
        {

            m_primaryReasonId = Conversions.ToInteger(row["PrimaryReasonId"]);

            m_primaryReasonName = Conversions.ToString(row["PrimaryReasonName"]);

            FlagClean();

        }

    }
}