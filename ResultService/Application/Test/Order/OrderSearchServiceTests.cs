using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.Order;
using Moq;
using Bioreference.ResultService.Application.Order;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Enum;

namespace BioReference.ResultService.Test.Order
{
    [TestFixture]
    public class OrderSearchServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private IOrderSearchService _orderSearchService;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _orderSearchService = new OrderSearchService(_mapperMock.Object);
        }

        [Test]
        public async Task Search_WithValidRequest_ReturnsExpectedResults()
        {
            // Arrange
            var request = new OrderSearchCriteria
            {
                AccessionNumber = "1206334",
                AccountNumber = "ACCT456",
                PatientName = "John Doe",
                StartDate = DateTime.Now.AddDays(-7),
                EndDate = DateTime.Now,
                TransmitStatus = transmitStatusTypeModel.PendingRelease,
                UserDivisionCodes = new List<string> { "NJ1" },
                EUID = 123
            };

            var expectedResponse = new List<OrderSearchModel>
            {
                new OrderSearchModel
                {
                    OrderId = 1,
                    CollectionDate = DateTime.Now,
                    ServiceDate = DateTime.Now
                }
            };

            _mapperMock.Setup(m => m.Map<List<OrderSearchModel>>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _orderSearchService.Search(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            _mapperMock.Verify(m => m.Map<List<OrderSearchModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task Search_WithZeroTransmitStatus_SetsNotSet()
        {
            // Arrange
            var request = new OrderSearchCriteria
            {
                TransmitStatus = 0,
                UserDivisionCodes = new List<string> { "NJ1" }
            };

            var expectedResponse = new List<OrderSearchModel>();
            _mapperMock.Setup(m => m.Map<List<OrderSearchModel>>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _orderSearchService.Search(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            _mapperMock.Verify(m => m.Map<List<OrderSearchModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task Search_WithZeroEUID_SetsNegativeOne()
        {
            // Arrange
            var request = new OrderSearchCriteria
            {
                EUID = 0,
                UserDivisionCodes = new List<string> { "NJ1" }
            };

            var expectedResponse = new List<OrderSearchModel>();
            _mapperMock.Setup(m => m.Map<List<OrderSearchModel>>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _orderSearchService.Search(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            _mapperMock.Verify(m => m.Map<List<OrderSearchModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task Search_WithNullRequest_ThrowsNullReferenceException()
        {
            // Arrange
            OrderSearchCriteria request = null;

            // Act & Assert
            var ex = Assert.ThrowsAsync<NullReferenceException>(async () =>
                await _orderSearchService.Search(request));
            Assert.That(ex.Message, Is.EqualTo("Object reference not set to an instance of an object."));
        }

    }
}