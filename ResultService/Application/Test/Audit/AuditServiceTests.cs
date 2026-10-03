using AutoMapper;
using Bioreference.Data.Audit;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.RapidResult;
using Bioreference.ResultService.Application.Audit;
using Bioreference.ResultService.Application.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BioReference.ResultService.Test.Audit
{
    [TestFixture]
    public class AuditServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private AuditService _auditService;
        private IRapidResultService _rapidResultService;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _rapidResultService = new Mock<IRapidResultService>().Object;

            _auditService = new AuditService(_mapperMock.Object, _rapidResultService);
        }

        [Test]
        public async Task Search_WhenAuditInfosFound_ReturnsMapperResult()
        {
            // Arrange
            var request = new AuditSearchCriteria
            {
                AccessionNumber = "1206334",
                ServiceDate = Convert.ToDateTime("2024-10-30T07:34:28")
            };

            var expectedResponse = new List<AuditModel>

        {
        new AuditModel
        {
            PropertyName = "Result",
            AuditType = "Update",
            FromValue = "Negative",
            ToValue = "Positive",
            UserName = "TestUser",
            TestCode = "COVID",
            TestName = "COVID-19 PCR",
            EventDate = DateTime.Now
        }
        };

            _mapperMock
                .Setup(m => m.Map<List<AuditModel>>(It.IsAny<ReportAuditManager.AuditInfo[]>()))
                .Returns(expectedResponse);

            // Act
            var result = await _auditService.Search(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            _mapperMock.Verify(
                m => m.Map<List<AuditModel>>(It.IsAny<ReportAuditManager.AuditInfo[]>()),
                Times.Once);
        }

        [Test]
        public async Task Search_WhenNoAuditInfosFound_ReturnsEmptyList()
        {
            // Arrange
            var request = new AuditSearchCriteria
            {
                AccessionNumber = "NONEXISTENT",
                ServiceDate = DateTime.Now
            };

            var emptyResponse = new List<AuditModel>();

            _mapperMock
                .Setup(m => m.Map<List<AuditModel>>(It.IsAny<ReportAuditManager.AuditInfo[]>()))
                .Returns(emptyResponse);

            // Act
            var result = await _auditService.Search(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }


        [Test]
        public async Task Search_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            AuditSearchCriteria request = null;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _auditService.Search(request));
            Assert.That(ex.ParamName, Is.EqualTo("request"));
        }
    }
}