using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Stickman.Command;

namespace Stickman.MemberService
{
    public class ProfileService : IBotService
    {
        public ProfileService(string id, string botId)
        {
            GlobalMessenger.RegisterReceiver(botId, (type, param) =>
            {
                if (type == "NewMessage" && param is MessageCreateEventArgs e)
                {
                    if (e.Guild != null)
                    {
                        EarnExp(e.Author.Id, e.Channel.Name, e.Message.Content);
                    }
                }
            });

            GlobalMessenger.RegisterReceiver(id, (type, param) =>
            {
                switch (type)
                {
                    case "ShowProfile":
                        if (param is CommandContextAdv ctxShow)
                        {
                            ShowProfile(ctxShow.Context);
                        }
                        break;

                    case "EditProfile":
                        if (param is CommandContextAdv<string> ctxEdit)
                        {
                            EditProfile(ctxEdit.Context, ctxEdit.Argument);
                        }
                        break;
                }
            });
        }

        private UserManager m_userManager = new UserManager("User");
        private DateTime m_saveTime;

        public void InitService(DiscordClient discord)
        {
            m_saveTime = DateTime.Now;
        }

        public void DisposeService(DiscordClient discord)
        {
            m_userManager.SaveChanges();
        }

        public void UpdateService(DiscordClient discord)
        {
            if (DateTime.Now > m_saveTime)
            {
                m_userManager.SaveChanges();

                m_saveTime = DateTime.Now + TimeSpan.FromMinutes(5.0);
            }
        }

        private void EarnExp(ulong userId, string channel, string content)
        {
            if (channel == "회원-인증")
            {
                return;
            }


            int exp = 1;

            if (channel == "작품" && content.Contains("cafe.naver.com/powdertoy/"))
            {
                exp = 500;
            }
            else if (channel == "질문")
            {
                exp = 10;
            }
            else if (channel == "일반")
            {
                exp = 4;
            }

            m_userManager.AddExp(userId, exp);
        }

        private void ShowProfile(CommandContext ctx)
        {
            DiscordUser target = ctx.Message.Author;

            if (ctx.Message.MentionedUsers.Count > 0)
            {
                target = ctx.Message.MentionedUsers[0];
            }

            var profile = m_userManager.GetProfileCopy(target.Id);


            var embed = new DiscordEmbedBuilder()
                .WithTitle(target.Username)
                .WithDescription(string.IsNullOrWhiteSpace(profile.Description) ? "\u200B" : profile.Description)
                .WithColor(new DiscordColor(255, 224, 160))
                .WithThumbnail(target.AvatarUrl)
                .AddField("Level", $"{profile.Level} ({profile.Exp}/{m_userManager.MaxExp} Exp)");

            ctx.RespondAsync(embed: embed.Build()).Wait();
        }

        private void EditProfile(CommandContext ctx, string description)
        {
            ulong userId = ctx.Message.Author.Id;

            m_userManager.EditDescription(userId, description);

            ctx.RespondAsync("Edited!").Wait();
        }
    }
}
