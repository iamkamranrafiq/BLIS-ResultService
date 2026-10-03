using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{
    public class CorrectedReasonType : Data.DataClassBase
    {

        #region Private Members

        private string m_reasonId;
        private string m_reasonName;

        #endregion
        public string ReasonId
        {
            get
            {
                return m_reasonId;
            }
        }

        public string ReasonName
        {
            get
            {
                return m_reasonName;
            }
        }

        #region Constructor

        internal CorrectedReasonType()
        {
        }
        #endregion

        internal void Load(DataRow row)
        {

            m_reasonId = Conversions.ToString(row["ReasonId"]);

            m_reasonName = Conversions.ToString(row["ReasonName"]);

            FlagClean();

        }

    }
}