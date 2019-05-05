using DependencyInjectionWorkshop.Adapter;
using DependencyInjectionWorkshop.Proxy;
using DependencyInjectionWorkshop.Repository;
using System;

namespace DependencyInjectionWorkshop.Models
{
    public abstract class AuthenticationDecoratorBase : IAuthenticationService
    {
        private readonly IAuthenticationService _authenticationService;

        protected AuthenticationDecoratorBase(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public virtual bool Verify(string accountId, string password, string otp)
        {
            var isValid = _authenticationService.Verify(accountId, password, otp);
            return isValid;
        }
    }

    public class NotificationDecorator : AuthenticationDecoratorBase
    {
        private readonly INotification _notification;

        public NotificationDecorator(IAuthenticationService authenticationService, INotification notification) : base(authenticationService)
        {
            _notification = notification;
        }

        private void PushMessage(string message)
        {
            _notification.PushMessage(message);
        }

        public override bool Verify(string accountId, string password, string otp)
        {
            var isValid = base.Verify(accountId, password, otp);
            if (!isValid)
            {
                PushMessage("Verify Failed");
            }
            return isValid;
        }
    }

    public interface IAuthenticationService
    {
        bool Verify(string accountId, string password, string otp);
    }

    public class FailCounterDecorator : AuthenticationDecoratorBase
    {
        private readonly IFailCounter _failCounter;

        public FailCounterDecorator(IAuthenticationService authenticationService, IFailCounter failCounter) : base(authenticationService)
        {
            _failCounter = failCounter;
        }

        private int Get(string accountId)
        {
            var failedCount = _failCounter.Get(accountId);
            return failedCount;
        }

        private void Add(string accountId)
        {
            _failCounter.Add(accountId);
        }

        private void Reset(string accountId)
        {
            _failCounter.Reset(accountId);
        }

        public override bool Verify(string accountId, string password, string otp)
        {
            CheckIsLock(accountId);
            var isValid = base.Verify(accountId, password, otp);
            if (isValid)
            {
                Reset(accountId);
            }
            else
            {
                Add(accountId);
            }
            return isValid;
        }

        public void CheckIsLock(string accountId)
        {
            var isLock = _failCounter.CheckIsLock(accountId);
            if (isLock)
            {
                throw new FailedTooManyTimesException();
            }
        }
    }

    public class LoggerDecorator : AuthenticationDecoratorBase
    {
        private readonly IFailCounter _failCounter;
        private readonly ILogger _logger;

        public LoggerDecorator(IAuthenticationService authenticationService, IFailCounter failCounter, ILogger logger) : base(authenticationService)
        {
            _failCounter = failCounter;
            _logger = logger;
        }

        private void Info(string accountId)
        {
            var failedCount = _failCounter.Get(accountId);
            _logger.Info($"Verify failed, AccountId: {accountId}, FailCount: {failedCount}");
        }

        public override bool Verify(string accountId, string password, string otp)
        {
            var isValid = base.Verify(accountId, password, otp);
            if (!isValid)
            {
                Info(accountId);
            }
            return isValid;
        }
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IProfile _profileRepository;
        private readonly IHash _sha256Adapter;
        private readonly IOtp _otpService;

        public AuthenticationService(IProfile profileRepository, IHash sha256Adapter, IOtp otpService)
        {
            _profileRepository = profileRepository;
            _sha256Adapter = sha256Adapter;
            _otpService = otpService;
        }

        public bool Verify(string accountId, string password, string otp)
        {
            var passwordFromDb = _profileRepository.GetPassword(accountId);

            var hashPassword = _sha256Adapter.GetHash(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId);

            var isValid = hashPassword == passwordFromDb && otp == currentOtp;

            return isValid;
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