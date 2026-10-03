
namespace Bioreference.LIS
{
    public class RequisitionSettings
    {

        private string m_RequisitionStatusURL;
        private string m_RequisitionTokenURL;
        private string m_RequisitionClientSecret;
        private string m_RequisitionClientId;
        private string m_RequisitionGrantType;

        public string RequisitionStatusURL
        {
            get
            {
                return m_RequisitionStatusURL;

            }
            set
            {
                m_RequisitionStatusURL = value;
            }
        }

        public string RequisitionTokenURL
        {
            get
            {
                return m_RequisitionTokenURL;

            }
            set
            {
                m_RequisitionTokenURL = value;
            }
        }

        public string RequisitionClientSecret
        {
            get
            {
                return m_RequisitionClientSecret;

            }
            set
            {
                m_RequisitionClientSecret = value;
            }
        }

        public string RequisitionClientId
        {
            get
            {
                return m_RequisitionClientId;

            }
            set
            {
                m_RequisitionClientId = value;
            }
        }

        public string RequisitionGrantType
        {
            get
            {
                return m_RequisitionGrantType;

            }
            set
            {
                m_RequisitionGrantType = value;
            }
        }

    }
}