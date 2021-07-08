using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        #region RecordEventStart

        [Test]
        public void RecordEventStart_NullScheduleTask_LogErrorOnly()
        {
            ScheduleTask task = null;
            int? customerId = null;

            _logger
                .Setup(p => p.Error(It.IsAny<string>(), It.IsAny<Exception>(), null))
                .Callback<string, Exception, Customer>((p, q, r) =>
                {
                    Assert.AreEqual("Cannot log the start of the schedule task event", p);
                    Assert.NotNull(q);
                });

            var result = Create().RecordEventStart(task, customerId);

            Assert.Null(result);

            _logger
                .Verify(p => p.Error(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Once);
        }

        [Test]
        public void RecordEventStart_Valid_NoCustomer_ScheduledEventCreated()
        {
            const int taskId = 1001;

            var task = new ScheduleTask
            {
                Id = taskId
            };

            int? customerId = null;

            var scheduleTaskEvent = Create().RecordEventStart(task, customerId);

            Assert.NotNull(scheduleTaskEvent);
            Assert.AreEqual(taskId, scheduleTaskEvent.ScheduleTaskId);
            Assert.False(scheduleTaskEvent.IsStartedManually);
            Assert.Null(scheduleTaskEvent.TriggeredByCustomerId);
        }

        [Test]
        public void RecordEventStart_Valid_WithCustomer_ManualEventCreated()
        {
            const int taskId = 1001;

            var task = new ScheduleTask
            {
                Id = taskId
            };

            int? customerId = 2001;

            var scheduleTaskEvent = Create().RecordEventStart(task, customerId);

            Assert.NotNull(scheduleTaskEvent);
            Assert.AreEqual(taskId, scheduleTaskEvent.ScheduleTaskId);
            Assert.True(scheduleTaskEvent.IsStartedManually);
            Assert.AreEqual(customerId, scheduleTaskEvent.TriggeredByCustomerId);
        }

        #endregion

        #region RecordEventEnd

        [Test]
        public void RecordEventEnd_NullEvent_ReturnNull()
        {
            ScheduleTaskEvent scheduleTaskEvent = null;

            var result = Create().RecordEventEnd(scheduleTaskEvent);

            Assert.Null(result);
        }

        [Test]
        public void RecordEventEnd_ExceptionSavingEntity_LogErrorOnly()
        {
            var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _logger
                .Setup(p => p.Error(It.IsAny<string>(), It.IsAny<Exception>(), null))
                .Callback<string, Exception, Customer>((p, q, r) =>
                {
                    Assert.AreEqual("Cannot log the end of the schedule task event", p);
                    Assert.IsInstanceOf<InvalidOperationException>(q);
                });

            _scheduleTaskEventRepository
                .Setup(p => p.Insert(It.IsAny<ScheduleTaskEvent>()))
                .Throws(new InvalidOperationException());

            var result = Create().RecordEventEnd(scheduleTaskEvent);

            Assert.Null(result);

            _logger
                .Verify(p => p.Error(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Once);
        }

        [Test]
        public void RecordEventEnd_Successful()
        {
            _currentDateTimeHelper
                .SetupSequence(p => p.UtcNow)
                .Returns(DateTime.Parse("02-Mar-2021 09:30:00"))
                .Returns(DateTime.Parse("02-Mar-2021 09:30:03"));

            var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _scheduleTaskEventRepository
                .Setup(p => p.Insert(It.IsAny<ScheduleTaskEvent>()))
                .Callback<ScheduleTaskEvent>(p =>
                {
                    Assert.NotNull(p);
                    Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), p.EventStartDateUtc);
                    Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:03"), p.EventEndDateUtc);
                    Assert.AreEqual(3000, p.TotalMilliseconds);
                });

            var result = Create().RecordEventEnd(scheduleTaskEvent);

            Assert.NotNull(result);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), result.EventStartDateUtc);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:03"), result.EventEndDateUtc);
            Assert.AreEqual(3000, result.TotalMilliseconds);

            _scheduleTaskEventRepository
                .Verify(p => p.Insert(It.IsAny<ScheduleTaskEvent>()), Times.Once);

            _logger
                .Verify(p => p.Error(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Never);
        }

        #endregion

        #region RecordEventError

        [Test]
        public void RecordEventError_NullEvent_ReturnNull()
        {
            ScheduleTaskEvent scheduleTaskEvent = null;

            var result = Create().RecordEventError(scheduleTaskEvent, BuildException());

            Assert.Null(result);
        }

        [Test]
        public void RecordEventError_ExceptionSavingEntity_LogErrorOnly()
        {
            var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _logger
                .Setup(p => p.Error(It.IsAny<string>(), It.IsAny<Exception>(), null))
                .Callback<string, Exception, Customer>((p, q, r) =>
                {
                    Assert.AreEqual("Cannot log the error of the schedule task event", p);
                    Assert.IsInstanceOf<InvalidOperationException>(q);
                });

            _scheduleTaskEventRepository
                .Setup(p => p.Insert(It.IsAny<ScheduleTaskEvent>()))
                .Throws(new InvalidOperationException());

            var result = Create().RecordEventError(scheduleTaskEvent, BuildException());

            Assert.Null(result);

            _logger
                .Verify(p => p.Error(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Once);
        }

        [Test]
        public void RecordEventError_Successful()
        {
            _currentDateTimeHelper
                .SetupSequence(p => p.UtcNow)
                .Returns(DateTime.Parse("02-Mar-2021 09:30:00"))
                .Returns(DateTime.Parse("02-Mar-2021 09:30:03"));

            var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _scheduleTaskEventRepository
                .Setup(p => p.Insert(It.IsAny<ScheduleTaskEvent>()))
                .Callback<ScheduleTaskEvent>(p =>
                {
                    Assert.NotNull(p);
                    Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), p.EventStartDateUtc);
                    Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:03"), p.EventEndDateUtc);
                    Assert.AreEqual(3000, p.TotalMilliseconds);
                    Assert.AreEqual("Operation is not valid due to the current state of the object.", p.ExceptionMessage);
                });

            var result = Create().RecordEventError(scheduleTaskEvent, BuildException());

            Assert.NotNull(result);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:00"), result.EventStartDateUtc);
            Assert.AreEqual(DateTime.Parse("02-Mar-2021 09:30:03"), result.EventEndDateUtc);
            Assert.AreEqual(3000, result.TotalMilliseconds);
            Assert.AreEqual("Operation is not valid due to the current state of the object.", result.ExceptionMessage);

            _scheduleTaskEventRepository
                .Verify(p => p.Insert(It.IsAny<ScheduleTaskEvent>()), Times.Once);

            _logger
                .Verify(p => p.Error(It.IsAny<string>(), It.IsAny<Exception>(), null), Times.Never);
        }

        #endregion

        #region PrepareLogListModel

        private void SetUpItems(ScheduleLogSearchModel model, IList<ScheduleTaskEvent> items, IList<ScheduleTask> tasks)
        {
            _scheduleTaskEventRepository
                .SetupGet(p => p.Table)
                .Returns(items.AsQueryable());

            _scheduleTaskRepository
                .SetupGet(p => p.Table)
                .Returns(tasks.AsQueryable());
        }

        [Test]
        public void PrepareLogListModel_NullModel_ThrowException()
        {
            ScheduleLogSearchModel model = null;

            Assert.Throws<ArgumentNullException>(() => Create().PrepareLogListModel(model));
        }

        [Test]
        public void PrepareLogListModel_DefaultSearch_ReturnItems()
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

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

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
        [TestCase(2001, "Task1", 1)]
        [TestCase(2002, "Task2", 2)]
        [TestCase(2003, null, 0)]
        [TestCase(-1, null, 3)]
        public void PrepareLogListModel_TaskFilter_ReturnCorrectItemsOnly(int scheduleTaskId, string expectedName, int expectedItems)
        {
            const int scheduleTaskId1 = 2001;
            const int scheduleTaskId2 = 2002;

            var model = new ScheduleLogSearchModel
            {
                Start = 0,
                Length = 10,
                ScheduleTaskId = scheduleTaskId
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

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

            Assert.AreEqual(expectedItems, results.Data.Count());

            if (!(expectedName is null))
            {
                Assert.True(results.Data.All(p => p.TaskName == expectedName));
            }
        }

        [Test]
        [TestCase(1, false, 2)]
        [TestCase(2, true, 1)]
        [TestCase(0, null, 3)]
        [TestCase(3, null, 3)]
        public void PrepareLogListModel_StatusFilter_ReturnCorrectItemsOnly(int stateId, bool? expectedIsError, int expectedItems)
        {
            const int scheduleTaskId1 = 2001;

            var model = new ScheduleLogSearchModel
            {
                Start = 0,
                Length = 10,
                StateId = stateId
            };

            IList<ScheduleTaskEvent> items = new List<ScheduleTaskEvent>
            {
                new ScheduleTaskEvent
                {
                    Id = 1001,
                    EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:03"),
                    TotalMilliseconds = 3000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsError = false
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse("03-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("03-Mar-2021 09:00:02"),
                    TotalMilliseconds = 2000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsError = false
                },
                new ScheduleTaskEvent
                {
                    Id = 1003,
                    EventStartDateUtc = DateTime.Parse("04-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("04-Mar-2021 09:00:04"),
                    TotalMilliseconds = 4000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsError = true
                }
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

            Assert.AreEqual(expectedItems, results.Data.Count());
            if (expectedIsError.HasValue)
            {
                Assert.True(results.Data.All(p => p.IsError == expectedIsError));
            }
        }

        [Test]
        [TestCase(1, false, 2)]
        [TestCase(2, true, 1)]
        [TestCase(0, null, 3)]
        [TestCase(3, null, 3)]
        public void PrepareLogListModel_TriggerFilter_ReturnCorrectItemsOnly(int triggerId, bool? expectedIsManual, int expectedItems)
        {
            const int scheduleTaskId1 = 2001;

            var model = new ScheduleLogSearchModel
            {
                Start = 0,
                Length = 10,
                TriggerTypeId = triggerId
            };

            IList<ScheduleTaskEvent> items = new List<ScheduleTaskEvent>
            {
                new ScheduleTaskEvent
                {
                    Id = 1001,
                    EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:03"),
                    TotalMilliseconds = 3000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = false
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse("03-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("03-Mar-2021 09:00:02"),
                    TotalMilliseconds = 2000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = false
                },
                new ScheduleTaskEvent
                {
                    Id = 1003,
                    EventStartDateUtc = DateTime.Parse("04-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("04-Mar-2021 09:00:04"),
                    TotalMilliseconds = 4000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = true
                }
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

            Assert.AreEqual(expectedItems, results.Data.Count());
            if (expectedIsManual.HasValue)
            {
                Assert.True(results.Data.All(p => p.IsStartedManually == expectedIsManual));
            }
        }

        [Test]
        [TestCase("01-Mar-2021", 3)]
        [TestCase("02-Mar-2021", 3)]
        [TestCase("03-Mar-2021", 2)]
        [TestCase("04-Mar-2021", 1)]
        [TestCase("05-Mar-2021", 0)]
        public void PrepareLogListModel_StartDateFromFilter_ReturnCorrectItemsOnly(string startDate, int expectedItems)
        {
            const int scheduleTaskId1 = 2001;

            var model = new ScheduleLogSearchModel
            {
                Start = 0,
                Length = 10,
                StartedOnFrom = DateTime.Parse(startDate)
            };

            IList<ScheduleTaskEvent> items = new List<ScheduleTaskEvent>
            {
                new ScheduleTaskEvent
                {
                    Id = 1001,
                    EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:03"),
                    TotalMilliseconds = 3000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = false
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse("03-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("03-Mar-2021 09:00:02"),
                    TotalMilliseconds = 2000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = false
                },
                new ScheduleTaskEvent
                {
                    Id = 1003,
                    EventStartDateUtc = DateTime.Parse("04-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("04-Mar-2021 09:00:04"),
                    TotalMilliseconds = 4000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = true
                }
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            _dateTimeHelper
                .SetupGet(p => p.CurrentTimeZone)
                .Returns(TimeZoneInfo.Utc);

            _dateTimeHelper
                .Setup(p => p.ConvertToUtcTime(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns<DateTime, TimeZoneInfo>((p, q) => p);

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

            Assert.AreEqual(expectedItems, results.Data.Count());
        }

        [Test]
        [TestCase("01-Mar-2021", 0)]
        [TestCase("02-Mar-2021", 1)]
        [TestCase("03-Mar-2021", 2)]
        [TestCase("04-Mar-2021", 3)]
        [TestCase("05-Mar-2021", 3)]
        public void PrepareLogListModel_StartDateToFilter_ReturnCorrectItemsOnly(string startDate, int expectedItems)
        {
            const int scheduleTaskId1 = 2001;

            var model = new ScheduleLogSearchModel
            {
                Start = 0,
                Length = 10,
                StartedOnTo = DateTime.Parse(startDate)
            };

            IList<ScheduleTaskEvent> items = new List<ScheduleTaskEvent>
            {
                new ScheduleTaskEvent
                {
                    Id = 1001,
                    EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:03"),
                    TotalMilliseconds = 3000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = false
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse("03-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("03-Mar-2021 09:00:02"),
                    TotalMilliseconds = 2000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = false
                },
                new ScheduleTaskEvent
                {
                    Id = 1003,
                    EventStartDateUtc = DateTime.Parse("04-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("04-Mar-2021 09:00:04"),
                    TotalMilliseconds = 4000,
                    ScheduleTaskId = scheduleTaskId1,
                    IsStartedManually = true
                }
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            _dateTimeHelper
                .SetupGet(p => p.CurrentTimeZone)
                .Returns(TimeZoneInfo.Utc);

            _dateTimeHelper
                .Setup(p => p.ConvertToUtcTime(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns<DateTime, TimeZoneInfo>((p, q) => p);

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

            Assert.AreEqual(expectedItems, results.Data.Count());
        }

        [Test]
        [TestCase("02-Mar-2021 09:00:00", "02-Mar-2021 09:00:00", 1002)]
        [TestCase("02-Mar-2021 09:00:01", "02-Mar-2021 09:00:00", 1001)]
        [TestCase("02-Mar-2021 09:00:00", "02-Mar-2021 09:00:01", 1002)]
        public void PrepareLogListModel_OrderCorrectly(string startDate1, string startDate2, int expectedFirstId)
        {
            const int scheduleTaskId1 = 2001;

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
                    EventStartDateUtc = DateTime.Parse(startDate1),
                    EventEndDateUtc = DateTime.Parse(startDate1)
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse(startDate2),
                    EventEndDateUtc = DateTime.Parse(startDate2)
                }
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            _dateTimeHelper
                .SetupGet(p => p.CurrentTimeZone)
                .Returns(TimeZoneInfo.Utc);

            _dateTimeHelper
                .Setup(p => p.ConvertToUtcTime(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns<DateTime, TimeZoneInfo>((p, q) => p);

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);


            Assert.AreEqual(2, results.Data.Count());

            var foundItems = results.Data.ToList();

            Assert.AreEqual(expectedFirstId, foundItems[0].Id);
            Assert.AreNotEqual(expectedFirstId, foundItems[1].Id);
        }

        [Test]
        [TestCase(1, 10, 10, 1010, 1001)]
        [TestCase(1, 20, 10, 1010, 1001)]
        [TestCase(0, 3, 3, 1010, 1008)]
        [TestCase(1, 3, 3, 1010, 1008)]
        [TestCase(4, 3, 3, 1007, 1005)]
        [TestCase(7, 3, 3, 1004, 1002)]
        [TestCase(10, 3, 1, 1001, 1001)]
        [TestCase(11, 3, 1, 1001, 1001)]
        [TestCase(12, 3, 0, null, null)]
        [TestCase(13, 3, 0, null, null)]
        [TestCase(14, 3, 0, null, null)]
        [TestCase(15, 3, 0, null, null)]
        public void PrepareLogListModel_TakePageCorrectly(int start, int length, int expectedCount, int? expectedFirstId, int? expectedLastId)
        {
            const int scheduleTaskId1 = 2001;

            var model = new ScheduleLogSearchModel
            {
                Start = start,
                Length = length
            };

            static ScheduleTaskEvent newEvent(int id)
            {
                return new ScheduleTaskEvent
                {
                    Id = id,
                    EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:00")
                };
            }

            IList<ScheduleTaskEvent> items = new List<ScheduleTaskEvent>
            {
                newEvent(1001),
                newEvent(1002),
                newEvent(1003),
                newEvent(1004),
                newEvent(1005),
                newEvent(1006),
                newEvent(1007),
                newEvent(1008),
                newEvent(1009),
                newEvent(1010),
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            _dateTimeHelper
                .SetupGet(p => p.CurrentTimeZone)
                .Returns(TimeZoneInfo.Utc);

            _dateTimeHelper
                .Setup(p => p.ConvertToUtcTime(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns<DateTime, TimeZoneInfo>((p, q) => p);

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

            Assert.AreEqual(expectedCount, results.Data.Count());

            var foundItems = results.Data.ToList();

            if (expectedFirstId.HasValue)
            {
                Assert.AreEqual(expectedFirstId, foundItems.First().Id);
                Assert.AreEqual(expectedLastId, foundItems.Last().Id);
            }
        }

        [Test]
        [TestCase(2001, 2001, 100, 300, -50, 50)]
        [TestCase(2001, 2001, 300, 100, 50, -50)]
        [TestCase(2001, 2001, 100, 100, 0, 0)]
        [TestCase(2001, 2001, 0, 0, null, null)]
        [TestCase(2001, 2002, 100, 300, 0, 0)]
        public void PrepareLogListModel_AverageTimeCorrect(int event1TaskId, int event2TaskId, int event1Time, int event2Time, long? expectedTask1Average, long? expectedTask2Average)
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
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:01"),
                    TotalMilliseconds = event1Time,
                    ScheduleTaskId = event1TaskId
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse("02-Mar-2021 09:00:00"),
                    EventEndDateUtc = DateTime.Parse("02-Mar-2021 09:00:01"),
                    TotalMilliseconds = event2Time,
                    ScheduleTaskId = event2TaskId
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

            _currentDateTimeHelper
                .SetupGet(p => p.UtcNow)
                .Returns(DateTime.Parse("01-Mar-2021 09:00:00"));

            _dateTimeHelper
                .SetupGet(p => p.CurrentTimeZone)
                .Returns(TimeZoneInfo.Utc);

            _dateTimeHelper
                .Setup(p => p.ConvertToUtcTime(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns<DateTime, TimeZoneInfo>((p, q) => p);

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

            Assert.AreEqual(2, results.Data.Count());

            var foundItems = results.Data.ToList();

            Assert.AreEqual(1001, foundItems[1].Id);
            Assert.AreEqual(expectedTask1Average, foundItems[1].TimeAgainstAverage);
            Assert.AreEqual(1002, foundItems[0].Id);
            Assert.AreEqual(expectedTask2Average, foundItems[0].TimeAgainstAverage);
        }

        [Test]
        public void PrepareLogListModel_AverageTimeCorrect_OneOldEvent_AverageDoesNotIncludeOldEvents()
        {
            const int scheduleTaskId1 = 2001;
            DateTime oldDate = DateTime.Parse("02-Mar-2020 09:00:00");
            DateTime currentDate = DateTime.Parse("02-Mar-2021 09:00:00");

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
                    EventStartDateUtc = oldDate,
                    EventEndDateUtc = oldDate,
                    TotalMilliseconds = 100,
                    ScheduleTaskId = scheduleTaskId1
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = currentDate,
                    EventEndDateUtc = currentDate,
                    TotalMilliseconds = 200,
                    ScheduleTaskId = scheduleTaskId1
                }
            };

            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = scheduleTaskId1,
                    Name = "Task1"
                }
            };

            _currentDateTimeHelper
                .SetupGet(p => p.UtcNow)
                .Returns(currentDate);

            _dateTimeHelper
                .SetupGet(p => p.CurrentTimeZone)
                .Returns(TimeZoneInfo.Utc);

            _dateTimeHelper
                .Setup(p => p.ConvertToUtcTime(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns<DateTime, TimeZoneInfo>((p, q) => p);

            SetUpItems(model, items, tasks);

            var results = Create().PrepareLogListModel(model);

            Assert.AreEqual(2, results.Data.Count());

            var foundItems = results.Data.ToList();

            Assert.AreEqual(1001, foundItems[1].Id);
            Assert.AreEqual(-50, foundItems[1].TimeAgainstAverage);
            Assert.AreEqual(1002, foundItems[0].Id);
            Assert.AreEqual(0, foundItems[0].TimeAgainstAverage);
        }

        #endregion

        #region GetScheduleTaskEventById

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

        #endregion

        #region ClearLog

        [Test]
        public void ClearLog_CallTruncate()
        {
            _scheduleTaskEventRepository
                .Setup(p => p.Truncate(false));

            Create().ClearLog();

            _scheduleTaskEventRepository
                .Verify(p => p.Truncate(false), Times.Once);
        }

        #endregion

        #region GetAvailableTasks

        [Test]
        public void GetAvailableTasks_ReturnAll()
        {
            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = 1001,
                    Name = "Task1"
                },
                new ScheduleTask
                {
                    Id = 1002,
                    Name = "Task2"
                }
            };

            _scheduleTaskRepository
                .SetupGet(p => p.Table)
                    .Returns(tasks.AsQueryable());

            var availableTasks = Create().GetAvailableTasks();

            Assert.NotNull(availableTasks);
            Assert.AreEqual(2, availableTasks.Count);
            Assert.AreEqual("1001", availableTasks[0].Value);
            Assert.AreEqual("Task1", availableTasks[0].Text);
            Assert.AreEqual("1002", availableTasks[1].Value);
            Assert.AreEqual("Task2", availableTasks[1].Text);
        }

        #endregion

        #region GetAvailableStates

        [Test]
        public void GetAvailableStates_ReturnAll()
        {
            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = 1001,
                    Name = "Task1"
                },
                new ScheduleTask
                {
                    Id = 1002,
                    Name = "Task2"
                }
            };

            _localizationService
                .Setup(p => p.GetResource("Plugins.Admin.ScheduleTaskLog.Success"))
                .Returns("Success1");

            _localizationService
                .Setup(p => p.GetResource("Plugins.Admin.ScheduleTaskLog.Error"))
                .Returns("Error1");

            var availableTasks = Create().GetAvailableStates();

            Assert.NotNull(availableTasks);
            Assert.AreEqual(2, availableTasks.Count);
            Assert.AreEqual("1", availableTasks[0].Value);
            Assert.AreEqual("Success1", availableTasks[0].Text);
            Assert.AreEqual("2", availableTasks[1].Value);
            Assert.AreEqual("Error1", availableTasks[1].Text);
        }

        #endregion

        #region GetAvailableTriggerTypes

        [Test]
        public void GetAvailableTriggerTypes_ReturnAll()
        {
            IList<ScheduleTask> tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Id = 1001,
                    Name = "Task1"
                },
                new ScheduleTask
                {
                    Id = 1002,
                    Name = "Task2"
                }
            };

            _localizationService
                .Setup(p => p.GetResource("Plugins.Admin.ScheduleTaskLog.ByScheduler"))
                .Returns("ByScheduler1");

            _localizationService
                .Setup(p => p.GetResource("Plugins.Admin.ScheduleTaskLog.ByUser"))
                .Returns("ByUser1");

            var availableTasks = Create().GetAvailableTriggerTypes();

            Assert.NotNull(availableTasks);
            Assert.AreEqual(2, availableTasks.Count);
            Assert.AreEqual("1", availableTasks[0].Value);
            Assert.AreEqual("ByScheduler1", availableTasks[0].Text);
            Assert.AreEqual("2", availableTasks[1].Value);
            Assert.AreEqual("ByUser1", availableTasks[1].Text);
        }

        #endregion

        #region PruneEvents

        [Test]
        public void PruneEvents_DeleteExpired()
        {
            var scheduledTaskEvents = new List<ScheduleTaskEvent>
            {
                new ScheduleTaskEvent
                {
                    Id = 1001,
                    EventStartDateUtc = DateTime.Parse("04-Mar-2021")
                },
                new ScheduleTaskEvent
                {
                    Id = 1002,
                    EventStartDateUtc = DateTime.Parse("05-Mar-2021")
                },
                new ScheduleTaskEvent
                {
                    Id = 1003,
                    EventStartDateUtc = DateTime.Parse("06-Mar-2021")
                },
                new ScheduleTaskEvent
                {
                    Id = 1004,
                    EventStartDateUtc = DateTime.Parse("07-Mar-2021")
                },
                new ScheduleTaskEvent
                {
                    Id = 1005,
                    EventStartDateUtc = DateTime.Parse("15-Mar-2021")
                },
                new ScheduleTaskEvent
                {
                    Id = 1006,
                    EventStartDateUtc = DateTime.Parse("22-Mar-2021")
                }
            }.AsQueryable();

            _scheduleTaskEventRepository
                .Setup(p => p.Delete(It.IsAny<Expression<Func<ScheduleTaskEvent, bool>>>()))
                .Callback<Expression<Func<ScheduleTaskEvent, bool>>>(p =>
                {
                    var deleted = scheduledTaskEvents.Where(p).ToList();
                    Assert.AreEqual(2, deleted.Count);
                    Assert.AreEqual(1001, deleted[0].Id);
                    Assert.AreEqual(1002, deleted[1].Id);
                });

            _currentDateTimeHelper
                .SetupGet(p => p.UtcNow)
                .Returns(DateTime.Parse("20-Mar-2021"));

            _settings
                .SetupGet(p => p.LogExpiryDays)
                .Returns(14);

            Create().PruneEvents();

            _scheduleTaskEventRepository
                .Verify(p => p.Delete(It.IsAny<Expression<Func<ScheduleTaskEvent, bool>>>()), Times.Once);
        }

        #endregion
    }
}
