using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jint;

namespace Stickman.Utility
{
    static class JsEngine
    {
        public static string Evaluate(string code, TimeSpan timeout)
        {
            try
            {
                string result = new Engine(cfg => cfg
                    .LimitRecursion(128)
                    .MaxStatements(1024)
                    .TimeoutInterval(timeout))
                    .Execute(code)
                    .GetCompletionValue()
                    .ToString();

                return result;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
