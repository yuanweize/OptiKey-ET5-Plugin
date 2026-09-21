using System.IO;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Runtime;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class RuntimeLocatorSecurityTests
    {
        private string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "OptiKey_SecurityTests_" + Path.GetRandomFileName());
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
        public void NonExistentOrTruncatedFile_RejectsPe64()
        {
            Assert.IsFalse(TobiiRuntimeLocator.VerifyPe64Architecture(Path.Combine(tempDir, "nonexistent.dll")));

            string emptyFile = Path.Combine(tempDir, "empty.dll");
            File.WriteAllBytes(emptyFile, new byte[10]);
            Assert.IsFalse(TobiiRuntimeLocator.VerifyPe64Architecture(emptyFile));
        }

        [Test]
        public void SyntheticPeHeader_ValidatesX64Correctly()
        {
            string x64Dll = Path.Combine(tempDir, "fake_x64.dll");
            CreateSyntheticPeBinary(x64Dll, machineArchitecture: 0x8664); // AMD64

            string x86Dll = Path.Combine(tempDir, "fake_x86.dll");
            CreateSyntheticPeBinary(x86Dll, machineArchitecture: 0x014C); // i386

            Assert.IsTrue(TobiiRuntimeLocator.VerifyPe64Architecture(x64Dll));
            Assert.IsFalse(TobiiRuntimeLocator.VerifyPe64Architecture(x86Dll));
        }

        private static void CreateSyntheticPeBinary(string filePath, ushort machineArchitecture)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                // DOS Header: "MZ"
                writer.Write((ushort)0x5A4D);
                // Pad to offset 0x3C
                byte[] pad = new byte[0x3C - 2];
                writer.Write(pad);

                // e_lfanew: Offset to PE header (set to 0x80)
                int peOffset = 0x80;
                writer.Write(peOffset);

                // Pad up to 0x80
                int currentPos = (int)fs.Position;
                writer.Write(new byte[peOffset - currentPos]);

                // PE Signature: "PE\0\0"
                writer.Write((uint)0x00004550);

                // Machine Architecture
                writer.Write(machineArchitecture);

                // Trailing bytes
                writer.Write(new byte[128]);
            }
        }
    }
}
