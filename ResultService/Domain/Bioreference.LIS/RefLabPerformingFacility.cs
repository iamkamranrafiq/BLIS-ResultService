using System.Data;
using Bioreference.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class RefLabPerformingFacility : DataClassBase
    {

        private int m_id;
        private int m_RefLabId;
        private string m_FacilityCode;
        private string m_FacilityId;

        public int Id
        {
            get
            {
                return m_id;
            }
        }

        public int RefLabId
        {
            get
            {
                return m_RefLabId;
            }
        }

        public int FacilityId
        {
            get
            {
                return Conversions.ToInteger(m_FacilityId);
            }
        }

        public string FacilityCode
        {
            get
            {
                return m_FacilityCode;
            }
        }

        internal void Load(DataRow row)
        {
            m_id = Conversions.ToInteger(row["Id"]);
            m_FacilityCode = Conversions.ToString(row["FacilityCode"]);
            m_FacilityId = Conversions.ToString(row["FacilityId"]);
            m_RefLabId = Conversions.ToInteger(row["RefLabID"]);

            FlagClean();
        }
    }
}