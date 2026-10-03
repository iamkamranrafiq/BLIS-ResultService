using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.Order;
using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bioreference.ResultService.Application.Order;
using Bioreference.ResultService.Application.Model;

namespace BioReference.ResultService.Test.Order
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private IOrderService _orderService;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _orderService = new OrderService(_mapperMock.Object);
        }

        [Test]
        public async Task FetchOrder_ValidOrderId_ReturnsCorrectOrderResponse()
        {
            // Arrange
            int orderId = 360047;
            var expectedOrderResponse = new OrderModel();
            var expectedSpecimens = new List<SpecimenModel>();
            var expectedTests = new List<TestModel>();
            var expectedAOEAnswers = new List<AOEAnswerModel>();

            // Setup mapper mocks with It.IsAny.
            _mapperMock.Setup(m => m.Map<OrderModel>(It.IsAny<object>()))
                .Returns(expectedOrderResponse);
            _mapperMock.Setup(m => m.Map<List<SpecimenModel>>(It.IsAny<object>()))
                .Returns(expectedSpecimens);
            _mapperMock.Setup(m => m.Map<List<TestModel>>(It.IsAny<object>()))
                .Returns(expectedTests);
            _mapperMock.Setup(m => m.Map<List<AOEAnswerModel>>(It.IsAny<object>()))
                .Returns(expectedAOEAnswers);

            // Act
            var result = await _orderService.FetchOrder(orderId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedOrderResponse));
            Assert.That(result.Specimens, Is.EqualTo(expectedSpecimens));
            Assert.That(result.Tests, Is.EqualTo(expectedTests));
            Assert.That(result.AOEAnswers, Is.EqualTo(expectedAOEAnswers));

            // Verify mapper was called
            _mapperMock.Verify(m => m.Map<OrderModel>(It.IsAny<object>()), Times.Once);
            _mapperMock.Verify(m => m.Map<List<SpecimenModel>>(It.IsAny<object>()), Times.Once);
            _mapperMock.Verify(m => m.Map<List<TestModel>>(It.IsAny<object>()), Times.Once);
            _mapperMock.Verify(m => m.Map<List<AOEAnswerModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task FetchOrder_WhenMapperReturnsNull_ReturnsNull()
        {
            // Arrange
            int orderId = 123;
            _mapperMock.Setup(m => m.Map<OrderModel>(It.IsAny<object>()))
                .Returns((OrderModel)null);

            // Act
            var result = await _orderService.FetchOrder(orderId);

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}