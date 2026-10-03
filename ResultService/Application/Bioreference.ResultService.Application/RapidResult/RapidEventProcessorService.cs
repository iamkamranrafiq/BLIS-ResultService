using Bioreference.LIS;
using Bioreference.ResultService.Common.Common;
using Bioreference.ResultService.Common.Common.RapidResult;
using Bioreference.ResultService.Common.Enumerations;
using System.Linq;


namespace Bioreference.ResultService.Application.RapidResult
{
    public class RapidEventProcessorService
    {
        private readonly Dictionary<EventRapidType, Action<EventRapidData, Bioreference.LIS.RapidResult>> _eventHandlers;
        private List<Bioreference.LIS.RapidResult.RapidResultReport> reportClearResults = new List<Bioreference.LIS.RapidResult.RapidResultReport>();

        public RapidEventProcessorService()
        {
            _eventHandlers = new Dictionary<EventRapidType, Action<EventRapidData, Bioreference.LIS.RapidResult>>
        {
            { EventRapidType.AddAccession, (data, rapidResult) => HandleAddAccession(data, rapidResult) },
            { EventRapidType.Delete, (data, rapidResult) => HandleDeleteAccession(data, rapidResult) },
            { EventRapidType.Release, (data, rapidResult) => HandleReleaseAccession(data, rapidResult) },
            { EventRapidType.ResultUpdate, (data, rapidResult) => HandleResultUpdate(data, rapidResult) },
            { EventRapidType.ReRun, (data, rapidResult) => HandleReRunAccession(data, rapidResult) },
            { EventRapidType.UndoReRun, (data, rapidResult) => HandleUndoReRunAccession(data, rapidResult) },
            { EventRapidType.Deactivate, (data, rapidResult) => HandleDeactivateOrder(data, rapidResult) },
            { EventRapidType.Save, (data, rapidResult) => HandleSaveRapidResult(data, rapidResult) },
            { EventRapidType.CloseOut, (data, rapidResult) => HandleCloseOutRapidResult(data, rapidResult) }
        };
        }

        public void ProcessEvents(List<EventRapid> events, Bioreference.LIS.RapidResult rapidResult)
        {
            foreach (var evt in events.OrderBy(p => p.Sequence))
            {
                if (_eventHandlers.TryGetValue(evt.Type, out var handler))
                {
                    handler(evt.Data, rapidResult);
                }
                else
                {
                    Console.WriteLine($"Unknown event type: {evt.Type}");
                }
            }
        }
        private void HandleReleaseAccession(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
            if (rapidResult.IsDirty)
            {
                 rapidResult.Save();
                rapidResult.Process(false); 
            }
            List<string> notReleaseAnalytes = new List<string>();

            foreach (Bioreference.LIS.RapidResult.AnalyteInfo analyte in rapidResult.Analytes)
            {
                if (!GlobalModule.HasAnalyteReleasePermissions(analyte.Analyte.Code))
                {
                    notReleaseAnalytes.Add(analyte.Analyte.Code);
                }
            }

            if (notReleaseAnalytes.Count > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (string s in notReleaseAnalytes)
                {
                    sb.Append(s + " ");
                }
                Console.WriteLine("You do not have permissions to release the following analytes: \n" + sb.ToString());
            }
            Bioreference.LIS.RapidResult.RapidResultReport[] rapidResultReport = rapidResult.FetchRapidResultReports(data.ChkPrelimOnly);
            foreach (var rapidReport in rapidResultReport)
            {                
                if (rapidReport != null)
                {
                    if (rapidReport.AccessionNbr == data.AccessionNbr)
                    {
                        foreach (var rapidResultAnalyte in rapidResult.List.Where(a => a.AnalyteCode == data.AnalyteCode &&
                                                                                  a.AccessionNbr == data.AccessionNbr &&
                                                                                  a.ReportAnalyteId == data.ReportAnalyteId))
                        {                                                                                        
                                if (!rapidResultAnalyte.IsControl && !notReleaseAnalytes.Contains(rapidResultAnalyte.AnalyteCode))
                                {
                                    rapidReport.Process(rapidResultAnalyte.AnalyteCode, transmitStatusType.Released, false);
                                }                              
                            
                            rapidReport.SaveReport();
                        }
                        foreach (var rapidReportAnalyte in rapidReport.List.Where(a => a.AnalyteCode == data.AnalyteCode &&
                                                                                  a.AccessionNbr == data.AccessionNbr &&
                                                                                  a.ReportAnalyteId == data.ReportAnalyteId))
                        {                        
                                RapidResultAnalyte rapidAnalyte = rapidResult.List.First(p => p.AnalyteCode == rapidReportAnalyte.AnalyteCode &&
                                                     p.AccessionNbr == rapidReportAnalyte.AccessionNbr &&
                                                     p.ReportAnalyteId == rapidReportAnalyte.ReportAnalyteId);
                                rapidAnalyte = rapidReportAnalyte;                               

                        }
                    }
                }

            }
        }  
        private void HandleCloseOutRapidResult(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
            rapidResult.Close();
        }
        private void HandleReRunAccession(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
            if (rapidResult.IsDirty)
            {
                 rapidResult.Save();
                rapidResult.Process(false); 
            }           
            Bioreference.LIS.RapidResult.RapidResultReport[] rapidResultReport = rapidResult.FetchRapidResultReports(data.ChkPrelimOnly);
            foreach (var rapidReport in rapidResultReport)
            {                
                if (rapidReport != null)
                {
                    if (rapidReport.AccessionNbr == data.AccessionNbr)
                    {
                        foreach (RapidResultAnalyte rapidResultAnalyte in rapidResult.List)
                        {
                            if (rapidResultAnalyte.AnalyteCode == data.AnalyteCode && rapidResultAnalyte.AccessionNbr == data.AccessionNbr && rapidResultAnalyte.ReportAnalyteId == data.ReportAnalyteId)
                            {   
                               rapidReport.Process(rapidResultAnalyte.AnalyteCode, transmitStatusType.HeldForRerun);
                            }
                            
                        }
                    }
                }

            }
        }  
        private void HandleUndoReRunAccession(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
            if (rapidResult.IsDirty)
            {
                 rapidResult.Save();
                rapidResult.Process(false); 
            }           
            Bioreference.LIS.RapidResult.RapidResultReport[] rapidResultReport = rapidResult.FetchRapidResultReports(data.ChkPrelimOnly);
            foreach (var rapidReport in rapidResultReport)
            {                
                if (rapidReport != null)
                {
                    if (rapidReport.AccessionNbr == data.AccessionNbr)
                    {
                        foreach (RapidResultAnalyte rapidResultAnalyte in rapidResult.List)
                        {                            
                            if (rapidResultAnalyte.TransmitStatus == transmitStatusType.HeldForRerun)
                            {
                                rapidReport.Process(rapidResultAnalyte.AnalyteCode, transmitStatusType.PendingRelease);
                            }

                        }
                    }
                }

            }
        }
        private void HandleDeactivateOrder(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
            foreach (RapidResultAnalyte rapidResultAnalyte in rapidResult.List)
            {
                if (rapidResultAnalyte.AccessionNbr == data.AccessionNbr)
                {
                    rapidResultAnalyte.IsDeactivated = true;
                }
            }

        }
        private void HandleAddAccession(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
            rapidResult.AddOrder(data.AccessionNbr, data.IsControl);
        }
        private void HandleResultUpdate(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
            foreach (RapidResultAnalyte rapidResultAnalyte in rapidResult.List)
            {
                if (rapidResultAnalyte.AnalyteCode == data.AnalyteCode && rapidResultAnalyte.AccessionNbr == data.AccessionNbr && rapidResultAnalyte.ReportAnalyteId == data.ReportAnalyteId)
                {
                    rapidResultAnalyte.ResultValue = data.ResultValue;
                }
            }         
        }

        private void HandleDeleteAccession(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
            Bioreference.LIS.RapidResult.RapidResultReport[] rapidResultReport = rapidResult.FetchRapidResultReports(data.ChkPrelimOnly);
            rapidResult.RemoveOrder(data.AccessionNbr,data.RowNumber);
            if (data.ClearResults)
            {                
                
                foreach (var wr in rapidResultReport)
                {
                    if (wr.AccessionNbr == data.AccessionNbr)
                    {
                        foreach (RapidResultAnalyte a in wr.List)
                        {
                            if (a.TransmitStatus == transmitStatusType.HeldForRerun ||
                                a.TransmitStatus == transmitStatusType.None ||
                                a.TransmitStatus == transmitStatusType.PendingRelease)
                            {
                                a.ResultValue = "";
                            }
                        }
                        reportClearResults.Add(wr);
                    }
                }
            }
        }

        private void HandleSaveRapidResult(EventRapidData data, Bioreference.LIS.RapidResult rapidResult)
        {
             rapidResult.Save();           
             rapidResult.Process(false);
            Bioreference.LIS.Report report = null;
            ReportAnalyte reportAnalyte = null;
            if (reportClearResults != null && reportClearResults.Count() > 0)
            {
                foreach (Bioreference.LIS.RapidResult.RapidResultReport r in reportClearResults)
                {
                    report = OrderManager.FetchReport(r.ReportId);
                    if (report != null)
                    {
                        foreach (RapidResultAnalyte a in r.List)
                        {
                            reportAnalyte = report.FindAnalyte(a.AnalyteCode, true);
                            if (!reportAnalyte.HasBeenReleased() && !a.IsReference && a.ResultValue == "")
                            {
                                reportAnalyte.ResultValue = "";
                            }
                        }
                        if (report.IsValid)
                        {
                            report.Save();
                        }
                    }
                }
            }
        }
    }
}
