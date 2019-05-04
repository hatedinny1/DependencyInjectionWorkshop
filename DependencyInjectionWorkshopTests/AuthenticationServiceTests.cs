using DependencyInjectionWorkshop.Adapter;
using DependencyInjectionWorkshop.Models;
using DependencyInjectionWorkshop.Proxy;
using DependencyInjectionWorkshop.Repository;
using NSubstitute;
using NUnit.Framework;

namespace DependencyInjectionWorkshopTests
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        private const string DefaultAccountId = "joey";
        private const int DefaultFailedCount = 91;
        private const string DefaultHashPassword = "my hash password";
        private const string DefaultOtp = "123456";
        private const string DefaultPassword = "pw";
        private AuthenticationService _authenticationService;
        private IFailCounter _failCounter;
        private ILogger _logger;
        private INotification _notification;
        private IOtp _otpService;
        private IProfile _profile;
        private IHash _sha256Adapter;

        [Test]
        public void account_is_lock()
        {
            _failCounter.CheckIsLock(DefaultAccountId).ReturnsForAnyArgs(true);
            TestDelegate action = () => _authenticationService.Verify(DefaultAccountId, DefaultHashPassword, DefaultOtp);
            Assert.Throws<FailedTooManyTimesException>(action);
        }

        [Test]
        public void add_failed_count_when_invalid()
        {
            WhenInvalid();
            ShouldAddFailedCount();
        }

        [Test]
        public void is_invalid_when_wrong_otp()
        {
            var isValid = WhenInvalid();
            ShouldBeInvalid(isValid);
        }

        [Test]
        public void is_valid()
        {
            GivenOtp(DefaultAccountId, DefaultOtp);
            GivenPassword(DefaultAccountId, DefaultHashPassword);
            GivenHash(DefaultPassword, DefaultHashPassword);

            var isValid = _authenticationService.Verify(DefaultAccountId, DefaultPassword, DefaultOtp);
            ShouldBeValid(isValid);
        }

        [Test]
        public void log_account_failed_count_when_invalid()
        {
            _failCounter.Get(DefaultAccountId).ReturnsForAnyArgs(DefaultFailedCount);

            WhenInvalid();

            LogShouldContain(DefaultAccountId, DefaultFailedCount.ToString());
        }

        [Test]
        public void notify_user_when_invalid()
        {
            WhenInvalid();
            ShouldNotifyUser();
        }

        [Test]
        public void reset_failed_count_when_valid()
        {
            WhenValid();
            ShouldResetFailedAccount();
        }

        [SetUp]
        public void SetUp()
        {
            _profile = Substitute.For<IProfile>();
            _otpService = Substitute.For<IOtp>();
            _notification = Substitute.For<INotification>();
            _failCounter = Substitute.For<IFailCounter>();
            _sha256Adapter = Substitute.For<IHash>();
            _logger = Substitute.For<ILogger>();
            _authenticationService = new AuthenticationService(_profile, _sha256Adapter, _otpService, _logger,
                _failCounter, _notification);
        }

        private static void ShouldBeInvalid(bool isValid)
        {
            Assert.IsFalse(isValid);
        }

        private static void ShouldBeValid(bool isValid)
        {
            Assert.IsTrue(isValid);
        }

        private void GivenHash(string password, string hashPassword)
        {
            _sha256Adapter.GetHash(password).ReturnsForAnyArgs(hashPassword);
        }

        private void GivenOtp(string accountId, string Otp)
        {
            _otpService.GetCurrentOtp(accountId).ReturnsForAnyArgs(Otp);
        }

        private void GivenPassword(string accountId, string password)
        {
            _profile.GetPassword(accountId).ReturnsForAnyArgs(password);
        }

        private void LogShouldContain(string accountId, string failedCount)
        {
            _logger.Received(1).Info(Arg.Is<string>(m => m.Contains(accountId) && m.Contains(failedCount)));
        }

        private void ShouldAddFailedCount()
        {
            _failCounter.Received().Add(Arg.Any<string>());
        }

        private void ShouldNotifyUser()
        {
            _notification.Received(1).PushMessage(Arg.Any<string>());
        }

        private void ShouldResetFailedAccount()
        {
            _failCounter.Received().Reset(Arg.Any<string>());
        }

        private bool WhenInvalid()
        {
            GivenOtp(DefaultAccountId, DefaultOtp);
            GivenPassword(DefaultAccountId, DefaultHashPassword);
            GivenHash(DefaultPassword, DefaultHashPassword);

            return _authenticationService.Verify(DefaultAccountId, DefaultPassword, "wrong otp");
        }

        private void WhenValid()
        {
            GivenOtp(DefaultAccountId, DefaultOtp);
            GivenPassword(DefaultAccountId, DefaultHashPassword);
            GivenHash(DefaultPassword, DefaultHashPassword);
            _authenticationService.Verify(DefaultAccountId, DefaultPassword, DefaultOtp);
        }
    }
}