using System;
using System.Collections.Generic;
using System.Data;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadTemplates : DataClassBase
    {

        private List<DiffPadTemplate> m_list;
        private List<DiffPadTemplate> m_deletedList;

        private DiffPadTemplates()
        {
            m_list = new List<DiffPadTemplate>();
            m_deletedList = new List<DiffPadTemplate>();
        }

        public DiffPadTemplate[] List
        {
            get
            {
                return m_list.ToArray();
            }
        }

        public static DiffPadTemplates Fetch()
        {

            return (DiffPadTemplates)DataFactory.Fetch(new Criteria());

        }

        public void AddToList(DiffPadTemplate template)
        {

            m_list.Add(template);

        }

        public void Delete(DiffPadTemplate template)
        {

            m_list.Remove(template);
            m_deletedList.Add(template);

        }

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            DataTable[] dt = da.ExecuteProcedure("lis_DiffPadTemplates_Fetch");

            Load(dt[0]);

        }

        protected override void DataFactory_Save()
        {

            foreach (DiffPadTemplate t in m_list)
            {
                if (t.IsDirty)
                    t.Save();
            }

            foreach (DiffPadTemplate t in m_deletedList)
            {
                if (!t.IsNew)
                    t.Delete();
            }

        }

        public override bool IsDirty
        {
            get
            {
                if (m_deletedList.Count > 0)
                    return true;
                foreach (DiffPadTemplate t in m_list)
                {
                    if (t.IsDirty)
                        return true;
                }
                return false;
            }
        }

        private void Load(DataTable table)
        {

            int templateId = 0;
            int templateCellId = 0;
            DiffPadTemplate template = null;
            DiffPadTemplateCell templateCell = null;
            DiffPadCell cell = null;
            DiffPadCellValue cellValue = null;

            foreach (DataRow r in table.Rows)
            {

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["DiffPadTemplateId"], templateId, false)))
                {
                    template = new DiffPadTemplate();
                    template.Load(r);
                    m_list.Add(template);
                    templateId = Conversions.ToInteger(r["DiffPadTemplateId"]);
                }

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["DiffPadTemplateCellId"], templateCellId, false)))
                {
                    templateCell = new DiffPadTemplateCell(template);

                    cell = new DiffPadCell();
                    cell.Load(r);

                    templateCell.Load(r, cell);
                    template.List.Add(templateCell);
                    templateCellId = Conversions.ToInteger(r["DiffPadTemplateCellId"]);
                }

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["DiffPadCellValueId"], 0, false)))
                {
                    cellValue = new DiffPadCellValue(cell);
                    cellValue.Load(r);
                    cell.ValueList.Add(cellValue);
                }

            }

        }

        [Serializable()]
        internal class Criteria
        {
            public Criteria()
            {
            }
        }

    }
}