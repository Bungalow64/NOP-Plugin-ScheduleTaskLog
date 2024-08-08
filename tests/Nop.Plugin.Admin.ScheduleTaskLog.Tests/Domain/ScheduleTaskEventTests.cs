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
            ClassicAssert.Throws<ArgumentNullException>(delegate
            { ScheduleTaskEvent.Start(null, new ScheduleTask()); });
        }

        [Test]
        public void Start_NullScheduleTask_ThrowException()
        {
            ClassicAssert.Throws<ArgumentNullException>(delegate
            { ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, null); });
        }

        [Test]
        public void Start_CurrentDateIsUsed()
        {
            var expectedDate = DateTime.Parse("25-Jun-2021");

            _currentDateTimeHelper.SetupGet(p => p.UtcNow).Returns(expectedDate);

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            ClassicAssert.AreEqual(expectedDate, data.EventStartDateUtc);
        }

        [Test]
        public void Start_TaskIdStored()
        {
            var task = new ScheduleTask
            {
                Id = 1002
            };

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, task);

            ClassicAssert.AreEqual(1002, data.ScheduleTaskId);
        }

        #endregion

        #region End

        [Test]
        public void End_NullCurrentDateTimeHelper_ThrowException()
        {
            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            ClassicAssert.Throws<ArgumentNullException>(delegate
            { data.End(null); });
        }

        [Test]
        public void End_CurrentDateIsUsed()
        {
            var expectedDate = DateTime.Parse("25-Jun-2021");

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _currentDateTimeHelper.SetupGet(p => p.UtcNow).Returns(expectedDate);

            data.End(_currentDateTimeHelper.Object);

            ClassicAssert.False(data.IsError);
            ClassicAssert.AreEqual(expectedDate, data.EventEndDateUtc);
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

            ClassicAssert.AreEqual(expectedStartDate, data.EventStartDateUtc);
            ClassicAssert.AreEqual(expectedEndDate, data.EventEndDateUtc);
        }

        #endregion

        #region Error

        [Test]
        public void Error_NullCurrentDateTimeHelper_ThrowException()
        {
            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            ClassicAssert.Throws<ArgumentNullException>(delegate
            { data.Error(null, new Exception()); });
        }

        [Test]
        public void Error_CurrentDateIsUsed()
        {
            var expectedDate = DateTime.Parse("25-Jun-2021");

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            _currentDateTimeHelper.SetupGet(p => p.UtcNow).Returns(expectedDate);

            data.Error(_currentDateTimeHelper.Object, new Exception());

            ClassicAssert.True(data.IsError);
            ClassicAssert.AreEqual(expectedDate, data.EventEndDateUtc);
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

            ClassicAssert.AreEqual(expectedStartDate, data.EventStartDateUtc);
            ClassicAssert.AreEqual(expectedEndDate, data.EventEndDateUtc);
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

            ClassicAssert.True(data.IsError);
            ClassicAssert.AreEqual("This is a thrown exception", data.ExceptionMessage);
            ClassicAssert.IsNotEmpty(data.ExceptionDetails);
        }

        [Test]
        public void Error_NullException_FlaggedAsErrorNoErrorDetails()
        {
            Exception caughtException = null;

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.Error(_currentDateTimeHelper.Object, caughtException);

            ClassicAssert.True(data.IsError);
            ClassicAssert.Null(data.ExceptionMessage);
            ClassicAssert.Null(data.ExceptionDetails);
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

            ClassicAssert.NotNull(data.ExceptionMessage);
            ClassicAssert.AreEqual(200, data.ExceptionMessage.Length);
            ClassicAssert.AreEqual("This is a thrown exception: xxxx", data.ExceptionMessage.Substring(0, 32));
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

            ClassicAssert.AreEqual(string.Empty, data.ExceptionMessage);
        }

        #endregion

        #region SetTriggeredManually

        [Test]
        public void SetTriggeredManually_PropertySet()
        {
            const int customerId = 100;

            var data = ScheduleTaskEvent.Start(_currentDateTimeHelper.Object, new ScheduleTask());

            data.SetTriggeredManually(customerId);

            ClassicAssert.True(data.IsStartedManually);
            ClassicAssert.AreEqual(customerId, data.TriggeredByCustomerId);
        }


        #endregion
    }
}
