﻿using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Migrations
{
    /// <summary>
    /// Creates the migration of the plugin entities
    /// </summary>
    [NopMigration("2021/06/25 09:00:00:0000000", "Nop.Plugin.Admin.ScheduleTaskLog schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            Create.TableFor<ScheduleTaskEvent>();

            Create.Index("IX_ScheduleTaskEvent_EventEndDateUtc_ScheduleTaskId").OnTable(nameof(ScheduleTaskEvent))
                .OnColumn(nameof(ScheduleTaskEvent.EventEndDateUtc)).Ascending()
                .OnColumn(nameof(ScheduleTaskEvent.ScheduleTaskId)).Ascending()
                .WithOptions().NonClustered();

            Create.Index("IX_ScheduleTaskEvent_EventStartDateUtc").OnTable(nameof(ScheduleTaskEvent))
                .OnColumn(nameof(ScheduleTaskEvent.EventStartDateUtc)).Ascending()
                .WithOptions().NonClustered();

            Create.Index("IX_ScheduleTaskEvent_ScheduleTaskId_EndDate_Total").OnTable(nameof(ScheduleTaskEvent))
                .OnColumn(nameof(ScheduleTaskEvent.ScheduleTaskId)).Ascending()
                .OnColumn(nameof(ScheduleTaskEvent.EventEndDateUtc)).Ascending()
                .OnColumn(nameof(ScheduleTaskEvent.TotalMilliseconds)).Ascending()
                .WithOptions().NonClustered();
        }
    }
}
