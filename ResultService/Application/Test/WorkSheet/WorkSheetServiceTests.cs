using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Lookup;
using Bioreference.ResultService.Abstractions.Application.WorkSheet;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Enum;
using Bioreference.ResultService.Application.Model.WorkSheet;
using Moq;

namespace Bioreference.ResultService.Application.Report.Tests
{
    [TestFixture]
    public class WorkSheetServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<ILookupService> _lookupServiceMock;
        private Mock<IWorkSheetService> _workSheetServiceMock;
        private WorkSheetService _workSheetService;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _lookupServiceMock = new Mock<ILookupService>();
            _workSheetServiceMock = new Mock<IWorkSheetService>();
            _workSheetService = new WorkSheetService(_mapperMock.Object, _lookupServiceMock.Object);


        }

        [Test]
        public async Task GetWorkSheetById_WhenWorkSheetExists_ReturnsWorkSheetReports()
        {
            // Arrange
            int workSheetId = 6;
            var expectedResponse = new List<WorkSheetReportModel>
        {
            new WorkSheetReportModel
            {
                RackId = "RACK001",
                RackWorksheetId = workSheetId,
                AccessionNbr = "ACC123",
                Sequence = 1,
                RackPosition = 1,
                ResultStatus = resultStatusTypeModel.Pending,
                TransmitStatus = transmitStatusTypeModel.PendingRelease,
                PendingCount = 2,
                AlertCount = 0,
                ReportId = 456,
                RackWorksheetTemplateId = 789
            }
        };

            _mapperMock
                .Setup(m => m.Map<List<WorkSheetReportModel>>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _workSheetService.GetWorkSheetById(workSheetId, 1, 100);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedResponse));
            _mapperMock.Verify(
                m => m.Map<List<WorkSheetReportModel>>(It.IsAny<object>()),
                Times.Once);
        }

        [Test]
        public async Task GetWorkSheetById_ShouldHandleVariousStatuses()
        {
            // Arrange
            int workSheetId = 6;
            var expectedResponse = new List<WorkSheetReportModel>
        {
            new WorkSheetReportModel
            {
                RackId = "RACK001",
                RackWorksheetId = workSheetId,
                AccessionNbr = "ACC123",
                ResultStatus = resultStatusTypeModel.Pending,
                TransmitStatus = transmitStatusTypeModel.PendingRelease
            },
            new WorkSheetReportModel
            {
                RackId = "RACK002",
                RackWorksheetId = workSheetId,
                AccessionNbr = "ACC124",
                ResultStatus = resultStatusTypeModel.Preliminary,
                TransmitStatus = transmitStatusTypeModel.Released
            },
            new WorkSheetReportModel
            {
                RackId = "RACK003",
                RackWorksheetId = workSheetId,
                AccessionNbr = "ACC125",
                ResultStatus = resultStatusTypeModel.Final,
                TransmitStatus = transmitStatusTypeModel.Reported
            },
            new WorkSheetReportModel
            {
                RackId = "RACK004",
                RackWorksheetId = workSheetId,
                AccessionNbr = "ACC126",
                ResultStatus = resultStatusTypeModel.OnHold,
                TransmitStatus = transmitStatusTypeModel.HeldForRerun
            }
        };

            _mapperMock
                .Setup(m => m.Map<List<WorkSheetReportModel>>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _workSheetService.GetWorkSheetById(workSheetId, 1, 4);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result[0].ResultStatus, Is.EqualTo(resultStatusTypeModel.Pending));
            Assert.That(result[0].TransmitStatus, Is.EqualTo(transmitStatusTypeModel.PendingRelease));
            Assert.That(result[1].ResultStatus, Is.EqualTo(resultStatusTypeModel.Preliminary));
            Assert.That(result[1].TransmitStatus, Is.EqualTo(transmitStatusTypeModel.Released));
            Assert.That(result[2].ResultStatus, Is.EqualTo(resultStatusTypeModel.Final));
            Assert.That(result[2].TransmitStatus, Is.EqualTo(transmitStatusTypeModel.Reported));
            Assert.That(result[3].ResultStatus, Is.EqualTo(resultStatusTypeModel.OnHold));
            Assert.That(result[3].TransmitStatus, Is.EqualTo(transmitStatusTypeModel.HeldForRerun));
        }

        [Test]
        public async Task GetWorkSheetById_WhenWorkSheetDoesNotExist_ReturnsEmptyList()
        {
            // Arrange
            int workSheetId = 999;
            var emptyResponse = new List<WorkSheetReportModel>();

            _mapperMock
                .Setup(m => m.Map<List<WorkSheetReportModel>>(It.IsAny<object>()))
                .Returns(emptyResponse);

            // Act
            var result = await _workSheetService.GetWorkSheetById(workSheetId, 1, 100);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetWorkSheets_WhenWorkSheetsExist_ReturnsFilteredWorkSheets()
        {
            // Arrange
            var request = new WorkSheetSearchCriteria
            {
                TemplateId = 18,
                DateFrom = DateTime.Now.AddDays(-100),
                DateTo = DateTime.Now,
                PageNumber = 1,
                PageSize = 100
            };

            var expectedResponse = new List<RackWorkSheetModel>
        {
            new RackWorkSheetModel
            {
                WorkSheetId = 123,
                CreatedBy = "TestUser",
                CreatedDate = DateTime.Now.AddDays(-5),
                Total = 10,
                Released = 5,
                AssignedTo = "LabTech1",
                Status = 1
            }
        };

            _mapperMock
                .Setup(m => m.Map<List<RackWorkSheetModel>>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _workSheetService.GetWorkSheets(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedResponse));
            _mapperMock.Verify(
                m => m.Map<List<RackWorkSheetModel>>(It.IsAny<object>()),
                Times.Once);
        }



        #region ReleaseCheckedWorkSheets Tests

        [Test]
        public async Task ReleaseCheckedWorkSheets_WithValidReports_ReturnsTrue()
        {
            // Arrange
            var reportIds = new List<int> { 105313 };
            var rackWorkSheetTemplateId = 18;
            var workSheetId = 42;

            var mockTemplates = Mock.Of<RackWorksheetTemplates>();
            _workSheetServiceMock.Setup(x => x.WorkSheetTemplatesInfo())
                .Returns(Task.FromResult(mockTemplates));

            // Act
            var result = await _workSheetService.ReleaseCheckedWorkSheets(reportIds, rackWorkSheetTemplateId, workSheetId);

            // Assert
            Assert.That(result, Is.True);
            _workSheetServiceMock.Verify(x => x.WorkSheetTemplatesInfo(), Times.Once);
        }

        [Test]
        public async Task ReleaseCheckedWorkSheets_WithNullReportIds_ReturnsFalse()
        {
            // Arrange
            List<int> reportIds = null;
            var rackWorkSheetTemplateId = 1;
            var workSheetId = 100;

            // Act
            var result = await _workSheetService.ReleaseCheckedWorkSheets(reportIds, rackWorkSheetTemplateId, workSheetId);

            // Assert
            Assert.That(result, Is.False);
            _workSheetServiceMock.Verify(x => x.WorkSheetTemplatesInfo(), Times.Never);
        }

        [Test]
        public async Task ReleaseCheckedWorkSheets_WhenLookupServiceThrows_ReturnsFalse()
        {
            // Arrange
            var reportIds = new List<int> { 1, 2, 3 };
            var rackWorkSheetTemplateId = 1;
            var workSheetId = 100;

            _workSheetServiceMock.Setup(x => x.WorkSheetTemplatesInfo())
                .Throws(new Exception("Service error"));

            // Act
            var result = await _workSheetService.ReleaseCheckedWorkSheets(reportIds, rackWorkSheetTemplateId, workSheetId);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region UpdateWorksheet Tests

        [Test]
        public async Task UpdateWorksheet_WithValidModel_ReturnsTrue()
        {
            // Arrange
            var model = new WorkSheetAddUpdateModel
            {
                Id = 1,
                RackWorkSheetTemplateId = 1,
                WorkSheetSpecimen = new List<WorkSheetSpecimenModel>
                {
                    new WorkSheetSpecimenModel
                    {
                        ID = 1,
                        AccessionNo = "ACC123",
                        RackId = "RACK1",
                        RackPosition = 1,
                        IsMarkForDelete = false
                    }
                }
            };

            var mockTemplates = Mock.Of<RackWorksheetTemplates>();
            _workSheetServiceMock.Setup(x => x.WorkSheetTemplatesInfo())
                .Returns(Task.FromResult(mockTemplates));

            // Act
            var result = await _workSheetService.UpdateWorksheet(model);

            // Assert
            Assert.That(result, Is.True);
            _workSheetServiceMock.Verify(x => x.WorkSheetTemplatesInfo(), Times.Once);
        }

        [Test]
        public async Task UpdateWorksheet_WithNullModel_ReturnsFalse()
        {
            // Arrange
            WorkSheetAddUpdateModel model = null;

            // Act
            var result = await _workSheetService.UpdateWorksheet(model);

            // Assert
            Assert.That(result, Is.False);
            _workSheetServiceMock.Verify(x => x.WorkSheetTemplatesInfo(), Times.Never);
        }

        [Test]
        public async Task UpdateWorksheet_WithEmptySpecimenList_ReturnsTrue()
        {
            // Arrange
            var model = new WorkSheetAddUpdateModel
            {
                Id = 1,
                RackWorkSheetTemplateId = 1,
                WorkSheetSpecimen = new List<WorkSheetSpecimenModel>()
            };

            var mockTemplates = Mock.Of<RackWorksheetTemplates>();
            _workSheetServiceMock.Setup(x => x.WorkSheetTemplatesInfo())
                .Returns(Task.FromResult(mockTemplates));

            // Act
            var result = await _workSheetService.UpdateWorksheet(model);

            // Assert
            Assert.That(result, Is.True);
            _workSheetServiceMock.Verify(x => x.WorkSheetTemplatesInfo(), Times.Once);
        }

        [Test]
        public async Task UpdateWorksheet_WithSpecimensMarkedForDeletion_ReturnsTrue()
        {
            // Arrange
            var model = new WorkSheetAddUpdateModel
            {
                Id = 1,
                RackWorkSheetTemplateId = 1,
                WorkSheetSpecimen = new List<WorkSheetSpecimenModel>
                {
                    new WorkSheetSpecimenModel
                    {
                        ID = 1,
                        AccessionNo = "ACC123",
                        RackId = "RACK1",
                        RackPosition = 1,
                        IsMarkForDelete = true
                    }
                }
            };

            var mockTemplates = Mock.Of<RackWorksheetTemplates>();
            _workSheetServiceMock.Setup(x => x.WorkSheetTemplatesInfo())
                .Returns(Task.FromResult(mockTemplates));

            // Act
            var result = await _workSheetService.UpdateWorksheet(model);

            // Assert
            Assert.That(result, Is.True);
            _workSheetServiceMock.Verify(x => x.WorkSheetTemplatesInfo(), Times.Once);
        }

        [Test]
        public async Task UpdateWorksheet_WithNewSpecimens_ReturnsTrue()
        {
            // Arrange
            var model = new WorkSheetAddUpdateModel
            {
                Id = 1,
                RackWorkSheetTemplateId = 1,
                WorkSheetSpecimen = new List<WorkSheetSpecimenModel>
                {
                    new WorkSheetSpecimenModel
                    {
                        ID = 0, // New specimen
                        AccessionNo = "ACC123",
                        RackId = "RACK1",
                        RackPosition = 1,
                        IsMarkForDelete = false
                    }
                }
            };

            var mockTemplates = Mock.Of<RackWorksheetTemplates>();
            _workSheetServiceMock.Setup(x => x.WorkSheetTemplatesInfo())
                .Returns(Task.FromResult(mockTemplates));

            // Act
            var result = await _workSheetService.UpdateWorksheet(model);

            // Assert
            Assert.That(result, Is.True);
            _workSheetServiceMock.Verify(x => x.WorkSheetTemplatesInfo(), Times.Once);
        }

        [Test]
        public async Task UpdateWorksheet_WhenLookupServiceThrows_ReturnsFalse()
        {
            // Arrange
            var model = new WorkSheetAddUpdateModel
            {
                Id = 122,
                RackWorkSheetTemplateId = 18,
                WorkSheetSpecimen = new List<WorkSheetSpecimenModel>()
            };

            _workSheetServiceMock.Setup(x => x.WorkSheetTemplatesInfo())
                .Throws(new Exception("Service error"));

            // Act
            var result = await _workSheetService.UpdateWorksheet(model);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

    }
}