using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Stickman.Command
{
    class MembershipCommand
    {
        [Command("profile")]
        [Description("회원 프로필 보기.")]
        public async Task ShowProfile(CommandContext ctx,
            [RemainingText, Description("볼 회원 언급. 비워두면 자신의 프로필을 봅니다.")]
            string user = "")
        {
            await ctx.TriggerTypingAsync();

            await GlobalMessenger.PushMessage("Membership", "ShowProfile",
                new CommandContextAdv<Tuple<string>>(ctx, Tuple.Create(user)));
        }

        [Command("introduce")]
        [Description("프로필 수정하기.")]
        public async Task EditProfile(CommandContext ctx,
           [RemainingText, Description("프로필에 표시될 내용.")]
            string description)
        {
            await ctx.TriggerTypingAsync();

            await GlobalMessenger.PushMessage("Membership", "EditProfile",
                new CommandContextAdv<Tuple<string>>(ctx, Tuple.Create(description)));
        }
    }
}
