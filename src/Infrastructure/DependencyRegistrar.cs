using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Admin.ScheduleTaskLog.Helpers;
using Nop.Plugin.Admin.ScheduleTaskLog.Services;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="appSettings">App settings</param>
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<CurrentDateTimeHelper>().As<ICurrentDateTimeHelper>().SingleInstance();
            builder.RegisterType<ScheduleTaskEventService>().As<IScheduleTaskEventService>().InstancePerLifetimeScope();
        }

        public int Order => 1;
    }
}
