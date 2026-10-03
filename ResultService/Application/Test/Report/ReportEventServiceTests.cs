using AutoMapper;
using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions;
using Bioreference.ResultService.Abstractions.Application.Report;
using Bioreference.ResultService.Application.Report;
using Bioreference.ResultService.Common.Common.Report;
using Bioreference.ResultService.Common.Enumerations;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Bioreference.ResultService.Abstractions.Application.Common;

namespace BioReference.ResultService.Test.Report
{
    [TestFixture]
    public class ReportEventServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<ICommonResultService> _resultCommonMock;
        private IReportService _reportService;
        private Mock<ILogger<ReportService>> _loggerMock;
        private Mock<IMessageProducer<Incident>> _producerMock;
        private Mock<ResultEventProcessorService> _eventProcessorMock;
        private Mock<IProducerProvider> _producerProviderMock;


        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _resultCommonMock = new Mock<ICommonResultService>();
            _loggerMock = new Mock<ILogger<ReportService>>();
            _producerMock = new Mock<IMessageProducer<Incident>>();
            _eventProcessorMock = new Mock<ResultEventProcessorService>();

            _producerProviderMock = new Mock<IProducerProvider>();
            _producerProviderMock
                .Setup(p => p.GetMessageProducer<Incident>(It.IsAny<string>()))
                .Returns(_producerMock.Object);

            _reportService = new ReportService(_mapperMock.Object, null, _loggerMock.Object, _resultCommonMock.Object, null, null);
            SetTestPrincipal("TestAPI", new[] { "TestRole" });
        }

        //[Test]
        //public async Task TestReflexEventProducer()
        //{
        //    var r = OrderManager.FetchReport("8012840");
        //    var a = r.FindAnalyte("0142", false);
        //    r.SetRuleEvaluationAnalyte(a);
        //    r.SendAddTestToVertex("TBD");
        //}

        [Test]
        public async Task HandleDeleteComment_ReportCommentType_AnalyteChildComment()
        {
            EventResults reportHandleDeleteComment = new EventResults
            {
                ReportId = 170209,
                Events = new List<EventResult>
            {
                    new EventResult
                {
                    Type = EventType.AddComment,
                    Sequence = 1,
                    Data = new EventResultData
                    {
                        CommentId = "246",
                        PanelCode = "",
                        AnalyteCode = "",
                        ReportAnalyteId = 2463763,
                        ReportPanelId = 0,
                        CommentTxt = "",
                        UpdatedValue = "",
                        ReportSourceType = null,
                        ReportCommentType = null
                    }
                },
                new EventResult
                {
                    Type = EventType.DeleteComment,
                    Sequence = 2,
                    Data = new EventResultData
                    {
                        CommentId = "",
                        PanelCode = "",
                        AnalyteCode = "",
                        ReportAnalyteId = 2463763,
                        ReportPanelId = 0,
                        CommentTxt = "NOTE: Specimen submitted is LIPEMIC. This may cause inaccurate results.",
                        UpdatedValue = "",
                        ReportSourceType = null,
                        ReportCommentType = ReportCommentType.AnalyteChildComment
                    }
                }
            }
            };

            _reportService.ResultUpdate(reportHandleDeleteComment);

        }

        [Test]
        public async Task HandleAddTest_Analyte()
        {
            EventResults reportHandleDeleteComment = new EventResults
            {
                ReportId = 170209,
                Events = new List<EventResult>
            {
                    new EventResult
                {
                    Type = EventType.AddTest,
                    Sequence = 1,
                    Data = new EventResultData
                    {
                        CommentId = "",
                        PanelCode = "",
                        AnalyteCode = "0003",
                        ReportAnalyteId = 2463763,
                        ReportPanelId = 0,
                        CommentTxt = "",
                        UpdatedValue = "",
                        ReportSourceType = null,
                        ReportCommentType = null
                    }
                }
            }
            };

            _reportService.ResultUpdate(reportHandleDeleteComment);

        }

        [Test]
        public async Task HandleDelete_Analyte()
        {
            EventResults reportHandleDeleteComment = new EventResults
            {
                ReportId = 170209,
                Events = new List<EventResult>
            {
                    new EventResult
                {
                    Type = EventType.DeleteAnalyte,
                    Sequence = 1,
                    Data = new EventResultData
                    {
                        CommentId = "",
                        PanelCode = "",
                        AnalyteCode = "",
                        ReportAnalyteId = 2463763,
                        ReportPanelId = 0,
                        CommentTxt = "",
                        UpdatedValue = "",
                        ReportSourceType = ReportSourceType.Analyte,
                        ReportCommentType = null
                    }
                }
            }
            };

            _reportService.ResultUpdate(reportHandleDeleteComment);

        }

        [Test]
        public async Task HandleDelete_Panel()
        {
            EventResults reportHandleDeleteComment = new EventResults
            {
                ReportId = 170209,
                Events = new List<EventResult>
            {
                    new EventResult
                {
                    Type = EventType.DeleteAnalyte,
                    Sequence = 1,
                    Data = new EventResultData
                    {
                        CommentId = "",
                        PanelCode = "",
                        AnalyteCode = "",
                        ReportAnalyteId = 0,
                        ReportPanelId = 159874,
                        CommentTxt = "",
                        UpdatedValue = "",
                        ReportSourceType = ReportSourceType.Panel,
                        ReportCommentType = null
                    }
                }
            }
            };

            _reportService.ResultUpdate(reportHandleDeleteComment);

        }

        [Test]
        public async Task HandleAddNote_Report()
        {
            EventResults reportHandleDeleteComment = new EventResults
            {
                ReportId = 170209,
                Events = new List<EventResult>
            {
                    new EventResult
                {
                    Type = EventType.AddNote,
                    Sequence = 1,
                    Data = new EventResultData
                    {
                        CommentId = "",
                        PanelCode = "",
                        AnalyteCode = "",
                        ReportAnalyteId = 0,
                        ReportPanelId = 0,
                        CommentTxt = "The Following test is verified.",
                        UpdatedValue = "",
                        ReportSourceType = null,
                        ReportCommentType = null
                    }
                }
            }
            };

            _reportService.ResultUpdate(reportHandleDeleteComment);

        }
        private void SetTestPrincipal(string userName, string[] roles)
        {
            Thread.CurrentPrincipal = new GenericPrincipal(
                new GenericIdentity(userName),
                roles
            );
        }
    }
}
