using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
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

        [Command("welcome")]
        [Description("회원 자격을 부여합니다.")]
        [RequireRolesAttribute("스탭")]
        public async Task WelcomeUser(CommandContext ctx,
           [RemainingText, Description("대상 멤버 언급.")]
            string dummy)
        {
            await ctx.TriggerTypingAsync();

            var users = ctx.Message.MentionedUsers;

            if (users.Count <= 0)
            {
                await ctx.RespondAsync("자격을 부여할 멤버들을 함께 언급해주세요.");
            }
            else
            {
                var role = ctx.Guild.GetRole(596712853268987916ul); // "회원" 역할.

                foreach (var user in users)
                {
                    var member = await ctx.Guild.GetMemberAsync(user.Id);
                    await ctx.Guild.GrantRoleAsync(member, role, "Welcome!");
                }

                await ctx.RespondAsync("완료!");
            }
        }
    }
}
