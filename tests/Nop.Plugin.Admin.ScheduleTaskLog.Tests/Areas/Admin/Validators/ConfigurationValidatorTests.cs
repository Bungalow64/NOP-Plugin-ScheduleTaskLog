using FluentValidation.TestHelper;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Validators;
using Nop.Web.MVC.Tests.Public.Validators;
using NUnit.Framework;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Tests.Areas.Admin.Validators
{
    [TestFixture]
    public class ConfigurationValidatorTests : BaseValidatorTests
    {
        private ConfigurationValidator Create()
        {
            return new ConfigurationValidator(_localizationService);
        }

        [Test]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public void ValidExpiry_NoError(int days)
        {
            var validator = Create();

            var model = new ConfigurationModel
            {
                LogExpiryDays = days
            };
            validator.ShouldNotHaveValidationErrorFor(x => x.LogExpiryDays, model);
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void InvalidExpiry_HasError(int days)
        {
            var validator = Create();

            var model = new ConfigurationModel
            {
                LogExpiryDays = days
            };
            validator.ShouldHaveValidationErrorFor(x => x.LogExpiryDays, model);
        }
    }
}
