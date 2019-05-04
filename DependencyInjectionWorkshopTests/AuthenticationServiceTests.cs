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
        [Test]
        public void is_valid()
        {
            var profile = Substitute.For<IProfile>();
            var otp = Substitute.For<IOtp>();
            var notification = Substitute.For<INotification>();
            var failCounter = Substitute.For<IFailCounter>();
            var hash = Substitute.For<IHash>();
            var logger = Substitute.For<ILogger>();
            var authenticationService = new AuthenticationService(profile, hash, otp, logger, failCounter, notification);
            otp.GetCurrentOtp("joey").ReturnsForAnyArgs("123456");
            profile.GetPassword("joey").ReturnsForAnyArgs("my hash password");
            hash.GetHash("pw").ReturnsForAnyArgs("my hash password");

            var isValid = authenticationService.Verify("joey", "pw", "123456");
            Assert.IsTrue(isValid);
        }
    }
}