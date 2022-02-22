using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;
using System.Data;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Mapping.Builders
{
    /// <summary>
    /// Builds the <see cref="ScheduleTaskEvent"/> entity
    /// </summary>
    public class ScheduleTaskEventBuilder : NopEntityBuilder<ScheduleTaskEvent>
    {
        #region Methods

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ScheduleTaskEvent.ScheduleTaskId)).AsInt32().ForeignKey<ScheduleTask>().OnDelete(Rule.Cascade)
                .WithColumn(nameof(ScheduleTaskEvent.EventStartDateUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(ScheduleTaskEvent.EventEndDateUtc)).AsDateTime2().Nullable()
                .WithColumn(nameof(ScheduleTaskEvent.TotalMilliseconds)).AsInt64().Nullable()
                .WithColumn(nameof(ScheduleTaskEvent.IsError)).AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn(nameof(ScheduleTaskEvent.ExceptionMessage)).AsString(200).Nullable()
                .WithColumn(nameof(ScheduleTaskEvent.ExceptionDetails)).AsString(int.MaxValue).Nullable()
                .WithColumn(nameof(ScheduleTaskEvent.IsStartedManually)).AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn(nameof(ScheduleTaskEvent.TriggeredByCustomerId)).AsInt32().ForeignKey<Customer>().OnDelete(Rule.Cascade).Nullable();
        }

        #endregion
    }
}