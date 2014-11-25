using System;
using System.Text;

namespace PushSharp.WebAPI.Utility
{
    public class Helpers
    {
        private static readonly Encoding encoding = Encoding.GetEncoding("iso-8859-1");

        /// <summary>
        /// Runs Base64 decoding N times
        /// </summary>
        /// <param name="value">The base64 string to decode.</param>
        /// <param name="times">Determines how many times should the entered value be decoded in repetition.</param>
        /// <returns>A decoded raw string value in format "uname:pwd".</returns>
        public static string DecodeBase64NTimes(string value, int times)
        {
            times--;
            string decoded = encoding.GetString(Convert.FromBase64String(value));

            if (times == 0)
                return decoded;
            else
                return DecodeBase64NTimes(decoded, times);
        }
    }
}