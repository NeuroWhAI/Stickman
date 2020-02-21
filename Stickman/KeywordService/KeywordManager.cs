using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace Stickman.KeywordService
{
    public class KeywordManager
    {
        public KeywordManager(string rootFolder)
        {
            RootFolder = rootFolder;

            if (Directory.Exists(rootFolder) == false)
            {
                Directory.CreateDirectory(rootFolder);
            }

            LoadAll();
        }

        public readonly string RootFolder;

        private ConcurrentDictionary<ulong, KeywordSet> m_keywordDic = new ConcurrentDictionary<ulong, KeywordSet>();

        private KeywordSet GetKeywordList(ulong user)
        {
            if (m_keywordDic.TryGetValue(user, out var set))
            {
                return set;
            }

            return null;
        }

        private void LoadAll()
        {
            Clear();

            foreach (string fileName in Directory.EnumerateFiles(RootFolder, "*.dat"))
            {
                var list = new KeywordSet();
                list.Load(fileName, out ulong user);

                m_keywordDic.AddOrUpdate(user, list, (key, val) => list);
            }
        }

        public void Clear()
        {
            foreach (var list in m_keywordDic.Values)
            {
                list.Dispose();
            }

            m_keywordDic.Clear();
        }

        public bool Contains(ulong user, string keyword)
        {
            var list = GetKeywordList(user);
            if (list != null)
            {
                return list.Contains(keyword);
            }

            return false;
        }

        public int KeywordCount(ulong user)
        {
            var list = GetKeywordList(user);
            if (list != null)
            {
                return list.Count;
            }

            return 0;
        }

        public void Add(ulong user, string keyword)
        {
            var list = m_keywordDic.GetOrAdd(user, (_) => new KeywordSet());

            if (list.Add(keyword))
            {
                list.Save(Path.Combine(RootFolder, user + ".dat"), user);
            }
        }

        public void Remove(ulong user, string keyword)
        {
            var list = GetKeywordList(user);
            if (list != null)
            {
                if (list.Remove(keyword))
                {
                    list.Save(Path.Combine(RootFolder, user + ".dat"), user);
                }
            }
        }

        public string[] GetKeywords(ulong user)
        {
            var list = GetKeywordList(user);
            if (list != null)
            {
                return list.ToArray();
            }

            return null;
        }

        public IEnumerable<ulong> CheckKeywordIn(string text)
        {
            List<ulong> userList = null;

            foreach (var kv in m_keywordDic)
            {
                ulong user = kv.Key;
                var list = kv.Value;

                if (list.CheckKeywordIn(text))
                {
                    if (userList == null)
                    {
                        userList = new List<ulong>();
                    }

                    userList.Add(user);

                    continue;
                }
            }

            return userList ?? Enumerable.Empty<ulong>();
        }
    }
}
