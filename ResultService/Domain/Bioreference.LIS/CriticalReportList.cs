using System;
using System.Collections.Generic;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class CriticalReportList : DataClassReadOnlyCollectionBase
    {

        internal void Add(CriticalReport cell)
        {
            List.Add(cell);
        }

        internal void Remove(CriticalReport cell)
        {
            List.Remove(cell);
        }

        public CriticalReport[] Find(string accountNumber)
        {

            var list = new List<CriticalReport>();
            foreach (CriticalReport c in List)
            {
                if ((c.AccountNumber ?? "") == (accountNumber ?? ""))
                {
                    list.Add(c);
                }
            }

            return list.ToArray();

        }

        public CriticalReport this[int index]
        {
            get
            {
                return (CriticalReport)List[index];
            }
        }

    }
}