using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.System.Profile;

namespace PushSharp.Universal
{
    public class Utility
    {
        public static string GetDeviceID()
        {
            // Vedran: A very big thanks to Matija Leskovar for contribution. 
            // https://github.com/matijaleskovar

            // retrieve the ID bytes
            byte[] hwIDBytes = HardwareIdentification.GetPackageSpecificToken(null).Id.ToArray();
            // map reduce the ID... fun...
            return hwIDBytes.Select(b => b.ToString()).Aggregate((b, next) => b + next);
        }

        public static string Base64Encode(string input)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }
    }
}
