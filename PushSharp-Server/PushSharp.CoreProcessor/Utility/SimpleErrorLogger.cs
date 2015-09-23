using System;
using System.IO;

namespace PushSharp.CoreProcessor.Utility
{
    public class SimpleErrorLogger
    {
        private readonly static object _locker = new Object();

        public static void LogError(Exception _ex, bool isInnerException = false)
        {
            // do it thread safe, not in a hurry, let the others sleep!
            lock (_locker)
            {
                var message = FormatEx(_ex);
                try
                {
                    using (StreamWriter sw = File.AppendText("C:\\Logs\\PushSharpDemo-Errors.log"))
                    {
                        string err = "";

                        if (isInnerException)
                            err += "[INNER EXCEPTION]";
                        else
                            err += Environment.NewLine + Environment.NewLine + "[EXCEPTION]";

                        err += "[" + DateTime.Now.ToString() + "]" + Environment.NewLine + "[" + message + "]";

                        sw.WriteLine(err);
                        sw.Flush();
                    }

                    if (_ex.InnerException != null)
                        LogError(_ex.InnerException, true);
                }
                catch (Exception ex)
                {
                    //the uncatchable :-(
                }
            }
        }

        public static void Trace(string msg)
        {
            System.Diagnostics.Trace.WriteLine("TRACE [" + DateTime.Now.ToLongTimeString() + "]: " + msg);
        }

        private static string FormatEx(Exception ex)
        {
            return "ERROR: " + ex.Message + Environment.NewLine + "STACKTRACE: " + ex.StackTrace;
        }
    }
}
