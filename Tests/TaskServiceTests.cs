using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using TaskManagementApp.Services;
using TaskManagementApp.ViewModels;

namespace Tests
{
    [TestClass]
    public class TaskServiceTests
    {
        private Mock<ApplicationDbContext> _contextMock;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<ILogger<TaskService>> _loggerMock;
        private TaskService _taskService;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _contextMock = new Mock<ApplicationDbContext>(options);
            _loggerMock = new Mock<ILogger<TaskService>>();

            // Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            // Setup mock DbSet
            var tasks = new List<TaskItem>().AsQueryable();
            var mockSet = new Mock<DbSet<TaskItem>>();
            mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Provider).Returns(tasks.Provider);
            mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Expression).Returns(tasks.Expression);
            mockSet.As<IQueryable<TaskItem>>().Setup(m => m.ElementType).Returns(tasks.ElementType);
            mockSet.As<IQueryable<TaskItem>>().Setup(m => m.GetEnumerator()).Returns(tasks.GetEnumerator());

            _contextMock.Setup(c => c.Tasks).Returns(mockSet.Object);

            _taskService = new TaskService(_contextMock.Object, _userManagerMock.Object, _loggerMock.Object);
        }

        [TestMethod]
        public async Task CreateTaskAsync_Should_Add_Task_To_Context()
        {
            // Arrange
            var model = new CreateTaskViewModel { Title = "Test Task" };
            var userId = "test-user-id";

            // Act
            await _taskService.CreateTaskAsync(model, userId);

            // Assert
            _contextMock.Verify(c => c.Tasks.Add(It.Is<TaskItem>(t => t.Title == "Test Task")), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.AtLeastOnce);
        }
    }
}