using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Stickman.Command
{
    class KeywordCommand
    {
        [Command("keyword")]
        [Description("키워드 알림 설정.")]
        public async Task QueryKeyword(CommandContext ctx,
            [RemainingText, Description("추가할 키워드. 이미 존재하면 삭제하며 생략시 목록을 보여줍니다.")]
            string keyword = "")
        {
            await ctx.TriggerTypingAsync();

            await GlobalMessenger.PushMessage("Keyword", "QueryKeyword",
                CommandContextAdv.Create(ctx, keyword));
        }
    }
}
