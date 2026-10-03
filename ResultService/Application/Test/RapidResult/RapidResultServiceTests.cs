using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.Common;
using Bioreference.ResultService.Application.RapidResult;
using Moq;

namespace Bioreference.ResultService.Application.Tests
{
    [TestFixture]
    public class RapidResultServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<ICommonResultService> _mapperCommonResult;
        private Mock<RapidEventProcessorService> _mapperRapidEventProcessor;
        private RapidResultService _rapidResultService;

        [SetUp]
        public void SetUp()
        {
            _mapperMock = new Mock<IMapper>();
            _rapidResultService = new RapidResultService(_mapperMock.Object, _mapperRapidEventProcessor.Object, _mapperCommonResult.Object);
        }

        [Test]
        public async Task RapidResults_WhenCalled_ReturnsRapidResults()
        {
            int templateId = 417;
            bool isCompleted = true;
            var result = await _rapidResultService.RapidResults(templateId, isCompleted, 1, 10);
            Assert.That(result, Is.Not.Null);
        }



        [Test]
        public async Task OutstandingRapidResults_WhenCalled_ReturnsStringData()
        {
            int templateId = 417;
            int pastDays = 7;

            var result = await _rapidResultService.OutstandingRapidResults(templateId, pastDays);

            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task RapidResult_Delete_RapidResultId()
        {
            int rapidResultId = 1114894;
            var result = await _rapidResultService.DeleteRapidResult(rapidResultId);
            Assert.IsTrue(result);
        }
    }
}
