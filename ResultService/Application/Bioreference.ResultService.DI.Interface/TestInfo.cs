
namespace Bioreference.ResultService.DI.Interface
{
    public class TestInfo
    {
        private string m_testCode;
        private string m_refCode;
        private string m_refOrderingCode;
        private string m_Name;
        private bool m_isSentOut;
        public TestInfo(string testCode, string RefCode, string Name, bool isSentOut, string RefOrderingCode)
        {
            m_testCode = testCode;
            m_refCode = RefCode;
            m_Name = Name;
            m_isSentOut = isSentOut;
            m_refOrderingCode = RefOrderingCode;
        }

        public string TestCode
        {
            get
            {
                return m_testCode;
            }
        }
        public string ReferenceCode
        {
            get
            {
                return m_refCode;
            }
        }
        public string ReferenceOrderingCode
        {
            get
            {
                return m_refOrderingCode;
            }
        }
        public string Name
        {
            get
            {
                return m_Name;
            }
        }
        public bool IsSentOut
        {
            get
            {
                return m_isSentOut;
            }
        }

    }
}