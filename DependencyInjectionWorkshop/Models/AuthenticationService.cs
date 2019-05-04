using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dapper;
using NLog;
using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        public bool Verify(string accountId, string password, string otp)
        {
            CheckIsLock(accountId);

            var passwordFromDb = GetPasswordFromDB(accountId);

            var hashPassword = GetHashPassword(password);

            var currentOtp = GetCurrentOtp(accountId);

            if (hashPassword == passwordFromDb && otp == currentOtp)
            {
                ResetFailCounter(accountId);
                return true;
            }

            AddFailedCount(accountId);

            var failedCount = GetFailedCount(accountId);
            LogFailedCount($"Verify failed, AccountId: {accountId}, FailCount: {failedCount}");

            Notify("my message");
            return false;

        }

        private static void Notify(string message)
        {
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { }, "my channel", message, "my bot name");
        }

        private static void LogFailedCount(string message)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }

        private static int GetFailedCount(string accountId)
        {
            var failedCountResponse = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") }.PostAsJsonAsync("api/failedCounter/GetFailedCount", accountId).Result;
            failedCountResponse.EnsureSuccessStatusCode();
            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        private static void AddFailedCount(string accountId)
        {
            var addRetryCountResponse = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") }.PostAsJsonAsync("api/failedCounter/Add", accountId).Result;
            addRetryCountResponse.EnsureSuccessStatusCode();
        }

        private static void ResetFailCounter(string accountId)
        {
            var resetRetryResponse = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") }.PostAsJsonAsync("api/failedCounter/Reset", accountId).Result;
            resetRetryResponse.EnsureSuccessStatusCode();
        }

        private static string GetCurrentOtp(string accountId)
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

        private static string GetHashPassword(string password)
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

        private static string GetPasswordFromDB(string accountId)
        {
            string currentPassword;
            using (var connection = new SqlConnection("datasource=db,password=abc"))
            {
                currentPassword = connection.Query<string>("spGetUserPassword", new { Id = accountId },
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }

            return currentPassword;
        }

        private static void CheckIsLock(string accountId)
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