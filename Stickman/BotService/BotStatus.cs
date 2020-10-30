using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Stickman.BotService
{
    public class BotStatus : IBotService
    {
        public BotStatus(string id)
        {
            LoadStatusList();

            GlobalMessenger.RegisterReceiver(id, (type, param) =>
            {
                switch (type)
                {
                    case "AddStatus":
                        AddStatus(param as string);
                        break;

                    case "RemoveStatus":
                        RemoveStatus(param as string);
                        break;
                }
            });
        }

        public void InitService(DiscordClient discord)
        {
            SetNextGage();
            SetNextStatus(discord);
        }

        public void DisposeService(DiscordClient discord)
        {
            // Empty
        }

        public void UpdateService(DiscordClient discord)
        {
            if (m_leftGage <= 0)
            {
                SetNextGage();
                SetNextStatus(discord);
            }
            else
            {
                m_leftGage -= 1;
            }
        }

        private Random m_rand = new Random();
        private int m_leftGage = 0;
        private List<string> m_statusList = new List<string>();
        private readonly object m_lkStatus = new object();

        private void AddStatus(string status)
        {
            lock (m_lkStatus)
            {
                if (!m_statusList.Contains(status))
                {
                    m_statusList.Add(status);
                }
            }

            SaveStatusList();
        }

        private void RemoveStatus(string status)
        {
            lock (m_lkStatus)
            {
                m_statusList.Remove(status);
            }

            SaveStatusList();
        }

        private void SaveStatusList()
        {
            using (var sw = new StreamWriter("status.txt"))
            {
                lock (m_lkStatus)
                {
                    foreach (string status in m_statusList)
                    {
                        sw.WriteLine(status);
                    }
                }

                sw.Close();
            }
        }

        private void LoadStatusList()
        {
            using (var sr = new StreamReader("status.txt"))
            {
                while (!sr.EndOfStream)
                {
                    string status = sr.ReadLine();

                    lock (m_lkStatus)
                    {
                        m_statusList.Add(status);
                    }
                }

                sr.Close();
            }
        }

        private void SetNextGage()
        {
            m_leftGage = 60 + m_rand.Next(60 * 60);
        }

        private void SetNextStatus(DiscordClient discord)
        {
            DiscordActivity activity = null;

            lock (m_lkStatus)
            {
                if (m_statusList.Count > 0)
                {
                    int index = m_rand.Next(m_statusList.Count * 2);

                    if (index < m_statusList.Count)
                    {
                        string status = m_statusList[index];

                        activity = new DiscordActivity(status);
                    }
                }
            }

            discord.UpdateStatusAsync(activity).Wait();
        }
    }
}
