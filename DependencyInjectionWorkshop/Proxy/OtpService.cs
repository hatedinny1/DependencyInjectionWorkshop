using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Proxy
{
    public interface IOtp
    {
        string GetCurrentOtp(string accountId);
    }

    public class OtpService : IOtp
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
}