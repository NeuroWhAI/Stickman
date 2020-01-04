using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Stickman.MemberService
{
    public class UserProfile
    {
        public UserProfile(ulong id)
        {
            Id = id;
            Description = string.Empty;
            Level = 1;
            Exp = 0;
        }

        public ulong Id { get; set; }
        public string Description { get; set; }
        public ulong Level { get; set; }
        public int Exp { get; set; }

        public UserProfile GetCopy()
        {
            var copy = new UserProfile(Id);
            copy.Description = Description;
            copy.Level = Level;
            copy.Exp = Exp;

            return copy;
        }

        public void Save(string filename)
        {
            using (var bw = new BinaryWriter(new FileStream(filename, FileMode.Create)))
            {
                bw.Write(Id);
                bw.Write(Description ?? string.Empty);
                bw.Write(Level);
                bw.Write(Exp);

                bw.Close();
            }
        }

        public void Load(string filename)
        {
            using (var br = new BinaryReader(new FileStream(filename, FileMode.Open)))
            {
                Id = br.ReadUInt64();
                Description = br.ReadString();
                Level = br.ReadUInt64();
                Exp = br.ReadInt32();

                br.Close();
            }
        }
    }
}
