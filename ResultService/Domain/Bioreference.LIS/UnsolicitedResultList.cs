using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class UnsolicitedResultList : DataClassCollectionBase
    {

        /// 
    /// <param name="result"></param>
        internal void Add(UnsolicitedResult result)
        {
            List.Add(result);
        }

        /// 
    /// <param name="index"></param>
        public UnsolicitedResult this[int index]
        {
            get
            {
                return (UnsolicitedResult)List[index];
            }
        }

        internal void Update()
        {

            foreach (UnsolicitedResult ur in List)
            {
                if (ur.IsDirty)
                    ur.Save();
            }

        }

    }
}