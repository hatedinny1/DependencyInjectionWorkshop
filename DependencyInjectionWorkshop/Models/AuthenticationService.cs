using System;
using DependencyInjectionWorkshop.Adapter;
using DependencyInjectionWorkshop.Proxy;
using DependencyInjectionWorkshop.Repository;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly ProfileRepository _profileRepository = new ProfileRepository();
        private readonly SlackAdapter _slackAdapter = new SlackAdapter();
        private readonly OtpService _otpService = new OtpService();
        private readonly Sha256Adapter _sha256Adapter = new Sha256Adapter();
        private readonly FailCounter _failCounter = new FailCounter();
        private readonly LogAdapter _logAdapter = new LogAdapter();

        public bool Verify(string accountId, string password, string otp)
        {
            _failCounter.CheckIsLock(accountId);

            var passwordFromDb = _profileRepository.GetPasswordFromDB(accountId);

            var hashPassword = _sha256Adapter.GetHashPassword(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId);

            if (hashPassword == passwordFromDb && otp == currentOtp)
            {
                _failCounter.ResetFailCounter(accountId);
                return true;
            }

            _failCounter.AddFailedCount(accountId);

            var failedCount = _failCounter.GetFailedCount(accountId);
            _logAdapter.LogFailedCount($"Verify failed, AccountId: {accountId}, FailCount: {failedCount}");

            _slackAdapter.Notify("my message");
            return false;
        }
    }

    public class FailedTooManyTimesException : Exception
    {
        public FailedTooManyTimesException() : base()
        {
        }

        public FailedTooManyTimesException(string message) : base(message)
        {
        }

        public FailedTooManyTimesException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}