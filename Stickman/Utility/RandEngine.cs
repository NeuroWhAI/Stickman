using System;
using System.Collections.Generic;
using System.Text;

namespace Stickman.Utility
{
    public static class RandEngine
    {
        private static readonly Random Engine = new Random();

        public static int GetInt(int min, int max)
        {
            return Engine.Next(min, max);
        }
    }
}
