using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Admin.ScheduleTaskLog.Services;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Tasks;
using NUnit.Framework;
using Moq;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Controllers;
using Task = System.Threading.Tasks.Task;
using Nop.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using static Nop.Tests.BaseNopTest;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Tasks;
using System;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Tests.Areas.Admin.Controllers
{
    [TestFixture]
    [NonParallelizable]
    public class TaskRunnerControllerTests
    {
        #region Init

        private Mock<ILocalizationService> _localizationService;
        private Mock<INotificationService> _notificationService;
        private Mock<IPermissionService> _permissionService;
        private Mock<IScheduleTaskService> _scheduleTaskService;
        private Mock<IScheduleTaskEventService> _scheduleTaskEventService;
        private Mock<IWorkContext> _workContext;
        private Mock<IWebHelper> _webHelper;

        [SetUp]
        public void Setup()
        {
            _localizationService = new Mock<ILocalizationService>(MockBehavior.Strict);
            _notificationService = new Mock<INotificationService>(MockBehavior.Strict);
            _permissionService = new Mock<IPermissionService>(MockBehavior.Strict);
            _scheduleTaskService = new Mock<IScheduleTaskService>(MockBehavior.Strict);
            _scheduleTaskEventService = new Mock<IScheduleTaskEventService>(MockBehavior.Strict);
            _workContext = new Mock<IWorkContext>(MockBehavior.Strict);
            _webHelper = new Mock<IWebHelper>(MockBehavior.Strict);

            var services = new ServiceCollection();
            services.AddTransient(p => _webHelper.Object);
            var serviceProvider = services.BuildServiceProvider();
            EngineContext.Replace(new NopTestEngine(serviceProvider));
        }

        private TaskRunnerController Create()
        {
            return new TaskRunnerController(
                _localizationService.Object,
                _notificationService.Object,
                _permissionService.Object,
                _scheduleTaskService.Object,
                _scheduleTaskEventService.Object,
                _workContext.Object);
        }

        #endregion

        #region RunNow

        [Test]
        public async Task RunNow_NoPermission_AccessDenied()
        {
            _permissionService
                .Setup(p => p.AuthorizeAsync(StandardPermissionProvider.ManageScheduleTasks))
                .ReturnsAsync(false);

            _webHelper
                .Setup(p => p.GetRawUrl(It.IsAny<HttpRequest>()))
                .Returns("https://www.example.com");

            var result = await Create().RunNow(1001);

            Assert.NotNull(result);
            Assert.IsInstanceOf<RedirectToActionResult>(result);

            var castResult = (RedirectToActionResult)result;
            Assert.AreEqual("AccessDenied", castResult.ActionName);
            Assert.AreEqual("Security", castResult.ControllerName);
        }

        [Test]
        public async Task RunNow_TaskNotFound_LogError_NoEventLogged()
        {
            const int id = 1001;

            _permissionService
                .Setup(p => p.AuthorizeAsync(StandardPermissionProvider.ManageScheduleTasks))
                .ReturnsAsync(true);

            _scheduleTaskService
                .Setup(p => p.GetTaskByIdAsync(id))
                .ReturnsAsync((ScheduleTask)null);

            _notificationService
                .Setup(p => p.ErrorNotificationAsync(It.IsAny<Exception>(), true))
                .Returns(Task.CompletedTask);

            _scheduleTaskEventService
                .Setup(p => p.RecordEventErrorAsync(It.IsAny<ScheduleTaskEvent>(), It.IsAny<Exception>()))
                .ReturnsAsync((ScheduleTaskEvent)null);

            var result = await Create().RunNow(id);

            Assert.NotNull(result);
            Assert.IsInstanceOf<RedirectToActionResult>(result);

            var castResult = (RedirectToActionResult)result;
            Assert.AreEqual("List", castResult.ActionName);
            Assert.AreEqual("ScheduleTask", castResult.ControllerName);
            Assert.True(castResult.RouteValues.ContainsKey("area"));
            Assert.AreEqual("Admin", castResult.RouteValues["area"]);

            _notificationService
                .Verify(p => p.ErrorNotificationAsync(It.IsAny<Exception>(), true), Times.Once);

            _scheduleTaskEventService
                .Verify(p => p.RecordEventErrorAsync(It.IsAny<ScheduleTaskEvent>(), It.IsAny<Exception>()), Times.Never);
        }

        #endregion
    }
}
