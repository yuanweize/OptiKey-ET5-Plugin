using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Security
{
    public interface IRuntimeTrustVerifier
    {
        RuntimeTrustResult VerifyFileTrust(string filePath);
    }

    /// <summary>
    /// Performs cryptographic Authenticode trust validation using WinVerifyTrust
    /// and X509 certificate subject inspection (Phase H / ADR-004).
    /// </summary>
    public class RuntimeTrustVerifier : IRuntimeTrustVerifier
    {
        private readonly IPluginLogger logger;

        public RuntimeTrustVerifier(IPluginLogger logger = null)
        {
            this.logger = logger;
        }

        public RuntimeTrustResult VerifyFileTrust(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return RuntimeTrustResult.Unsigned("File does not exist.");
            }

            // 1. Extract X509 certificate subject (signer identity)
            string signerSubject = null;
            bool signerMatchesTobii = false;

            try
            {
                var cert = X509Certificate.CreateFromSignedFile(filePath);
                if (cert != null)
                {
                    signerSubject = cert.Subject;
                    signerMatchesTobii = !string.IsNullOrEmpty(signerSubject) &&
                        signerSubject.IndexOf("Tobii", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch (Exception ex)
            {
                logger?.Debug($"Failed to extract X509 certificate from '{PathSanitizer.Sanitize(filePath)}': {ex.Message}");
            }

            // 2. Perform WinVerifyTrust chain verification
            try
            {
                var fileInfo = new WinVerifyTrustNative.WINTRUST_FILE_INFO
                {
                    cbStruct = (uint)Marshal.SizeOf(typeof(WinVerifyTrustNative.WINTRUST_FILE_INFO)),
                    pcwszFilePath = filePath,
                    hFile = IntPtr.Zero,
                    pgKnownSubject = IntPtr.Zero
                };

                IntPtr pFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinVerifyTrustNative.WINTRUST_FILE_INFO)));
                try
                {
                    Marshal.StructureToPtr(fileInfo, pFileInfo, false);

                    var wvtData = new WinVerifyTrustNative.WINTRUST_DATA
                    {
                        cbStruct = (uint)Marshal.SizeOf(typeof(WinVerifyTrustNative.WINTRUST_DATA)),
                        pPolicyCallbackData = IntPtr.Zero,
                        pSIPClientData = IntPtr.Zero,
                        dwUIChoice = WinVerifyTrustNative.WTD_UI_NONE,
                        fdwRevocationChecks = WinVerifyTrustNative.WTD_REVOKE_NONE,
                        dwUnionChoice = WinVerifyTrustNative.WTD_CHOICE_FILE,
                        pFile = pFileInfo,
                        dwStateAction = WinVerifyTrustNative.WTD_STATEACTION_IGNORE,
                        hWVTStateData = IntPtr.Zero,
                        pwszURLReference = null,
                        dwProvFlags = 0,
                        dwUIContext = 0,
                        pSignatureSettings = IntPtr.Zero
                    };

                    int result = WinVerifyTrustNative.WinVerifyTrust(
                        IntPtr.Zero,
                        WinVerifyTrustNative.WINTRUST_ACTION_GENERIC_VERIFY_V2,
                        ref wvtData);

                    SignatureStatus sigStatus;
                    ChainStatus chainStatus;
                    string diagMsg;

                    switch (result)
                    {
                        case WinVerifyTrustNative.ERROR_SUCCESS:
                            sigStatus = SignatureStatus.Valid;
                            chainStatus = ChainStatus.Trusted;
                            diagMsg = "Authenticode signature is valid and trusted.";
                            break;

                        case WinVerifyTrustNative.TRUST_E_NOSIGNATURE:
                            sigStatus = SignatureStatus.Unsigned;
                            chainStatus = ChainStatus.NotApplicable;
                            diagMsg = "Binary is not digitally signed.";
                            break;

                        case WinVerifyTrustNative.TRUST_E_BAD_DIGEST:
                            sigStatus = SignatureStatus.HashMismatch;
                            chainStatus = ChainStatus.NotApplicable;
                            diagMsg = "Binary signature digest mismatch (tampered or corrupted).";
                            break;

                        case WinVerifyTrustNative.CERT_E_UNTRUSTEDROOT:
                            sigStatus = SignatureStatus.Valid;
                            chainStatus = ChainStatus.UntrustedRoot;
                            diagMsg = "Certificate chains to an untrusted root authority.";
                            break;

                        case WinVerifyTrustNative.CERT_E_CHAINING:
                            sigStatus = SignatureStatus.Valid;
                            chainStatus = ChainStatus.IncompleteChain;
                            diagMsg = "Certificate chain is broken or incomplete.";
                            break;

                        case WinVerifyTrustNative.CERT_E_EXPIRED:
                            sigStatus = SignatureStatus.Valid;
                            chainStatus = ChainStatus.Expired;
                            diagMsg = "Certificate has expired.";
                            break;

                        case WinVerifyTrustNative.TRUST_E_EXPLICIT_DISTRUST:
                        case WinVerifyTrustNative.CERT_E_REVOKED:
                            sigStatus = SignatureStatus.Valid;
                            chainStatus = ChainStatus.Revoked;
                            diagMsg = "Certificate has been explicitly revoked or distrusted.";
                            break;

                        default:
                            sigStatus = (signerSubject != null) ? SignatureStatus.Error : SignatureStatus.Unsigned;
                            chainStatus = (signerSubject != null) ? ChainStatus.UntrustedRoot : ChainStatus.NotApplicable;
                            diagMsg = $"WinVerifyTrust returned non-zero code: 0x{result:X8}";
                            break;
                    }

                    return new RuntimeTrustResult(
                        sigStatus,
                        chainStatus,
                        signerSubject,
                        signerMatchesTobii,
                        result,
                        diagMsg);
                }
                finally
                {
                    Marshal.FreeHGlobal(pFileInfo);
                }
            }
            catch (DllNotFoundException)
            {
                // Running on non-Windows environment (e.g. cross-platform tests)
                return new RuntimeTrustResult(
                    signerSubject != null ? SignatureStatus.Valid : SignatureStatus.Unsigned,
                    ChainStatus.NotApplicable,
                    signerSubject,
                    signerMatchesTobii,
                    0,
                    "wintrust.dll not available on host platform.");
            }
            catch (Exception ex)
            {
                return new RuntimeTrustResult(
                    SignatureStatus.Error,
                    ChainStatus.NotApplicable,
                    signerSubject,
                    signerMatchesTobii,
                    -1,
                    $"WinVerifyTrust exception: {ex.Message}");
            }
        }
    }
}
