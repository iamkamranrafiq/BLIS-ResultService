using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportList : DataClassReadOnlyCollectionBase
    {

        public ReportInfo Find(int reportId)
        {

            foreach (ReportInfo r in List)
            {
                if (r.ReportId == reportId)
                    return r;
            }
            return null;
        }

        internal void Add(ReportInfo resultInfo)
        {
            List.Add(resultInfo);
        }

        public ReportInfo this[int index]
        {
            get
            {
                return (ReportInfo)List[index];
            }
        }

    }
}