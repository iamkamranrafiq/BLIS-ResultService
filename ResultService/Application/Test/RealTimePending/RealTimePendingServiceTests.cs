using AutoMapper;
using Bioreference.Common.TestMaster;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.RealTimePending;
using Bioreference.ResultService.Application.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.RealTimePending.Tests
{
    [TestFixture]
    public class RealTimePendingServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private RealTimePendingService _realTimePendingService;

        [SetUp]
        public void SetUp()
        {
            _mapperMock = new Mock<IMapper>();
            _realTimePendingService = new RealTimePendingService(_mapperMock.Object);
        }

        [Test]
        public void RealTimePendingFetch_WhenSearchCriteriaIsNull_ThrowsArgumentNullException()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _realTimePendingService.RealTimePendingFetch(null));
        }

        [Test]
        public async Task RealTimePendingFetch_WhenValidSearchCriteria_ReturnsPendings()
        {
           
            var searchCriteria = new RTPSearchCriteria
            {
                PendingListId = 333,
                RdoEnableDate = true,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(-1),
                LastHours = 0,
                ExcludeHours = 0
            };  
           
            var result = await _realTimePendingService.RealTimePendingFetch(searchCriteria);     
            Assert.That(result, Is.Not.Null);
            
        }

        [Test]
        public void DetermineStartEnd_WhenRdoEnableDateIsTrue_SetsStartEndDatesCorrectly()
        {
           
            var searchCriteria = new RTPSearchCriteria
            {
                RdoEnableDate = true,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1)
            };           
            _realTimePendingService.DetermineStartEnd(searchCriteria);         
            Assert.That(searchCriteria.StartDate.Date, Is.EqualTo(DateTime.Now.Date));
            Assert.That(searchCriteria.EndDate.Date, Is.EqualTo(DateTime.Now.Date));
        }

        [Test]
        public void DetermineStartEnd_WhenRdoEnableDateIsFalse_SetsStartEndDatesRelativeToNow()
        {
            
            var searchCriteria = new RTPSearchCriteria
            {
                RdoEnableDate = false,
                LastHours = 5,
                ExcludeHours = 2
            };
            var expectedStart = DateTime.Now.AddHours(-5);
            var expectedEnd = DateTime.Now.AddHours(-2);           
            _realTimePendingService.DetermineStartEnd(searchCriteria);           
            Assert.That(searchCriteria.StartDate, Is.EqualTo(expectedStart).Within(TimeSpan.FromSeconds(1)));
            Assert.That(searchCriteria.EndDate, Is.EqualTo(expectedEnd).Within(TimeSpan.FromSeconds(1)));
        }
    }
}