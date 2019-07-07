using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

        [Command("js")]
        [Description("스크립트 실행.")]
        public async Task EvalScript(CommandContext ctx,
            [RemainingText, Description("실행할 코드.")]
            string code)
        {
            await ctx.TriggerTypingAsync();

            await ctx.RespondAsync(JsEngine.Evaluate(code, TimeSpan.FromSeconds(5)));
        }
    }
}
