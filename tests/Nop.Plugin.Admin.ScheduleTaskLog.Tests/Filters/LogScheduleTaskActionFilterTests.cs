using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using Nop.Plugin.Admin.ScheduleTaskLog.Filters;
using Nop.Plugin.Admin.ScheduleTaskLog.Settings;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Tasks;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Factories;
using NUnit.Framework;
using AdminController = Nop.Web.Areas.Admin.Controllers;
using RootController = Nop.Web.Controllers;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Tests.Services
{
    [NonParallelizable]
    [TestFixture]
    public class LogScheduleTaskActionFilterTests
    {
        private Mock<ScheduleTaskLogSettings> _settings;
        private Mock<ControllerActionDescriptor> _actionDescriptor;

        [OneTimeSetUp]
        public void Setup()
        {
            _settings = new Mock<ScheduleTaskLogSettings>(MockBehavior.Strict);
            _actionDescriptor = new Mock<ControllerActionDescriptor>(MockBehavior.Loose);
        }

        private LogScheduleTaskActionFilter Create()
        {
            return new LogScheduleTaskActionFilter(_settings.Object);
        }

        private ActionExecutingContext CreateContext(Controller controller = null, Dictionary<string, object> actionArguments = null)
        {
            var modelState = new ModelStateDictionary();
            var context = new ActionContext(
                Mock.Of<HttpContext>(),
                Mock.Of<RouteData>(),
                _actionDescriptor.Object,
                modelState
                );

            return new ActionExecutingContext(
                context,
                new List<IFilterMetadata>(),
                actionArguments ?? new Dictionary<string, object>(),
                controller ?? Mock.Of<Controller>()
                );
        }

        private static Controller CreateLogController()
        {
            return new LogController(
                Mock.Of<ICustomerActivityService>(),
                Mock.Of<ILocalizationService>(),
                Mock.Of<ILogger>(),
                Mock.Of<ILogModelFactory>(),
                Mock.Of<INotificationService>(),
                Mock.Of<IPermissionService>());
        }

        private static Controller CreateRootScheduleTaskController()
        {
            return new RootController.ScheduleTaskController(
                Mock.Of<IScheduleTaskService>());
        }

        private static Controller CreateAdminScheduleTaskController()
        {
            return new AdminController.ScheduleTaskController(
                Mock.Of<ICustomerActivityService>(),
                Mock.Of<ILocalizationService>(),
                Mock.Of<INotificationService>(),
                Mock.Of<IPermissionService>(),
                Mock.Of<IScheduleTaskModelFactory>(),
                Mock.Of<IScheduleTaskService>());
        }

        [Test]
        public void OnActionExecuting_LogDisabled_NoRedirect()
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(true);

            var context = CreateContext(CreateRootScheduleTaskController());

            Create().OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Test]
        public void OnActionExecuting_DifferentController_NoRedirect()
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(false);

            var context = CreateContext(CreateLogController());

            Create().OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Test]
        public void OnActionExecuting_SameController_DifferentAction_NoRedirect()
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(false);

            _actionDescriptor
                .SetupGet(p => p.ActionName)
                .Returns("OtherAction");

            var context = CreateContext(CreateRootScheduleTaskController());

            Create().OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Test]
        [TestCase("RunTask")]
        [TestCase("runtask")]
        [TestCase("RUNTASK")]
        public void OnActionExecuting_RootScheduleTaskController_RunTaskAction_HasRedirect(string actionName)
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(false);

            _actionDescriptor
                .SetupGet(p => p.ActionName)
                .Returns(actionName);

            const string actionArgumentName = "taskType";
            const string actionArgumentValue = "type1";

            var context = CreateContext(CreateRootScheduleTaskController(), new() { { actionArgumentName, actionArgumentValue } });

            Create().OnActionExecuting(context);

            Assert.NotNull(context.Result);
            Assert.IsInstanceOf<RedirectToRouteResult>(context.Result);
            var castResult = (RedirectToRouteResult)context.Result;
            Assert.AreEqual("TaskRunner", castResult.RouteValues["controller"]);
            Assert.AreEqual("RunTask", castResult.RouteValues["action"]);
            Assert.AreEqual(actionArgumentValue, castResult.RouteValues["taskType"]);
            Assert.AreEqual(string.Empty, castResult.RouteValues["area"]);
        }

        [Test]
        [TestCase("RunTask")]
        [TestCase("runtask")]
        [TestCase("RUNTASK")]
        public void OnActionExecuting_RootScheduleTaskController_RunTaskAction_LogDisabled_NoRedirect(string actionName)
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(true);

            _actionDescriptor
                .SetupGet(p => p.ActionName)
                .Returns(actionName);

            const string actionArgumentName = "taskType";
            const string actionArgumentValue = "type1";

            var context = CreateContext(CreateRootScheduleTaskController(), new() { { actionArgumentName, actionArgumentValue } });

            Create().OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Test]
        [TestCase("RunNow")]
        [TestCase("runnow")]
        [TestCase("RUNNOW")]
        public void OnActionExecuting_AdminScheduleTaskController_RunNowAction_HasRedirect(string actionName)
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(false);

            _actionDescriptor
                .SetupGet(p => p.ActionName)
                .Returns(actionName);

            const string actionArgumentName = "id";
            const string actionArgumentValue = "1234";

            var context = CreateContext(CreateAdminScheduleTaskController(), new() { { actionArgumentName, actionArgumentValue } });

            Create().OnActionExecuting(context);

            Assert.NotNull(context.Result);
            Assert.IsInstanceOf<RedirectToRouteResult>(context.Result);
            var castResult = (RedirectToRouteResult)context.Result;
            Assert.AreEqual("TaskRunner", castResult.RouteValues["controller"]);
            Assert.AreEqual("RunNow", castResult.RouteValues["action"]);
            Assert.AreEqual(actionArgumentValue, castResult.RouteValues["id"]);
            Assert.AreEqual("Admin", castResult.RouteValues["area"]);
        }

        [Test]
        [TestCase("RunNow")]
        [TestCase("runnow")]
        [TestCase("RUNNOW")]
        public void OnActionExecuting_AdminScheduleTaskController_RunNowAction_LogsDisabled_NoRedirect(string actionName)
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(true);

            _actionDescriptor
                .SetupGet(p => p.ActionName)
                .Returns(actionName);

            const string actionArgumentName = "id";
            const string actionArgumentValue = "1234";

            var context = CreateContext(CreateAdminScheduleTaskController(), new() { { actionArgumentName, actionArgumentValue } });

            Create().OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Test]
        [TestCase("RunNow")]
        [TestCase("runnow")]
        [TestCase("RUNNOW")]
        public void OnActionExecuting_RootScheduleTaskController_RunNowAction_NoRedirect(string actionName)
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(false);

            _actionDescriptor
                .SetupGet(p => p.ActionName)
                .Returns(actionName);

            const string actionArgumentName = "taskType";
            const string actionArgumentValue = "type1";

            var context = CreateContext(CreateRootScheduleTaskController(), new() { { actionArgumentName, actionArgumentValue } });

            Create().OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Test]
        [TestCase("RunTask")]
        [TestCase("runtask")]
        [TestCase("RUNTASK")]
        public void OnActionExecuting_AdminScheduleTaskController_RunTaskAction_NoRedirect(string actionName)
        {
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(false);

            _actionDescriptor
                .SetupGet(p => p.ActionName)
                .Returns(actionName);

            const string actionArgumentName = "id";
            const string actionArgumentValue = "1234";

            var context = CreateContext(CreateAdminScheduleTaskController(), new() { { actionArgumentName, actionArgumentValue } });

            Create().OnActionExecuting(context);

            Assert.Null(context.Result);
        }
    }
}
