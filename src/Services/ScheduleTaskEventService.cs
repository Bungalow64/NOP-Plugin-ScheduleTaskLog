using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class ScheduleTaskEventService : IScheduleTaskEventService
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

        public virtual async Task<ScheduleTaskEvent> RecordEventStartAsync(ScheduleTask scheduleTask, int? customerId = null)
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
                await _logger.ErrorAsync($"Cannot log the start of the schedule task event", ex);
                return null;
            }
        }

        public virtual async Task<ScheduleTaskEvent> RecordEventEndAsync(ScheduleTaskEvent scheduleTaskEvent)
        {
            if (scheduleTaskEvent is null)
            {
                return null;
            }

            try
            {
                scheduleTaskEvent.End(_currentDateTimeHelper);
                await _scheduleTaskEventRepository.InsertAsync(scheduleTaskEvent);
                return scheduleTaskEvent;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Cannot log the end of the schedule task event", ex);
                return null;
            }
        }

        public virtual async Task<ScheduleTaskEvent> RecordEventErrorAsync(ScheduleTaskEvent scheduleTaskEvent, Exception exc)
        {
            if (scheduleTaskEvent is null)
            {
                return null;
            }

            try
            {
                scheduleTaskEvent.Error(_currentDateTimeHelper, exc);
                await _scheduleTaskEventRepository.InsertAsync(scheduleTaskEvent);
                return scheduleTaskEvent;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Cannot log the error of the schedule task event", ex);
                return null;
            }
        }

        public virtual async Task<ScheduleLogListModel> PrepareLogListModelAsync(ScheduleLogSearchModel searchModel)
        {
            if (searchModel is null)
            {
                throw new ArgumentNullException(nameof(searchModel));
            }

            var startedFromValue = searchModel.StartedOnFrom.HasValue
                ? (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartedOnFrom.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()) : null;
            var startedToValue = searchModel.StartedOnTo.HasValue
                ? (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartedOnTo.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1) : null;

            var logItems = await _scheduleTaskEventRepository
                .GetAllPagedAsync(query =>
                {
                    if (searchModel.ScheduleTaskId > 0)
                    {
                        query = query.Where(p => p.ScheduleTaskId == searchModel.ScheduleTaskId);
                    }
                    if (searchModel.StateId > 0)
                    {
                        query = query.Where(p => p.IsError == (searchModel.StateId == ERROR_STATE_ID));
                    }
                    if (searchModel.TriggerTypeId > 0)
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

            var tasks = await GetAllTasksAsync();
            var averages = await GetAverageTimesAsync();

            var model = await new ScheduleLogListModel().PrepareToGridAsync(searchModel, logItems, () =>
            {
                return logItems.SelectAwait(async logItem => await ToModelAsync(logItem, tasks, averages));
            });

            return model;
        }

        public async Task<ScheduleLogModel> GetScheduleTaskEventByIdAsync(int id)
        {
            var logItem = await _scheduleTaskEventRepository.GetByIdAsync(id);
            if (logItem is null)
            {
                return null;
            }

            var tasks = await GetAllTasksAsync(logItem.ScheduleTaskId);
            var averages = await GetAverageTimesAsync(logItem.ScheduleTaskId);

            return await ToModelAsync(logItem, tasks, averages);
        }

        public Task ClearLogAsync()
        {
            return _scheduleTaskEventRepository.TruncateAsync();
        }

        public async Task<IList<SelectListItem>> GetAvailableTasksAsync()
        {
            var tasks = await GetAllTasksAsync();

            return await tasks
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
                .ToListAsync();
        }

        public async Task<IList<SelectListItem>> GetAvailableStatesAsync()
        {
            var states = new List<SelectListItem>
            {
                new SelectListItem(await _localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.Success"), SUCCESS_STATE_ID.ToString()),
                new SelectListItem(await _localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.Error"), ERROR_STATE_ID.ToString())
            };

            return states;
        }

        public async Task<IList<SelectListItem>> GetAvailableTriggerTypesAsync()
        {
            var triggerTypes = new List<SelectListItem>
            {
                new SelectListItem(await _localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.ByScheduler"), TRIGGER_SCHEDULE_ID.ToString()),
                new SelectListItem(await _localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.ByUser"), TRIGGER_USER_ID.ToString())
            };

            return triggerTypes;
        }

        public Task PruneEventsAsync()
        {
            return _scheduleTaskEventRepository.DeleteAsync(p => p.EventStartDateUtc < _currentDateTimeHelper.UtcNow.AddDays(_settings.LogExpiryDays * -1));
        }

        private async Task<ScheduleLogModel> ToModelAsync(ScheduleTaskEvent logItem, IList<ScheduleTask> tasks, IDictionary<int, double> averages)
        {
            var logModel = logItem.ToModel<ScheduleLogModel>();
            logModel.EventStartDateUtc = await _dateTimeHelper.ConvertToUserTimeAsync(logItem.EventStartDateUtc, DateTimeKind.Utc);
            logModel.EventEndDateUtc = await _dateTimeHelper.ConvertToUserTimeAsync(logItem.EventEndDateUtc, DateTimeKind.Utc);
            logModel.TaskName = tasks.FirstOrDefault(p => p.Id == logItem.ScheduleTaskId)?.Name;
            logModel.TimeAgainstAverage = GetTimeAgainstAverage(logItem.ScheduleTaskId, logItem.TotalMilliseconds, averages);
            if (logItem.TriggeredByCustomerId.HasValue)
            {
                var customer = await _customerService.GetCustomerByIdAsync(logItem.TriggeredByCustomerId.Value);
                logModel.TriggeredByCustomerEmail = customer?.Email;
            }
            return logModel;
        }

        private async Task<IList<ScheduleTask>> GetAllTasksAsync(int? taskId = null)
        {
            if (taskId.HasValue)
            {
                var task = await _scheduleTaskRepository.GetByIdAsync(taskId);
                if (task is not null)
                {
                    return new List<ScheduleTask> { task };
                }
                return new List<ScheduleTask> { };
            }
            return await _scheduleTaskRepository.GetAllAsync(query => query, cache => default);
        }

        private async Task<Dictionary<int, double>> GetAverageTimesAsync(int? taskId = null)
        {
            var query = _scheduleTaskEventRepository
                .Table
                .Where(p => p.EventEndDateUtc > _currentDateTimeHelper.UtcNow.AddDays(-14));

            if (taskId.HasValue)
            {
                query = query.Where(p => p.ScheduleTaskId == taskId.Value);
            }

            return await query
                .GroupBy(p => p.ScheduleTaskId, p => p.TotalMilliseconds)
                .ToDictionaryAsync(p => p.Key, p => p.Average());
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
    }
}
