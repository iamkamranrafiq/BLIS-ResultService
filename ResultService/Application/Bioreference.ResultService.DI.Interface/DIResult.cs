using Bioreference.Common.Lab;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.DI.Interface
{
    public class DIResult
    {
        private Patient oPatient = null;
        private PatientVisit oPatVisit = null;
        private List<Report> oReports = null;

        private string oOrderStatus = string.Empty;
        private string oServiceDate = string.Empty;

        private string sComments = string.Empty;
        private List<string> aOrderComments = new List<string>();
        private List<NTE> cNtes = null;

        public DIResult()
        {
            oReports = new List<Report>();
        }

        public List<string> OrderComments
        {
            get
            {
                return aOrderComments;
            }
        }

        public string ServiceDate
        {
            get
            {
                return oServiceDate;
            }
            set
            {
                oServiceDate = value;
            }
        }


        public string OrderStatus
        {
            get
            {
                return oOrderStatus;
            }
            set
            {
                oOrderStatus = value;
            }
        }

        public Patient rPatient
        {
            get
            {
                return oPatient;
            }
            set
            {
                oPatient = value;
            }
        }

        public PatientVisit rPatientVisit
        {
            get
            {
                return oPatVisit;
            }
            set
            {
                oPatVisit = value;
            }
        }

        public List<Report> rReports
        {
            get
            {
                return oReports;
            }
            set
            {
                oReports = value;
            }
        }


        public string Comments
        {
            get
            {
                return sComments;
            }
            set
            {
                sComments = value;
                AddNteInternal();
            }
        }

        public List<NTE> NTEs
        {
            get
            {
                return cNtes;
            }
        }

        private void AddNteInternal()
        {
            if (cNtes is null)
                cNtes = new List<NTE>();
            else
                cNtes.Clear();
            List<NTE> cComNte;
            cComNte = Common.GetNteFromComment(sComments);
            foreach (NTE Note in cComNte)
                cNtes.Add(Note);
        }

        public Report FindReportAnalyte(string code)
        {

            foreach (Report Item in rReports)
            {
                foreach (ReportAnalyte a in Item.AnalyteList)
                {
                    if ((a.Code ?? "") == (code ?? ""))
                    {
                        return Item;
                    }
                }
            }
            return null;

        }

        public Report FindReport(string code)
        {

            foreach (Report Item in rReports)
            {
                if ((Item.TestCode ?? "") == (code ?? ""))
                {
                    return Item;
                }
            }
            return null;

        }

    }

    //public class DIResult
    //{
    //    public Patient Patient { get; set; }
    //    public PatientVisit PatientVisit { get; set; }
    //    public List<Report> Reports {  get; set; }
    //    public string OrderStatus { get; set; }
    //    public string ServiceDate { get; set; }
    //    private string comments { get; set; }
    //    private List<string> orderComments;
    //    private List<NTE> ntes;

    //    public DIResult() 
    //    {
    //        Reports=new List<Report>();
    //        orderComments=new List<string>();
    //    }

    //    public List<string> OrderComments
    //    {
    //        get
    //        {
    //            return orderComments;
    //        }
    //    }

    //    public string Comments
    //    {
    //        get
    //        {
    //            return comments;
    //        }
    //        set
    //        {
    //            comments = value;
    //            AddNteInternal();
    //        }
    //    }

    //    public List<NTE> NTEs
    //    {
    //        get
    //        {
    //            return NTEs;
    //        }
    //    }

    //    private void AddNteInternal()
    //    {
    //        ntes=new List<NTE>();
    //        List<NTE> notes = Common.GetNteFromComment(comments);
    //        foreach (NTE nte in notes)
    //        {
    //            ntes.Add(nte);
    //        }
    //    }

    //    public Report FindReportAnalyte(string code)
    //    {
    //        foreach (Report report in Reports)
    //            foreach (ReportAnalyte analyte in report.AnalyteList)
    //                if (analyte.Code == code)
    //                    return report;
    //        return null;
    //    }

    //    public Report FindReport(string code)
    //    {
    //        foreach (Report report in Reports)
    //            if (report.TestCode == code)
    //                return report;
    //        return null;
    //    }
    //}
}
