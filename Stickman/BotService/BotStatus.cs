using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Stickman.BotService
{
    public class BotStatus : IBotService
    {
        public BotStatus()
        {
            m_statusList = new List<string>()
            {
                "The Powder Toy",
                "Logisim",
                "숨쉬기",
                "인생",
                "전투",
                "비밀",
                "Universe Sandbox",
                "코딩",
                "모딩",
                "즐거운",
                "행복한",
                "끔찍한",
                "신나는",
                "엄청난",
                "대단한",
                "신비로운",
                "심각한",
                "자연스러운",
                "사랑스러운",
                "착한",
                "나쁜",
                "성스러운",
                "구질구질한",
                "소심한",
                "대담한",
                "귀여운",
                "더러운",
                "시원한",
                "어마무시한",
                "예쁜",
                "찰진",
                "혼란한",
                "어지러운",
                "떨어지는",
                "날아가는",
                "발사되는",
                "게임",
                "공부",
                "힘든",
                "지루한",
                "구수한",
                "깨알같은",
                "신성한",
                "알맞은",
                "플레이",
            };
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
        private List<string> m_statusList = null;

        private void SetNextGage()
        {
            m_leftGage = 60 + m_rand.Next(60 * 60);
        }

        private void SetNextStatus(DiscordClient discord)
        {
            int index = m_rand.Next(m_statusList.Count * 2);

            if (index < m_statusList.Count)
            {
                string status = m_statusList[index];

                discord.UpdateStatusAsync(new DiscordGame(status)).Wait();
            }
            else
            {
                // Clear status.
                discord.UpdateStatusAsync().Wait();
            }
        }
    }
}
