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
                    // 서버 메세지이며 봇이 아닌 경우만 확인.
                    if (e.Guild != null && !e.Author.IsBot)
                    {
                        ulong userId = e.Author.Id;
                        ulong channelId = e.Channel.Id;

                        int punishTime = UpdateSpamGage(userId, channelId, e.Message.Timestamp);

                        // 스팸으로 판단되고 멤버가 역할이 있고 스탭이 아니면 정지.
                        if (punishTime != 0)
                        {
                            // NOTE: 잦은 디스코드 API 호출을 피하기 위해 다중 if로 분리.

                            var member = e.Guild.GetMemberAsync(userId).Result;

                            if (member.Roles.Count() > 0
                                && member.Roles.All(role => role.Id != DiscordConstants.StaffRoleId))
                            {
                                // 정지 예정 큐에 넣음.
                                m_executionQue.Enqueue(Tuple.Create(userId, punishTime));
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
                        if (param is CommandContextAdv<int> ctxPunish)
                        {
                            var ctx = ctxPunish.Context;
                            var users = ctx.Message.MentionedUsers;

                            foreach (var user in users)
                            {
                                PunishUser(ctx.Client, user.Id, ctxPunish.Argument);
                            }
                        }
                        break;

                    case "ReleaseUser":
                        if (param is CommandContextAdv ctxRelease)
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
        private readonly object m_fileSyncObj = new object();

        private ConcurrentQueue<Tuple<ulong, int>> m_executionQue = new ConcurrentQueue<Tuple<ulong, int>>();
        private ConcurrentDictionary<ulong, DateTime> m_endTimes = new ConcurrentDictionary<ulong, DateTime>();

        private bool m_spamResetRequired = false;
        private ConcurrentDictionary<ulong, SpamUser> m_spamUsers = new ConcurrentDictionary<ulong, SpamUser>();
        private ConcurrentDictionary<ulong, SpamMessage> m_lastChannelMsg = new ConcurrentDictionary<ulong, SpamMessage>();
        private readonly TimeSpan m_maxSpamDelay = TimeSpan.FromSeconds(8.0);
        private readonly int m_spamGageScale = 100;
        private readonly int m_triggerPunishTime = 5;

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
            var now = DateTime.Now;


            // 활정 예약이 있으면 처리.
            while (!m_executionQue.IsEmpty)
            {
                if (m_executionQue.TryDequeue(out var data))
                {
                    ulong id = data.Item1;
                    int punishTime = data.Item2;

                    PunishUser(discord, id, punishTime);
                }
            }


            // 활정 끝났는지 확인하고 처리.
            var endTimes = m_endTimes.ToArray();
            foreach (var kv in endTimes)
            {
                ulong id = kv.Key;
                var endTime = kv.Value;

                if (now >= endTime)
                {
                    ReleaseUser(discord, id);
                }
            }


            // 일정 기간마다 가중 처벌 정보를 리셋함.
            if (now.DayOfWeek == DayOfWeek.Sunday)
            {
                if (m_spamResetRequired)
                {
                    m_spamResetRequired = false;

                    m_spamUsers.Clear();
                    SavePunishments();
                }
            }
            else
            {
                m_spamResetRequired = true;
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

            // TODO: 아래 조건 유효하게 변경.
            // 회원 역할이 있어야 활정을 받을 수 있음.
            //if (member.Roles.Count() > 0 && member.Roles.Any(role => role.Name == "회원"))
            {
                member.GrantRoleAsync(punishRole, reason).Wait();
                member.RevokeRoleAsync(memberRole, reason).Wait();
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

            // TODO: 아래 조건 유효하게 변경.
            // 정지 역할이 있어야 활정을 풀 수 있음.
            //if (member.Roles.Count() > 0 && member.Roles.Any(role => role.Name == "정지"))
            {
                member.GrantRoleAsync(memberRole, reason).Wait();
                member.RevokeRoleAsync(punishRole, reason).Wait();
            }


            SavePunishments();
        }

        private int UpdateSpamGage(ulong userId, ulong channelId, DateTimeOffset timestamp)
        {
            // 조용, 인증 채널에선 도배 판정하지 않음.
            if (channelId == DiscordConstants.QuietChannelId
                || channelId == DiscordConstants.AuthChannelId)
            {
                return 0;
            }


            var newMsg = new SpamMessage
            {
                Author = userId,
                CreationTime = timestamp,
            };


            if (m_lastChannelMsg.TryGetValue(channelId, out SpamMessage prevMsg))
            {
                // 가장 최근의 메세지로 갱신.
                m_lastChannelMsg.AddOrUpdate(channelId, newMsg, (key, val) =>
                {
                    return (newMsg.CreationTime > val.CreationTime) ? newMsg : val;
                });


                if (userId == prevMsg.Author)
                {
                    var delay = timestamp - prevMsg.CreationTime;

                    if (delay >= m_maxSpamDelay)
                    {
                        // 스팸 게이지 초기화.
                        m_spamUsers.AddOrUpdate(userId, SpamUser.Empty, (key, usr) => usr.ResetSpamGage());
                    }
                    else
                    {
                        if (timestamp == prevMsg.CreationTime)
                        {
                            // 시간 해상도가 1초이므로 시간이 같다는 것은 시간차가 최대 1초라는 것.
                            // 따라서 충분히 작은 시간으로 딜레이를 가정한다.
                            delay = TimeSpan.FromSeconds(0.01);
                        }

                        // 기존 유저 정보 얻음.
                        SpamUser spamUser;
                        if (!m_spamUsers.TryGetValue(userId, out spamUser))
                        {
                            spamUser = new SpamUser();
                        }

                        // 스팸 수치 계산.
                        int spamGage = (int)Math.Round((1.0 - (delay / m_maxSpamDelay)) * m_spamGageScale);
                        spamUser.SpamGage += spamGage;

                        int punishTime = spamUser.SpamGage / m_spamGageScale;

                        // 처벌 시간이 트리거에 도달하면 처벌이 이뤄지고 가중되도록 카운트를 증가시킴.
                        if (punishTime >= m_triggerPunishTime)
                        {
                            spamUser.PunishCount += 1;
                        }

                        // 변경된 유저 정보 갱신.
                        m_spamUsers.AddOrUpdate(userId, spamUser, (key, old) =>
                        {
                            return (spamUser.PunishCount >= old.PunishCount) ? spamUser : old;
                        });

                        if (punishTime >= m_triggerPunishTime)
                        {
                            // 가중 처벌이 계산된 처벌 시간 반환.
                            return punishTime * spamUser.PunishCount * spamUser.PunishCount;
                        }
                    }
                }
                else
                {
                    // 대화 중인 것으로 판정하여 도배로 보지 않음.
                    m_spamUsers.AddOrUpdate(userId, SpamUser.Empty, (key, usr) => usr.ResetSpamGage());
                    m_spamUsers.AddOrUpdate(prevMsg.Author, SpamUser.Empty, (key, usr) => usr.ResetSpamGage());
                }
            }
            else
            {
                // 가장 최근의 메세지로 초기화.
                m_lastChannelMsg.AddOrUpdate(channelId, newMsg, (key, val) =>
                {
                    return (newMsg.CreationTime > val.CreationTime) ? newMsg : val;
                });
            }


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
            lock (m_fileSyncObj)
            {
                using (var bw = new BinaryWriter(new FileStream(PunishmentFile, FileMode.Create)))
                {
                    var data = m_endTimes.ToArray();

                    bw.Write(data.Length);

                    foreach (var kv in data)
                    {
                        ulong id = kv.Key;
                        var endTime = kv.Value;

                        bw.Write(id);
                        bw.Write(endTime.ToBinary());

                        if (m_spamUsers.TryGetValue(id, out SpamUser user))
                        {
                            bw.Write(user.PunishCount);
                        }
                        else
                        {
                            bw.Write(0);
                        }
                    }

                    bw.Close();
                }
            }
        }

        private void LoadPunishments()
        {
            if (!File.Exists(PunishmentFile))
            {
                return;
            }

            lock (m_fileSyncObj)
            {
                using (var br = new BinaryReader(new FileStream(PunishmentFile, FileMode.Open)))
                {
                    int cnt = br.ReadInt32();

                    for (int i = 0; i < cnt; ++i)
                    {
                        ulong id = br.ReadUInt64();
                        DateTime endTime = DateTime.FromBinary(br.ReadInt64());
                        m_endTimes.AddOrUpdate(id, endTime, (key, val) => endTime);

                        int punishCount = br.ReadInt32();
                        var spamUser = new SpamUser { PunishCount = punishCount };
                        m_spamUsers.AddOrUpdate(id, spamUser, (key, val) => spamUser);
                    }

                    br.Close();
                }
            }
        }
    }
}
