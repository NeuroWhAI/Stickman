using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace Stickman.MemoService
{
    public static class MemoManager
    {
        static MemoManager()
        {
            Directory.CreateDirectory(Root);
        }

        public static string Root { get; set; } = "Memo";

        private static Dictionary<string, Memo> m_dic = new Dictionary<string, Memo>();
        private static readonly object m_lockDic = new object();

        private static List<Memo> m_recentMemos = new List<Memo>();
        private static readonly object m_lockRecentMemos = new object();

        private static string ConvertTitle(string title)
        {
            return Regex.Replace(title,
                string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()))),
                "_");
        }

        public static string GetMemoPath(string title)
        {
            return Path.Combine(Root, ConvertTitle(title) + ".txt");
        }

        public static string GetRevisionPath(string title, int revision)
        {
            return Path.Combine(Root, $"{ConvertTitle(title)}.{revision}.rev");
        }

        public static Memo GetMemo(string title)
        {
            title = ConvertTitle(title);


            lock (m_lockDic)
            {
                if (m_dic.ContainsKey(title))
                {
                    return m_dic[title].Clone();
                }
            }


            string path = Path.Combine(Root, title + ".txt");

            if (File.Exists(path))
            {
                var memo = new Memo();

                lock (m_lockDic)
                {
                    memo.Load(path);
                    m_dic.Add(title, memo);
                }


                return memo.Clone();
            }


            return null;
        }

        public static int UpdateMemo(string title, string content)
        {
            var oldMemo = GetMemo(title);


            string rawTitle = title;
            title = ConvertTitle(title);

            string path = Path.Combine(Root, title + ".txt");
            string backupPath = string.Empty;


            if (oldMemo == null)
            {
                string lastBackupPath = Path.Combine(Root, $"{title}.bak");

                if (File.Exists(lastBackupPath))
                {
                    oldMemo = new Memo();

                    lock (m_lockDic)
                    {
                        oldMemo.Load(lastBackupPath);
                    }

                    if (oldMemo.Title != rawTitle)
                    {
                        oldMemo = null;
                    }
                }
            }

            if (oldMemo != null)
            {
                backupPath = Path.Combine(Root, $"{title}.{oldMemo.Revision}.rev");
            }


            var memo = new Memo
            {
                Title = rawTitle,
                Content = content,
                Revision = (oldMemo?.Revision ?? -1) + 1,
            };

            lock (m_lockDic)
            {
                if (oldMemo != null)
                {
                    oldMemo.Save(backupPath);
                }

                m_dic[title] = memo;
                memo.Save(path);

                memo.LastModifiedTime = File.GetLastWriteTimeUtc(path);
            }


            lock (m_lockRecentMemos)
            {
                m_recentMemos.Add(memo.Clone());

                if (m_recentMemos.Count > 20)
                {
                    m_recentMemos.RemoveAt(0);
                }
            }


            return memo.Revision;
        }

        public static bool AppendMemo(string title, string content)
        {
            var memo = GetMemo(title);

            if (memo != null)
            {
                UpdateMemo(title, memo.Content + content);

                return true;
            }

            return false;
        }

        public static void DeleteMemo(string title)
        {
            var memo = GetMemo(title);


            string rawTitle = title;
            title = ConvertTitle(title);

            string path = Path.Combine(Root, title + ".txt");


            lock (m_lockDic)
            {
                if (m_dic.ContainsKey(title))
                {
                    m_dic.Remove(title);
                }

                if (memo != null)
                {
                    string backupPath = Path.Combine(Root, $"{title}.bak");
                    memo.Save(backupPath);

                    string revPath = GetRevisionPath(rawTitle, memo.Revision);
                    memo.Save(revPath);
                }

                File.Delete(path);
            }
        }

        public static IEnumerable<string> ListMemo(string keyword)
        {
            return Directory.EnumerateFiles(Root, "*.txt")
                .Where((file) => Path.GetFileNameWithoutExtension(file).Contains(keyword))
                .Select(x => Path.GetFileNameWithoutExtension(x));
        }

        public static List<Memo> ListRecentMemo()
        {
            var temp = new List<Memo>();

            lock (m_lockRecentMemos)
            {
                temp.AddRange(m_recentMemos);
            }

            return temp;
        }

        public static bool RevertMemo(string title)
        {
            string rawTitle = title;
            var memo = GetMemo(title);


            title = ConvertTitle(title);

            if (memo == null)
            {
                string lastBackupPath = Path.Combine(Root, $"{title}.bak");

                if (File.Exists(lastBackupPath))
                {
                    memo = new Memo();

                    lock (m_lockDic)
                    {
                        memo.Load(lastBackupPath);

                        if (memo.Title == rawTitle)
                        {
                            var tempMemo = memo.Clone();
                            tempMemo.Revision += 1;
                            tempMemo.Content = " ";

                            string path = Path.Combine(Root, $"{title}.txt");
                            tempMemo.Save(path);
                        }
                    }

                    if (memo.Title == rawTitle)
                    {
                        if (string.IsNullOrWhiteSpace(memo.Content))
                        {
                            DeleteMemo(rawTitle);
                        }
                        else
                        {
                            UpdateMemo(rawTitle, memo.Content);
                        }

                        return true;
                    }
                }
            }
            else if (memo.Revision > 0)
            {
                string revPath = Path.Combine(Root, $"{title}.{memo.Revision - 1}.rev");

                if (File.Exists(revPath))
                {
                    var revMemo = new Memo();

                    lock (m_lockDic)
                    {
                        revMemo.Load(revPath);
                    }

                    if (revMemo.Title == rawTitle)
                    {
                        if (string.IsNullOrWhiteSpace(revMemo.Content))
                        {
                            DeleteMemo(rawTitle);
                        }
                        else
                        {
                            UpdateMemo(rawTitle, revMemo.Content);
                        }

                        return true;
                    }
                }
            }


            return false;
        }

        public static bool RevertMemo(string title, int revision)
        {
            var revMemo = GetRevision(title, revision);

            if (revMemo != null)
            {
                if (string.IsNullOrWhiteSpace(revMemo.Content))
                {
                    DeleteMemo(title);
                }
                else
                {
                    UpdateMemo(title, revMemo.Content);
                }

                return true;
            }


            return false;
        }

        public static Memo GetRevision(string title, int revision)
        {
            var memo = GetMemo(title);


            string rawTitle = title;
            title = ConvertTitle(title);

            string revPath = Path.Combine(Root, $"{title}.{revision}.rev");

            if (memo != null && memo.Title == rawTitle && memo.Revision == revision)
            {
                return memo;
            }

            if (File.Exists(revPath))
            {
                var revMemo = new Memo();

                lock (m_lockDic)
                {
                    revMemo.Load(revPath);
                }

                if (revMemo.Title == rawTitle)
                {
                    return revMemo;
                }
            }


            return null;
        }
    }
}
