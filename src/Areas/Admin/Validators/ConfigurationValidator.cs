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

        public ConfigurationValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.LogExpiryDays)
                .GreaterThan(0)
                .WithMessage(localizationService.GetResource("Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays.MustBePositive"));
        }

        #endregion
    }
}