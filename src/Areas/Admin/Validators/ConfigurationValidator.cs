using FluentValidation;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Validators
{
    /// <summary>
    /// Represents configuration model validator
    /// </summary>
    public class ConfigurationValidator : BaseNopValidator<ConfigurationModel>
    {
        #region Ctor

        /// <summary>
        /// Creates the validator for the <see cref="ConfigurationModel"/>
        /// </summary>
        /// <param name="localizationService"></param>
        public ConfigurationValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.LogExpiryDays)
                .GreaterThan(0)
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays.MustBePositive"));
        }

        #endregion
    }
}