using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Bioreference.Data;
using Bioreference.Data.Client;

namespace Bioreference.LIS
{

    [Serializable()]
    public class NoBillCodes : DataClassBase
    {

        private NoBillCodeList _list;

        internal NoBillCodes()
        {
            _list = new NoBillCodeList();
        }

        public NoBillCodeList List
        {
            get
            {
                return _list;
            }
        }

        internal void Update()
        {
            _list.Update();
        }

        public override bool IsDirty
        {
            get
            {
                return base.IsDirty || _list.IsDirty;
            }
        }

        public static NoBillCodes Fetch()
        {
            return (NoBillCodes)DataFactory.Fetch(new Criteria());
        }

        public NoBillCode Find(string testCode, int validationTypeId)
        {

            foreach (NoBillCode noBill in _list)
            {
                if ((noBill.TestCode ?? "") == (testCode ?? "") & (int)noBill.ValidationTypeId == validationTypeId & !noBill.IsComponent)
                {
                    return noBill;
                }
            }
            return null;

        }

        public List<NoBillCode> FindAll(string testCode, NoBillCode.NoBillValidationType validationTypeId)
        {

            List<NoBillCode> list;
            list = new List<NoBillCode>();
            foreach (NoBillCode noBill in _list)
            {
                if ((noBill.TestCode ?? "") == (testCode ?? "") & noBill.ValidationTypeId == validationTypeId & !noBill.IsComponent || noBill.TestCode.ToLower().Equals("all") & noBill.ValidationTypeId == validationTypeId)
                {
                    noBill.ApplyForAllTestCodes = noBill.TestCode.ToLower().Equals("all");
                    list.Add(noBill);
                }
            }
            return list;

        }

        public NoBillCode Find(string testCode, string panelCode, int validationTypeId)
        {

            foreach (NoBillCode noBill in _list)
            {
                if ((noBill.TestCode ?? "") == (testCode ?? "") & (int)noBill.ValidationTypeId == validationTypeId & noBill.IsComponent & (noBill.ParentPanelCode ?? "") == (panelCode ?? ""))
                {
                    return noBill;
                }
            }
            return null;

        }

        public List<NoBillCode> FindAll(string testCode, string panelCode, int validationTypeId)
        {

            List<NoBillCode> list;
            list = new List<NoBillCode>();
            foreach (NoBillCode noBill in _list)
            {
                if ((noBill.TestCode ?? "") == (testCode ?? "") & (int)noBill.ValidationTypeId == validationTypeId & noBill.IsComponent & (noBill.ParentPanelCode ?? "") == (panelCode ?? "") || noBill.TestCode.ToLower().Equals("all") & (int)noBill.ValidationTypeId == validationTypeId)
                {
                    list.Add(noBill);
                }
            }
            return list;

        }

        public bool AddNoBillCode(string testCode, string thresholdLowValue, string thresholdHighValue, bool doNotReport, string performingFacilities, int validationTypeId, string statusToSend, List<string> textRanges)
        {
            bool addResult = false;
            NoBillCode noBill = null;
            noBill = (NoBillCode)_list.Find(testCode);

            if (noBill is null)
            {
                noBill = new NoBillCode();
                noBill.TestCode = testCode;
                noBill.ThresholdHighValue = thresholdHighValue;
                noBill.ThresholdLowValue = thresholdLowValue;
                noBill.DoNotReport = doNotReport;
                noBill.PerformingFacilities = performingFacilities;
                noBill.ValidationTypeId = (NoBillCode.NoBillValidationType)validationTypeId;
                noBill.StatusToSend = statusToSend;
                noBill.TextRanges = textRanges;
                noBill.Update();
                List.Add(noBill);
                addResult = true;
            }

            return addResult;

        }

        public bool DeleteBillCode(int billCodeId)
        {
            NoBillCode noBill = null;
            noBill = (NoBillCode)_list.Find(billCodeId);

            if (noBill is not null)
            {
                List.Remove(noBill);
                List.Update();
            }

            return true;

        }

        protected override void DataFactory_Fetch(object crit)
        {

            // Dim c As Criteria = crit
            var da = new DataWrapper(Configuration.ConnectionString);
            DataTable[] dt;
            try
            {
                dt = da.ExecuteProcedure("lis_NoBillCodes_Fetch");

                if (dt[0].Rows.Count > 0)
                {
                    Load(dt[0]);
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.ToString());
                throw;
            }

        }


        private void Load(DataTable table)
        {

            NoBillCode noBill = null;

            foreach (DataRow row in table.Rows)
            {

                noBill = new NoBillCode();
                noBill.Load(row);
                _list.Add(noBill);

            }

        }
        #region Inner Criteria Class

        [Serializable()]
        internal class Criteria
        {

            public Criteria()
            {
            }

        }

        #endregion
    }
}