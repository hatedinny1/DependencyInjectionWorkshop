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
        private const string DefaultHashPassword = "my hash password";
        private const string DefaultOtp = "123456";
        private const string DefaultPassword = "pw";
        private IProfile _profile;
        private IOtp _otpService;
        private INotification _slackAdapter;
        private IFailCounter _failCounter;
        private IHash _sha256Adapter;
        private ILogger _nLogAdapter;
        private AuthenticationService _authenticationService;

        [SetUp]
        public void SetUp()
        {
            _profile = Substitute.For<IProfile>();
            _otpService = Substitute.For<IOtp>();
            _slackAdapter = Substitute.For<INotification>();
            _failCounter = Substitute.For<IFailCounter>();
            _sha256Adapter = Substitute.For<IHash>();
            _nLogAdapter = Substitute.For<ILogger>();
            _authenticationService = new AuthenticationService(_profile, _sha256Adapter, _otpService, _nLogAdapter, _failCounter, _slackAdapter);
        }

        [Test]
        public void is_valid()
        {
            GivenOtp(DefaultAccountId, DefaultOtp);
            GivenPassword(DefaultAccountId, DefaultHashPassword);
            GivenHash(DefaultPassword, DefaultHashPassword);

            var isValid = _authenticationService.Verify(DefaultAccountId, DefaultPassword, DefaultOtp);
            Assert.IsTrue(isValid);
        }

        private void GivenOtp(string accountId, string Otp)
        {
            _otpService.GetCurrentOtp(accountId).ReturnsForAnyArgs(Otp);
        }

        private void GivenHash(string password, string hashPassword)
        {
            _sha256Adapter.GetHash(password).ReturnsForAnyArgs(hashPassword);
        }

        private void GivenPassword(string accountId, string password)
        {
            _profile.GetPassword(accountId).ReturnsForAnyArgs(password);
        }
    }
}