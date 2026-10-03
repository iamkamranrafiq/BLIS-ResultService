using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Report;
using Bioreference.ResultService.Abstractions.Application.Report;
using Moq;
using System.Security.Principal;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Enum;
using System.Collections;
using Bioreference.ResultService.Abstractions.Application.Brad;
using Bioreference.ResultService.Abstractions.Application.Order;
using Bioreference.Contracts.Result;
using Bioreference.Messaging.Abstractions;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Bioreference.ResultService.Abstractions.Application.Common;

namespace BioReference.ResultService.Test.Report
{
    [TestFixture]
    public class ReportServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<ICommonResultService> _resultCommonMock;
        private IReportService _reportService;
        private Mock<IOrderService> _orderServiceMock;
        private Mock<ILogger<ReportService>> _loggerMock;
        private Mock<IMessageProducer<Incident>> _producerMock;
        private Mock<ResultEventProcessorService> _eventProcessorMock;
        private Mock<IProducerProvider> _producerProviderMock;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _resultCommonMock = new Mock<ICommonResultService>();
            _orderServiceMock = new Mock<IOrderService>();
            _loggerMock = new Mock<ILogger<ReportService>>();
            _producerMock = new Mock<IMessageProducer<Incident>>();
            _eventProcessorMock = new Mock<ResultEventProcessorService>();

            _producerProviderMock = new Mock<IProducerProvider>();
            _producerProviderMock
                .Setup(p => p.GetMessageProducer<Incident>(It.IsAny<string>()))
                .Returns(_producerMock.Object);

            _reportService = new ReportService(_mapperMock.Object, _orderServiceMock.Object, _loggerMock.Object, _resultCommonMock.Object,null,null);
            SetTestPrincipal("TestAPI", new[] { "TestRole" });
        }

        [Test]
        public async Task ReportFetch_ValidAccessionNumber_ReturnsMappedReportDTO()
        {

            string accessionNbr = "1218038";
            DateTime dateServiced = new DateTime(2024, 12, 24, 10, 42, 06);

            var expectedDto = new ReportModel();

            _mapperMock.Setup(m => m.Map<ReportModel>(It.IsAny<object>())).Returns(expectedDto);


            var result = await _reportService.ReportFetch(accessionNbr, dateServiced);
            Assert.That(result, Is.EqualTo(expectedDto));
            Assert.That(result.AccessionNumber, Is.EqualTo(expectedDto.AccessionNumber));
            _mapperMock.Verify(m => m.Map<ReportModel>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task ReportFetchByReportId_ValidReportId_ReturnsMappedReportDTO()
        {

            int reportId = 168448;
            var expectedDto = new ReportModel();

            _mapperMock.Setup(m => m.Map<ReportModel>(It.IsAny<object>())).Returns(expectedDto);

            var result = await _reportService.ReportFetchByReportId(reportId, false, null);
            Assert.That(result, Is.EqualTo(expectedDto));
           
        }

        [Test]
        public async Task ReportFetch_WhenMapperReturnsNull_ReturnsNull()
        {

            string accessionNbr = "1206259";
            DateTime dateServiced = new DateTime(2024, 10, 30, 01, 46, 59);
            var expectedDto = new ReportModel();

            _mapperMock.Setup(m => m.Map<ReportModel>(It.IsAny<object>())).Returns((ReportModel)null);

            var result = await _reportService.ReportFetch(accessionNbr, dateServiced);
            Assert.That(result, Is.EqualTo(expectedDto));
        }

        [Test]
        public async Task ReportFetchByReportId_WhenMapperReturnsNull_ReturnsNull()
        {

            int reportId = 165759;

            _mapperMock.Setup(m => m.Map<ReportModel>(It.IsAny<object>())).Returns((ReportModel)null);

            var result = await _reportService.ReportFetchByReportId(reportId);
            Assert.That(result, Is.Null);
        }
        private void SetTestPrincipal(string userName, string[] roles)
        {
            Thread.CurrentPrincipal = new GenericPrincipal(
                new GenericIdentity(userName),
                roles
            );
        }

        [Test]
        public async Task FetchReports_WhenReportsExist_ReturnsReportInfoDTOs()
        {
            // Arrange
            string analyteCode = "0286";
            transmitStatusType status = transmitStatusType.PendingRelease;
            bool searchPanels = true;

            var expectedResponse = new List<ReportInfoSearchCriteria>
            {
                new ReportInfoSearchCriteria
                {
                    OrderId = 1,
                    ReportId = 100,
                    OrderDate = DateTime.Now,
                    Priority = OrderPriorityModel.Routine,
                    PendingCount = 2,
                    ResultStatus = resultStatusTypeModel.Pending,
                    FlagCount = 1,
                    PatientName = "John Doe",
                    AccountNumber = "ACC001",
                    HasPreviousResultValue = true,
                    AccessionNbr = "ACC123",
                    EUID = 12345,
                    TransmitStatus = transmitStatusTypeModel.PendingRelease,
                    IsClinicalTrial = false,
                    DivisionList = "DIV1,DIV2"
                }
            };

            _mapperMock
                .Setup(m => m.Map<List<ReportInfoSearchCriteria>>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _reportService.FetchReports(analyteCode, status, searchPanels);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            var reportInfo = result[0];
            Assert.Multiple(() =>
            {
                Assert.That(reportInfo.AccessionNbr, Is.EqualTo("ACC123"));
                Assert.That(reportInfo.ResultStatus, Is.EqualTo(resultStatusTypeModel.Pending));
                Assert.That(reportInfo.TransmitStatus, Is.EqualTo(transmitStatusTypeModel.PendingRelease));
                Assert.That(reportInfo.OrderId, Is.EqualTo(1));
                Assert.That(reportInfo.ReportId, Is.EqualTo(100));
                Assert.That(reportInfo.PatientName, Is.EqualTo("John Doe"));
            });
        }

        [Test]
        public async Task FetchReports_WhenNoReportsExist_ReturnsEmptyList()
        {
            // Arrange
            string analyteCode = "NONEXISTENT";
            transmitStatusType status = transmitStatusType.None;
            bool searchPanels = false;

            var emptyResponse = new List<ReportInfoSearchCriteria>();

            _mapperMock
                .Setup(m => m.Map<List<ReportInfoSearchCriteria>>(It.IsAny<object>()))
                .Returns(emptyResponse);

            // Act
            var result = await _reportService.FetchReports(analyteCode, status, searchPanels);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FetchReports_WithNullAnalyteCode_ThrowsArgumentNullException()
        {
            // Arrange
            string analyteCode = null;
            transmitStatusType status = transmitStatusType.PendingRelease;
            bool searchPanels = true;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _reportService.FetchReports(analyteCode, status, searchPanels));
            Assert.That(ex.ParamName, Is.EqualTo("analyteCode"));
        }

        [Test]
        public async Task FetchCriteria_WhenCriteriaExists_ReturnsCriteriaDefinitionDTO()
        {
            // Arrange
            string className = "Bioreference.LIS.ReportAnalyte.Code";
            var expectedResponse = new CriteriaDefinitionModel
            {
                Id = 1,
                DisplayName = "Test Criteria",
                ClassName = className,
                PropertyName = "TestProperty",
                CriteriaCode = "TEST001",
                FullClassName = "Namespace.TestCriteria",
                UnitList = new[] { "mg/dL", "mmol/L" },
                ValueList = new[] { "Positive", "Negative" },
              //  ParameterList = Array.Empty<CriteriaParameterModel>(),
                AllowRange = true,
                EvaluateAsBoolean = false,
                CriteriaValues = new List<CriteriaValueModel>()
            };

            _mapperMock
                .Setup(m => m.Map<CriteriaDefinitionModel>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _reportService.FetchCriteria(className);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.ClassName, Is.EqualTo(className));
                Assert.That(result.DisplayName, Is.EqualTo("Test Criteria"));
                Assert.That(result.UnitList, Has.Length.EqualTo(2));
                Assert.That(result.ValueList, Has.Length.EqualTo(2));
            });
        }

        [Test]
        public void FetchCriteria_WithNullClassName_ThrowsArgumentNullException()
        {
            // Arrange
            string className = null;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _reportService.FetchCriteria(className));
            Assert.That(ex.ParamName, Is.EqualTo("className"));
        }

        [Test]
        public void FetchCriteria_WithEmptyClassName_ThrowsArgumentNullException()
        {
            // Arrange
            string className = string.Empty;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _reportService.FetchCriteria(className));
            Assert.That(ex.ParamName, Is.EqualTo("className"));
        }
        [Test]
        public async Task CorrectedReasonMapping_ShouldReturnResult()
        {
            var result = await _reportService.CorrectedReasonMapping();
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task DepartmentResponsible_ShouldReturnResult()
        {
            var result = await _reportService.DepartmentResponsible();
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<List<DictionaryEntry>>(result);
        }

        [Test]
        public async Task ReportingDepartment_ShouldReturnResult()
        {
            var result = await _reportService.ReportingDepartment();
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<List<DictionaryEntry>>(result);
        }


        [Test]
        public async Task GetResponsibleLabsDropdown_ReturnsExpectedDictionary()
        {
            // Arrange
            var expectedLabs = new Dictionary<string, string>
            {
                { "LAB1", "Laboratory 1" },
                { "LAB2", "Laboratory 2" }
            };

            // Act
            var result = await _reportService.GetResponsibleLabsDropdown();

            // Assert
            Assert.NotNull(result);
            Assert.IsInstanceOf<Dictionary<string, string>>(result);
            // Add more specific assertions based on expected data
        }

        [Test]
        public async Task GetSignificancesDropdown_ReturnsExpectedDictionary()
        {
            // Arrange
            var expectedSignificances = new Dictionary<string, string>
            {
                { "SIG1", "Significance 1" },
                { "SIG2", "Significance 2" }
            };

            // Act
            var result = await _reportService.GetSignificancesDropdown();

            // Assert
            Assert.NotNull(result);
            Assert.IsInstanceOf<Dictionary<string, string>>(result);
            // Add more specific assertions based on expected data
        }

        [Test]
        public async Task SaveCorrectedReasons_WithValidData_ReturnsTrue()
        {
            // Arrange
            var correctedReason = new CorrectedReasonsModel
            {
                TestCode = "6315",
                ReasonTypeId = 18,
                PrimaryReasonId = 30,
                SecondaryReasonId = 18,
                DeptResponsible = null,
                ControllableId = 2,
                SignificanceId = 2,
                Description = "bnn",
                ReportId = 161828,
                ReportAnalyteId = 2303351,
                AccessionNbr = "1204099",
                IsAgencyReportable = false,
                CausedBy = "11",
                CriticalResult = true,
                ReportingDept = null,
                ResponsibleLab = "Hackensack - NJ2",
                OrgPerformingFacility = "b"
            };

            List<CorrectedReasonsModel> correctedReasonsList= new List<CorrectedReasonsModel>();
            correctedReasonsList.Add(correctedReason);
            // Act
            var result = await _reportService.SaveCorrectedReasons(correctedReasonsList);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task SaveCorrectedReasons_WithInvalidData_ReturnsFalse()
        {
            // Arrange
            var correctedReason = new CorrectedReasonsModel
            {
                // Set invalid or missing required properties
                ReportId = 0
            };
            List<CorrectedReasonsModel> correctedReasonsList = new List<CorrectedReasonsModel>();
            correctedReasonsList.Add(correctedReason);

            // Act
            var result = await _reportService.SaveCorrectedReasons(correctedReasonsList);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task SaveCorrectedReasons_WithNullData_ThrowsArgumentNullException()
        {
            // Arrange
            List<CorrectedReasonsModel> correctedReason = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _reportService.SaveCorrectedReasons(correctedReason));
        }

        [Test]
        public async Task GetResponsibleLabsDropdown_ReturnsNonEmptyDictionary()
        {
            // Act
            var result = await _reportService.GetResponsibleLabsDropdown();

            // Assert
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
        }

        [Test]
        public async Task GetSignificancesDropdown_ReturnsNonEmptyDictionary()
        {
            // Act
            var result = await _reportService.GetSignificancesDropdown();

            // Assert
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
        }

    }
}
