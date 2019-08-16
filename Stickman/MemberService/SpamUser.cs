using System;
using System.Collections.Generic;
using System.Text;

namespace Stickman.MemberService
{
    struct SpamUser
    {
        public SpamUser(int spamGage = 0, int punishCount = 0)
        {
            SpamGage = spamGage;
            PunishCount = punishCount;
        }

        public static readonly SpamUser Empty = new SpamUser();

        public int SpamGage;
        public int PunishCount;

        public SpamUser ResetSpamGage()
        {
            SpamUser temp = this;
            temp.SpamGage = 0;

            return temp;
        }
    }
}
