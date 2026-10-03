using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class CorrectedReasonSecondary : Data.DataClassBase
    {

        #region Private Members

        private int m_secondaryReasonId;
        private string m_secondaryReasonName;

        #endregion

        #region Properties

        public int SecondaryReasonId
        {
            get
            {
                return m_secondaryReasonId;
            }
        }

        public string SecondaryReasonName
        {
            get
            {
                return m_secondaryReasonName;
            }
        }

        #endregion

        #region Constructor
        internal CorrectedReasonSecondary()
        {
        }
        #endregion


        internal void Load(DataRow row)
        {

            m_secondaryReasonId = Conversions.ToInteger(row["SecondaryReasonId"]);

            m_secondaryReasonName = Conversions.ToString(row["SecondaryReasonName"]);

            FlagClean();

        }

    }
}