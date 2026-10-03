using AutoMapper;
using Bioreference.Common.TestMaster;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Brad;
using Bioreference.ResultService.Abstractions.Application.Lookup;
using Bioreference.ResultService.Abstractions.Application.Report;
using Bioreference.ResultService.Abstractions.Application.WorkSheet;
using Bioreference.ResultService.Application.Brad;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Lookup;
using Bioreference.ResultService.Application.Report;
using Moq;
using System.Security.Principal;
using Bioreference.ResultService.Application.Model.Enum;

namespace BioReference.ResultService.Test.Lookup
{
    [TestFixture]
    public class LookupServiceTests
    {
        private Mock<IMapper> _mapperMock;
        private Mock<IBradService> _bradServiceMock;
        private ILookupService _lookupService;
        private IBradService _bradService;
        private IReportService _reportService;
        private IWorkSheetService _workSheetService;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _bradServiceMock = new Mock<IBradService>();
            _lookupService = new LookupService(_mapperMock.Object, _bradServiceMock.Object);
            _bradService = new BradService(_mapperMock.Object);    
            _reportService = new Mock<IReportService>().Object;
            _workSheetService = new Mock<IWorkSheetService>().Object;
            SetTestPrincipal("TestAPI", new[] { "TestRole" });
        }

        [Test]
        public async Task GetTestCodeGroups_ValidRequest_ReturnsMappedDTO()
        {
            var expectedDto = new TestCodeGroupModel();

            _mapperMock.Setup(m => m.Map<TestCodeGroupModel>(It.IsAny<object>())).Returns(expectedDto);

            var result = await _lookupService.GetTestCodeGroups(false,true);

         //   Assert.That(result, Is.EqualTo(expectedDto));
            _mapperMock.Verify(m => m.Map<TestCodeGroupsModel>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task GetTestCodeGroups_WhenMapperReturnsNull_ReturnsNull()
        {
         
            _mapperMock.Setup(m => m.Map<TestCodeGroupsModel>(It.IsAny<object>())).Returns((TestCodeGroupsModel)null);
       
            var result = await _lookupService.GetTestCodeGroups(false, true);
       
            Assert.That(result, Is.Null);
        }
    

        [Test]
        public async Task GetAuditUsers_NoUsers_ReturnsEmptyList()
        {          
            int reportId = 165776;
            var result = await _reportService.GetAuditUsers(reportId);            
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetComments_ValidRequest_ReturnsMappedList()
        {
       
            var expectedCommentsDto = new List<CommentTypeModel> ();

            _mapperMock.Setup(m => m.Map<List<CommentTypeModel>>(It.IsAny<object>())).Returns(expectedCommentsDto);
            var result = await _lookupService.GetComments(CommentTypeModel.Canned);         
            _mapperMock.Verify(m => m.Map<List<CommentModel>>(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task GetComments_WhenMapperReturnsNull_ReturnsNull()
        {           
            _mapperMock.Setup(m => m.Map<List<CommentTypeModel>>(It.IsAny<object>())).Returns((List<CommentTypeModel>)null);
            var result = await _lookupService.GetComments(CommentTypeModel.Canned);          
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetWorkSheetsTemplates_WhenTemplatesExist_ReturnsDictionary()
        {
            // Arrange
            var expectedTemplates = new Dictionary<int, string>
            {
                { 1, "Template 1" },
                { 2, "Template 2" },
                { 3, "Template 3" }
            };

            // Act
            var result = await _workSheetService.GetWorkSheetsTemplates();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<Dictionary<int, string>>());
        }

        [Test]
        public async Task GetWalkInFridges_WhenFridgesExist_ReturnsList()
        {
            // Arrange
            var expectedFridges = new List<string>
            {
                "Fridge 1",
                "Fridge 2",
                "Fridge 3"
            };

            _bradServiceMock
                .Setup(x => x.GetWalkInFridges())
                .ReturnsAsync(expectedFridges);

            // Act
            var result = await _bradService.GetWalkInFridges();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedFridges));
            _bradServiceMock.Verify(x => x.GetWalkInFridges(), Times.Once);
        }

        [Test]
        public async Task GetDivisions_WhenDivisionsExist_ReturnsDictionary()
        {
            // Arrange
            var expectedDivisions = new Dictionary<int, string>
            {
                { 1, "Division 1" },
                { 2, "Division 2" },
                { 3, "Division 3" }
            };

            // Act
            var result = await _lookupService.GetDivisions();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<Dictionary<int, string>>());
        }

        [Test]
        public async Task GetPendingList_WhenListsExist_ReturnsDictionary()
        {
            // Arrange
            var expectedPendingLists = new Dictionary<int, string>
            {
                { 1, "Pending List 1" },
                { 2, "Pending List 2" },
                { 3, "Pending List 3" }
            };

            // Act
            var result = await _lookupService.GetPendingList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<Dictionary<int, string>>());
        }

        [Test]
        public async Task GetWalkInFridges_WhenNoFridges_ReturnsEmptyList()
        {
            // Arrange
            _bradServiceMock
                .Setup(x => x.GetWalkInFridges())
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _bradService.GetWalkInFridges();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _bradServiceMock.Verify(x => x.GetWalkInFridges(), Times.Once);
        }

        [Test]
        public async Task GetSpecimenTypeList_WhenSpecimensExist_ReturnsList()
        {
            // Arrange
            var expectedSpecimens = new List<SpecimenTypeInfo>
            {
                new SpecimenTypeInfo
                {
                    SpecimenTypeID = 1,
                    SpecimenTypeName = "Blood"
                },
                new SpecimenTypeInfo
                {
                    SpecimenTypeID = 2,
                    SpecimenTypeName = "Urine"
                },
                new SpecimenTypeInfo
                {
                    SpecimenTypeID = 3,
                    SpecimenTypeName = "Swab"
                }
            };

            _bradServiceMock
                .Setup(x => x.GetSpecimenTypeList())
                .ReturnsAsync(expectedSpecimens);

            // Act
            var result = await _bradService.GetSpecimenTypeList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].SpecimenTypeID, Is.EqualTo(1));
                Assert.That(result[0].SpecimenTypeName, Is.EqualTo("Blood"));
                Assert.That(result[1].SpecimenTypeID, Is.EqualTo(2));
                Assert.That(result[1].SpecimenTypeName, Is.EqualTo("Urine"));
                Assert.That(result[2].SpecimenTypeID, Is.EqualTo(3));
                Assert.That(result[2].SpecimenTypeName, Is.EqualTo("Swab"));
            });
            _bradServiceMock.Verify(x => x.GetSpecimenTypeList(), Times.Once);
        }

        [Test]
        public async Task GetSpecimenTypeList_WhenNoSpecimens_ReturnsEmptyList()
        {
            // Arrange
            _bradServiceMock
                .Setup(x => x.GetSpecimenTypeList())
                .ReturnsAsync(new List<SpecimenTypeInfo>());

            // Act
            var result = await _bradService.GetSpecimenTypeList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            _bradServiceMock.Verify(x => x.GetSpecimenTypeList(), Times.Once);
        }

        [Test]
        public async Task GetSpecimenTypeList_WhenServiceThrowsException_PropagatesException()
        {
            // Arrange
            _bradServiceMock
                .Setup(x => x.GetSpecimenTypeList())
                .ThrowsAsync(new Exception("Service error"));

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _bradService.GetSpecimenTypeList());
            Assert.That(ex.Message, Is.EqualTo("Service error"));
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
