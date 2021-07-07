using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Tasks;
using Nop.Core.Infrastructure.Mapper;
using Nop.Data;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
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
        #region Init

        private Mock<ICurrentDateTimeHelper> _currentDateTimeHelper;
        private Mock<IDateTimeHelper> _dateTimeHelper;
        private Mock<IRepository<ScheduleTask>> _scheduleTaskRepository;
        private Mock<IRepository<ScheduleTaskEvent>> _scheduleTaskEventRepository;
        private Mock<ILocalizationService> _localizationService;
        private Mock<ILogger> _logger;
        private Mock<ICustomerService> _customerService;
        private Mock<ScheduleTaskLogSettings> _settings;

        [SetUp]
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
                .Setup(p => p.ConvertToUserTimeAsync(It.IsAny<DateTime>(), DateTimeKind.Utc))
                .Returns<DateTime, DateTimeKind>((p, q) => Task.FromResult(p));

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

        private Exception BuildException()
        {
            Exception caughtException = null;

            try
            {
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            return caughtException;
        }

        #endregion

        #region GetScheduleTaskEventByIdAsync

        [Test]
        public async Task GetScheduleTaskEventByIdAsync_NoItemFound_ReturnNull()
        {
            const int id = 1001;

            _scheduleTaskEventRepository
                .Setup(p => p.GetByIdAsync(id, null, true))
                .ReturnsAsync((ScheduleTaskEvent)null);

            var result = await Create().GetScheduleTaskEventByIdAsync(id);

            Assert.Null(result);
        }

        [Test]
        public async Task GetScheduleTaskEventByIdAsync_ValidObject_ReturnFullDetails()
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
                .Setup(p => p.GetByIdAsync(id, null, true))
                    .ReturnsAsync(scheduleTaskEvent);

            _scheduleTaskRepository
                .Setup(p => p.GetByIdAsync(scheduleTaskId1, null, true))
                    .ReturnsAsync(scheduleTasks[0]);

            _scheduleTaskRepository
                .Setup(p => p.GetByIdAsync(scheduleTaskId2, null, true))
                    .ReturnsAsync(scheduleTasks[1]);

            _scheduleTaskRepository
                .Setup(p => p.GetAllAsync(It.IsAny<Func<IQueryable<ScheduleTask>, IQueryable<ScheduleTask>>>(), It.IsAny<Func<IStaticCacheManager, CacheKey>>(), true))
                    .ReturnsAsync(scheduleTasks);

            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                    .Returns(events.AsQueryable());

            _currentDateTimeHelper
                .SetupGet(p => p.UtcNow)
                .Returns(DateTime.Parse("04-Mar-2021"));

            var result = await Create().GetScheduleTaskEventByIdAsync(id);

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
        public async Task GetScheduleTaskEventByIdAsync_IsError_ReturnErrorDetails()
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
                .Setup(p => p.GetByIdAsync(id, null, true))
                    .ReturnsAsync(scheduleTaskEvent);

            _scheduleTaskRepository
                .Setup(p => p.GetByIdAsync(scheduleTaskId1, null, true))
                    .ReturnsAsync(scheduleTasks[0]);

            _scheduleTaskRepository
                .Setup(p => p.GetAllAsync(It.IsAny<Func<IQueryable<ScheduleTask>, IQueryable<ScheduleTask>>>(), It.IsAny<Func<IStaticCacheManager, CacheKey>>(), true))
                    .ReturnsAsync(scheduleTasks);

            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                    .Returns(events.AsQueryable());

            var result = await Create().GetScheduleTaskEventByIdAsync(id);

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
        public async Task GetScheduleTaskEventByIdAsync_TwoEventsSameTask_TimeAgainstAverageIsCorrect(int eventMilliseconds, int otherEventMilliseconds, double? expectedTimeAgaistAverage)
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
                .Setup(p => p.GetByIdAsync(id, null, true))
                    .ReturnsAsync(scheduleTaskEvent1);

            _scheduleTaskRepository
                .Setup(p => p.GetByIdAsync(scheduleTaskId1, null, true))
                    .ReturnsAsync(scheduleTasks[0]);

            _scheduleTaskRepository
                .Setup(p => p.GetAllAsync(It.IsAny<Func<IQueryable<ScheduleTask>, IQueryable<ScheduleTask>>>(), It.IsAny<Func<IStaticCacheManager, CacheKey>>(), true))
                    .ReturnsAsync(scheduleTasks);

            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                    .Returns(events.AsQueryable());

            _currentDateTimeHelper
                .SetupGet(p => p.UtcNow)
                .Returns(DateTime.Parse("04-Mar-2021"));

            var result = await Create().GetScheduleTaskEventByIdAsync(id);

            Assert.NotNull(result);
            Assert.AreEqual(expectedTimeAgaistAverage, result.TimeAgainstAverage);
        }

        #endregion

        #region RecordEventStartAsync

        [Test]
        public async Task RecordEventStartAsync_NullScheduleTask_LogErrorOnly()
        {
            ScheduleTask task = null;
            int? customerId = null;

            _logger
                .Setup(p => p.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), null))
                .Callback<string, Exception, Customer>((p, q, r) =>
                {
                    Assert.AreEqual("Cannot log the start of the schedule task event", p);
                    Assert.NotNull(q);
                })
                .Returns(Task.CompletedTask);

            var result = await Create().RecordEventStartAsync(task, customerId);

            Assert.Null(result);

            _logger
                .Verify(p => p.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Once);
        }

        [Test]
        public async Task RecordEventStartAsync_Valid_NoCustomer_ScheduledEventCreated()
        {
            const int taskId = 1001;

            var task = new ScheduleTask
            {
                Id = taskId
            };

            int? customerId = null;

            var scheduleTaskEvent = await Create().RecordEventStartAsync(task, customerId);

            Assert.NotNull(scheduleTaskEvent);
            Assert.AreEqual(taskId, scheduleTaskEvent.ScheduleTaskId);
            Assert.False(scheduleTaskEvent.IsStartedManually);
            Assert.Null(scheduleTaskEvent.TriggeredByCustomerId);
        }

        [Test]
        public async Task RecordEventStartAsync_Valid_WithCustomer_ManualEventCreated()
        {
            const int taskId = 1001;

            var task = new ScheduleTask
            {
                Id = taskId
            };

            int? customerId = 2001;

            var scheduleTaskEvent = await Create().RecordEventStartAsync(task, customerId);

            Assert.NotNull(scheduleTaskEvent);
            Assert.AreEqual(taskId, scheduleTaskEvent.ScheduleTaskId);
            Assert.True(scheduleTaskEvent.IsStartedManually);
            Assert.AreEqual(customerId, scheduleTaskEvent.TriggeredByCustomerId);
        }

        #endregion

        #region RecordEventEndAsync

        [Test]
        public async Task RecordEventEndAsync_NullEvent_ReturnNull()
        {
            ScheduleTaskEvent scheduleTaskEvent = null;

            var result = await Create().RecordEventEndAsync(scheduleTaskEvent);

            Assert.Null(result);
        }

        [Test]
        public async Task RecordEventEndAsync_ExceptionSavingEntity_LogErrorOnly()
        {
            var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _logger
                .Setup(p => p.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), null))
                .Callback<string, Exception, Customer>((p, q, r) =>
                {
                    Assert.AreEqual("Cannot log the end of the schedule task event", p);
                    Assert.IsInstanceOf<InvalidOperationException>(q);
                })
                .Returns(Task.CompletedTask);

            _scheduleTaskEventRepository
                .Setup(p => p.InsertAsync(It.IsAny<ScheduleTaskEvent>(), true))
                .ThrowsAsync(new InvalidOperationException());

            var result = await Create().RecordEventEndAsync(scheduleTaskEvent);

            Assert.Null(result);

            _logger
                .Verify(p => p.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Once);
        }

        [Test]
        public async Task RecordEventEndAsync_Successful()
        {
            _currentDateTimeHelper
                .SetupSequence(p => p.UtcNow)
                .Returns(DateTime.Parse("02-Mar-2021 09:30:00"))
                .Returns(DateTime.Parse("02-Mar-2021 09:30:03"));

            var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _scheduleTaskEventRepository
                .Setup(p => p.InsertAsync(It.IsAny<ScheduleTaskEvent>(), true))
                .Callback<ScheduleTaskEvent, bool>((p, q) =>
                {
                    Assert.NotNull(p);
                    Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), p.EventStartDateUtc);
                    Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:03"), p.EventEndDateUtc);
                    Assert.AreEqual(3000, p.TotalMilliseconds);
                })
                .Returns(Task.CompletedTask);

            var result = await Create().RecordEventEndAsync(scheduleTaskEvent);

            Assert.NotNull(result);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), result.EventStartDateUtc);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:03"), result.EventEndDateUtc);
            Assert.AreEqual(3000, result.TotalMilliseconds);

            _scheduleTaskEventRepository
                .Verify(p => p.InsertAsync(It.IsAny<ScheduleTaskEvent>(), true), Times.Once);

            _logger
                .Verify(p => p.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Never);
        }

        #endregion

        #region RecordEventErrorAsync

        [Test]
        public async Task RecordEventErrorAsync_NullEvent_ReturnNull()
        {
            ScheduleTaskEvent scheduleTaskEvent = null;

            var result = await Create().RecordEventErrorAsync(scheduleTaskEvent, BuildException());

            Assert.Null(result);
        }

        [Test]
        public async Task RecordEventErrorAsync_ExceptionSavingEntity_LogErrorOnly()
        {
            var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _logger
                .Setup(p => p.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), null))
                .Callback<string, Exception, Customer>((p, q, r) =>
                {
                    Assert.AreEqual("Cannot log the error of the schedule task event", p);
                    Assert.IsInstanceOf<InvalidOperationException>(q);
                })
                .Returns(Task.CompletedTask);

            _scheduleTaskEventRepository
                .Setup(p => p.InsertAsync(It.IsAny<ScheduleTaskEvent>(), true))
                .ThrowsAsync(new InvalidOperationException());

            var result = await Create().RecordEventErrorAsync(scheduleTaskEvent, BuildException());

            Assert.Null(result);

            _logger
                .Verify(p => p.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Once);
        }

        [Test]
        public async Task RecordEventErrorAsync_Successful()
        {
            _currentDateTimeHelper
                .SetupSequence(p => p.UtcNow)
                .Returns(DateTime.Parse("02-Mar-2021 09:30:00"))
                .Returns(DateTime.Parse("02-Mar-2021 09:30:03"));

            var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _scheduleTaskEventRepository
                .Setup(p => p.InsertAsync(It.IsAny<ScheduleTaskEvent>(), true))
                .Callback<ScheduleTaskEvent, bool>((p, q) =>
                {
                    Assert.NotNull(p);
                    Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), p.EventStartDateUtc);
                    Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:03"), p.EventEndDateUtc);
                    Assert.AreEqual(3000, p.TotalMilliseconds);
                    Assert.AreEqual("Operation is not valid due to the current state of the object.", p.ExceptionMessage);
                })
                .Returns(Task.CompletedTask);

            var result = await Create().RecordEventErrorAsync(scheduleTaskEvent, BuildException());

            Assert.NotNull(result);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), result.EventStartDateUtc);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:03"), result.EventEndDateUtc);
            Assert.AreEqual(3000, result.TotalMilliseconds);
            Assert.AreEqual("Operation is not valid due to the current state of the object.", result.ExceptionMessage);

            _scheduleTaskEventRepository
                .Verify(p => p.InsertAsync(It.IsAny<ScheduleTaskEvent>(), true), Times.Once);

            _logger
                .Verify(p => p.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Never);
        }

        #endregion

        #region PrepareLogListModelAsync

        [Test]
        public void PrepareLogListModelAsync_NullModel_ThrowException()
        {
            ScheduleLogSearchModel model = null;

            Assert.ThrowsAsync<ArgumentNullException>(() => Create().PrepareLogListModelAsync(model));
        }

        [Test]
        public async Task PrepareLogListModelAsync_DefaultSearch_ReturnItems()
        {
            const int scheduleTaskId1 = 2001;
            const int scheduleTaskId2 = 2002;

            var model = new ScheduleLogSearchModel
            {
                Start = 0,
                Length = 10
            };

            IList<ScheduleTaskEvent> items = new List<ScheduleTaskEvent>
            {
                new ScheduleTaskEvent
                {
                    Id = 1001,
                    EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:03"),
                    TotalMilliseconds = 3000,
                    ScheduleTaskId = scheduleTaskId1
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse("03-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("03-Mar-2021 09:00:02"),
                    TotalMilliseconds = 2000,
                    ScheduleTaskId = scheduleTaskId2
                },
                new ScheduleTaskEvent
                {
                    Id = 1003,
                    EventStartDateUtc = DateTime.Parse("04-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("04-Mar-2021 09:00:04"),
                    TotalMilliseconds = 4000,
                    ScheduleTaskId = scheduleTaskId2
                }
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
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

            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                .Returns(items.AsQueryable());

            _scheduleTaskEventRepository
                .Setup(p => p.GetAllPagedAsync(
                    It.IsAny<Func<IQueryable<ScheduleTaskEvent>, IQueryable<ScheduleTaskEvent>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .Returns<Func<IQueryable<ScheduleTaskEvent>, IQueryable<ScheduleTaskEvent>>, int, int, bool, bool>(async (query, pageIndex, pageSize, s, t) =>
                {
                    return await query(items.AsQueryable()).ToPagedListAsync(pageIndex, pageSize, s);
                });

            _scheduleTaskRepository
                .Setup(p => p.GetAllAsync(
                    It.IsAny<Func<IQueryable<ScheduleTask>, IQueryable<ScheduleTask>>>(),
                    It.IsAny<Func<IStaticCacheManager, CacheKey>>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(tasks));

            var results = await Create().PrepareLogListModelAsync(model);

            Assert.AreEqual(3, results.Data.Count());

            var foundItems = results.Data.ToList();

            Assert.AreEqual(1003, foundItems[0].Id);
            Assert.AreEqual("Task2", foundItems[0].TaskName);
            Assert.AreEqual(1002, foundItems[1].Id);
            Assert.AreEqual("Task2", foundItems[1].TaskName);
            Assert.AreEqual(1001, foundItems[2].Id);
            Assert.AreEqual("Task1", foundItems[2].TaskName);
        }

        [Test]
        public async Task PrepareLogListModelAsync_TaskFilter_ReturnTaskItemsOnly()
        {
            const int scheduleTaskId1 = 2001;
            const int scheduleTaskId2 = 2002;

            var model = new ScheduleLogSearchModel
            {
                Start = 0,
                Length = 10,
                ScheduleTaskId = scheduleTaskId2
            };

            IList<ScheduleTaskEvent> items = new List<ScheduleTaskEvent>
            {
                new ScheduleTaskEvent
                {
                    Id = 1001,
                    EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:03"),
                    TotalMilliseconds = 3000,
                    ScheduleTaskId = scheduleTaskId1
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse("03-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("03-Mar-2021 09:00:02"),
                    TotalMilliseconds = 2000,
                    ScheduleTaskId = scheduleTaskId2
                },
                new ScheduleTaskEvent
                {
                    Id = 1003,
                    EventStartDateUtc = DateTime.Parse("04-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("04-Mar-2021 09:00:04"),
                    TotalMilliseconds = 4000,
                    ScheduleTaskId = scheduleTaskId2
                }
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
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

            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                .Returns(items.AsQueryable());

            _scheduleTaskEventRepository
                .Setup(p => p.GetAllPagedAsync(
                    It.IsAny<Func<IQueryable<ScheduleTaskEvent>, IQueryable<ScheduleTaskEvent>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .Returns<Func<IQueryable<ScheduleTaskEvent>, IQueryable<ScheduleTaskEvent>>, int, int, bool, bool>(async (query, pageIndex, pageSize, s, t) =>
                {
                    return await query(items.AsQueryable()).ToPagedListAsync(pageIndex, pageSize, s);
                });

            _scheduleTaskRepository
                .Setup(p => p.GetAllAsync(
                    It.IsAny<Func<IQueryable<ScheduleTask>, IQueryable<ScheduleTask>>>(),
                    It.IsAny<Func<IStaticCacheManager, CacheKey>>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(tasks));

            var results = await Create().PrepareLogListModelAsync(model);

            Assert.AreEqual(2, results.Data.Count());

            var foundItems = results.Data.ToList();

            Assert.AreEqual(1003, foundItems[0].Id);
            Assert.AreEqual("Task2", foundItems[0].TaskName);
            Assert.AreEqual(1002, foundItems[1].Id);
            Assert.AreEqual("Task2", foundItems[1].TaskName);
        }

        #endregion
    }
}
