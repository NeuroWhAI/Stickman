using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Jurassic;

namespace Stickman.Utility
{
    static class JsEngine
    {
        public static string Evaluate(string code, TimeSpan timeout)
        {
            string result = string.Empty;

            var job = new Thread(() =>
            {
                try
                {
                    var engine = new ScriptEngine();
                    engine.RecursionDepthLimit = 128;
                    
                    result = engine.Evaluate(code).ToString();
                }
                catch (Exception e)
                {
                    result = e.Message;
                }
            });

            job.Start();

            job.Join(timeout);

            if (job.ThreadState != ThreadState.Stopped)
            {
                job.Abort();

                return "TIME_OUT";
            }

            return result;
        }

        private static bool CheckRecursiveLoop(string code)
        {
            throw new NotImplementedException();
        }
    }
}
