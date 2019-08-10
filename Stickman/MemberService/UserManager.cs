using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Stickman.MemberService
{
    public class UserManager
    {
        public UserManager(string root)
        {
            Root = root;
            Directory.CreateDirectory(root);
        }

        private readonly string Root;

        public readonly int MaxExp = 1000;

        private Dictionary<ulong, UserCache> m_cache = new Dictionary<ulong, UserCache>();
        private readonly object m_syncCache = new object();

        public UserProfile GetProfileCopy(ulong id)
        {
            lock (m_syncCache)
            {
                return GetUser(id).GetCopy();
            }
        }

        public void AddExp(ulong id, int exp)
        {
            if (exp <= 0)
            {
                return;
            }

            lock (m_syncCache)
            {
                var user = GetUser(id);

                user.Exp += exp;
                if (user.Exp >= MaxExp)
                {
                    user.Level += (ulong)(user.Exp / MaxExp);
                    user.Exp %= MaxExp;
                }
            }
        }

        public void EditDescription(ulong id, string description)
        {
            lock (m_syncCache)
            {
                var user = GetUser(id);

                user.Description = description;
            }
        }

        public void SaveChanges()
        {
            lock (m_syncCache)
            {
                foreach (var user in m_cache.Values)
                {
                    if (!user.Saved)
                    {
                        user.Save(Path.Combine(Root, user.Id + ".dat"));

                        user.Saved = true;
                    }
                }
            }
        }

        private UserCache GetUser(ulong id)
        {
            UserCache cache = null;

            if (m_cache.ContainsKey(id))
            {
                cache = m_cache[id];
            }
            else
            {
                string filename = Path.Combine(Root, id + ".dat");

                var profile = new UserProfile(id);

                if (File.Exists(filename))
                {
                    profile.Load(filename);
                }
                else
                {
                    profile.Save(filename);
                }

                cache = new UserCache(profile);

                m_cache.Add(id, cache);
            }

            return cache;
        }
    }
}
