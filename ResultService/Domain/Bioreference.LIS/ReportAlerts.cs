using System;
using System.Collections.Generic;
using System.Data;
using Bioreference.Common.Lab;
using Bioreference.Data;

namespace Bioreference.LIS
{

    [Serializable()]
    public class ReportAlerts : DataClassBase
    {

        private ReportAlertList m_list;
        private ReportAnalyte m_parent;

        internal ReportAlerts(ReportAnalyte parent)
        {
            m_parent = parent;
            m_list = new ReportAlertList();
        }

        public ReportAlertList List
        {
            get
            {
                return m_list;
            }
        }

        public bool AnyHaveInstruction()
        {

            foreach (ReportAlert a in m_list)
            {
                if (!string.IsNullOrEmpty(a.Alert.Instruction))
                    return true;
            }

            return default;

        }

        internal void Load(DataTable table)
        {

            ReportAlert alert;
            foreach (DataRow r in table.Rows)
            {
                alert = new ReportAlert(m_parent);
                alert.Load(r);
                m_list.Add(alert);
            }

        }

        internal void Update()
        {

            m_list.Update();

        }

        public void Add(Alert alert)
        {

            var f = new ReportAlert(m_parent, alert);
            m_list.Add(f);

        }

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || m_list.IsDirty;
            }
        }

        public void DeleteAll()
        {

            foreach (ReportAlert c in List)
            {
                List.Remove(c);
                DeleteAll();
                break;
            }

        }

        public List<string> GetAuditItems()
        {
            var lst = new List<string>();

            foreach (ReportAlert c in List)
                lst.AddRange(c.GetFormattedAuditItems);

            return lst;

        }

    }
}