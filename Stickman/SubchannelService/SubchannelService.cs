using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Stickman.Command;

namespace Stickman.SubchannelService
{
    class SubchannelService : IBotService
    {
        public SubchannelService(string id)
        {
            GlobalMessenger.RegisterReceiver(id, (type, param) =>
            {
                CommandContext ctx = null;
                string response = null;

                switch (type)
                {
                    case "CreateChannel":
                        if (param is CommandContextAdv<Tuple<string, string>> ctxCreate
                            && ctxCreate.Context.Guild != null)
                        {
                            ctx = ctxCreate.Context;
                            string name = ctxCreate.Argument.Item1;
                            string subject = ctxCreate.Argument.Item2;

                            response = CreateChannel(ctx.Guild, name, subject);
                        }
                        break;

                    case "ListChannels":
                        if (param is CommandContextAdv ctxList
                            && ctxList.Context.Guild != null)
                        {
                            ctx = ctxList.Context;
                            response = ListChannels(ctx.Guild);
                        }
                        break;

                    case "JoinChannel":
                        if (param is CommandContextAdv<string> ctxJoin
                            && ctxJoin.Context.Guild != null)
                        {
                            ctx = ctxJoin.Context;
                            string name = ctxJoin.Argument;

                            response = JoinChannel(ctx.Guild, ctx.User.Id, name);
                        }
                        break;

                    case "LeaveChannel":
                        if (param is CommandContextAdv<string> ctxLeave
                            && ctxLeave.Context.Guild != null)
                        {
                            ctx = ctxLeave.Context;
                            string name = ctxLeave.Argument;

                            response = LeaveChannel(ctx.Guild, ctx.User.Id, name);
                        }
                        break;

                    case "ReopenChannel":
                        if (param is CommandContextAdv<string> ctxOpen
                            && ctxOpen.Context.Guild != null)
                        {
                            ctx = ctxOpen.Context;
                            string name = ctxOpen.Argument;

                            response = ReopenChannel(name);
                        }
                        break;
                }

                if (ctx != null && !string.IsNullOrEmpty(response))
                {
                    ctx.RespondAsync(response).Wait();
                }
            });
        }

        private readonly string ROLE_PREFIX = "Sub-";

        public void InitService(DiscordClient discord)
        {
            
        }

        public void DisposeService(DiscordClient discord)
        {
            
        }

        public void UpdateService(DiscordClient discord)
        {
            // TODO: 오랫동안 대화가 없는 서브 채널은 휴면 상태로 전환.
        }

        private string CreateChannel(DiscordGuild guild, string name, string subject)
        {
            var group = guild.GetChannel(DiscordConstants.SubchannelGroupId);

            if (group.Children.Any((chan) => chan.Name == name))
            {
                return "해당 이름의 채널이 이미 존재합니다.";
            }


            // 채널 전용 역할 생성.
            DiscordRole chanRole = null;
            try
            {
                chanRole = guild.CreateRoleAsync(ROLE_PREFIX + name, color: DiscordColor.Grayple, mentionable: true).Result;
            }
            catch (Exception e)
            {
                return e.Message;
            }

            // 서브 채널 생성.
            DiscordChannel subchannel = null;
            try
            {
                subchannel = guild.CreateChannelAsync(name, ChannelType.Text, parent: group).Result;
                subchannel.ModifyAsync(topic: subject).Wait();
            }
            catch (Exception e)
            {
                guild.DeleteRoleAsync(chanRole, "Fail to create a subchannel.").Wait();
                subchannel?.DeleteAsync("Fail to create a subchannel.").Wait();
                return e.Message;
            }

            // Private 채널로 만듦.
            try
            {
                var everyoneRole = guild.GetRole(DiscordConstants.EveryoneRoleId);
                subchannel.AddOverwriteAsync(everyoneRole, Permissions.None, Permissions.AccessChannels).Wait();
                subchannel.AddOverwriteAsync(chanRole, Permissions.AccessChannels, Permissions.None).Wait();
            }
            catch (Exception e)
            {
                guild.DeleteRoleAsync(chanRole, "Fail to initialize a subchannel.").Wait();
                subchannel.DeleteAsync("Fail to initialize a subchannel.").Wait();
                return e.Message;
            }


            return $@"'{name}' 채널이 생성되었습니다.
`?join {name}`으로 들어갈 수 있습니다.";
        }

        private string ListChannels(DiscordGuild guild)
        {
            var group = guild.GetChannel(DiscordConstants.SubchannelGroupId);

            if (group.Children.Count() > 0)
            {
                var buffer = new StringBuilder("[서브 채널 목록]\n");

                foreach (var chan in group.Children)
                {
                    buffer.AppendLine($"**{chan.Name}** : {chan.Topic}");
                }

                return buffer.ToString();
            }
            else
            {
                return "서브 채널이 없습니다.";
            }
        }

        private string JoinChannel(DiscordGuild guild, ulong userId, string name)
        {
            var subchannels = guild.GetChannel(DiscordConstants.SubchannelGroupId);

            var targetChan = (from chan in subchannels.Children
                             where chan.Name == name
                             select chan).FirstOrDefault();

            if (targetChan == null)
            {
                return "없는 채널입니다.";
            }


            var user = guild.GetMemberAsync(userId).Result;

            if (CheckUserSubchannelRole(user, name))
            {
                return "이미 참여한 채널입니다.";
            }


            string roleName = ROLE_PREFIX + name;

            var chanRole = (from role in guild.Roles
                           where role.Name == roleName
                           select role).FirstOrDefault();

            if (chanRole == null)
            {
                return "참가용 역할을 찾을 수 없습니다.";
            }


            user.GrantRoleAsync(chanRole, "Join the subchannel.").Wait();


            return "채널에 참여하였습니다.";
        }

        private string LeaveChannel(DiscordGuild guild, ulong userId, string name)
        {
            string roleName = ROLE_PREFIX + name;
            var user = guild.GetMemberAsync(userId).Result;

            var chanRole = (from role in user.Roles
                            where role.Name == roleName
                            select role).FirstOrDefault();

            if (chanRole == null)
            {
                return "참여하지 않은 채널입니다.";
            }


            user.RevokeRoleAsync(chanRole, "Leave the subchannel.").Wait();


            return "채널을 나갔습니다.";
        }

        private string ReopenChannel(string name)
        {
            throw new NotImplementedException();
        }

        private bool CheckUserSubchannelRole(DiscordMember user, string channelName)
        {
            return user.Roles.Any((role) => role.Name == ROLE_PREFIX + channelName);
        }
    }
}
