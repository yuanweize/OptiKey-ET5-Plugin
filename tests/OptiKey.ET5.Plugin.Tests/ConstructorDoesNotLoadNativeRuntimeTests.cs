using System;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Runtime;

namespace OptiKey.ET5.Plugin.Tests
{
    /// <summary>
    /// Critical regression tests verifying that constructing plugin types NEVER
    /// triggers native DLL loading or environment probing during object instantiation.
    ///
    /// OptiKey's plugin discovery inspects plugin types by instantiating them or
    /// reflecting over constructors. Any early native load in a constructor causes
    /// OptiKey to fail to load the plugin or crash if native DLLs are missing.
    /// </summary>
    [TestFixture]
    public class ConstructorDoesNotLoadNativeRuntimeTests
    {
        [Test]
        public void ET5PointService_ParameterlessConstructor_DoesNotThrowOrTouchNative()
        {
            // Instantiating ET5PointService with default constructor must succeed
            // unconditionally on any machine, even without Tobii DLLs present.
            ET5PointService service = null;
            Assert.DoesNotThrow(() =>
            {
                service = new ET5PointService();
            });

            Assert.That(service, Is.Not.Null);

            // Safe disposal without starting
            Assert.DoesNotThrow(() =>
            {
                service.Dispose();
            });
        }

        [Test]
        public void TobiiGazeProvider_DefaultConstructor_DoesNotLoadNative()
        {
            TobiiGazeProvider provider = null;
            Assert.DoesNotThrow(() =>
            {
                provider = new TobiiGazeProvider();
            });

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.IsConnected, Is.False);

            Assert.DoesNotThrow(() =>
            {
                provider.Dispose();
            });
        }

        [Test]
        public void PluginConfiguration_Load_DoesNotRequireNativeRuntime()
        {
            PluginConfiguration config = null;
            Assert.DoesNotThrow(() =>
            {
                config = PluginConfiguration.Load();
            });

            Assert.That(config, Is.Not.Null);
            Assert.That(config.AllowUnverifiedTobiiDevice, Is.False, "Strict mode must be default");
        }
    }
}
