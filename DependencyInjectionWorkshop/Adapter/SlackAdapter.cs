using SlackAPI;

namespace DependencyInjectionWorkshop.Adapter
{
    public interface INotification
    {
        void PushMessage(string message);
    }

    public class SlackAdapter : INotification
    {
        public void PushMessage(string message)
        {
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { }, "my channel", message, "my bot name");
        }
    }
}