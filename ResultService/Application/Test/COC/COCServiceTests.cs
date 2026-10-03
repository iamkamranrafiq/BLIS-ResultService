using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.COC;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.COC;
using Bioreference.ResultService.Application.Model.Enum;
using Bioreference.ResultService.Application.Order;
using Bioreference.ResultService.Common.Common;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Tests
{
    [TestFixture]
    public class COCServiceTests
    {
        private Mock<IMapper> _mockMapper;
        private COCService _cocService;

        [SetUp]
        public void Setup()
        {
            _mockMapper = new Mock<IMapper>();
            _cocService = new COCService(_mockMapper.Object);
        }

        [Test]
        public async Task FetchCOCReport_ReturnsExpectedData()
        {
            // Arrange
            var mockResponse = new { List = new List<object>() };
            _mockMapper.Setup(m => m.Map<List<COCBatchReportModel>>(It.IsAny<object>()))
                .Returns(new List<COCBatchReportModel>());

            // Act
            var result = await _cocService.FetchCOCReport(false, 1,10);

            // Assert
            Assert.NotNull(result);
            _mockMapper.Verify(m => m.Map<List<COCBatchReportModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task FetchCOCReportItem_ReturnsExpectedData()
        {
            // Arrange
            var mockResponse = new { List = new List<object>() };
            _mockMapper.Setup(m => m.Map<List<COCBatchReportItemModel>>(It.IsAny<object>()))
                .Returns(new List<COCBatchReportItemModel>());

            // Act
            var result = await _cocService.FetchCOCReportItem(10);

            // Assert
            Assert.NotNull(result);
            _mockMapper.Verify(m => m.Map<List<COCBatchReportItemModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task CreateCOCSequence_CallsCreateNewAndReturnsResponse()
        {
            // Arrange
            var criteria = new COCSearchCriteria
            {
                Month = "12",
                Day = "12",
                Year = "2025",
                SequenceStart = "190000199",
                SequenceEnd = "190000200"
            };

            _mockMapper.Setup(m => m.Map<COCModel>(It.IsAny<object>()))
                .Returns(new COCModel());
            _mockMapper.Setup(m => m.Map<List<COCBatchAccessionModel>>(It.IsAny<object>()))
                .Returns(new List<COCBatchAccessionModel>());

            // Act
            var result = await _cocService.CreateCOCSequence(criteria);

            // Assert
            Assert.NotNull(result);
        }

        [Test]
        public async Task RemoveCOC_CallsRemoveAccessionAndReturnsStatus()
        {
            // Arrange
            string accessionNumber = "190000197";
            int batchId = 14;
            var mockStatus = new CocBatchAccessionEditStatusModel();

            _mockMapper.Setup(m => m.Map<CocBatchAccessionEditStatusModel>(It.IsAny<object>()))
                .Returns(mockStatus);

            // Act
            var result = await _cocService.RemoveCOC(accessionNumber, batchId);

            // Assert
            Assert.AreSame(mockStatus, result);
            _mockMapper.Verify(m => m.Map<CocBatchAccessionEditStatusModel>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task AddCOCAccession_CallsAddAccessionAndReturnsStatus()
        {
            // Arrange
            string accessionNumber = "190000197";
            int batchId = 14;
            var mockStatus = new CocBatchAccessionEditStatusModel();

            _mockMapper.Setup(m => m.Map<CocBatchAccessionEditStatusModel>(It.IsAny<object>()))
                .Returns(mockStatus);

            // Act
            var result = await _cocService.AddCOCAccession(accessionNumber, batchId);

            // Assert
            Assert.AreSame(mockStatus, result);
            _mockMapper.Verify(m => m.Map<CocBatchAccessionEditStatusModel>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task ModifyCOCSequence_CallsModifyBatchSetupAndReturnsResponse()
        {
            // Arrange
            var criteria = new COCSearchCriteria
            {
                BatchId = 10,
                Month = "12",
                Day = "12",
                Year = "2025",
                SequenceStart = "190000199",
                SequenceEnd = "190000200"
            };

            _mockMapper.Setup(m => m.Map<COCModel>(It.IsAny<object>()))
                .Returns(new COCModel());
            _mockMapper.Setup(m => m.Map<List<COCBatchAccessionModel>>(It.IsAny<object>()))
                .Returns(new List<COCBatchAccessionModel>());

            // Act
            var result = await _cocService.ModifyCOCSequence(criteria);

            // Assert
            Assert.NotNull(result);
        }

        [Test]
        public async Task ReleaseBatch_WithValidReports_ReturnsTrue()
        {
            // Arrange
            var reportIds = new List<int> { 169927 };

            // Act
            var result = await _cocService.ReleaseBatch(reportIds);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task ClosedReOpenBatch_WithValidInputs_ReturnsStatus()
        {
            // Arrange
            int batchId = 14;
            bool isClosed = false;
            var mockStatus = new CocBatchAccessionEditStatusModel();

            _mockMapper.Setup(m => m.Map<CocBatchAccessionEditStatusModel>(It.IsAny<object>()))
                .Returns(mockStatus);

            // Act
            var result = await _cocService.ClosedReOpenBatch(isClosed, batchId);

            // Assert
            Assert.AreSame(mockStatus, result);
            _mockMapper.Verify(m => m.Map<CocBatchAccessionEditStatusModel>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task ClosedReOpenBatch_WithInvalidStatus_ReturnsFailureStatus()
        {
            // Arrange
            int batchId = 1;
            bool isClosed = true;

            var mockReportItems = new List<COCBatchReportItemModel>
            {
                new COCBatchReportItemModel
                {
                    ResultStatus = resultStatusTypeModel.Preliminary,
                    TransmitStatus = transmitStatusTypeModel.PendingRelease
                }
            };

            // Set up FetchCOCReportItem to return items with invalid status
            _mockMapper.Setup(m => m.Map<List<COCBatchReportItemModel>>(It.IsAny<object>()))
                .Returns(mockReportItems);

            // Act
            var result = await _cocService.ClosedReOpenBatch(isClosed, batchId);

            // Assert
            Assert.AreEqual(CocBatchCreateStatusModel.Failure, result.Status);
            StringAssert.Contains("Not permitted to close Batch", result.Message);
        }
    }
}