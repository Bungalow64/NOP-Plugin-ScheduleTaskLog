using Moq;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;
using Nop.Plugin.Admin.ScheduleTaskLog.Helpers;
using NUnit.Framework;
using System;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Tests.Domain
{

    [TestFixture]
    public class ScheduleTaskEventTests
    {
        private Mock<ICurrentDateTimeHelper> _currentDateTimeHelper;

        [OneTimeSetUp]
        public void Setup()
        {
            _currentDateTimeHelper = new Mock<ICurrentDateTimeHelper>(MockBehavior.Strict);
            _currentDateTimeHelper.SetupGet(p => p.UtcNow).Returns(DateTime.UtcNow);
        }

        #region Start

        [Test]
        public void Start_NullCurrentDateTimeHelper_ThrowException()
        {
            Assert.Throws<ArgumentNullException>(delegate
            { ScheduleTaskEvent.Start(null, new ScheduleTask()); });
        }

        [Test]
        public void Start_NullScheduleTask_ThrowException()
        {
            Assert.Throws<ArgumentNullException>(delegate
            { ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, null); });
        }

        [Test]
        public void Start_CurrentDateIsUsed()
        {
            var expectedDate = DateTime.Parse("25-Jun-2021");

            _currentDateTimeHelper.SetupGet(p => p.UtcNow).Returns(expectedDate);

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            Assert.AreEqual(expectedDate, data.EventStartDateUtc);
        }

        [Test]
        public void Start_TaskIdStored()
        {
            var task = new ScheduleTask
            {
                Id = 1002
            };

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, task);

            Assert.AreEqual(1002, data.ScheduleTaskId);
        }

        #endregion

        #region End

        [Test]
        public void End_NullCurrentDateTimeHelper_ThrowException()
        {
            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            Assert.Throws<ArgumentNullException>(delegate
            { data.End(null); });
        }

        [Test]
        public void End_CurrentDateIsUsed()
        {
            var expectedDate = DateTime.Parse("25-Jun-2021");

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _currentDateTimeHelper.SetupGet(p => p.UtcNow).Returns(expectedDate);

            data.End(_currentDateTimeHelper.Object);

            Assert.False(data.IsError);
            Assert.AreEqual(expectedDate, data.EventEndDateUtc);
        }

        [Test]
        public void End_CurrentDateIsUsed_StartDateUnaffected()
        {
            var expectedStartDate = DateTime.Parse("25-Jun-2021");
            var expectedEndDate = DateTime.Parse("26-Jun-2021");

            _currentDateTimeHelper
                .SetupSequence(p => p.UtcNow)
                .Returns(expectedStartDate)
                .Returns(expectedEndDate);

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.End(_currentDateTimeHelper.Object);

            Assert.AreEqual(expectedStartDate, data.EventStartDateUtc);
            Assert.AreEqual(expectedEndDate, data.EventEndDateUtc);
        }

        #endregion

        #region Error

        [Test]
        public void Error_NullCurrentDateTimeHelper_ThrowException()
        {
            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            Assert.Throws<ArgumentNullException>(delegate
            { data.Error(null, new Exception()); });
        }

        [Test]
        public void Error_CurrentDateIsUsed()
        {
            var expectedDate = DateTime.Parse("25-Jun-2021");

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _currentDateTimeHelper.SetupGet(p => p.UtcNow).Returns(expectedDate);

            data.Error(_currentDateTimeHelper.Object, new Exception());

            Assert.True(data.IsError);
            Assert.AreEqual(expectedDate, data.EventEndDateUtc);
        }

        [Test]
        public void Error_CurrentDateIsUsed_StartDateUnaffected()
        {
            var expectedStartDate = DateTime.Parse("25-Jun-2021");
            var expectedEndDate = DateTime.Parse("26-Jun-2021");

            _currentDateTimeHelper
                .SetupSequence(p => p.UtcNow)
                .Returns(expectedStartDate)
                .Returns(expectedEndDate);

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.Error(_currentDateTimeHelper.Object, new Exception());

            Assert.AreEqual(expectedStartDate, data.EventStartDateUtc);
            Assert.AreEqual(expectedEndDate, data.EventEndDateUtc);
        }

        [Test]
        public void Error_ExceptionDetailStored()
        {
            Exception caughtException;
            try
            {
                throw new InvalidOperationException("This is a thrown exception");
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.Error(_currentDateTimeHelper.Object, caughtException);

            Assert.True(data.IsError);
            Assert.AreEqual("This is a thrown exception", data.ExceptionMessage);
            Assert.IsNotEmpty(data.ExceptionDetails);
        }

        [Test]
        public void Error_NullException_FlaggedAsErrorNoErrorDetails()
        {
            Exception caughtException = null;

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.Error(_currentDateTimeHelper.Object, caughtException);

            Assert.True(data.IsError);
            Assert.Null(data.ExceptionMessage);
            Assert.Null(data.ExceptionDetails);
        }

        [Test]
        public void Error_LongExceptionMessage_Truncate()
        {
            Exception caughtException;
            try
            {
                throw new InvalidOperationException("This is a thrown exception: " + new string('x', 300));
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.Error(_currentDateTimeHelper.Object, caughtException);

            Assert.NotNull(data.ExceptionMessage);
            Assert.AreEqual(200, data.ExceptionMessage.Length);
            Assert.AreEqual("This is a thrown exception: xxxx", data.ExceptionMessage.Substring(0, 32));
        }

        [Test]
        public void Error_NoExceptionMessage_NoMessageStored()
        {
            Exception caughtException;
            try
            {
                throw new InvalidOperationException(string.Empty);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.Error(_currentDateTimeHelper.Object, caughtException);

            Assert.AreEqual(string.Empty, data.ExceptionMessage);
        }

        #endregion

        #region SetTriggeredManually

        [Test]
        public void SetTriggeredManually_PropertySet()
        {
            const int customerId = 100;

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.SetTriggeredManually(customerId);

            Assert.True(data.IsStartedManually);
            Assert.AreEqual(customerId, data.TriggeredByCustomerId);
        }


        #endregion
    }
}
