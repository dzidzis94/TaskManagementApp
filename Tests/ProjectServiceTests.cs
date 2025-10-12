using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using TaskManagementApp.Services;

namespace Tests
{
    [TestClass]
    public class ProjectServiceTests
    {
        private Mock<ApplicationDbContext> _contextMock;
        private Mock<ILogger<ProjectService>> _loggerMock;
        private ProjectService _projectService;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _contextMock = new Mock<ApplicationDbContext>(options);
            _loggerMock = new Mock<ILogger<ProjectService>>();

            // Setup mock DbSet
            var projects = new List<Project>().AsQueryable();
            var mockSet = new Mock<DbSet<Project>>();
            mockSet.As<IQueryable<Project>>().Setup(m => m.Provider).Returns(projects.Provider);
            mockSet.As<IQueryable<Project>>().Setup(m => m.Expression).Returns(projects.Expression);
            mockSet.As<IQueryable<Project>>().Setup(m => m.ElementType).Returns(projects.ElementType);
            mockSet.As<IQueryable<Project>>().Setup(m => m.GetEnumerator()).Returns(projects.GetEnumerator());

            _contextMock.Setup(c => c.Projects).Returns(mockSet.Object);

            _projectService = new ProjectService(_contextMock.Object, _loggerMock.Object);
        }

        [TestMethod]
        public async Task CreateProjectAsync_Should_Add_Project_To_Context()
        {
            // Arrange
            var project = new Project { Name = "Test Project", Description = "Test Description" };

            // Act
            await _projectService.CreateProjectAsync(project, null);

            // Assert
            _contextMock.Verify(c => c.Projects.Add(It.Is<Project>(p => p.Name == "Test Project")), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }
    }
}