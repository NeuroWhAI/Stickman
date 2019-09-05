using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;

namespace Stickman.Command
{
    class SubchannelCommand
    {
        [Command("channel")]
        [Description("서브 채널을 생성합니다.")]
        public async Task CreateChannel(CommandContext ctx,
            [RemainingText, Description("채널 이름. 10자 이하이며 영어, 한글 등의 문자와 붙임표(-)만 허용됩니다.")]
            string name = "")
        {
            name = name.Trim().Replace(' ', '-');

            await ctx.TriggerTypingAsync();


            if (ctx.Guild == null)
            {
                await ctx.RespondAsync("서버에서 명령을 사용해주세요.");
                return;
            }


            if (string.IsNullOrWhiteSpace(name))
            {
                await ctx.RespondAsync("채널 이름은 공백일 수 없습니다.");
                return;
            }
            else if (name.Length > 10)
            {
                await ctx.RespondAsync("채널 이름의 길이가 제한을 초과하였습니다.");
                return;
            }
            else if (!name.ToCharArray().All((ch) => ch == '-' || char.IsLetterOrDigit(ch)))
            {
                await ctx.RespondAsync("채널 이름에 유효하지 않은 기호가 있습니다.");
                return;
            }


            await ctx.RespondAsync($@"'{name}' 채널의 주제는 무엇으로 할까요?
이 정보는 `channels` 명령을 통해 다른 사람이 볼 수 있습니다.
무엇을 하는 채널인지 쉽게 알 수 있도록 간단명료하게 작성해주세요.");

            var interactivity = ctx.Client.GetInteractivityModule();
            var msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(10));

            if (msg == null)
            {
                await ctx.RespondAsync($"'{name}' 채널 생성이 취소되었습니다.");
                return;
            }


            string subject = msg.Message.Content.Replace('\n', ' ');

            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync($@"입력하신 정보가 맞나요? (Y/N)
채널명 : '{name}'
주제 : '{subject}'");

            msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(2));

            if (msg == null)
            {
                await ctx.RespondAsync($"'{name}' 채널 생성이 취소되었습니다.");
                return;
            }
            else
            {
                string res = msg.Message.Content.ToLower();

                var yesList = new[] { "y", "yes", "ㅇ", "ㅇㅇ", "네", "예", "응", "어", "넹", "넵", "그래" };

                if (!yesList.Contains(res))
                {
                    await ctx.RespondAsync($"'{name}' 채널 생성이 취소되었습니다.");
                    return;
                }
            }


            // 요청자가 스탭이 아니면 스탭의 허락을 구함.
            if (!ctx.Member.Roles.Any((role) => role.Id == DiscordConstants.StaffRoleId))
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"<@&{DiscordConstants.StaffRoleId}>" + "의 수락을 기다리는 중입니다. (Y/N)");

                msg = await interactivity.WaitForMessageAsync(xm =>
                {
                    // 요청한 채널에서
                    if (xm.ChannelId == ctx.Channel.Id)
                    {
                        string res = xm.Content.ToLower();
                        if (res != "y" && res != "n")
                        {
                            return false;
                        }

                        // 스탭이 말을 했으면
                        var member = ctx.Guild.GetMemberAsync(xm.Author.Id).Result;
                        if (member.Roles.Any((role) => role.Id == DiscordConstants.StaffRoleId))
                        {
                            return true;
                        }
                    }

                    return false;
                }, TimeSpan.FromMinutes(30));

                if (msg == null)
                {
                    await ctx.RespondAsync($"'{name}' 채널 생성이 취소되었습니다.");
                    return;
                }
                else
                {
                    string res = msg.Message.Content.ToLower();

                    if (res == "y")
                    {
                        await ctx.RespondAsync("채널 생성이 수락되었습니다.");
                    }
                    else
                    {
                        await ctx.RespondAsync("채널 생성이 거부되었습니다.");
                        return;
                    }
                }
            }


            await ctx.TriggerTypingAsync();

            await GlobalMessenger.PushMessage("Subchannel", "CreateChannel",
                CommandContextAdv.Create(ctx, name, subject));
        }

        [Command("channels")]
        [Description("서브 채널 목록을 봅니다.")]
        public async Task ListChannels(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            if (ctx.Guild == null)
            {
                await ctx.RespondAsync("서버에서 명령을 사용해주세요.");
                return;
            }

            await GlobalMessenger.PushMessage("Subchannel", "ListChannels",
                CommandContextAdv.Create(ctx));
        }

        [Command("join")]
        [Description("서브 채널에 참여합니다.")]
        public async Task JoinChannel(CommandContext ctx,
           [RemainingText, Description("참여할 채널 이름.")]
            string name = "")
        {
            name = name.Trim().Replace(' ', '-');

            await ctx.TriggerTypingAsync();

            if (ctx.Guild == null)
            {
                await ctx.RespondAsync("서버에서 명령을 사용해주세요.");
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                await ctx.RespondAsync("채널 이름은 공백일 수 없습니다.");
                return;
            }

            await GlobalMessenger.PushMessage("Subchannel", "JoinChannel",
                CommandContextAdv.Create(ctx, name));
        }

        [Command("leave")]
        [Description("서브 채널에서 나갑니다.")]
        public async Task LeaveChannel(CommandContext ctx,
           [RemainingText, Description("나갈 채널 이름.")]
            string name = "")
        {
            name = name.Trim().Replace(' ', '-');

            await ctx.TriggerTypingAsync();

            if (ctx.Guild == null)
            {
                await ctx.RespondAsync("서버에서 명령을 사용해주세요.");
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                await ctx.RespondAsync("채널 이름은 공백일 수 없습니다.");
                return;
            }

            await GlobalMessenger.PushMessage("Subchannel", "LeaveChannel",
                CommandContextAdv.Create(ctx, name));
        }

        /*[Command("reopen")]
        [Description("휴면 상태로 전환된 서브 채널을 다시 활성화시킵니다.")]
        public async Task ReopenChannel(CommandContext ctx,
           [RemainingText, Description("채널 이름.")]
            string name = "")
        {
            name = name.Trim().Replace(' ', '-');

            await ctx.TriggerTypingAsync();

            if (ctx.Guild == null)
            {
                await ctx.RespondAsync("서버에서 명령을 사용해주세요.");
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                await ctx.RespondAsync("채널 이름은 공백일 수 없습니다.");
                return;
            }

            await GlobalMessenger.PushMessage("Subchannel", "ReopenChannel",
                CommandContextAdv.Create(ctx, name));
        }*/
    }
}
