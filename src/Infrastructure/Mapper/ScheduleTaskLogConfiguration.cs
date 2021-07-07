using AutoMapper;
using Nop.Core.Infrastructure.Mapper;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Infrastructure.Mapper
{
    /// <summary>
    /// Represents AutoMapper configuration for plugin models
    /// </summary>
    public class ScheduleTaskLogConfiguration : Profile, IOrderedMapperProfile
    {
        #region Ctor

        /// <summary>
        /// Creates an instance of <see cref="ScheduleTaskLogConfiguration"/>
        /// </summary>
        public ScheduleTaskLogConfiguration()
        {
            CreateMap<ScheduleTaskEvent, ScheduleLogModel>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Order of this mapper implementation
        /// </summary>
        public int Order => 1;

        #endregion
    }
}