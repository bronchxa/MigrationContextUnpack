using System;
using System.Runtime.InteropServices;
using System.Security;

namespace MigrationContextUnpack.Sources.CSharp
{
    public static class SecureStringExtension
    {
        public static string SecureStringToString(this SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
