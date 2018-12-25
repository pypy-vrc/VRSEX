using DiscordRPC;
using System;

namespace VRSEX
{
    public static class Discord
    {
        private static DiscordRpcClient m_Client;
        private static RichPresence m_RichPresence;

        public static void RemovePresence()
        {
            if (m_RichPresence != null)
            {
                m_RichPresence = null;
                m_Client.SetPresence(null);
            }
        }

        public static void SetPresence(string details, string state)
        {
            if (m_RichPresence != null)
            {
                if (string.IsNullOrEmpty(m_RichPresence.Details) != string.IsNullOrEmpty(details) ||
                    string.IsNullOrEmpty(m_RichPresence.State) != string.IsNullOrEmpty(state) ||
                    !string.Equals(m_RichPresence.Details, details) ||
                    !string.Equals(m_RichPresence.State, state))
                {
                    m_RichPresence.Details = details;
                    m_RichPresence.State = state;
                    m_Client.SetPresence(m_RichPresence);
                }
            }
            else
            {
                m_RichPresence = new RichPresence()
                {
                    Details = details,
                    State = state,
                    Timestamps = new Timestamps(DateTime.UtcNow, null),
                    Assets = new Assets()
                    {
                        LargeImageKey = "default",
                        LargeImageText = "VRChat",
                    }
                };
                m_Client.SetPresence(m_RichPresence);
            }
        }

        public static void Update()
        {
            m_Client.Invoke();
        }

        public static void Start()
        {
            m_Client = new DiscordRpcClient("525953831020920832");
            /*m_Client.Logger = new ConsoleLogger() { Colored = true };
            m_Client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };
            m_Client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };*/
            m_Client.Initialize();
        }

        public static void Stop()
        {
            m_Client.Dispose();
        }
    }
}