using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Tasks;
using Nop.Data;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;
using Nop.Plugin.Admin.ScheduleTaskLog.Helpers;
using Nop.Plugin.Admin.ScheduleTaskLog.Settings;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Services
{
    /// <summary>
    /// Handles all interactions with the schedule task event log
    /// </summary>
    public partial class ScheduleTaskEventService : IScheduleTaskEventService
    {
        private readonly ICurrentDateTimeHelper _currentDateTimeHelper;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IRepository<ScheduleTask> _scheduleTaskRepository;
        private readonly IRepository<ScheduleTaskEvent> _scheduleTaskEventRepository;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly ICustomerService _customerService;
        private readonly ScheduleTaskLogSettings _settings;

        private const int SUCCESS_STATE_ID = 1;
        private const int ERROR_STATE_ID = 2;

        private const int TRIGGER_SCHEDULE_ID = 1;
        private const int TRIGGER_USER_ID = 2;

        /// <summary>
        /// Creates an instance of <see cref="ScheduleTaskEventService"/>
        /// </summary>
        /// <param name="currentDateTimeHelper"></param>
        /// <param name="dateTimeHelper"></param>
        /// <param name="scheduleTaskRepository"></param>
        /// <param name="scheduleTaskEventRepository"></param>
        /// <param name="localizationService"></param>
        /// <param name="logger"></param>
        /// <param name="customerService"></param>
        /// <param name="settings"></param>
        public ScheduleTaskEventService(
            ICurrentDateTimeHelper currentDateTimeHelper,
            IDateTimeHelper dateTimeHelper,
            IRepository<ScheduleTask> scheduleTaskRepository,
            IRepository<ScheduleTaskEvent> scheduleTaskEventRepository,
            ILocalizationService localizationService,
            ILogger logger,
            ICustomerService customerService,
            ScheduleTaskLogSettings settings)
        {
            _currentDateTimeHelper = currentDateTimeHelper;
            _dateTimeHelper = dateTimeHelper;
            _scheduleTaskRepository = scheduleTaskRepository;
            _scheduleTaskEventRepository = scheduleTaskEventRepository;
            _localizationService = localizationService;
            _logger = logger;
            _customerService = customerService;
            _settings = settings;
        }

        /// <inheritdoc/>
        public virtual ScheduleTaskEvent RecordEventStart(ScheduleTask scheduleTask, int? customerId = null)
        {
            try
            {
                var scheduleTaskEvent = ScheduleTaskEvent.Start(_currentDateTimeHelper, scheduleTask);
                if (customerId.HasValue)
                {
                    scheduleTaskEvent = scheduleTaskEvent.SetTriggeredManually(customerId.Value);
                }
                return scheduleTaskEvent;
            }
            catch (Exception ex)
            {
                _logger.Error($"Cannot log the start of the schedule task event", ex);
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual ScheduleTaskEvent RecordEventEnd(ScheduleTaskEvent scheduleTaskEvent)
        {
            if (scheduleTaskEvent is null)
            {
                return null;
            }

            try
            {
                scheduleTaskEvent.End(_currentDateTimeHelper);
                _scheduleTaskEventRepository.Insert(scheduleTaskEvent);
                return scheduleTaskEvent;
            }
            catch (Exception ex)
            {
                _logger.Error($"Cannot log the end of the schedule task event", ex);
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual ScheduleTaskEvent RecordEventError(ScheduleTaskEvent scheduleTaskEvent, Exception exc)
        {
            if (scheduleTaskEvent is null)
            {
                return null;
            }

            try
            {
                scheduleTaskEvent.Error(_currentDateTimeHelper, exc);
                _scheduleTaskEventRepository.Insert(scheduleTaskEvent);
                return scheduleTaskEvent;
            }
            catch (Exception ex)
            {
                _logger.Error($"Cannot log the error of the schedule task event", ex);
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual ScheduleLogListModel PrepareLogListModel(ScheduleLogSearchModel searchModel)
        {
            if (searchModel is null)
            {
                throw new ArgumentNullException(nameof(searchModel));
            }

            var startedFromValue = searchModel.StartedOnFrom.HasValue
                ? (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartedOnFrom.Value, _dateTimeHelper.CurrentTimeZone) : null;
            var startedToValue = searchModel.StartedOnTo.HasValue
                ? (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartedOnTo.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1) : null;

            var logItems = GetAllPaged(query =>
                {
                    if (searchModel.ScheduleTaskId > 0)
                    {
                        query = query.Where(p => p.ScheduleTaskId == searchModel.ScheduleTaskId);
                    }
                    if (searchModel.StateId > 0 && searchModel.StateId <= ERROR_STATE_ID)
                    {
                        query = query.Where(p => p.IsError == (searchModel.StateId == ERROR_STATE_ID));
                    }
                    if (searchModel.TriggerTypeId > 0 && searchModel.TriggerTypeId <= TRIGGER_USER_ID)
                    {
                        query = query.Where(p => p.IsStartedManually == (searchModel.TriggerTypeId == TRIGGER_USER_ID));
                    }
                    if (startedFromValue.HasValue)
                    {
                        query = query.Where(p => p.EventStartDateUtc >= startedFromValue.Value);
                    }
                    if (startedToValue.HasValue)
                    {
                        query = query.Where(p => p.EventStartDateUtc < startedToValue.Value);
                    }
                    query = query.OrderByDescending(p => p.EventStartDateUtc).ThenByDescending(p => p.Id);
                    return query;
                }, searchModel.Page - 1, searchModel.PageSize);

            var tasks = GetAllTasks();
            var averages = GetAverageTimes();

            var model = new ScheduleLogListModel().PrepareToGrid(searchModel, logItems, () =>
            {
                return logItems.Select(logItem => ToModel(logItem, tasks, averages));
            });

            return model;
        }

        /// <inheritdoc/>
        public virtual ScheduleLogModel GetScheduleTaskEventById(int id)
        {
            var logItem = _scheduleTaskEventRepository.GetById(id);
            if (logItem is null)
            {
                return null;
            }

            var tasks = GetAllTasks(logItem.ScheduleTaskId);
            var averages = GetAverageTimes(logItem.ScheduleTaskId);

            return ToModel(logItem, tasks, averages);
        }

        /// <inheritdoc/>
        public virtual void ClearLog()
        {
            _scheduleTaskEventRepository.Truncate();
        }

        /// <inheritdoc/>
        public virtual IList<SelectListItem> GetAvailableTasks()
        {
            var tasks = GetAllTasks();

            return tasks
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
                .ToList();
        }

        /// <inheritdoc/>
        public virtual IList<SelectListItem> GetAvailableStates()
        {
            var states = new List<SelectListItem>
            {
                new SelectListItem(_localizationService.GetResource("Plugins.Admin.ScheduleTaskLog.Success"), SUCCESS_STATE_ID.ToString()),
                new SelectListItem(_localizationService.GetResource("Plugins.Admin.ScheduleTaskLog.Error"), ERROR_STATE_ID.ToString())
            };

            return states;
        }

        /// <inheritdoc/>
        public virtual IList<SelectListItem> GetAvailableTriggerTypes()
        {
            var triggerTypes = new List<SelectListItem>
            {
                new SelectListItem(_localizationService.GetResource("Plugins.Admin.ScheduleTaskLog.ByScheduler"), TRIGGER_SCHEDULE_ID.ToString()),
                new SelectListItem(_localizationService.GetResource("Plugins.Admin.ScheduleTaskLog.ByUser"), TRIGGER_USER_ID.ToString())
            };

            return triggerTypes;
        }

        /// <inheritdoc/>
        public virtual void PruneEvents()
        {
            _scheduleTaskEventRepository.Delete(p => p.EventStartDateUtc < _currentDateTimeHelper.UtcNow.AddDays(_settings.LogExpiryDays * -1));
        }

        private ScheduleLogModel ToModel(ScheduleTaskEvent logItem, IList<ScheduleTask> tasks, IDictionary<int, double> averages)
        {
            var logModel = logItem.ToModel<ScheduleLogModel>();
            logModel.EventStartDateUtc = _dateTimeHelper.ConvertToUserTime(logItem.EventStartDateUtc, DateTimeKind.Utc);
            logModel.EventEndDateUtc = _dateTimeHelper.ConvertToUserTime(logItem.EventEndDateUtc, DateTimeKind.Utc);
            logModel.TaskName = tasks.FirstOrDefault(p => p.Id == logItem.ScheduleTaskId)?.Name;
            logModel.TimeAgainstAverage = GetTimeAgainstAverage(logItem.ScheduleTaskId, logItem.TotalMilliseconds, averages);
            if (logItem.TriggeredByCustomerId.HasValue)
            {
                var customer = _customerService.GetCustomerById(logItem.TriggeredByCustomerId.Value);
                logModel.TriggeredByCustomerEmail = customer?.Email;
            }
            return logModel;
        }

        private IList<ScheduleTask> GetAllTasks(int? taskId = null)
        {
            if (taskId.HasValue)
            {
                var task = _scheduleTaskRepository.GetById(taskId);
                if (!(task is null))
                {
                    return new List<ScheduleTask> { task };
                }
                return new List<ScheduleTask> { };
            }
            return GetAll(_scheduleTaskRepository.Table, query => query);
        }

        private Dictionary<int, double> GetAverageTimes(int? taskId = null)
        {
            var query = _scheduleTaskEventRepository
                .Table
                .Where(p => p.EventEndDateUtc > _currentDateTimeHelper.UtcNow.AddDays(-14));

            if (taskId.HasValue)
            {
                query = query.Where(p => p.ScheduleTaskId == taskId.Value);
            }

            return query
                .GroupBy(p => p.ScheduleTaskId, p => p.TotalMilliseconds)
                .ToDictionary(p => p.Key, p => p.Average());
        }

        private static double? GetTimeAgainstAverage(int scheduleTaskId, double totalMilliseconds, IDictionary<int, double> averages)
        {
            if (!averages.ContainsKey(scheduleTaskId))
            {
                return null;
            }

            var average = averages[scheduleTaskId];

            if (average == 0)
            {
                return null;
            }

            return (totalMilliseconds - average) / average * 100;
        }

        private IPagedList<ScheduleTaskEvent> GetAllPaged(Func<IQueryable<ScheduleTaskEvent>, IQueryable<ScheduleTaskEvent>> func = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false)
        {
            var query = _scheduleTaskEventRepository.Table;

            query = func != null ? func(query) : query;

            return ToPagedList(query, pageIndex, pageSize, getOnlyTotalCount);
        }

        private static IPagedList<T> ToPagedList<T>(IQueryable<T> source, int pageIndex, int pageSize, bool getOnlyTotalCount = false)
        {
            if (source == null)
            {
                return new PagedList<T>(new List<T>(), pageIndex, pageSize);
            }

            //min allowed page size is 1
            pageSize = Math.Max(pageSize, 1);

            var count = source.Count();

            var data = new List<T>();

            if (!getOnlyTotalCount)
            {
                data.AddRange(source.Skip(pageIndex * pageSize).Take(pageSize).ToList());
            }

            return new PagedList<T>(data, pageIndex, pageSize, count);
        }
        private IList<TEntity> GetAll<TEntity>(IQueryable<TEntity> table, Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null)
        {
            IList<TEntity> getAll()
            {
                var query = table;
                query = func != null ? func(query) : query;

                return query.ToList();
            }

            return getAll();
        }
    }
}
