using System;
using System.IO;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Tests.Diagnostics
{
    [TestFixture]
    public class PathSanitizerTests
    {
        [Test]
        public void Sanitize_NullOrEmptyOrWhitespace_ReturnsSameOrEmpty()
        {
            Assert.That(PathSanitizer.Sanitize(null), Is.EqualTo(string.Empty));
            Assert.That(PathSanitizer.Sanitize(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(PathSanitizer.Sanitize("   "), Is.EqualTo("   "));
        }

        [Test]
        public void Sanitize_PathOutsideUserProfile_ReturnsUnchanged()
        {
            string programFilesPath = @"C:\Program Files\Tobii\tobii_stream_engine.dll";
            Assert.That(PathSanitizer.Sanitize(programFilesPath), Is.EqualTo(programFilesPath));

            string systemRootPath = @"C:\Windows\System32\drivers\tobii.sys";
            Assert.That(PathSanitizer.Sanitize(systemRootPath), Is.EqualTo(systemRootPath));
        }

        [Test]
        public void Sanitize_PathInsideUserProfile_ReplacesWithMacro()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(userProfile))
            {
                Assert.Pass("UserProfile not defined on current environment.");
                return;
            }

            string sensitivePath = Path.Combine(userProfile, "AppData", "Local", "Tobii", "tobii_stream_engine.dll");
            string sanitized = PathSanitizer.Sanitize(sensitivePath);

            Assert.That(sanitized, Does.StartWith("%USERPROFILE%"));
            Assert.That(sanitized, Does.Not.Contain(userProfile));
            Assert.That(sanitized, Does.EndWith(@"AppData\Local\Tobii\tobii_stream_engine.dll")
                .Or.EndsWith(@"AppData/Local/Tobii/tobii_stream_engine.dll"));
        }
    }
}
