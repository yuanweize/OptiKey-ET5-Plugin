using System;
using System.Runtime.InteropServices;

namespace OptiKey.ET5.Plugin.Security
{
    /// <summary>
    /// P/Invoke definitions for WinVerifyTrust API in wintrust.dll.
    /// </summary>
    internal static class WinVerifyTrustNative
    {
        public static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 =
            new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

        public const uint WTD_CHOICE_FILE = 1;
        public const uint WTD_STATEACTION_IGNORE = 0;
        public const uint WTD_REVOKE_NONE = 0;
        public const uint WTD_UI_NONE = 2;

        public const int ERROR_SUCCESS = 0;
        public const int TRUST_E_NOSIGNATURE = unchecked((int)0x800B0100);
        public const int TRUST_E_BAD_DIGEST = unchecked((int)0x80096010);
        public const int CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109);
        public const int CERT_E_CHAINING = unchecked((int)0x800B010A);
        public const int CERT_E_EXPIRED = unchecked((int)0x800B0101);
        public const int CERT_E_REVOKED = unchecked((int)0x800B010C);
        public const int TRUST_E_EXPLICIT_DISTRUST = unchecked((int)0x800B0111);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
            ref WINTRUST_DATA pWVTData);
    }
}
