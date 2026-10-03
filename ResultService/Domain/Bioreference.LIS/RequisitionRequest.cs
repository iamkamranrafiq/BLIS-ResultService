using System.Collections.Generic;

namespace Bioreference.LIS
{
    public class RequisitionRequest
    {
        private List<string> m_accessionNbr = new List<string>();
        private string m_dateOfService;

        public string DivisionType
        {
            get
            {
                return "Clinical";
            }
        }

        public List<string> Accessions
        {
            get
            {
                return m_accessionNbr;
            }
        }

        public string DateOfService
        {
            get
            {
                return m_dateOfService;
            }
            set
            {
                m_dateOfService = value;
            }
        }

        public void AddAccession(List<string> Accessionlist, bool clearExistingAccessions = false)
        {
            if (clearExistingAccessions)
            {
                m_accessionNbr.Clear();
            }
            m_accessionNbr = Accessionlist;
        }
    }
}