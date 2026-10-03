using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Tools;
using Bioreference.RuleEngine;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Tools.Tests
{
    [TestFixture]
    public class ToolsServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private ToolsService _toolsService;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _toolsService = new ToolsService(_mapperMock.Object);
        }

        [Test]
        public async Task GetRules_WhenRuleSetIdIsZero_ReturnsMappedRules()
        {
            // Arrange
            int ruleSetId = 0;
            var expectedRules = new List<RuleDTO>
            {
                new RuleDTO
                {
                    Priority = 1,
                    Name = "High Priority Rule"
                },
                new RuleDTO
                {
                    Priority = 2,
                    Name = "Medium Priority Rule"
                }
            };

            _mapperMock
                .Setup(m => m.Map<List<RuleDTO>>(It.IsAny<object>()))
                .Returns(expectedRules);

            // Act
            var result = await _toolsService.GetRules(ruleSetId, 1, 2);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Rules.Count, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Rules[0].Priority, Is.EqualTo(1));
                Assert.That(result.Rules[0].Name, Is.EqualTo("High Priority Rule"));
                Assert.That(result.Rules[1].Priority, Is.EqualTo(2));
                Assert.That(result.Rules[1].Name, Is.EqualTo("Medium Priority Rule"));
            });
            Assert.That(result.LastUpdated, Is.Not.EqualTo(default(DateTime)));
            _mapperMock.Verify(m => m.Map<List<RuleDTO>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task GetRules_WhenRuleSetIdIsNotZero_ReturnsMappedRules()
        {
            // Arrange
            int ruleSetId = 1;
            var expectedRules = new List<RuleDTO>
            {
                new RuleDTO
                {
                    Priority = 1,
                    Name = "Custom Rule"
                }
            };

            _mapperMock
                .Setup(m => m.Map<List<RuleDTO>>(It.IsAny<object>()))
                .Returns(expectedRules);

            // Act
            var result = await _toolsService.GetRules(ruleSetId, 1, 1000000);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Rules.Count, Is.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result.Rules[0].Priority, Is.EqualTo(1));
                Assert.That(result.Rules[0].Name, Is.EqualTo("Custom Rule"));
            });
            Assert.That(result.LastUpdated, Is.Not.EqualTo(default(DateTime)));
            _mapperMock.Verify(m => m.Map<List<RuleDTO>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task TNPSearch_WhenResultsExist_ReturnsMappedResults()
        {
            // Arrange
            var request = new TNPSearchCriteria
            {
                PendlingListId = 333,
                StartDate = DateTime.Now.AddDays(-7),
                EndDate = DateTime.Now,
                Account = "",
                TestCodes = ""
            };

            var expectedResponse = new List<TNPSearchModel>
            {
                new TNPSearchModel
                {
                    AccessionNbr = "ACC123",
                    Dos = DateTime.Now.Date,
                    PanelCode = "RESPANEL",
                    TestCode = "COVID",
                    ResultStatus = "Pending"
                },
                new TNPSearchModel
                {
                    AccessionNbr = "ACC124",
                    Dos = DateTime.Now.Date,
                    PanelCode = "RESPANEL",
                    TestCode = "FLU",
                    ResultStatus = "Final"
                }
            };

            _mapperMock
                .Setup(m => m.Map<List<TNPSearchModel>>(It.IsAny<object>()))
                .Returns(expectedResponse);

            // Act
            var result = await _toolsService.TNPSearch(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].AccessionNbr, Is.EqualTo("ACC123"));
                Assert.That(result[0].TestCode, Is.EqualTo("COVID"));
                Assert.That(result[0].ResultStatus, Is.EqualTo("Pending"));
                Assert.That(result[1].AccessionNbr, Is.EqualTo("ACC124"));
                Assert.That(result[1].TestCode, Is.EqualTo("FLU"));
                Assert.That(result[1].ResultStatus, Is.EqualTo("Final"));
            });
            _mapperMock.Verify(m => m.Map<List<TNPSearchModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task TNPSearch_WhenNoResultsExist_ReturnsEmptyList()
        {
            // Arrange
            var request = new TNPSearchCriteria
            {
                PendlingListId = 333,
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now,
                Account = "NONEXISTENT",
                TestCodes = "INVALID"
            };

            _mapperMock
                .Setup(m => m.Map<List<TNPSearchModel>>(It.IsAny<object>()))
                .Returns(new List<TNPSearchModel>());

            // Act
            var result = await _toolsService.TNPSearch(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _mapperMock.Verify(m => m.Map<List<TNPSearchModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void TNPSearch_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            TNPSearchCriteria request = null;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _toolsService.TNPSearch(request));
            Assert.That(ex.ParamName, Is.EqualTo("request"));
        }

        [Test]
        public void TNPSearch_WithInvalidDateRange_ThrowsArgumentException()
        {
            // Arrange
            var request = new TNPSearchCriteria
            {
                PendlingListId = 333,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(-7),
                Account = "TEST123",
                TestCodes = "COVID"
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _toolsService.TNPSearch(request));
            Assert.That(ex.Message, Contains.Substring("Invalid date range"));
        }



        #region ReleaseToReporting Tests

        [Test]
        public async Task ReleaseToReporting_WithValidModel_ReturnsTrue()
        {
            // Arrange
            var models = new List<ReleaseToReportingModel>
            {
                new ReleaseToReportingModel
                {
                    OrderID = 1,
                    IsReportHold = false
                }
            };

            // Act
            var result = await _toolsService.ReleaseToReporting(models);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ReleaseToReporting_WithNullModel_ReturnsFalse()
        {
            // Arrange
            List<ReleaseToReportingModel> models = null;

            // Act
            var result = await _toolsService.ReleaseToReporting(models);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ReleaseToReporting_WithEmptyList_ReturnsFalse()
        {
            // Arrange
            var models = new List<ReleaseToReportingModel>();

            // Act
            var result = await _toolsService.ReleaseToReporting(models);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ReleaseToReporting_WithMultipleOrders_ReturnsTrue()
        {
            // Arrange
            var models = new List<ReleaseToReportingModel>
            {
                new ReleaseToReportingModel { OrderID = 1, IsReportHold = false },
                new ReleaseToReportingModel { OrderID = 2, IsReportHold = true }
            };

            // Act
            var result = await _toolsService.ReleaseToReporting(models);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ReleaseToReporting_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var models = new List<ReleaseToReportingModel>
            {
                new ReleaseToReportingModel { OrderID = -1, IsReportHold = false } // Invalid ID to trigger exception
            };

            // Act
            var result = await _toolsService.ReleaseToReporting(models);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region ReleaseBulkTNP Tests

        [Test]
        public async Task ReleaseBulkTNP_WithValidModel_ReturnsTrue()
        {
            // Arrange
            var models = new List<BulkTNPReleaseModel>
            {
                new BulkTNPReleaseModel
                {
                    AccessionNumber = "ACC123",
                    TestCode = "TEST1",
                    PanelCode = "PANEL1",
                    CommentType = "TYPE1",
                    Comment = "Test Comment"
                }
            };

            // Act
            var result = await _toolsService.ReleaseBulkTNP(models);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ReleaseBulkTNP_WithNullModel_ReturnsFalse()
        {
            // Arrange
            List<BulkTNPReleaseModel> models = null;

            // Act
            var result = await _toolsService.ReleaseBulkTNP(models);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ReleaseBulkTNP_WithEmptyList_ReturnsFalse()
        {
            // Arrange
            var models = new List<BulkTNPReleaseModel>();

            // Act
            var result = await _toolsService.ReleaseBulkTNP(models);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ReleaseBulkTNP_WithMultipleRecords_ReturnsTrue()
        {
            // Arrange
            var models = new List<BulkTNPReleaseModel>
            {
                new BulkTNPReleaseModel
                {
                    AccessionNumber = "ACC123",
                    TestCode = "TEST1",
                    PanelCode = "PANEL1",
                    CommentType = "TYPE1",
                    Comment = "Test Comment 1"
                },
                new BulkTNPReleaseModel
                {
                    AccessionNumber = "ACC124",
                    TestCode = "TEST2",
                    PanelCode = "PANEL2",
                    CommentType = "TYPE2",
                    Comment = "Test Comment 2"
                }
            };

            // Act
            var result = await _toolsService.ReleaseBulkTNP(models);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ReleaseBulkTNP_WithInvalidAccessionNumber_ReturnsFalse()
        {
            // Arrange
            var models = new List<BulkTNPReleaseModel>
            {
                new BulkTNPReleaseModel
                {
                    AccessionNumber = "", // Invalid accession number
                    TestCode = "TEST1",
                    PanelCode = "PANEL1",
                    CommentType = "TYPE1",
                    Comment = "Test Comment"
                }
            };

            // Act
            var result = await _toolsService.ReleaseBulkTNP(models);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ReleaseBulkTNP_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var models = new List<BulkTNPReleaseModel>
            {
                new BulkTNPReleaseModel
                {
                    AccessionNumber = null, // Should trigger exception
                    TestCode = "TEST1",
                    PanelCode = "PANEL1",
                    CommentType = "TYPE1",
                    Comment = "Test Comment"
                }
            };

            // Act
            var result = await _toolsService.ReleaseBulkTNP(models);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

    }
}