using System;
using System.Collections.Generic;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Runtime;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class RuntimeCapabilitiesTests
    {
        #region Capability construction and flag verification

        [Test]
        public void FullCapabilities_AllFlagsTrue()
        {
            var caps = CreateCapabilities(
                apiCreate: true, apiDestroy: true, enumerate: true,
                deviceCreate: true, deviceDestroy: true,
                gazeSubscribe: true, gazeUnsubscribe: true, processCallbacks: true,
                waitForCallbacks: true,
                reconnect: true, errorMessage: true);

            Assert.IsTrue(caps.HasApiCreate);
            Assert.IsTrue(caps.HasApiDestroy);
            Assert.IsTrue(caps.HasEnumerateDevices);
            Assert.IsTrue(caps.HasDeviceCreate);
            Assert.IsTrue(caps.HasDeviceDestroy);
            Assert.IsTrue(caps.HasGazePointSubscribe);
            Assert.IsTrue(caps.HasGazePointUnsubscribe);
            Assert.IsTrue(caps.HasProcessCallbacks);
            Assert.IsTrue(caps.HasWaitForCallbacks);
            Assert.IsTrue(caps.HasDeviceReconnect);
            Assert.IsTrue(caps.HasErrorMessage);

            Assert.IsTrue(caps.CanCreateApi);
            Assert.IsTrue(caps.CanEnumerate);
            Assert.IsTrue(caps.CanCreateDevice);
            Assert.IsTrue(caps.CanSubscribeGaze);

            Assert.AreEqual(0, caps.MissingRequired.Count);
            Assert.AreEqual(0, caps.MissingOptional.Count);
        }

        [Test]
        public void EmptyCapabilities_AllFlagsFalse()
        {
            var caps = CreateCapabilities(
                apiCreate: false, apiDestroy: false, enumerate: false,
                deviceCreate: false, deviceDestroy: false,
                gazeSubscribe: false, gazeUnsubscribe: false, processCallbacks: false,
                waitForCallbacks: false,
                reconnect: false, errorMessage: false);

            Assert.IsFalse(caps.CanCreateApi);
            Assert.IsFalse(caps.CanEnumerate);
            Assert.IsFalse(caps.CanCreateDevice);
            Assert.IsFalse(caps.CanSubscribeGaze);
        }

        #endregion

        #region Core Required missing prevents higher capabilities

        [Test]
        public void MissingApiCreate_CannotCreateApi()
        {
            var caps = CreateCapabilities(apiCreate: false);
            Assert.IsFalse(caps.CanCreateApi);
            Assert.IsFalse(caps.CanEnumerate);
            Assert.IsFalse(caps.CanCreateDevice);
            Assert.IsFalse(caps.CanSubscribeGaze);
        }

        [Test]
        public void MissingApiDestroy_CannotCreateApi()
        {
            var caps = CreateCapabilities(apiDestroy: false);
            Assert.IsFalse(caps.CanCreateApi);
        }

        [Test]
        public void MissingEnumerate_CannotCreateApi()
        {
            var caps = CreateCapabilities(enumerate: false);
            Assert.IsFalse(caps.CanCreateApi);
            Assert.IsFalse(caps.CanEnumerate);
        }

        #endregion

        #region Device Required missing prevents device capabilities

        [Test]
        public void MissingDeviceCreate_CannotCreateDevice()
        {
            var caps = CreateCapabilities(deviceCreate: false);
            Assert.IsTrue(caps.CanCreateApi);
            Assert.IsFalse(caps.CanCreateDevice);
            Assert.IsFalse(caps.CanSubscribeGaze);
        }

        [Test]
        public void MissingDeviceDestroy_CannotCreateDevice()
        {
            var caps = CreateCapabilities(deviceDestroy: false);
            Assert.IsTrue(caps.CanCreateApi);
            Assert.IsFalse(caps.CanCreateDevice);
        }

        #endregion

        #region Gaze Required missing prevents gaze subscription

        [Test]
        public void MissingGazeSubscribe_CannotSubscribeGaze()
        {
            var caps = CreateCapabilities(gazeSubscribe: false);
            Assert.IsTrue(caps.CanCreateDevice);
            Assert.IsFalse(caps.CanSubscribeGaze);
        }

        [Test]
        public void MissingGazeUnsubscribe_CannotSubscribeGaze()
        {
            var caps = CreateCapabilities(gazeUnsubscribe: false);
            Assert.IsTrue(caps.CanCreateDevice);
            Assert.IsFalse(caps.CanSubscribeGaze);
        }

        [Test]
        public void MissingProcessCallbacks_CannotSubscribeGaze()
        {
            var caps = CreateCapabilities(processCallbacks: false);
            Assert.IsTrue(caps.CanCreateDevice);
            Assert.IsFalse(caps.CanSubscribeGaze);
        }

        #endregion

        #region Strategy Dependent does not affect CanSubscribeGaze

        [Test]
        public void MissingWaitForCallbacks_CanStillSubscribeGaze()
        {
            // WaitForCallbacks is strategy-dependent; its absence does not prevent gaze subscription.
            // The callback processing strategy selector will determine what to do.
            var caps = CreateCapabilities(waitForCallbacks: false);
            Assert.IsTrue(caps.CanSubscribeGaze);
            Assert.IsFalse(caps.HasWaitForCallbacks);
        }

        #endregion

        #region Optional exports do not affect any capability gate

        [Test]
        public void MissingReconnect_AllCapabilitiesIntact()
        {
            var caps = CreateCapabilities(reconnect: false);
            Assert.IsTrue(caps.CanCreateApi);
            Assert.IsTrue(caps.CanCreateDevice);
            Assert.IsTrue(caps.CanSubscribeGaze);
            Assert.IsFalse(caps.HasDeviceReconnect);
        }

        [Test]
        public void MissingErrorMessage_AllCapabilitiesIntact()
        {
            var caps = CreateCapabilities(errorMessage: false);
            Assert.IsTrue(caps.CanSubscribeGaze);
            Assert.IsFalse(caps.HasErrorMessage);
        }

        #endregion

        #region Missing lists accurately recorded

        [Test]
        public void MissingRequired_RecordsExportNames()
        {
            var missing = new List<string> { "tobii_api_create", "tobii_api_destroy" };
            var caps = new RuntimeCapabilities(
                hasApiCreate: false, hasApiDestroy: false, hasEnumerateDevices: true,
                hasDeviceCreate: true, hasDeviceDestroy: true,
                hasGazePointSubscribe: true, hasGazePointUnsubscribe: true,
                hasProcessCallbacks: true, hasWaitForCallbacks: true,
                hasDeviceReconnect: true, hasErrorMessage: true,
                missingRequired: missing.AsReadOnly(),
                missingOptional: new List<string>().AsReadOnly());

            Assert.AreEqual(2, caps.MissingRequired.Count);
            Assert.Contains("tobii_api_create", (System.Collections.ICollection)caps.MissingRequired);
            Assert.Contains("tobii_api_destroy", (System.Collections.ICollection)caps.MissingRequired);
        }

        [Test]
        public void MissingOptional_RecordsExportNames()
        {
            var missingOpt = new List<string> { "tobii_device_reconnect" };
            var caps = new RuntimeCapabilities(
                hasApiCreate: true, hasApiDestroy: true, hasEnumerateDevices: true,
                hasDeviceCreate: true, hasDeviceDestroy: true,
                hasGazePointSubscribe: true, hasGazePointUnsubscribe: true,
                hasProcessCallbacks: true, hasWaitForCallbacks: true,
                hasDeviceReconnect: false, hasErrorMessage: true,
                missingRequired: new List<string>().AsReadOnly(),
                missingOptional: missingOpt.AsReadOnly());

            Assert.AreEqual(1, caps.MissingOptional.Count);
            Assert.AreEqual("tobii_device_reconnect", caps.MissingOptional[0]);
        }

        [Test]
        public void NullMissingLists_DefaultToEmpty()
        {
            var caps = new RuntimeCapabilities(
                hasApiCreate: true, hasApiDestroy: true, hasEnumerateDevices: true,
                hasDeviceCreate: true, hasDeviceDestroy: true,
                hasGazePointSubscribe: true, hasGazePointUnsubscribe: true,
                hasProcessCallbacks: true, hasWaitForCallbacks: true,
                hasDeviceReconnect: true, hasErrorMessage: true,
                missingRequired: null,
                missingOptional: null);

            Assert.IsNotNull(caps.MissingRequired);
            Assert.IsNotNull(caps.MissingOptional);
            Assert.AreEqual(0, caps.MissingRequired.Count);
            Assert.AreEqual(0, caps.MissingOptional.Count);
        }

        #endregion

        #region Metadata properties

        [Test]
        public void RuntimeVersion_StoredCorrectly()
        {
            var caps = CreateCapabilities(runtimeVersion: "7.2.0.1234");
            Assert.AreEqual("7.2.0.1234", caps.RuntimeVersion);
        }

        [Test]
        public void RuntimePath_StoredCorrectly()
        {
            var caps = CreateCapabilities(runtimePath: @"C:\Program Files\Tobii\tobii_stream_engine.dll");
            Assert.AreEqual(@"C:\Program Files\Tobii\tobii_stream_engine.dll", caps.RuntimePath);
        }

        #endregion

        #region CallbackProcessingStrategy enum coverage

        [Test]
        public void CallbackProcessingStrategy_DefaultIsUnknown()
        {
            CallbackProcessingStrategy strategy = default;
            Assert.AreEqual(CallbackProcessingStrategy.Unknown, strategy);
        }

        [Test]
        public void CallbackProcessingStrategy_AllValuesAreDefined()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(CallbackProcessingStrategy), CallbackProcessingStrategy.Unknown));
            Assert.IsTrue(Enum.IsDefined(typeof(CallbackProcessingStrategy), CallbackProcessingStrategy.WaitAndProcess));
            Assert.IsTrue(Enum.IsDefined(typeof(CallbackProcessingStrategy), CallbackProcessingStrategy.ProcessOnly));
            Assert.IsTrue(Enum.IsDefined(typeof(CallbackProcessingStrategy), CallbackProcessingStrategy.RuntimeHostIsolated));
        }

        #endregion

        #region Diagnostics-only mode: partial capabilities for inventory

        [Test]
        public void DiagnosticsMode_CanEnumerateWithoutGaze()
        {
            // A runtime might have only core exports, sufficient for diagnostics/inventory
            // but not for gaze streaming.
            var caps = CreateCapabilities(
                gazeSubscribe: false, gazeUnsubscribe: false,
                processCallbacks: false, waitForCallbacks: false);

            Assert.IsTrue(caps.CanCreateApi, "Diagnostics requires API creation");
            Assert.IsTrue(caps.CanEnumerate, "Diagnostics requires enumeration");
            Assert.IsTrue(caps.CanCreateDevice, "Diagnostics can create device for info");
            Assert.IsFalse(caps.CanSubscribeGaze, "Cannot subscribe gaze without gaze exports");
        }

        #endregion

        #region Helper

        private static RuntimeCapabilities CreateCapabilities(
            bool apiCreate = true, bool apiDestroy = true, bool enumerate = true,
            bool deviceCreate = true, bool deviceDestroy = true,
            bool gazeSubscribe = true, bool gazeUnsubscribe = true,
            bool processCallbacks = true, bool waitForCallbacks = true,
            bool reconnect = true, bool errorMessage = true,
            string runtimeVersion = null, string runtimePath = null)
        {
            return new RuntimeCapabilities(
                hasApiCreate: apiCreate,
                hasApiDestroy: apiDestroy,
                hasEnumerateDevices: enumerate,
                hasDeviceCreate: deviceCreate,
                hasDeviceDestroy: deviceDestroy,
                hasGazePointSubscribe: gazeSubscribe,
                hasGazePointUnsubscribe: gazeUnsubscribe,
                hasProcessCallbacks: processCallbacks,
                hasWaitForCallbacks: waitForCallbacks,
                hasDeviceReconnect: reconnect,
                hasErrorMessage: errorMessage,
                missingRequired: new List<string>().AsReadOnly(),
                missingOptional: new List<string>().AsReadOnly(),
                runtimeVersion: runtimeVersion,
                runtimePath: runtimePath);
        }

        #endregion
    }
}
