using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using Stickman.Command;

namespace Stickman.KeywordService
{
    public class KeywordService : IBotService
    {
        public KeywordService(string id, string botId)
        {
            m_manager = new KeywordManager(id);

            GlobalMessenger.RegisterReceiver(botId, (type, param) =>
            {
                if (type == "NewMessage" && param is MessageCreateEventArgs e)
                {
                    string msg = e.Message.Content;

                    // 서버 메세지이며
                    // 봇이 아니고
                    // 멘션이 없으면
                    if (e.Guild != null && !e.Channel.IsPrivate
                        && !e.Author.IsBot
                        && !e.MentionedUsers.Any())
                    {
                        ulong userId = e.Author.Id;
                        ulong channelId = e.Channel.Id;

                        // 자기 자신은 제외하고 해당 채널을 볼 권한이 있는 사람만 필터링.
                        var keywordUsers = from keywordUser in m_manager.CheckKeywordIn(msg)
                                           where userId != keywordUser
                                               && e.Channel.PermissionsFor(e.Guild.GetMemberAsync(keywordUser).Result).HasPermission(Permissions.AccessChannels)
                                           select keywordUser;

                        lock (m_syncQueue)
                        {
                            string noti = $"[{e.Guild.Name} : {e.Channel.Name}] {e.Author.Username}\n\"{msg.Trim()}\"";
                            foreach (ulong targetUser in keywordUsers)
                            {
                                m_queue.Add((targetUser, noti));
                            }
                        }
                    }
                }
            });

            GlobalMessenger.RegisterReceiver(id, (type, param) =>
            {
                switch (type)
                {
                    case "QueryKeyword":
                        if (param is CommandContextAdv<string> ctxQuery)
                        {
                            var ctx = ctxQuery.Context;
                            
                            string res = QueryKeyword(ctx.User.Id, ctxQuery.Argument);

                            ctx.RespondAsync(res);
                        }
                        break;
                }
            });
        }

        private readonly KeywordManager m_manager;
        private readonly List<(ulong, string)> m_queue = new List<(ulong, string)>();
        private readonly object m_syncQueue = new object();

        public void InitService(DiscordClient discord)
        {
            
        }

        public void DisposeService(DiscordClient discord)
        {
            m_manager.Clear();
        }

        public void UpdateService(DiscordClient discord)
        {
            (ulong, string)[] queue = null;

            lock (m_syncQueue)
            {
                if (m_queue.Count > 0)
                {
                    queue = m_queue.ToArray();
                    m_queue.Clear();
                }
            }


            if (queue != null)
            {
                var jobs = new List<Task>();

                foreach (var (user, msg) in queue)
                {
                    var task = Task.Factory.StartNew(async () =>
                    {
                        var dm = await discord.CreateDmAsync(await discord.GetUserAsync(user));
                        await dm.SendMessageAsync(msg);
                    });
                    jobs.Add(task);
                }

                Task.WaitAll(jobs.ToArray());
            }
        }

        private string QueryKeyword(ulong id, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                // 목록
                var list = m_manager.GetKeywords(id);

                if (list != null && list.Length > 0)
                {
                    var buffer = new StringBuilder("```");

                    foreach (string key in list)
                    {
                        buffer.AppendLine(key);
                    }

                    buffer.Append("```");

                    return buffer.ToString();
                }
                else
                {
                    return "Empty!";
                }
            }
            else if (m_manager.Contains(id, keyword))
            {
                // 삭제
                m_manager.Remove(id, keyword);
                return "Removed!";
            }
            else
            {
                // 추가
                if (m_manager.KeywordCount(id) < 10)
                {
                    m_manager.Add(id, keyword);
                    return "Added!";
                }
                else
                {
                    return "Count limit reached!";
                }
            }
        }
    }
}
