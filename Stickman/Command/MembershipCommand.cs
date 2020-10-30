using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Stickman.Command
{
    class MembershipCommand : BaseCommandModule
    {
        [Command("profile")]
        [Description("회원 프로필 보기.")]
        public async Task ShowProfile(CommandContext ctx,
            [RemainingText, Description("볼 회원 언급. 비워두면 자신의 프로필을 봅니다.")]
            string user = "")
        {
            await ctx.TriggerTypingAsync();

            await GlobalMessenger.PushMessage("Membership", "ShowProfile",
                CommandContextAdv.Create(ctx));
        }

        [Command("introduce")]
        [Description("프로필 수정하기.")]
        public async Task EditProfile(CommandContext ctx,
           [RemainingText, Description("프로필에 표시될 내용.")]
            string description)
        {
            await ctx.TriggerTypingAsync();

            await GlobalMessenger.PushMessage("Membership", "EditProfile",
                CommandContextAdv.Create(ctx, description ?? string.Empty));
        }

        [Command("welcome")]
        [Description("회원 자격을 부여합니다.")]
        [RequireRoles(RoleCheckMode.Any, "스탭")]
        public async Task WelcomeUser(CommandContext ctx,
           [RemainingText, Description("대상 멤버 언급.")]
            string mention)
        {
            await ctx.TriggerTypingAsync();

            var users = ctx.Message.MentionedUsers;

            if (users.Count <= 0)
            {
                await ctx.RespondAsync("자격을 부여할 멤버들을 함께 언급해주세요.");
            }
            else
            {
                var role = ctx.Guild.GetRole(DiscordConstants.MemberRoleId); // "회원" 역할.

                foreach (var user in users)
                {
                    var member = await ctx.Guild.GetMemberAsync(user.Id);
                    await member.GrantRoleAsync(role, "Welcome!");
                }

                await ctx.RespondAsync("완료!");
            }
        }

        [Command("punish")]
        [Description("특정 멤버를 일정 기간 정지시킵니다.")]
        [RequireRoles(RoleCheckMode.Any, "스탭")]
        public async Task PunishUser(CommandContext ctx,
           [Description("대상 멤버 언급.")]
            string mention,
           [Description("정지시킬 시간(초). 음수일 경우 무기한 정지합니다.")]
            int time)
        {
            await ctx.TriggerTypingAsync();

            var users = ctx.Message.MentionedUsers;

            if (users.Count == 1)
            {
                await GlobalMessenger.PushMessage("Judge", "PunishUser",
                    CommandContextAdv.Create(ctx, time));

                await ctx.RespondAsync("처리 완료.");
            }
            else
            {
                await ctx.RespondAsync("대상을 하나만 지정해주세요.");
            }
        }

        [Command("release")]
        [Description("특정 멤버의 정지를 해제합니다.")]
        [RequireRoles(RoleCheckMode.Any, "스탭")]
        public async Task ReleaseUser(CommandContext ctx,
           [RemainingText, Description("대상 멤버 언급.")]
            string mention)
        {
            await ctx.TriggerTypingAsync();

            var users = ctx.Message.MentionedUsers;

            if (users.Count == 1)
            {
                await GlobalMessenger.PushMessage("Judge", "ReleaseUser",
                    CommandContextAdv.Create(ctx));

                await ctx.RespondAsync("처리 완료.");
            }
            else
            {
                await ctx.RespondAsync("대상을 하나만 지정해주세요.");
            }
        }
    }
}
