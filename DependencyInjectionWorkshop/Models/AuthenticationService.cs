using DependencyInjectionWorkshop.Adapter;
using DependencyInjectionWorkshop.Proxy;
using DependencyInjectionWorkshop.Repository;
using System;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly IProfile _profileRepository;
        private readonly IHash _sha256Adapter;
        private readonly IOtp _otpService;
        private readonly ILogger _nLogAdapter;
        private readonly IFailCounter _failCounter;
        private readonly INotification _slackAdapter;

        //public AuthenticationService()
        //{
        //    _profileRepository = new ProfileRepository();
        //    _sha256Adapter = new Sha256Adapter();
        //    _otpService = new OtpService();
        //    _nLogAdapter = new NLogAdapter();
        //    _failCounter = new FailCounter();
        //    _slackAdapter = new SlackAdapter();
        //}

        public AuthenticationService(IProfile profileRepository, IHash sha256Adapter, IOtp otpService, ILogger nLogAdapter, IFailCounter failCounter, INotification slackAdapter)
        {
            _profileRepository = profileRepository;
            _sha256Adapter = sha256Adapter;
            _otpService = otpService;
            _nLogAdapter = nLogAdapter;
            _failCounter = failCounter;
            _slackAdapter = slackAdapter;
        }

        public bool Verify(string accountId, string password, string otp)
        {
            var isLock = _failCounter.CheckIsLock(accountId);
            if (isLock)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profileRepository.GetPassword(accountId);

            var hashPassword = _sha256Adapter.GetHash(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId);

            if (hashPassword == passwordFromDb && otp == currentOtp)
            {
                _failCounter.Reset(accountId);
                return true;
            }

            _failCounter.Add(accountId);

            var failedCount = _failCounter.Get(accountId);
            _nLogAdapter.Info($"Verify failed, AccountId: {accountId}, FailCount: {failedCount}");

            _slackAdapter.PushMessage("my message");
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