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
            var httpClient = new HttpClient() { BaseAddress = new Uri("http://joey.dev/") };

            var isLockedResponse = httpClient.PostAsJsonAsync("api/failedCounter/IsLocked", accountId).Result;
            isLockedResponse.EnsureSuccessStatusCode();
            var isLocked = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }

            string currentPassword;
            using (var connection = new SqlConnection("datasource=db,password=abc"))
            {
                currentPassword = connection.Query<string>("spGetUserPassword", new { Id = accountId },
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }

            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            var hashPassword = hash.ToString();

            var response = httpClient.PostAsJsonAsync("api/otps", accountId).Result;
            string currentOtp;
            if (response.IsSuccessStatusCode)
            {
                currentOtp = response.Content.ReadAsAsync<string>().Result;
            }
            else
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }

            if (hashPassword == currentPassword && otp == currentOtp)
            {
                var resetRetryResponse = httpClient.PostAsJsonAsync("api/failedCounter/Reset", accountId).Result;
                resetRetryResponse.EnsureSuccessStatusCode();
                return true;
            }

            var failedCountResponse = httpClient.PostAsJsonAsync("api/failedCounter/GetFailedCount", accountId).Result;
            failedCountResponse.EnsureSuccessStatusCode();
            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info($"Verify failed, AccountId: {accountId}, FailCount: {failedCount}");

            var addRetryCountResponse = httpClient.PostAsJsonAsync("api/failedCounter/Add", accountId).Result;
            addRetryCountResponse.EnsureSuccessStatusCode();
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { }, "my channel", "my message", "my bot name");
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