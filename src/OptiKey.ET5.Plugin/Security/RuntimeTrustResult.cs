using System;

namespace OptiKey.ET5.Plugin.Security
{
    /// <summary>
    /// Status of the cryptographic signature on a binary.
    /// </summary>
    public enum SignatureStatus
    {
        /// <summary>Binary has no embedded Authenticode signature.</summary>
        Unsigned = 0,

        /// <summary>Binary has an embedded signature whose hash is valid.</summary>
        Valid = 1,

        /// <summary>Binary signature hash does not match content (tampered/corrupted).</summary>
        HashMismatch = 2,

        /// <summary>Verification failed due to an environmental or file read error.</summary>
        Error = 3
    }

    /// <summary>
    /// Status of the certificate trust chain.
    /// </summary>
    public enum ChainStatus
    {
        /// <summary>No certificate chain to evaluate (unsigned).</summary>
        NotApplicable = 0,

        /// <summary>Chain resolves to a trusted root authority in Windows certificate store.</summary>
        Trusted = 1,

        /// <summary>Certificate is self-signed or chains to an untrusted root.</summary>
        UntrustedRoot = 2,

        /// <summary>A certificate in the chain has expired.</summary>
        Expired = 3,

        /// <summary>Certificate has been explicitly revoked or distrusted.</summary>
        Revoked = 4,

        /// <summary>Certificate chain broken or incomplete.</summary>
        IncompleteChain = 5
    }

    /// <summary>
    /// Detailed, structured trust verification result for a native binary.
    /// Separates signature validity, chain trust, and publisher identity as required
    /// by ADR / security guidelines.
    /// </summary>
    public sealed class RuntimeTrustResult
    {
        public SignatureStatus SignatureStatus { get; }
        public ChainStatus ChainStatus { get; }
        public string SignerSubject { get; }
        public bool SignerMatchesTobii { get; }
        public int WinVerifyTrustResultCode { get; }
        public string DiagnosticMessage { get; }

        public bool IsFullyTrusted =>
            SignatureStatus == SignatureStatus.Valid &&
            ChainStatus == ChainStatus.Trusted &&
            SignerMatchesTobii;

        public RuntimeTrustResult(
            SignatureStatus signatureStatus,
            ChainStatus chainStatus,
            string signerSubject,
            bool signerMatchesTobii,
            int winVerifyTrustResultCode = 0,
            string diagnosticMessage = null)
        {
            SignatureStatus = signatureStatus;
            ChainStatus = chainStatus;
            SignerSubject = signerSubject;
            SignerMatchesTobii = signerMatchesTobii;
            WinVerifyTrustResultCode = winVerifyTrustResultCode;
            DiagnosticMessage = diagnosticMessage ?? string.Empty;
        }

        public static RuntimeTrustResult Unsigned(string message = "Binary is not signed.")
        {
            return new RuntimeTrustResult(
                SignatureStatus.Unsigned,
                ChainStatus.NotApplicable,
                null,
                false,
                unchecked((int)0x800B0100), // TRUST_E_NOSIGNATURE
                message);
        }

        public override string ToString()
        {
            return $"[TrustResult: Sig={SignatureStatus}, Chain={ChainStatus}, TobiiSigner={SignerMatchesTobii}, Subject={SignerSubject ?? "None"}]";
        }
    }
}
