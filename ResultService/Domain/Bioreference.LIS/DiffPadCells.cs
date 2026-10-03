using System;
using System.Data;
using Bioreference.Data;
using Bioreference.Data.Client;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class DiffPadCells : DataClassBase
    {

        #region Private Members

        private DiffPadCellList m_list = null;

        #endregion

        #region Constructor

        private DiffPadCells()
        {
            m_list = new DiffPadCellList();
        }

        #endregion

        #region Public Properties

        public DiffPadCellList List
        {
            get
            {
                return m_list;
            }
        }

        #endregion

        #region Public Functions

        public static DiffPadCells Fetch()
        {

            return (DiffPadCells)DataFactory.Fetch(new Criteria());

        }

        public DiffPadCell AddCell()
        {

            var c = new DiffPadCell();
            List.Add(c);
            return c;

        }

        public void DeleteCell(int cellID)
        {

            foreach (DiffPadCell c in List)
            {
                if (c.Id == cellID)
                {
                    List.Remove(c);
                    break;
                }
            }

        }

        #endregion

        #region Data Functions

        protected override void DataFactory_Fetch(object criteria)
        {

            Criteria c = (Criteria)criteria;
            var da = new DataWrapper(Configuration.ConnectionString);

            DataTable[] dt = da.ExecuteProcedure("lis_DiffPadCells_Fetch");

            Load(dt[0]);

        }

        protected override void DataFactory_Save()
        {

            List.Update();

        }

        private void Load(DataTable table)
        {

            DiffPadCell c = null;
            DiffPadCellValue v = null;
            int cellId = 0;

            foreach (DataRow r in table.Rows)
            {

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(cellId, r["DiffPadCellId"], false)))
                {
                    c = new DiffPadCell();
                    c.Load(r);
                    List.Add(c);
                    cellId = Conversions.ToInteger(r["DiffPadCellId"]);
                }

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectNotEqual(r["DiffPadCellValueId"], 0, false)))
                {
                    v = new DiffPadCellValue(c);
                    v.Load(r);
                    c.ValueList.Add(v);
                }

            }

            FlagClean();

        }

        #endregion

        #region Inner Criteria Class

        [Serializable()]
        public class Criteria
        {

            internal Criteria()
            {
            }

        }

        #endregion

        public override bool IsDirty
        {
            get
            {
                return m_list.IsDirty;
            }
        }

    }
}