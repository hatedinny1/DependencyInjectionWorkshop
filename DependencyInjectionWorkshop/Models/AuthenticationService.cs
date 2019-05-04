using Dapper;
using NLog;
using SlackAPI;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace DependencyInjectionWorkshop.Models
{
    public class ProfileRepository
    {
        public string GetPasswordFromDB(string accountId)
        {
            string currentPassword;
            using (var connection = new SqlConnection("datasource=db,password=abc"))
            {
                currentPassword = connection.Query<string>("spGetUserPassword", new { Id = accountId },
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }

            return currentPassword;
        }
    }

    public class SlackAdapter
    {
        public void Notify(string message)
        {
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { }, "my channel", message, "my bot name");
        }
    }

    public class OtpService
    {
        public string GetCurrentOtp(string accountId)
        {
            var response = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") }.PostAsJsonAsync("api/otps", accountId).Result;
            string currentOtp;
            if (response.IsSuccessStatusCode)
            {
                currentOtp = response.Content.ReadAsAsync<string>().Result;
            }
            else
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }

            return currentOtp;
        }
    }

    public class Sha256Adapter
    {
        public string GetHashPassword(string password)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            var hashPassword = hash.ToString();
            return hashPassword;
        }
    }

    public class FailCounter
    {
        public void ResetFailCounter(string accountId)
        {
            var resetRetryResponse = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") }.PostAsJsonAsync("api/failedCounter/Reset", accountId).Result;
            resetRetryResponse.EnsureSuccessStatusCode();
        }

        public void AddFailedCount(string accountId)
        {
            var addRetryCountResponse = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") }.PostAsJsonAsync("api/failedCounter/Add", accountId).Result;
            addRetryCountResponse.EnsureSuccessStatusCode();
        }

        public int GetFailedCount(string accountId)
        {
            var failedCountResponse = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") }.PostAsJsonAsync("api/failedCounter/GetFailedCount", accountId).Result;
            failedCountResponse.EnsureSuccessStatusCode();
            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        public void CheckIsLock(string accountId)
        {
            var isLockedResponse = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") }.PostAsJsonAsync("api/failedCounter/IsLocked", accountId).Result;
            isLockedResponse.EnsureSuccessStatusCode();
            var isLocked = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }
        }
    }

    public class LogAdapter
    {
        public void LogFailedCount(string message)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }
    }

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