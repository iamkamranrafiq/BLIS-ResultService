using Bioreference.Common.Lab;
using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.ResultService.Common.Common.Report;
using Bioreference.ResultService.Common.Enumerations;
using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.MsForms;
using System.ComponentModel;
using LabComment = Bioreference.Common.Lab;
using TMComment = Bioreference.Common.TestMaster;

namespace Bioreference.ResultService.Application.Report
{
    public class ResultEventProcessorService
    {
        private readonly Dictionary<EventType, Action<EventResult, Bioreference.LIS.Report, List<EventResult>>> _eventHandlers;
        private readonly IMessageProducer<Reflex> _reflexProducer;

        public ResultEventProcessorService(IProducerProvider provider = null)
        {
            _eventHandlers = new Dictionary<EventType, Action<EventResult, Bioreference.LIS.Report, List<EventResult>>>
                             {
                                 { EventType.ResultUpdate, (evt, report, _) => HandleResultUpdate(evt.Data, report) },
                                 { EventType.SaveReport, (_, report, _) => HandleSaveReport(report) },
                                 { EventType.DeleteComment, (evt, report, _) => HandleDeleteComment(evt.Data, report) },
                                 { EventType.DeletePanel, (evt, report, _) => HandleDeletePanelAndAnalyte(evt.Data, report) },
                                 { EventType.DeleteAnalyte, (evt, report, _) => HandleDeletePanelAndAnalyte(evt.Data, report) },
                                 { EventType.AddComment, (evt, report,evts) => HandleAddComment(evt.Data, report,evts) },
                                 { EventType.AddNote, (evt, report, _) => HandleAddNoteComment(evt.Data, report) },
                                 { EventType.AddTest, (evt, report, _) => HandleAddTest(evt.Data, report) },
                                 { EventType.MarkAsRelease, (evt, report, _) => HandleMarkAsReleased(evt.Data, report, ReportReleaseType.Released) },
                                 { EventType.MarkAsPendingRelease, (evt, report, _) => HandleMarkAsReleased(evt.Data, report, ReportReleaseType.Pending) },
                                 { EventType.MarkAsPreliminaryRelease, (evt, report, _) => HandleMarkAsReleased(evt.Data, report, ReportReleaseType.Preliminary) },
                                 { EventType.ResetStatus, (evt, report, _) => HandleResetStatus(evt.Data, report) },
                                 { EventType.FlagUpdate, (evt, report, _) => HandleFlagUpdate(evt.Data, report) },
                                 { EventType.MarkFlagAsDelete, (evt, report, _) => HandleMarkFlagAsDelete(evt.Data, report) },
                                 { EventType.MarkFlagAsUnDelete, (evt, report, _) => HandleMarkFlagAsUnDelete(evt.Data, report) },
                                 { EventType.UnitUpdate, (evt, report, _) => HandleUnitUpdate(evt.Data, report) },
                                 { EventType.ReferenceRangeUpdate, (evt, report, _) => HandleReferenceRangeUpdate(evt.Data, report) },
                                 { EventType.PerformingFacilityUpdate, (evt, report, _) => HandlePerformingFacilityUpdate(evt.Data, report) },
                                 { EventType.DoubleEntry, (evt, report, _) => HandleDoubleEntryUpdate(evt.Data, report) },

                             };
            _reflexProducer = provider.GetMessageProducer<Reflex>();
        }

        public void ProcessEvents(List<EventResult> events, Bioreference.LIS.Report report)
        {

            foreach (var evt in events.OrderBy(p => p.Sequence))
            {
                if (_eventHandlers.TryGetValue(evt.Type, out var handler))
                {
                    handler(evt, report, events);
                }
                else
                {
                    Console.WriteLine($"Unknown event type: {evt.Type}");
                }
            }
        }

        private void HandleResetStatus(EventResultData data, Bioreference.LIS.Report report)
        {
            bool isResultManual = true;
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, isResultManual, report);
            analyte.ResultStatus = resultStatusType.Preliminary;
            analyte.ManualReReleaseFlag = true;
            analyte.MarkAsPendingReleased();
        }

        private void HandleDoubleEntryUpdate(EventResultData data, Bioreference.LIS.Report report)
        {
            bool isResultManual = true;
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, isResultManual, report);
            analyte.AuditDoubleEntry = true;
        }

        private void HandlePerformingFacilityUpdate(EventResultData data, Bioreference.LIS.Report report)
        {
            bool isResultManual = true;
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, isResultManual, report);
            analyte.SetPerformingFacility(data.UpdatedValue);
        }

        private void HandleResultUpdate(EventResultData data, Bioreference.LIS.Report report)
        {
            bool isResultManual = true;
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, isResultManual, report);
            if (data.UpdatedValue != null)
                analyte.SetResultValue(data.UpdatedValue, isResultManual);
        }

        private void HandleSaveReport(Bioreference.LIS.Report report)
        {
            Bioreference.LIS.Report.CheckCalcPerfomingLocation(report);
            report.Save();
        }
        private void HandleDeletePanelAndAnalyte(EventResultData data, Bioreference.LIS.Report report)
        {
            switch (data.ReportSourceType)
            {
                case ReportSourceType.Analyte:
                    HandleDeleteAnalyte(data.ReportAnalyteId, report);
                    break;
                case ReportSourceType.Panel:
                    HandleDeletePanel(data.ReportPanelId, report);
                    break;
                default:
                    Console.WriteLine($"Unknown release source type: {data.ReportSourceType}");
                    break;
            }


        }
        private void HandleAddNoteComment(EventResultData data, Bioreference.LIS.Report report)
        {
            report.Comments.AddComment(data.CommentTxt);
            this.HandleSaveReport(report);
        }
        private void HandleDeleteAnalyte(long reportAnalyteId, Bioreference.LIS.Report report)
        {
            report.RemoveAnalyte(reportAnalyteId);
            this.HandleSaveReport(report);
        }

        private void HandleDeletePanel(long reportPanelId, Bioreference.LIS.Report report)
        {
            report.RemovePanel(Convert.ToInt32(reportPanelId));
            this.HandleSaveReport(report);
        }
        private void HandleAddTest(EventResultData data, Bioreference.LIS.Report report)
        {
            if (string.IsNullOrEmpty(data.AnalyteCode))
                return;

            string[] list = data.AnalyteCode.ToUpper().Split(',');
            List<string> addedList = new List<string>();
            foreach (string s in list)
            {
                if (report.FindAnalyte(s, false) != null || report.FindAnalytePanel(s) != null)
                {
                    Console.WriteLine($"Test Code '{s}' already exists for this order.");
                    continue;
                }

                if (report.AddByTestCodeAndOrderedCode(s, s, data.UserDivisionCode, data.UserDivisionCode))
                {
                    addedList.Add(s);
                }
            }
            if ((addedList.Count) == list.Length)
            {
                this.HandleSaveReport(report);
            }
        }
        private void HandleAddComment(EventResultData data, Bioreference.LIS.Report report,List<EventResult> lstevents)
        {
            if (data.IsTMComment)
            {
                HandleTMComment(data, report, lstevents);
            }

            if (data.IsLabComment)
            {
                HandleLabComment(data, report, lstevents);
            }

            if (!data.IsTMComment && !data.IsLabComment)
            {
                HandleCorrectedOtherComment(data, report,lstevents);
            }
        }

        private void HandleCorrectedOtherComment(EventResultData data, Bioreference.LIS.Report report, List<EventResult> evts)
        {
            int priority = Convert.ToInt32(data.CommentId);

            switch (data.ReportCommentType)
            {
                case ReportCommentType.PanelComment:
                    var panel = GetAnalytePanel(data.ReportPanelId, report);
                    panel.AddCommentForCommentUpdate("Add");
                    panel.Comments.AddComment(data.CommentTxt, priority);
                    break;

                case ReportCommentType.AnalyteComment:
                    var analyte = GetReportAnalyte(data.ReportAnalyteId, true, report);
                    if(!checkifAnalytehaveTNP(evts,data.ReportAnalyteId))  
                        analyte.AddCommentForCommentUpdate("Add");
                    analyte.Comments.AddComment(data.CommentTxt, priority);
                    break;
            }
        }

        private bool checkifAnalytehaveTNP(List<EventResult> evts, long reportAnalyteId)
        {
            return evts.Exists(evt => evt.Data.ReportAnalyteId == reportAnalyteId && evt.Type == EventType.ResultUpdate && evt.Data.UpdatedValue.ToUpper() == "TNP");
        }

        private void HandleTMComment(EventResultData data, Bioreference.LIS.Report report, List<EventResult> evts)
        {
            long commentId = Convert.ToInt64(data.CommentId);
            ReportComment reportComment = null;
            var selectedTMComment = GetTMComment(commentId);
            if (selectedTMComment == null) return;

            switch (data.ReportCommentType)
            {
                case ReportCommentType.PanelComment:
                    var panel = GetAnalytePanel(data.ReportPanelId, report);
                    panel.AddCommentForCommentUpdate("Add");
                    reportComment = panel.Comments.AddComment(selectedTMComment, false);
                    break;

                case ReportCommentType.AnalyteChildComment:
                case ReportCommentType.AnalyteComment:
                    var analyte = GetReportAnalyte(data.ReportAnalyteId, true, report);
                    if (!checkifAnalytehaveTNP(evts, data.ReportAnalyteId))
                        analyte.AddCommentForCommentUpdate("Add");
                    bool isParentComment = data.ReportCommentType == ReportCommentType.AnalyteComment;
                    reportComment = analyte.Comments.AddComment(selectedTMComment, false, isParentComment);
                    break;
            }

            if (data.InternalNoteId >= 0)
            {
                if (reportComment != null)
                {
                    reportComment.InternalNoteId = data.InternalNoteId;
                    reportComment.InternalNote = data.InternalNoteTxt;
                }
            }
        }

        private void HandleMarkFlagAsUnDelete(EventResultData data, LIS.Report report)
        {
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, true, report);
            analyte.MarkFlagAsUndeleted();
        }

        private void HandleMarkFlagAsDelete(EventResultData data, LIS.Report report)
        {
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, true, report);
            analyte.MarkFlagAsDeleted();
        }

        private void HandleFlagUpdate(EventResultData data, LIS.Report report)
        {
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, true, report);
            if (analyte.RapidResultId == 0)
            {
                analyte.FlagValue = data.UpdatedValue;
            }
            analyte.UpdateFlagValue(data.UpdatedValue);
        }
        private void HandleReferenceRangeUpdate(EventResultData data, LIS.Report report)
        {
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, true, report);
            analyte.UpdateReferenceRange(data.UpdatedValue);
        }

        private void HandleUnitUpdate(EventResultData data, LIS.Report report)
        {
            ReportAnalyte analyte = GetReportAnalyte(data.ReportAnalyteId, true, report);
            analyte.UpdateUnits(data.UpdatedValue);
        }

        private void HandleLabComment(EventResultData data, Bioreference.LIS.Report report, List<EventResult> evts)
        {
            long commentId = Convert.ToInt64(data.CommentId);
            var analyte = GetReportAnalyte(data.ReportAnalyteId, true, report);
            var selectedLabComment = GetLabComment(commentId, analyte);
            if (selectedLabComment == null) return;
            if(!data.IsTNPComment &&  !checkifAnalytehaveTNP(evts, data.ReportAnalyteId))
            {
                analyte.AddCommentForCommentUpdate("Add");
            }
            analyte.Comments.AddComment(selectedLabComment, false);
        }

        private TMComment.Comment? GetTMComment(long commentId)
        {
            bool bCommentFound = false;
            var tnpComments = TMComment.Comments.Fetch(TMComment.CommentType.TestNotPerformed);
            if (tnpComments != null)
            {
                foreach (TMComment.Comment comment in tnpComments.List)
                {
                    if (comment.AssignedID == null)
                        continue;

                    if (long.TryParse(comment.AssignedID.ToString(), out long analyteCommentId) && analyteCommentId == commentId)
                    {
                        return comment;
                    }

                }
            }
            if (!bCommentFound)
            {
                var nonTNPComments = TMComment.Comments.Fetch(TMComment.CommentType.Canned);
                if (nonTNPComments != null)
                {
                    foreach (TMComment.Comment comment in nonTNPComments.List)
                    {
                        if (comment.AssignedID == null)
                            continue;

                        if (long.TryParse(comment.AssignedID.ToString(), out long analyteCommentId) && analyteCommentId == commentId)
                        {
                            return comment;
                        }

                    }
                }

            }

            return null;
        }

        private LabComment.Comment? GetLabComment(long commentId, ReportAnalyte? analyte)
        {
            if (analyte?.Analyte?.CommentsSelection == null)
                return null;

            foreach (LabComment.Comment comment in analyte.Analyte.CommentsSelection)
            {
                if (comment.AssignedID == null)
                    continue;

                if (long.TryParse(comment.AssignedID.ToString(), out long analyteCommentId) && analyteCommentId == commentId)
                {
                    return comment;
                }
            }

            return null;
        }





        private void HandleMarkAsReleased(EventResultData data, Bioreference.LIS.Report report, ReportReleaseType releaseType)
        {
            switch (data.ReportSourceType)
            {
                case ReportSourceType.Analyte:
                    MarkAnalyteAsReleased(data.ReportAnalyteId, report, releaseType);
                    break;
                case ReportSourceType.Panel:
                    MarkPanelAsReleased(data.ReportPanelId, report, releaseType);
                    break;
                default:
                    Console.WriteLine($"Unknown release source type: {data.ReportSourceType}");
                    break;
            }
        }

        private void MarkAnalyteAsReleased(long? reportAnalyteId, Bioreference.LIS.Report report, ReportReleaseType releaseType)
        {
            var analyte = GetReportAnalyte(reportAnalyteId, false, report);
            if (analyte == null) return;

            switch (releaseType)
            {
                case ReportReleaseType.Preliminary: analyte.MarkAsPreliminaryReleased(); break;
                case ReportReleaseType.Pending: analyte.MarkAsPendingReleased(); break;
                case ReportReleaseType.Released: analyte.MarkAsReleased(); break;
            }
        }

        private void MarkPanelAsReleased(long? reportPanelId, Bioreference.LIS.Report report, ReportReleaseType releaseType)
        {
            var panel = GetAnalytePanel(reportPanelId, report);
            if (panel == null) return;

            switch (releaseType)
            {
                case ReportReleaseType.Preliminary: MarkPanelPreliminaryRelease(panel); break;
                case ReportReleaseType.Pending: MarkPanelRelease(panel); break;
                case ReportReleaseType.Released: MarkPanelRelease(panel); break;
            }
        }
        private void MarkPanelPreliminaryRelease(ReportAnalytePanel panel)
        {
            panel.MarkAsPreliminaryReleased();
        }
        private void MarkPanelRelease(ReportAnalytePanel panel, bool checkAll = false)
        {
                
            Dictionary<string, string> dcitanalytes = new Dictionary<string, string>();          
            bool panelReleaseStatus = false;
            bool bypass = false;
            bool release = true;          

            foreach (ReportAnalyte a in panel.Analytes.List)
            {
                if (a.CanEnableInstrument() && string.IsNullOrEmpty(a.InstrumentId))
                {
                    dcitanalytes.Add(a.Code, a.AnalyteName);
                }

                if (a.IsPresumptiveHold)
                {                   
                    break;
                }
            }
            foreach (ReportAnalyte a in panel.Analytes.List)
            {
                if (a.ReleasedStatus > 0)
                {     
                    if (a.PreviousResultValue == "QNS" || a.GetOrgResultValue() == "QNS")
                    {
                        panelReleaseStatus = true;
                    }

                    break;
                }
            }

            if (panel.IsPanelSampleRequested() && panel.PanelIsPartiallyResulted() && panelReleaseStatus)
            {
                bypass = true;
            }
            

            if (dcitanalytes.Count > 0)
            {
                release = false;

            }

            bool canPrelimRelease = panel.CanPrelimRelease();
            if(checkAll)
            {
                release = true;
            }
            if (release)
            {               
                    if (canPrelimRelease || panel.IsPanelSampleRequested())
                    {
                        if (!bypass)
                        {
                            panel.MarkAsPreliminaryReleased();
                        }
                        else
                        {
                            panel.MarkedAsPreliminary = false;
                            panel.ReleaseFromUI = true;
                            panel.MarkAsReleased();
                        }
                    }
                    else
                    {
                        panel.MarkedAsPreliminary = false;
                        panel.ReleaseFromUI = true;
                        panel.MarkAsReleased();
                    }
              
            }
            else
            {                
               panel.MarkAsPendingRelease();              
            }
        }       

        public ReportAnalyte GetReportAnalyte(long? id, bool findInPanel, Bioreference.LIS.Report report)
        {
            ReportAnalyte reportAnalyte = null;
            foreach (ReportAnalyte item in report.Analytes.List)
            {
                if (item.ID == id)
                {
                    reportAnalyte = item;
                }
            }
            if (findInPanel && reportAnalyte == null)
            {
                reportAnalyte = GetReportAnalyteChild(id, report);
            }
            return reportAnalyte;
        }

        public ReportAnalyte GetReportAnalyteChild(long? id, Bioreference.LIS.Report report)
        {
            ReportAnalyte reportAnalyte = null;
            foreach (ReportAnalytePanel p in report.AnalytePanels.List)
            {
                foreach (ReportAnalyte item in p.Analytes.List)
                {
                    if (item.ID == id)
                    {
                        reportAnalyte = item;
                        break;
                    }
                }
                if (reportAnalyte != null)
                {
                    break;
                }
            }
            return reportAnalyte;
        }

        public ReportAnalytePanel GetAnalytePanel(long? id, Bioreference.LIS.Report report)
        {
            ReportAnalytePanel reportAnalytePanel = null;
            foreach (ReportAnalytePanel item in report.AnalytePanels.List)
            {
                if (item.Id == id)
                {
                    reportAnalytePanel = item;
                    break;
                }
            }
            return reportAnalytePanel;
        }

        private void HandleDeleteComment(EventResultData data, Bioreference.LIS.Report report)
        {
            long commentId = Convert.ToInt64(String.IsNullOrEmpty(data.CommentId) ? 0 : data.CommentId);
            switch (data.ReportCommentType)
            {
                case ReportCommentType.PanelComment:
                    HandlePanelComment(commentId, data.ReportAnalyteId, data.ReportPanelId, data.CommentTxt, report);
                    break;

                case ReportCommentType.AnalyteChildComment:
                    HandleAnalyteChildComment(commentId, data.ReportAnalyteId, data.ReportPanelId, data.CommentTxt, report);
                    break;

                case ReportCommentType.AnalyteComment:
                    HandleAnalyteComment(commentId, data.ReportAnalyteId, data.ReportPanelId, data.CommentTxt, report);
                    break;
            }
        }

        public ReportComment GetReportComment(long? idComment, long? idAnalyte, long? idPanel, string? comment, Bioreference.LIS.Report report)
        {
            ReportComment reportComment = null;

            foreach (ReportAnalytePanel analytePanel in report.AnalytePanels.List)
            {
                foreach (ReportAnalyte reportAnalyte in analytePanel.Analytes.List)
                {
                    foreach (ReportComment cmd in reportAnalyte.Comments.List)
                    {
                        if (reportAnalyte.ID == idAnalyte && cmd.ID == idComment && cmd.Text.Equals(comment))
                        {
                            return cmd;
                        }
                    }
                }

                foreach (ReportComment cmd in analytePanel.Comments.List)
                {
                    if (analytePanel.Id == idPanel && cmd.ID == idComment && cmd.Text.Equals(comment))
                    {
                        return cmd;
                    }
                }
            }

            foreach (ReportAnalyte reportAnalyte in report.Analytes.List)
            {
                foreach (ReportComment cmd in reportAnalyte.Comments.List)
                {
                    if (reportAnalyte.ID == idAnalyte && cmd.ID == idComment && cmd.Text.Equals(comment))
                    {
                        return cmd;
                    }
                }
            }

            return reportComment;
        }

        private void HandlePanelComment(long? idComment, long? idAnalyte, long? idPanel, string? commentTxt, Bioreference.LIS.Report report)
        {
            ReportComment comment = GetReportComment(idComment, idAnalyte, idPanel, commentTxt, report);
            bool cmdFound = false;
            if (comment != null)
            {
                ReportAnalytePanel panel = GetAnalytePanel(idPanel, report);
                foreach (ReportComment c in panel.Comments.List)
                {
                    if (c.Text == comment.Text)
                    {
                        cmdFound = true;
                    }
                }
                if (cmdFound)
                {
                    panel.AddCommentForCommentUpdate("Delete");
                    panel.Comments.DeleteComment(comment);
                    if (comment.CommentType == Bioreference.Common.TestMaster.CommentType.TestNotPerformed)
                    {
                        PanelCheckAndRemoveTNPResultValue(panel);
                    }

                }
            }
        }
        private void HandleAnalyteComment(long? idComment, long? idAnalyte, long? idPanel, string? commentTxt, Bioreference.LIS.Report report)
        {
            var comment = GetReportComment(idComment, idAnalyte, idPanel, commentTxt, report);
            bool cmdFound = false;
            if (comment != null)
            {
                ReportAnalyte analyte = GetReportAnalyte(idAnalyte, false, report);
                foreach (ReportComment c in analyte.Comments.List)
                {
                    if (c.Text == comment.Text)
                    {
                        cmdFound = true;
                    }
                }

                if (cmdFound)
                {
                    analyte.AddCommentForCommentUpdate("Delete");
                    analyte.Comments.DeleteComment(comment);
                    if (comment.CommentType == Bioreference.Common.TestMaster.CommentType.TestNotPerformed)
                    {
                        CheckAndRemoveTNPResultValue(analyte);
                    }
                }
            }
        }
        private void HandleAnalyteChildComment(long? idComment, long? idAnalyte, long? idPanel, string? commentTxt, Bioreference.LIS.Report report)
        {
            var comment = GetReportComment(idComment, idAnalyte, idPanel, commentTxt, report);
            bool cmdFound = false;
            if (comment != null)
            {
                var analyte = GetReportAnalyteChild(idAnalyte, report);
                foreach (ReportComment c in analyte.Comments.List)
                {
                    if (c.Text == comment.Text)
                    {
                        cmdFound = true;
                    }
                }
                if (cmdFound)
                {
                    analyte.AddCommentForCommentUpdate("Delete");
                    analyte.Comments.DeleteComment(comment);
                    if (comment.CommentType == Bioreference.Common.TestMaster.CommentType.TestNotPerformed)
                    {
                        CheckAndRemoveTNPResultValue(analyte);
                    }
                }
            }
        }
        private void PanelCheckAndRemoveTNPResultValue(ReportAnalytePanel panel)
        {
            foreach (ReportAnalyte analyte in panel.Analytes.List)
            {
                CheckAndRemoveTNPResultValue(analyte);
            }
            PanelRemoveTNPComments(panel);
        }

        private void PanelRemoveTNPComments(ReportAnalytePanel panel)
        {
            List<ReportComment> commentsToRemove = new List<ReportComment>();

            foreach (ReportComment comment in panel.Comments.List)
            {
                if (comment.CommentType == Bioreference.Common.TestMaster.CommentType.TestNotPerformed)
                {
                    commentsToRemove.Add(comment);
                }
            }

            foreach (ReportComment comment in commentsToRemove)
            {
                panel.Comments.DeleteComment(comment);
            }
        }

        private void CheckAndRemoveTNPResultValue(ReportAnalyte analyte)
        {
            if (analyte.ResultValue == "TNP")
            {
                RemoveTNPComments(analyte);
                analyte.ResultValue = string.Empty;
            }
        }
        private void RemoveTNPComments(ReportAnalyte analyte)
        {
            List<ReportComment> commentsToRemove = new List<ReportComment>();

            foreach (ReportComment comment in analyte.Comments.List)
            {
                if (comment.CommentType == Bioreference.Common.TestMaster.CommentType.TestNotPerformed)
                {
                    commentsToRemove.Add(comment);
                }
            }

            foreach (ReportComment comment in commentsToRemove)
            {
                analyte.Comments.DeleteComment(comment);
            }
        }

    }

}
