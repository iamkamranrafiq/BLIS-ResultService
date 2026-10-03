using System;
using System.Collections.Generic;
using System.Data;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class Attachments : DataClassBase
    {

        private AttachmentList m_list;
        private ReportAnalyte m_parent;

        internal Attachments(ReportAnalyte parent)
        {
            m_parent = parent;
            m_list = new AttachmentList();
        }

        public AttachmentList List
        {
            get
            {
                return m_list;
            }
        }

        internal void Load(DataTable table)
        {

            Attachment objAttachment;
            foreach (DataRow r in table.Rows)
            {
                objAttachment = new Attachment(m_parent);
                objAttachment.Load(r);
                m_list.Add(objAttachment);
            }
        }

        internal void Update()
        {

            m_list.Update();

        }

        public void Add(Attachment _attachment)
        {
            if (List.Count > 0)
            {
                DeleteAll();
            }
            m_list.Add(_attachment);

        }

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_list.IsDirty;
            }
        }

        public void DeleteAttachment(Attachment attachment)
        {
            attachment.MarkParentCorrected();
            attachment.Delete();
            List.Remove(attachment);
        }

        public void DeleteAll()
        {
            foreach (Attachment att in List)
            {
                List.Remove(att);
                DeleteAll();
                break;
            }
        }


        public List<string> GetAuditItems()
        {
            var lst = new List<string>();

            foreach (ReportComment c in List)
                lst.AddRange(c.GetFormattedAuditItems);

            return lst;

        }

        public void ResetAudit()
        {
            foreach (ReportComment c in m_list)
                c.ResetAuditItems();
        }

    }
}