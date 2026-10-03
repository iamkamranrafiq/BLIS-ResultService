using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadResultValue : DataClassBase
    {

        private string m_code = "";
        private string m_value = "";
        private DiffPadResult m_parent = null;

        internal DiffPadResultValue(DiffPadResult parent)
        {
            m_parent = parent;
        }

        public string Code
        {
            get
            {
                return m_code;
            }
            set
            {
                if ((m_code ?? "") != (value.Trim() ?? ""))
                {
                    m_code = value.Trim();
                    FlagDirty();
                }
            }
        }

        public string Value
        {
            get
            {
                return m_value;
            }
            set
            {
                if ((m_value ?? "") != (value.Trim() ?? ""))
                {
                    m_value = value.Trim();
                    FlagDirty();
                }
            }
        }

    }
}