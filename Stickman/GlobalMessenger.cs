using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Stickman
{
    public static class GlobalMessenger
    {
        public delegate void Receiver(string type, object param);

        static GlobalMessenger()
        {
            m_receivers = new Dictionary<string, List<Receiver>>();
        }

        public static void Start()
        {
            m_started = true;
        }

        public static async Task PushMessage(string id, string type, object param)
        {
            if (!m_started)
            {
                throw new InvalidOperationException("not started yet");
            }

            if (m_receivers.ContainsKey(id))
            {
                await Task.Yield();

                Parallel.ForEach(m_receivers[id], receiver => receiver(type, param));
            }
        }

        public static void RegisterReceiver(string id, Receiver receiver)
        {
            if (m_started)
            {
                throw new InvalidOperationException("already started");
            }

            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }

            if (m_receivers.ContainsKey(id))
            {
                m_receivers[id].Add(receiver);
            }
            else
            {
                var list = new List<Receiver>();
                list.Add(receiver);

                m_receivers.Add(id, list);
            }
        }

        private static bool m_started = false;
        private static Dictionary<string, List<Receiver>> m_receivers = null;
    }
}
