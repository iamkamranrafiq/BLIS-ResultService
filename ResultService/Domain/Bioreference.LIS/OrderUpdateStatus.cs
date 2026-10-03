using System;
using System.Collections.Generic;

namespace Bioreference.LIS
{
    [Serializable()]
    public class OrderUpdateStatus
    {

        internal List<string> m_updatedProperties = new List<string>();
        internal List<string> m_testCodesRereleased = new List<string>();

        public OrderUpdateStatus()
        {
        }

        /// <summary>
    /// Determines if any properties were updated that would trigger a release.
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public bool CheckForRelease
        {
            get
            {
                return m_updatedProperties.Count > 0;
            }
        }
        public List<string> ReleasedTests
        {
            get
            {
                return m_testCodesRereleased;
            }
        }

        public List<string> UpdatedProperties
        {
            get
            {
                return m_updatedProperties;
            }
        }

        internal void AddProperty(string propertyName)
        {
            if (!m_updatedProperties.Contains(propertyName))
            {
                m_updatedProperties.Add(propertyName);
            }
        }

    }
}