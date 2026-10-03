
namespace Bioreference.LIS
{
    public class BRADSettings
    {

        private string m_BRADAPIPassword;
        private string m_BRADAPIBaseURL;
        private string m_BRADAPIToken_Timeout;
        private string m_BRADAPIUsername;
        private string m_BRADAPIWindowsUsername;


        public string BRADAPIPassword
        {
            get
            {
                return m_BRADAPIPassword;

            }
            set
            {
                m_BRADAPIPassword = value;
            }
        }

        public string BRADAPIBaseURL
        {
            get
            {
                return m_BRADAPIBaseURL;

            }
            set
            {
                m_BRADAPIBaseURL = value;
            }
        }

        public string BRADAPIToken_Timeout
        {
            get
            {
                return m_BRADAPIToken_Timeout;

            }
            set
            {
                m_BRADAPIToken_Timeout = value;
            }
        }

        public string BRADAPIUsername
        {
            get
            {
                return m_BRADAPIUsername;

            }
            set
            {
                m_BRADAPIUsername = value;
            }
        }

        public string BRADAPIWindowsUsername
        {
            get
            {
                return m_BRADAPIWindowsUsername;

            }
            set
            {
                m_BRADAPIWindowsUsername = value;
            }
        }

    }
}