using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Stickman.Utility;

namespace Stickman.Command
{
    public class BasicCommand
    {
        [Command("ping")]
        [Description("봇 테스트.")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync("pong!");
        }

        [Command("shutdown")]
        [Description("봇 종료.")]
        [RequireOwner, Hidden]
        public async Task Shutdown(CommandContext ctx)
        {
            await ctx.RespondAsync("Terminating...");

            await GlobalMessenger.PushMessage("Stickman", "Shutdown", string.Empty);
        }

        [Command("js")]
        [Description("스크립트 실행.")]
        public async Task EvalScript(CommandContext ctx,
            [RemainingText, Description("실행할 코드.")]
            string code)
        {
            await ctx.TriggerTypingAsync();

            await ctx.RespondAsync(JsEngine.Evaluate(code, TimeSpan.FromSeconds(3)));
        }

        [Command("status")]
        [Description("상태 추가.")]
        [RequireOwner, Hidden]
        public async Task AddStatus(CommandContext ctx,
            [RemainingText, Description("상태 메세지.")]
            string status)
        {
            await ctx.TriggerTypingAsync();

            await GlobalMessenger.PushMessage("BotStatus", "AddStatus", status);

            await ctx.RespondAsync("Added!");
        }

        [Command("delstatus")]
        [Description("상태 제거.")]
        [RequireOwner, Hidden]
        public async Task RemoveStatus(CommandContext ctx,
            [RemainingText, Description("상태 메세지.")]
            string status)
        {
            await ctx.TriggerTypingAsync();

            await GlobalMessenger.PushMessage("BotStatus", "RemoveStatus", status);

            await ctx.RespondAsync("Removed!");
        }

        [Command("pick")]
        [Description("무작위 항목 선택.")]
        public async Task PickRandom(CommandContext ctx,
            [RemainingText, Description("목록. 개행, 쉼표, 공백 순으로 분리 우선도 높음.")]
            string listStr)
        {
            if (string.IsNullOrWhiteSpace(listStr))
            {
                return;
            }

            await ctx.TriggerTypingAsync();

            char token = ' ';

            if (listStr.Contains('\n'))
            {
                token = '\n';
            }
            else if (listStr.Contains(','))
            {
                token = ',';
            }

            var list = listStr.Split(token).ToList();
            list.RemoveAll(s => string.IsNullOrWhiteSpace(s));

            if (list.Count > 0)
            {
                string choice = list[RandEngine.GetInt(0, list.Count)].Trim();
                await ctx.RespondAsync($"{ctx.User.Mention}님을 위한 선택: {choice}");
            }
        }
    }
}
