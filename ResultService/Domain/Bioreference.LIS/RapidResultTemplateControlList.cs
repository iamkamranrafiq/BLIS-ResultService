using System;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class RapidResultTemplateControlList : DataClassCollectionBase
    {

        private RapidResultTemplate m_parent;

        internal RapidResultTemplateControlList(RapidResultTemplate parent)
        {

            m_parent = parent;

        }

        internal void Add(RapidResultTemplateControl control)
        {

            List.Add(control);

        }

        internal void Remove(RapidResultTemplateControl control)
        {

            List.Remove(control);

        }

        public void Remove(string name)
        {
            var c = Find(name);
            if (!(c == null))
            {
                List.Remove(c);
            }
        }

        public RapidResultTemplateControl this[int index]
        {
            get
            {
                return (RapidResultTemplateControl)List[index];
            }
        }

        public RapidResultTemplateControl Find(string name)
        {

            foreach (RapidResultTemplateControl Item in List)
            {
                if (Item.Name.Equals(name))
                {
                    return Item;
                }
            }
            return null;

        }

        internal void Update()
        {

            foreach (RapidResultTemplateControl c in List)
            {
                if (c.IsDirty)
                {
                    c.Update();
                }
            }
            foreach (RapidResultTemplateControl c in DeletedItemsList)
                c.Delete();

        }

    }
}