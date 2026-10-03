using System.Data;
using Bioreference.LIS.Helper;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{
    public class OutboundMessagesCHM
    {

        #region Private Members

        private string m_AccessionNumber = null;
        private string m_Message = null;
        private int m_MessageId = default;

        #endregion

        #region Public Properties
        public string AccessionNumber
        {
            get
            {
                return m_AccessionNumber;
            }

        }

        public int MessageId
        {
            get
            {
                return m_MessageId;
            }
        }

        public string Message
        {
            get
            {
                return m_Message;
            }
        }

        #endregion

        #region Constructor
        internal OutboundMessagesCHM()
        {
        }

        #endregion

        internal void Load(DataRow row)
        {

            m_AccessionNumber = Conversions.ToString(row["AccessionNumber"]);
            m_MessageId = Conversions.ToInteger(row["MessageId"]);
            m_Message = Conversions.ToString(row["Message"]).NormalizeToWindows();

        }
    }
}