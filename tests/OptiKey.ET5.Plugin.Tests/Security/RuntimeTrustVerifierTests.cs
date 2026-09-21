using System;
using System.IO;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Runtime;
using OptiKey.ET5.Plugin.Runtime.Discovery;
using OptiKey.ET5.Plugin.Security;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class RuntimeTrustVerifierTests
    {
        private string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "OptiKey_TrustTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void RuntimeTrustResult_IsFullyTrusted_RequiresAllThreeCriteria()
        {
            // 1. All true -> Fully trusted
            var fullyTrusted = new RuntimeTrustResult(
                SignatureStatus.Valid,
                ChainStatus.Trusted,
                "CN=Tobii AB, O=Tobii AB, L=Danderyd, C=SE",
                signerMatchesTobii: true);
            Assert.IsTrue(fullyTrusted.IsFullyTrusted);

            // 2. Valid signature + Tobii signer, but Untrusted Root -> Not fully trusted
            var untrustedRoot = new RuntimeTrustResult(
                SignatureStatus.Valid,
                ChainStatus.UntrustedRoot,
                "CN=Tobii AB",
                signerMatchesTobii: true);
            Assert.IsFalse(untrustedRoot.IsFullyTrusted);

            // 3. Valid signature + Trusted root, but non-Tobii signer -> Not fully trusted
            var nonTobiiSigner = new RuntimeTrustResult(
                SignatureStatus.Valid,
                ChainStatus.Trusted,
                "CN=Contoso Ltd",
                signerMatchesTobii: false);
            Assert.IsFalse(nonTobiiSigner.IsFullyTrusted);

            // 4. Hash mismatch / tampered -> Not fully trusted
            var tampered = new RuntimeTrustResult(
                SignatureStatus.HashMismatch,
                ChainStatus.Trusted,
                "CN=Tobii AB",
                signerMatchesTobii: true);
            Assert.IsFalse(tampered.IsFullyTrusted);

            // 5. Unsigned -> Not fully trusted
            var unsigned = RuntimeTrustResult.Unsigned();
            Assert.IsFalse(unsigned.IsFullyTrusted);
        }

        [Test]
        public void RuntimeTrustResult_UnsignedFactory_SetsExpectedDefaults()
        {
            var result = RuntimeTrustResult.Unsigned("Custom missing file message");

            Assert.That(result.SignatureStatus, Is.EqualTo(SignatureStatus.Unsigned));
            Assert.That(result.ChainStatus, Is.EqualTo(ChainStatus.NotApplicable));
            Assert.IsNull(result.SignerSubject);
            Assert.IsFalse(result.SignerMatchesTobii);
            Assert.IsFalse(result.IsFullyTrusted);
            Assert.That(result.DiagnosticMessage, Is.EqualTo("Custom missing file message"));
        }

        [Test]
        public void RuntimeTrustVerifier_NonExistentFile_ReturnsUnsigned()
        {
            var verifier = new RuntimeTrustVerifier();
            var result = verifier.VerifyFileTrust(Path.Combine(tempDir, "does_not_exist.dll"));

            Assert.That(result.SignatureStatus, Is.EqualTo(SignatureStatus.Unsigned));
            Assert.IsFalse(result.SignerMatchesTobii);
            Assert.IsFalse(result.IsFullyTrusted);
        }

        [Test]
        public void RuntimeTrustVerifier_EmptyFile_ReturnsUnsignedWithoutThrowing()
        {
            string emptyFile = Path.Combine(tempDir, "empty.dll");
            File.WriteAllBytes(emptyFile, new byte[16]);

            var verifier = new RuntimeTrustVerifier();
            RuntimeTrustResult result = null;

            Assert.DoesNotThrow(() =>
            {
                result = verifier.VerifyFileTrust(emptyFile);
            });

            Assert.IsNotNull(result);
            Assert.That(result.SignatureStatus, Is.EqualTo(SignatureStatus.Unsigned));
            Assert.IsFalse(result.SignerMatchesTobii);
        }

        [Test]
        public void TobiiRuntimeLocator_RejectsCandidateIfSignerDoesNotMatchTobii()
        {
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[]
            {
                new SimpleDiscoverySource(fakeDll)
            });

            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.Valid,
                ChainStatus.Trusted,
                "CN=Malicious Actor Inc",
                signerMatchesTobii: false));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsFalse(result.IsFound);
            Assert.That(result.FailureReason, Does.Contain("was not found"));
        }

        [Test]
        public void TobiiRuntimeLocator_AcceptsCandidateIfTobiiSignerMatches_EvenIfChainUntrusted()
        {
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[]
            {
                new SimpleDiscoverySource(fakeDll)
            });

            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.Valid,
                ChainStatus.UntrustedRoot,
                "CN=Tobii AB, O=Tobii AB",
                signerMatchesTobii: true));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsTrue(result.IsFound);
            Assert.IsTrue(result.IsSigVerified);
            Assert.That(result.Publisher, Does.Contain("Tobii"));
            Assert.IsNotNull(result.TrustResult);
            Assert.That(result.TrustResult.ChainStatus, Is.EqualTo(ChainStatus.UntrustedRoot));
            Assert.IsFalse(result.TrustResult.IsFullyTrusted); // Valid signer, but chain is untrusted
        }

        [Test]
        public void TobiiRuntimeLocator_AcceptsFullyTrustedTobiiCandidate()
        {
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[]
            {
                new SimpleDiscoverySource(fakeDll)
            });

            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.Valid,
                ChainStatus.Trusted,
                "CN=Tobii AB, O=Tobii AB, L=Danderyd, C=SE",
                signerMatchesTobii: true));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsTrue(result.IsFound);
            Assert.IsTrue(result.IsSigVerified);
            Assert.That(result.Publisher, Does.Contain("Tobii"));
            Assert.IsNotNull(result.TrustResult);
            Assert.IsTrue(result.TrustResult.IsFullyTrusted);
        }

        [Test]
        public void TobiiRuntimeLocator_RejectsCandidateIfSignatureHashMismatch_EvenIfSignerMatchesTobii()
        {
            // SEC-01: Tampered or corrupted binary with TRUST_E_BAD_DIGEST must NEVER be accepted
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[] { new SimpleDiscoverySource(fakeDll) });
            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.HashMismatch,
                ChainStatus.NotApplicable,
                "CN=Tobii AB, O=Tobii AB",
                signerMatchesTobii: true,
                winVerifyTrustResultCode: unchecked((int)0x80096010), // TRUST_E_BAD_DIGEST
                diagnosticMessage: "Binary signature digest mismatch."));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsFalse(result.IsFound, "Must reject tampered binary despite matching Tobii signer metadata.");
            Assert.That(result.FailureReason, Does.Contain("was not found"));
        }

        [Test]
        public void TobiiRuntimeLocator_RejectsCandidateIfSignatureUnsigned_EvenIfSignerMatchesTobii()
        {
            // SEC-01: Unsigned binary must NEVER be accepted
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[] { new SimpleDiscoverySource(fakeDll) });
            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.Unsigned,
                ChainStatus.NotApplicable,
                "CN=Tobii AB",
                signerMatchesTobii: true));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsFalse(result.IsFound, "Must reject unsigned binary despite matching Tobii subject metadata.");
        }

        [Test]
        public void TobiiRuntimeLocator_RejectsCandidateIfSignatureError_EvenIfSignerMatchesTobii()
        {
            // SEC-01: Verification error must fail closed
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[] { new SimpleDiscoverySource(fakeDll) });
            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.Error,
                ChainStatus.NotApplicable,
                "CN=Tobii AB",
                signerMatchesTobii: true));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsFalse(result.IsFound, "Must fail closed on signature verification error.");
        }

        [Test]
        public void TobiiRuntimeLocator_RejectsCandidateIfCertificateRevoked_EvenIfSignatureValidAndSignerMatchesTobii()
        {
            // SEC-01: Explicitly revoked certificate must NEVER be accepted
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[] { new SimpleDiscoverySource(fakeDll) });
            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.Valid,
                ChainStatus.Revoked,
                "CN=Tobii AB",
                signerMatchesTobii: true,
                winVerifyTrustResultCode: unchecked((int)0x800B0111), // TRUST_E_EXPLICIT_DISTRUST
                diagnosticMessage: "Certificate revoked."));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsFalse(result.IsFound, "Must reject candidate whose certificate has been revoked.");
        }

        [Test]
        public void TobiiRuntimeLocator_RejectsCandidateIfCertificateRevokedWithCertERevoked_EvenIfSignatureValidAndSignerMatchesTobii()
        {
            // SEC-01: Explicitly revoked certificate (CERT_E_REVOKED) must NEVER be accepted
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[] { new SimpleDiscoverySource(fakeDll) });
            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.Valid,
                ChainStatus.Revoked,
                "CN=Tobii AB",
                signerMatchesTobii: true,
                winVerifyTrustResultCode: unchecked((int)0x800B010C), // CERT_E_REVOKED
                diagnosticMessage: "Certificate revoked."));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsFalse(result.IsFound, "Must reject candidate whose certificate has been revoked via CERT_E_REVOKED.");
        }

        [Test]
        public void TobiiRuntimeLocator_RejectsCandidateIfWinVerifyTrustReturnsUnknownErrorCode_EvenIfSignerMatchesTobii()
        {
            // SEC-01 / Fail-closed: An unknown non-zero WinVerifyTrust error code must result in SignatureStatus.Error and rejection
            string fakeDll = Path.Combine(tempDir, "tobii_stream_engine.dll");
            CreateSyntheticPe64(fakeDll);

            var discovery = new CompositeRuntimeDiscovery(new[] { new SimpleDiscoverySource(fakeDll) });
            var fakeVerifier = new FakeTrustVerifier(new RuntimeTrustResult(
                SignatureStatus.Error,
                ChainStatus.UntrustedRoot,
                "CN=Tobii AB",
                signerMatchesTobii: true,
                winVerifyTrustResultCode: unchecked((int)0x80092004), // CRYPT_E_NOT_FOUND
                diagnosticMessage: "Unknown verification error."));

            var locator = new TobiiRuntimeLocator(discovery: discovery, trustVerifier: fakeVerifier);
            var result = locator.LocateRuntime();

            Assert.IsFalse(result.IsFound, "Must reject candidate when WinVerifyTrust encounters an unhandled error code.");
        }

        private static void CreateSyntheticPe64(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                // DOS Header: "MZ"
                writer.Write((ushort)0x5A4D);
                // Pad to offset 0x3C
                writer.Write(new byte[0x3C - 2]);

                // e_lfanew: Offset to PE header (0x80)
                writer.Write(0x80);

                // Pad up to 0x80
                writer.Write(new byte[0x80 - (int)fs.Position]);

                // PE Signature: "PE\0\0"
                writer.Write((uint)0x00004550);

                // Machine Architecture: AMD64 (0x8664)
                writer.Write((ushort)0x8664);

                // Trailing bytes
                writer.Write(new byte[128]);
            }
        }

        private class SimpleDiscoverySource : IRuntimeDiscoverySource
        {
            private readonly string path;
            public string SourceName => "SimpleTest";

            public SimpleDiscoverySource(string path)
            {
                this.path = path;
            }

            public System.Collections.Generic.IEnumerable<string> DiscoverCandidatePaths()
            {
                yield return path;
            }
        }

        private class FakeTrustVerifier : IRuntimeTrustVerifier
        {
            private readonly RuntimeTrustResult resultToReturn;

            public FakeTrustVerifier(RuntimeTrustResult resultToReturn)
            {
                this.resultToReturn = resultToReturn;
            }

            public RuntimeTrustResult VerifyFileTrust(string filePath) => resultToReturn;
        }
    }
}
