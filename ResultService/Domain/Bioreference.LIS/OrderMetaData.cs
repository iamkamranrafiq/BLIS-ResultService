
namespace Bioreference.LIS
{
    public class OrderMetaData
    {

        private string m_value = string.Empty;
        private string m_name = string.Empty;

        public OrderMetaData(string value, string name)
        {
            m_value = value;
            m_name = name;
        }

        public string Value
        {
            get
            {
                return m_value;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

    }
}