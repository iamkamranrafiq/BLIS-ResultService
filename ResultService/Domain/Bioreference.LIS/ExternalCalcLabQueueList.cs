using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ExternalCalcLabQueueList : DataClassCollectionBase
    {

        internal ExternalCalcLabQueueList()
        {
        }

        internal void Add(ExternalCalcLabQueue externalCalcLabQueue)
        {
            List.Add(externalCalcLabQueue);
        }

        internal void Remove(ExternalCalcLabQueue externalCalcLabQueue)
        {
            List.Remove(externalCalcLabQueue);
        }

        public ExternalCalcLabQueue this[int index]
        {
            get
            {
                return (ExternalCalcLabQueue)List[index];
            }
        }

        internal void Update()
        {
            foreach (ExternalCalcLabQueue w in List)
                w.Save();
            // If Not Me.DeletedItemsList Is Nothing Then
            // For Each w As ExternalCalcLabQueue In MyBase.DeletedItemsList
            // If w.IsNew = False Then w.Delete()
            // Next
            // End If
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;
                foreach (ExternalCalcLabQueue w in List)
                {
                    if (w.IsDirty)
                        return true;
                }
                return false;
            }
        }

    }
}