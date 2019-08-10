using System;
using System.Collections.Generic;
using System.Text;

namespace Stickman.MemberService
{
    public class UserCache
    {
        public UserCache(UserProfile profile, bool saved = true)
        {
            Saved = saved;
            m_profile = profile;
        }

        public bool Saved { get; set; }

        private UserProfile m_profile;
        public ulong Id => m_profile.Id;
        public string Description
        {
            get => m_profile.Description;
            set
            {
                Saved = false;
                m_profile.Description = value;
            }
        }
        public ulong Level
        {
            get => m_profile.Level;
            set
            {
                Saved = false;
                m_profile.Level = value;
            }
        }
        public int Exp
        {
            get => m_profile.Exp;
            set
            {
                Saved = false;
                m_profile.Exp = value;
            }
        }

        public UserProfile GetCopy() => m_profile.GetCopy();
        public void Save(string filename) => m_profile.Save(filename);
    }
}
