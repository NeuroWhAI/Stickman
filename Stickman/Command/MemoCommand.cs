using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Stickman.MemoService;

namespace Stickman.Command
{
    public class MemoCommand
    {
        [Command("memo")]
        [Description("메모를 보거나 편집합니다.")]
        public async Task Memo(CommandContext ctx,
            [Description("메모 이름. 공백 문자가 있는 경우 큰따옴표로 감싸세요.")]
            string title,
            [RemainingText, Description("설정할 메모 내용. 비워두면 이름에 해당하는 메모를 봅니다.")]
            string content = null)
        {
            await ctx.TriggerTypingAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                var steps = new List<string>();

                while (steps.Contains(title) == false)
                {
                    var memo = MemoManager.GetMemo(title);

                    if (memo == null)
                    {
                        await ctx.RespondAsync("Not exists!");

                        break;
                    }
                    else
                    {
                        string redirectionCmd = "!redirect ";

                        if (memo.Content.StartsWith(redirectionCmd))
                        {
                            steps.Add(title);

                            title = memo.Content.Substring(redirectionCmd.Length);
                        }
                        else
                        {
                            await ctx.RespondAsync(embed: memo.Build());

                            break;
                        }
                    }
                }
            }
            else
            {
                int revision = MemoManager.UpdateMemo(title, content.Trim());

                if (revision < 0)
                {
                    await ctx.RespondAsync("Error!");
                }
                else if (revision == 0)
                {
                    await ctx.RespondAsync("Created!");
                }
                else
                {
                    await ctx.RespondAsync("Updated!");
                }
            }
        }

        [Command("delmemo")]
        [Description("메모를 삭제합니다.")]
        public async Task DeleteMemo(CommandContext ctx,
            [Description("삭제할 메모 이름.")]
            string title)
        {
            await ctx.TriggerTypingAsync();

            MemoManager.DeleteMemo(title);

            await ctx.RespondAsync("Deleted!");
        }

        [Command("append")]
        [Description("메모 내용을 추가합니다.")]
        public async Task AppendMemo(CommandContext ctx,
            [Description("메모 이름.")]
            string title,
            [RemainingText, Description("추가할 내용.")]
            string content)
        {
            await ctx.TriggerTypingAsync();

            bool result = MemoManager.AppendMemo(title, content);

            await ctx.RespondAsync(result ? "Appended!" : "Not exists!");
        }

        [Command("appendln")]
        [Description("메모에 새로운 줄을 만들고 내용을 추가합니다.")]
        public async Task AppendLineMemo(CommandContext ctx,
            [Description("메모 이름.")]
            string title,
            [RemainingText, Description("추가할 내용.")]
            string content)
        {
            await ctx.TriggerTypingAsync();


            if (content.StartsWith("```"))
            {
                if (content.Length > 6)
                {
                    content = content.Substring(3, content.Length - 6).Trim();
                }
            }

            bool result = MemoManager.AppendMemo(title, "\n" + content);

            await ctx.RespondAsync(result ? "Appended!" : "Not exists!");
        }

        [Command("rawmemo")]
        [Description("메모의 원본 내용을 봅니다.")]
        public async Task RawMemo(CommandContext ctx,
            [Description("메모 이름.")]
            string title)
        {
            await ctx.TriggerTypingAsync();

            var memo = MemoManager.GetMemo(title);

            if (memo == null)
            {
                await ctx.RespondAsync("Not exists!");
            }
            else
            {
                string path = Path.GetTempFileName();

                try
                {
                    File.WriteAllText(path + ".txt", memo.Content);

                    await ctx.RespondWithFileAsync(path + ".txt",
                        string.Format("```\n{0}\n```", memo.Content));
                }
                finally
                {
                    File.Delete(path);
                    File.Delete(path + ".txt");
                }
            }
        }

        [Command("memos")]
        [Description("메모를 검색합니다.")]
        public async Task ListMemo(CommandContext ctx,
            [RemainingText, Description("검색어.")]
            string keyword)
        {
            await ctx.TriggerTypingAsync();

            var memos = MemoManager.ListMemo(keyword);


            var text = new StringBuilder("```\n");

            int cnt = 0;
            foreach (var title in memos)
            {
                text.AppendLine(title);

                ++cnt;
                if (cnt >= 20)
                {
                    text.AppendLine("...");
                    break;
                }
            }

            text.AppendLine("```");


            if (cnt > 0)
            {
                await ctx.RespondAsync(text.ToString());
            }
            else
            {
                await ctx.RespondAsync("Not exists!");
            }
        }

        [Command("recent")]
        [Description("최근에 수정된 메모를 검색합니다.")]
        public async Task ListRecentMemo(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var memos = MemoManager.ListRecentMemo();


            var text = new StringBuilder("```\n");

            foreach (var memo in memos)
            {
                var time = memo.LastModifiedTime;
                time = time.AddHours(9.0); // To UTC+9

                text.Append($"{memo.Title} (rev{memo.Revision}) - ");
                text.Append(time.ToShortDateString());
                text.Append(" ");
                text.AppendLine(time.ToShortTimeString());
            }

            text.Append("```");


            if (memos.Count > 0)
            {
                await ctx.RespondAsync(text.ToString());
            }
            else
            {
                await ctx.RespondAsync("Not exists!");
            }
        }

        [Command("revision"), Aliases("rev")]
        [Description("메모의 특정 리비전 원본 내용을 봅니다.")]
        public async Task RevisionMemo(CommandContext ctx,
            [Description("메모 이름.")]
            string title,
            [Description("볼 리비전 번호.")]
            int revision)
        {
            await ctx.TriggerTypingAsync();

            var memo = MemoManager.GetRevision(title, revision);

            if (memo == null)
            {
                await ctx.RespondAsync("Not exists!");
            }
            else
            {
                string path = Path.GetTempFileName();

                try
                {
                    File.WriteAllText(path + ".txt", memo.Content);

                    await ctx.RespondWithFileAsync(path + ".txt",
                        string.Format("rev{0}\n```\n{1}\n```", revision, memo.Content));
                }
                finally
                {
                    File.Delete(path);
                    File.Delete(path + ".txt");
                }
            }
        }

        [Command("revert")]
        [Description("메모를 이전 혹은 특정 리비전으로 되돌립니다.")]
        public async Task RevertMemo(CommandContext ctx,
            [Description("메모 이름.")]
            string title,
            [Description("리비전 번호. 비워두면 이전 리비전으로 되돌립니다.")]
            int revision = -1)
        {
            await ctx.TriggerTypingAsync();

            bool success = false;

            if (revision < 0)
            {
                success = MemoManager.RevertMemo(title);
            }
            else
            {
                success = MemoManager.RevertMemo(title, revision);
            }

            if (success)
            {
                await ctx.RespondAsync("Success!");
            }
            else
            {
                await ctx.RespondAsync("Fail!");
            }
        }
    }
}
