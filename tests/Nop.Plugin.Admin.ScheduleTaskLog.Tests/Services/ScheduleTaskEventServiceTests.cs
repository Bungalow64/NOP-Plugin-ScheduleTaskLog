using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Nop.Core.Caching;
using Nop.Core.Domain.Tasks;
using Nop.Core.Infrastructure.Mapper;
using Nop.Data;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;
using Nop.Plugin.Admin.ScheduleTaskLog.Helpers;
using Nop.Plugin.Admin.ScheduleTaskLog.Infrastructure.Mapper;
using Nop.Plugin.Admin.ScheduleTaskLog.Services;
using Nop.Plugin.Admin.ScheduleTaskLog.Settings;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using NUnit.Framework;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Tests.Services
{
    [TestFixture]
    public class ScheduleTaskEventServiceTests
    {
        private Mock<ICurrentDateTimeHelper> _currentDateTimeHelper;
        private Mock<IDateTimeHelper> _dateTimeHelper;
        private Mock<IRepository<ScheduleTask>> _scheduleTaskRepository;
        private Mock<IRepository<ScheduleTaskEvent>> _scheduleTaskEventRepository;
        private Mock<ILocalizationService> _localizationService;
        private Mock<ILogger> _logger;
        private Mock<ICustomerService> _customerService;
        private Mock<ScheduleTaskLogSettings> _settings;

        [OneTimeSetUp]
        public void Setup()
        {
            _currentDateTimeHelper = new Mock<ICurrentDateTimeHelper>(MockBehavior.Strict);
            _dateTimeHelper = new Mock<IDateTimeHelper>(MockBehavior.Strict);
            _scheduleTaskRepository = new Mock<IRepository<ScheduleTask>>(MockBehavior.Strict);
            _scheduleTaskEventRepository = new Mock<IRepository<ScheduleTaskEvent>>(MockBehavior.Strict);
            _localizationService = new Mock<ILocalizationService>(MockBehavior.Strict);
            _logger = new Mock<ILogger>(MockBehavior.Strict);
            _customerService = new Mock<ICustomerService>(MockBehavior.Strict);
            _settings = new Mock<ScheduleTaskLogSettings>(MockBehavior.Strict);
            _settings
                .SetupGet(p => p.DisableLog)
                .Returns(false);

            _settings
                .SetupGet(p => p.LogExpiryDays)
                .Returns(14);

            _dateTimeHelper
                .Setup(p => p.ConvertToUserTime(It.IsAny<DateTime>(), DateTimeKind.Utc))
                .Returns<DateTime, DateTimeKind>((p, q) => p);

            _currentDateTimeHelper.SetupGet(p => p.UtcNow).Returns(DateTime.UtcNow);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(typeof(ScheduleTaskLogConfiguration));
            });

            AutoMapperConfiguration.Init(config);
        }

        private ScheduleTaskEventService Create()
        {
            return new ScheduleTaskEventService(
                _currentDateTimeHelper.Object,
                _dateTimeHelper.Object,
                _scheduleTaskRepository.Object,
                _scheduleTaskEventRepository.Object,
                _localizationService.Object,
                _logger.Object,
                _customerService.Object,
                _settings.Object);
        }

        [Test]
        public void GetScheduleTaskEventById_NoItemFound_ReturnNull()
        {
            const int id = 1001;

            _scheduleTaskEventRepository
                .Setup(p => p.GetById(id))
                .Returns((ScheduleTaskEvent)null);

            var result = Create().GetScheduleTaskEventById(id);

            Assert.Null(result);
        }

        [Test]
        public void GetScheduleTaskEventById_ValidObject_ReturnFullDetails()
        {
            const int id = 1001;
            const int scheduleTaskId1 = 2001;
            const int scheduleTaskId2 = 2002;

            var scheduleTaskEvent = new ScheduleTaskEvent
            {
                Id = id,
                ScheduleTaskId = scheduleTaskId1,
                EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:30:00"),
                EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:30:02"),
                TotalMilliseconds = 2000,
                ExceptionDetails = string.Empty,
                ExceptionMessage = string.Empty
            };

            var scheduleTasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                },
                new ScheduleTask
                {
                    Id = scheduleTaskId2,
                    Name = "Task2"
                }
            };

            var events = new List<ScheduleTaskEvent>
            {
                scheduleTaskEvent
            };

            _scheduleTaskEventRepository
                .Setup(p => p.GetById(id))
                    .Returns(scheduleTaskEvent);

            _scheduleTaskRepository
                .Setup(p => p.GetById(scheduleTaskId1))
                    .Returns(scheduleTasks[0]);

            _scheduleTaskRepository
                .Setup(p => p.GetById(scheduleTaskId2))
                    .Returns(scheduleTasks[1]);

            _scheduleTaskRepository
                .SetupGet(p => p.Table)
                    .Returns(scheduleTasks.AsQueryable());

            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                    .Returns(events.AsQueryable());

            _currentDateTimeHelper
                .SetupGet(p => p.UtcNow)
                .Returns(DateTime.Parse("04-Mar-2021"));

            var result = Create().GetScheduleTaskEventById(id);

            Assert.NotNull(result);
            Assert.AreEqual(id, result.Id);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:02"), result.EventEndDateUtc);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), result.EventStartDateUtc);
            Assert.False(result.IsError);
            Assert.AreEqual("Task1", result.TaskName);
            Assert.AreEqual(0, result.TimeAgainstAverage);
            Assert.AreEqual(2000, result.TotalMilliseconds);
        }

        [Test]
        public void GetScheduleTaskEventById_IsError_ReturnErrorDetails()
        {
            const int id = 1001;
            const int scheduleTaskId1 = 2001;

            var scheduleTaskEvent = new ScheduleTaskEvent
            {
                Id = id,
                ScheduleTaskId = scheduleTaskId1,
                EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:30:00"),
                EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:30:02"),
                TotalMilliseconds = 2000,
                ExceptionDetails = "Error Details1",
                ExceptionMessage = "Error Message1",
                IsError = true
            };

            var scheduleTasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            var events = new List<ScheduleTaskEvent>
            {
                scheduleTaskEvent
            };

            _scheduleTaskEventRepository
                .Setup(p => p.GetById(id))
                    .Returns(scheduleTaskEvent);

            _scheduleTaskRepository
                .Setup(p => p.GetById(scheduleTaskId1))
                    .Returns(scheduleTasks[0]);

            _scheduleTaskRepository
                .SetupGet(p => p.Table)
                    .Returns(scheduleTasks.AsQueryable());

            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                    .Returns(events.AsQueryable());

            var result = Create().GetScheduleTaskEventById(id);

            Assert.NotNull(result);
            Assert.True(result.IsError);
            Assert.AreEqual("Error Details1", result.ExceptionDetails);
            Assert.AreEqual("Error Message1", result.ExceptionMessage);
        }

        [Test]
        [TestCase(3000, 1000, 50)]
        [TestCase(1000, 3000, -50)]
        [TestCase(1000, 1000, 0)]
        [TestCase(1000, 0, 100)]
        [TestCase(0, 1000, -100)]
        [TestCase(0, 0, null)]
        public void GetScheduleTaskEventById_TwoEventsSameTask_TimeAgainstAverageIsCorrect(int eventMilliseconds, int otherEventMilliseconds, double? expectedTimeAgaistAverage)
        {
            const int id = 1001;
            const int scheduleTaskId1 = 2001;

            var scheduleTaskEvent1 = new ScheduleTaskEvent
            {
                Id = id,
                ScheduleTaskId = scheduleTaskId1,
                EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:30:00"),
                EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:30:02"),
                TotalMilliseconds = eventMilliseconds
            };

            var scheduleTaskEvent2 = new ScheduleTaskEvent
            {
                Id = 1002,
                ScheduleTaskId = scheduleTaskId1,
                EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:30:00"),
                EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:30:02"),
                TotalMilliseconds = otherEventMilliseconds
            };

            var scheduleTasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            var events = new List<ScheduleTaskEvent>
            {
                scheduleTaskEvent1,
                scheduleTaskEvent2
            };

            _scheduleTaskEventRepository
                .Setup(p => p.GetById(id))
                    .Returns(scheduleTaskEvent1);

            _scheduleTaskRepository
                .Setup(p => p.GetById(scheduleTaskId1))
                    .Returns(scheduleTasks[0]);

            _scheduleTaskRepository
                .SetupGet(p => p.Table)
                    .Returns(scheduleTasks.AsQueryable());

            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                    .Returns(events.AsQueryable());

            _currentDateTimeHelper
                .SetupGet(p => p.UtcNow)
                .Returns(DateTime.Parse("04-Mar-2021"));

            var result = Create().GetScheduleTaskEventById(id);

            Assert.NotNull(result);
            Assert.AreEqual(expectedTimeAgaistAverage, result.TimeAgainstAverage);
        }
    }
}
