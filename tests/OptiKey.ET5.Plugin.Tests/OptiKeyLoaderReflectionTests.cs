using System;
using System.Linq;
using System.Reflection;
using JuliusSweetland.OptiKey.Contracts;
using NUnit.Framework;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class OptiKeyLoaderReflectionTests
    {
        [Test]
        public void OptiKeyDllLoaderSimulation_DiscoversExactlyOnePointService()
        {
            // Exact reflection pattern executed by OptiKey DllLoader.cs
            Assembly targetAssembly = typeof(ET5PointService).Assembly;

            var types = targetAssembly.GetTypes();
            var pointServiceTypes = types
                .Where(t => typeof(IPointService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            Assert.AreEqual(1, pointServiceTypes.Count,
                "The production plugin assembly must export exactly ONE concrete class implementing IPointService.");

            Type pluginType = pointServiceTypes[0];
            Assert.AreEqual("OptiKey.ET5.Plugin.ET5PointService", pluginType.FullName);
        }

        [Test]
        public void ParameterlessConstructor_InstantiatesWithoutExceptions()
        {
            // OptiKey instantiates plugins dynamically:
            // dllService = (IPointService)Activator.CreateInstance(typeToLoad);
            Type pluginType = typeof(ET5PointService);

            object instance = null;
            Assert.DoesNotThrow(() =>
            {
                instance = Activator.CreateInstance(pluginType);
            }, "Activator.CreateInstance must NEVER throw an exception from the default constructor.");

            Assert.IsNotNull(instance);
            Assert.IsInstanceOf<IPointService>(instance);
            Assert.IsInstanceOf<INotifyErrors>(instance);
            Assert.IsInstanceOf<IDisposable>(instance);

            var pointService = (IPointService)instance;
            var disposable = (IDisposable)pointService;

            // Dispose must succeed cleanly without errors
            Assert.DoesNotThrow(() => disposable.Dispose());
        }

        [Test]
        public void SyntheticProviderAssembly_DoesNotImplementIPointServiceDirectly()
        {
            // Ensure the synthetic harness does not register as an IPointService to avoid confusing loaders
            Assembly syntheticAssembly = typeof(OptiKey.ET5.Plugin.Synthetic.SyntheticGazeProvider).Assembly;
            var types = syntheticAssembly.GetTypes();
            var pointServiceTypes = types
                .Where(t => typeof(IPointService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            Assert.AreEqual(0, pointServiceTypes.Count,
                "The synthetic harness assembly must NOT implement IPointService directly.");
        }
    }
}
