using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class AttachmentList : DataClassCollectionBase
    {

        internal void Add(Attachment _attachment)
        {
            List.Add(_attachment);
        }

        internal void Remove(Attachment _attachment)
        {
            List.Remove(_attachment);
        }

        public override bool IsDirty
        {
            get
            {
                if (!(DeletedItemsList == null) && DeletedItemsList.Count > 0)
                    return true;

                foreach (Attachment objAttachment in List)
                {
                    if (objAttachment.IsDirty)
                        return true;
                }

                return false;

            }

        }

        public Attachment this[int index]
        {
            get
            {
                return (Attachment)List[index];
            }
        }

        internal void Update()
        {

            foreach (Attachment objAttachment in List)
            {
                if (objAttachment.IsDirty)
                {
                    objAttachment.Update();
                }
            }

            if (!(DeletedItemsList == null))
            {
                foreach (Attachment objAttachment in DeletedItemsList)
                {
                    if (objAttachment.IsNew == false)
                        objAttachment.Delete();
                }
                DeletedItemsList.Clear();
            }


        }
    }
}