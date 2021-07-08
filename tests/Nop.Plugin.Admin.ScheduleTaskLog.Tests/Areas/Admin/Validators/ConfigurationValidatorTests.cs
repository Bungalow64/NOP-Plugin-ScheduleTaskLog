using FluentValidation.TestHelper;
using Moq;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Validators;
using Nop.Services.Localization;
using NUnit.Framework;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Tests.Areas.Admin.Validators
{
    [TestFixture]
    public class ConfigurationValidatorTests
    {
        private const string MUST_BE_POSITIVE_MESSAGE = "Must be positive";
        private Mock<ILocalizationService> _localizationService;

        [OneTimeSetUp]
        public void Setup()
        {
            _localizationService = new Mock<ILocalizationService>(MockBehavior.Strict);
            _localizationService
                .Setup(p => p.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays.MustBePositive"))
                .ReturnsAsync(MUST_BE_POSITIVE_MESSAGE);
        }

        private ConfigurationValidator Create()
        {
            return new ConfigurationValidator(_localizationService.Object);
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
