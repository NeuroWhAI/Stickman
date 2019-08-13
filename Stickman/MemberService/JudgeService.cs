using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Stickman.Command;

namespace Stickman.MemberService
{
    public class JudgeService : IBotService
    {
        public JudgeService(string id, string botId)
        {
            Root = id;
            PunishmentFile = Path.Combine(id, "punish.dat");

            Directory.CreateDirectory(id);


            GlobalMessenger.RegisterReceiver(botId, (type, param) =>
            {
                if (type == "NewMessage" && param is MessageCreateEventArgs e)
                {
                    ulong userId = e.Author.Id;

                    // 서버 메세지이며 봇이 아니고 조용 채널도 아닌 경우만 확인.
                    if (e.Guild != null && !e.Author.IsBot && e.Channel.Name != "조용")
                    {
                        int punishTime = UpdateSpamGage(userId, e.Message.Timestamp);

                        // 스팸으로 판단되고 멤버가 스탭이 아니면 정지.
                        if (punishTime != 0)
                        {
                            // NOTE: 잦은 디스코드 API 호출을 피하기 위해 다중 if로 분리.

                            var member = e.Guild.GetMemberAsync(userId).Result;

                            if (member.Roles.Count() > 0
                                && member.Roles.All(role => role.Name != "스탭"))
                            {
                                m_executionQue.Enqueue(Tuple.Create(userId, punishTime));

                                string timeText = TimeToText(punishTime);
                                e.Message.RespondAsync($"{e.Author.Mention}님 도배로 {timeText} 정지입니다.").Wait();
                            }
                        }
                    }
                }
            });

            GlobalMessenger.RegisterReceiver(id, (type, param) =>
            {
                switch (type)
                {
                    case "PunishUser":
                        if (param is CommandContextAdv<Tuple<int>> ctxPunish)
                        {
                            var ctx = ctxPunish.Context;
                            var users = ctx.Message.MentionedUsers;

                            foreach (var user in users)
                            {
                                PunishUser(ctx.Client, user.Id, ctxPunish.Arguments.Item1);
                            }
                        }
                        break;

                    case "ReleaseUser":
                        if (param is CommandContextAdv<Tuple<string>> ctxRelease)
                        {
                            var ctx = ctxRelease.Context;
                            var users = ctx.Message.MentionedUsers;

                            foreach (var user in users)
                            {
                                ReleaseUser(ctx.Client, user.Id);
                            }
                        }
                        break;
                }
            });
        }

        private readonly string Root;
        private readonly string PunishmentFile;

        private ConcurrentQueue<Tuple<ulong, int>> m_executionQue = new ConcurrentQueue<Tuple<ulong, int>>();
        private ConcurrentDictionary<ulong, DateTime> m_endTimes = new ConcurrentDictionary<ulong, DateTime>();

        public void InitService(DiscordClient discord)
        {
            LoadPunishments();
        }

        public void DisposeService(DiscordClient discord)
        {
            SavePunishments();
        }

        public void UpdateService(DiscordClient discord)
        {
            // 활정 예약이 있으면 처리.
            while (!m_executionQue.IsEmpty)
            {
                if (m_executionQue.TryDequeue(out var data))
                {
                    PunishUser(discord, data.Item1, data.Item2);
                }
            }


            var now = DateTime.Now;
            var endTimes = m_endTimes.ToArray();

            // 활정 끝났는지 확인하고 처리.
            foreach (var kv in endTimes)
            {
                ulong id = kv.Key;
                var endTime = kv.Value;

                if (now >= endTime)
                {
                    ReleaseUser(discord, id);
                }
            }
        }

        private void PunishUser(DiscordClient discord, ulong id, int time)
        {
            DateTime endTime;

            if (time < 0)
            {
                endTime = DateTime.MaxValue;
            }
            else
            {
                endTime = DateTime.Now.AddSeconds(time);
            }

            m_endTimes.AddOrUpdate(id, endTime, (key, val) => endTime);


            var guild = discord.GetGuildAsync(DiscordConstants.GuildId).Result;
            var member = guild.GetMemberAsync(id).Result;
            var memberRole = guild.GetRole(DiscordConstants.MemberRoleId);
            var punishRole = guild.GetRole(DiscordConstants.PunishRoleId);
            string reason = $"Stop user for {time} seconds.";

            // 회원 역할이 있어야 활정을 받을 수 있음.
            if (member.Roles.Count() > 0 && member.Roles.Any(role => role.Name == "회원"))
            {
                guild.RevokeRoleAsync(member, memberRole, reason);
                guild.GrantRoleAsync(member, punishRole, reason);
            }


            SavePunishments();
        }

        private void ReleaseUser(DiscordClient discord, ulong id)
        {
            if (m_endTimes.ContainsKey(id))
            {
                m_endTimes.TryRemove(id, out DateTime _);
            }


            var guild = discord.GetGuildAsync(DiscordConstants.GuildId).Result;
            var member = guild.GetMemberAsync(id).Result;
            var memberRole = guild.GetRole(DiscordConstants.MemberRoleId);
            var punishRole = guild.GetRole(DiscordConstants.PunishRoleId);
            string reason = "Release the user.";

            // 정지 역할이 있어야 활정을 풀 수 있음.
            if (member.Roles.Count() > 0 && member.Roles.Any(role => role.Name == "정지"))
            {
                guild.GrantRoleAsync(member, memberRole, reason);
                guild.RevokeRoleAsync(member, punishRole, reason);
            }


            SavePunishments();
        }

        private int UpdateSpamGage(ulong id, DateTimeOffset timestamp)
        {
            // TODO: 
            return 0;
        }

        private string TimeToText(int time)
        {
            if (time < 0)
            {
                return "무기한";
            }
            else
            {
                if (time < 60)
                {
                    return $"{time}초";
                }
                else if (time < 60 * 60)
                {
                    return $"{time / 60}분";
                }
                else
                {
                    return $"{time / 60 / 60}시간";
                }
            }
        }

        private void SavePunishments()
        {
            using (var bw = new BinaryWriter(new FileStream(PunishmentFile, FileMode.Create)))
            {
                var data = m_endTimes.ToArray();

                bw.Write(data.Length);

                foreach (var kv in data)
                {
                    bw.Write(kv.Key);
                    bw.Write(kv.Value.ToBinary());
                }

                bw.Close();
            }
        }

        private void LoadPunishments()
        {
            if (!File.Exists(PunishmentFile))
            {
                return;
            }

            using (var br = new BinaryReader(new FileStream(PunishmentFile, FileMode.Open)))
            {
                int cnt = br.ReadInt32();

                for (int i = 0; i < cnt; ++i)
                {
                    ulong id = br.ReadUInt64();
                    DateTime endTime = DateTime.FromBinary(br.ReadInt64());

                    m_endTimes.AddOrUpdate(id, endTime, (key, val) => endTime);
                }

                br.Close();
            }
        }
    }
}
