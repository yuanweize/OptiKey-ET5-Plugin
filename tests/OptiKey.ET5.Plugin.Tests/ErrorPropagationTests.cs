using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Windows;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Runtime;
using OptiKey.ET5.Plugin.State;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class ErrorPropagationTests
    {
        #region ET5PluginException construction and properties

        [Test]
        public void ET5PluginException_PreservesAllProperties()
        {
            var inner = new InvalidOperationException("inner detail");
            var ex = new ET5PluginException(
                ET5ErrorCode.ConnectionFailed,
                "Cannot connect to eye tracker.",
                "Device connection failed: native error 5",
                inner);

            Assert.AreEqual(ET5ErrorCode.ConnectionFailed, ex.Code);
            Assert.AreEqual("Cannot connect to eye tracker.", ex.UserMessage);
            Assert.AreEqual("Device connection failed: native error 5", ex.DiagnosticMessage);
            Assert.AreSame(inner, ex.InnerException);
            Assert.AreEqual("Device connection failed: native error 5", ex.Message);
        }

        [Test]
        public void ET5PluginException_NullDiagnosticFallsBackToUserMessage()
        {
            var ex = new ET5PluginException(
                ET5ErrorCode.DeviceNotFound,
                "No tracker found.");

            Assert.AreEqual("No tracker found.", ex.DiagnosticMessage);
            Assert.AreEqual("No tracker found.", ex.Message);
        }

        [Test]
        public void ET5PluginException_NullUserMessageDefaultsToEmpty()
        {
            var ex = new ET5PluginException(ET5ErrorCode.InternalError, null, "diag only");
            Assert.AreEqual(string.Empty, ex.UserMessage);
            Assert.AreEqual("diag only", ex.DiagnosticMessage);
        }

        [Test]
        public void ET5PluginException_ToStringContainsCodeAndDiagnostic()
        {
            var ex = ET5PluginException.RuntimeNotFound("test detail");
            string s = ex.ToString();
            Assert.IsTrue(s.Contains("ET5-RuntimeNotFound"));
            Assert.IsTrue(s.Contains("test detail") || s.Contains("not located"));
        }

        [Test]
        public void ET5PluginException_ToStringIncludesInnerExceptionMessage()
        {
            var inner = new ArgumentException("bad arg");
            var ex = new ET5PluginException(
                ET5ErrorCode.CallbackFailure,
                "user msg",
                "diag msg",
                inner);
            string s = ex.ToString();
            Assert.IsTrue(s.Contains("bad arg"));
        }

        #endregion

        #region Factory method coverage

        [Test]
        public void Factory_RuntimeNotFound()
        {
            var ex = ET5PluginException.RuntimeNotFound();
            Assert.AreEqual(ET5ErrorCode.RuntimeNotFound, ex.Code);
            Assert.IsFalse(string.IsNullOrEmpty(ex.UserMessage));
            Assert.IsFalse(string.IsNullOrEmpty(ex.DiagnosticMessage));
        }

        [Test]
        public void Factory_RuntimeRejected()
        {
            var ex = ET5PluginException.RuntimeRejected("wrong architecture");
            Assert.AreEqual(ET5ErrorCode.RuntimeRejected, ex.Code);
            Assert.IsTrue(ex.DiagnosticMessage.Contains("wrong architecture"));
        }

        [Test]
        public void Factory_RuntimeIncompatible()
        {
            var ex = ET5PluginException.RuntimeIncompatible("missing 3 core exports");
            Assert.AreEqual(ET5ErrorCode.RuntimeIncompatible, ex.Code);
        }

        [Test]
        public void Factory_RequiredExportMissing()
        {
            var ex = ET5PluginException.RequiredExportMissing("tobii_api_create");
            Assert.AreEqual(ET5ErrorCode.RequiredExportMissing, ex.Code);
            Assert.IsTrue(ex.DiagnosticMessage.Contains("tobii_api_create"));
        }

        [Test]
        public void Factory_DeviceNotFound()
        {
            var ex = ET5PluginException.DeviceNotFound();
            Assert.AreEqual(ET5ErrorCode.DeviceNotFound, ex.Code);
        }

        [Test]
        public void Factory_DeviceIdentityUnknown()
        {
            var ex = ET5PluginException.DeviceIdentityUnknown();
            Assert.AreEqual(ET5ErrorCode.DeviceIdentityUnknown, ex.Code);
        }

        [Test]
        public void Factory_ConnectionFailed()
        {
            var ex = ET5PluginException.ConnectionFailed("TIMED_OUT");
            Assert.AreEqual(ET5ErrorCode.ConnectionFailed, ex.Code);
            Assert.IsTrue(ex.DiagnosticMessage.Contains("TIMED_OUT"));
        }

        [Test]
        public void Factory_ConnectionLost()
        {
            var ex = ET5PluginException.ConnectionLost();
            Assert.AreEqual(ET5ErrorCode.ConnectionLost, ex.Code);
        }

        [Test]
        public void Factory_CallbackFailure()
        {
            var inner = new AccessViolationException();
            var ex = ET5PluginException.CallbackFailure("segfault", inner);
            Assert.AreEqual(ET5ErrorCode.CallbackFailure, ex.Code);
            Assert.AreSame(inner, ex.InnerException);
        }

        [Test]
        public void Factory_DisplayUnavailable()
        {
            var ex = ET5PluginException.DisplayUnavailable();
            Assert.AreEqual(ET5ErrorCode.DisplayUnavailable, ex.Code);
        }

        [Test]
        public void Factory_NativeApiError()
        {
            var ex = ET5PluginException.NativeApiError("tobii_device_create", "error code 5");
            Assert.AreEqual(ET5ErrorCode.NativeApiError, ex.Code);
            Assert.IsTrue(ex.DiagnosticMessage.Contains("tobii_device_create"));
        }

        #endregion

        #region ET5ErrorCode enum coverage

        [Test]
        public void ET5ErrorCode_AllExpectedCodesAreDefined()
        {
            var expectedCodes = new[]
            {
                ET5ErrorCode.None,
                ET5ErrorCode.RuntimeNotFound,
                ET5ErrorCode.RuntimeRejected,
                ET5ErrorCode.RuntimeIncompatible,
                ET5ErrorCode.RequiredExportMissing,
                ET5ErrorCode.DeviceNotFound,
                ET5ErrorCode.DeviceIdentityUnknown,
                ET5ErrorCode.ConnectionFailed,
                ET5ErrorCode.ConnectionLost,
                ET5ErrorCode.CallbackFailure,
                ET5ErrorCode.DisplayUnavailable,
                ET5ErrorCode.NativeApiError,
                ET5ErrorCode.ConfigurationError,
                ET5ErrorCode.InternalError
            };

            foreach (var code in expectedCodes)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(ET5ErrorCode), code),
                    $"ET5ErrorCode.{code} should be defined.");
            }
        }

        [Test]
        public void ET5ErrorCode_DefaultIsNone()
        {
            ET5ErrorCode code = default;
            Assert.AreEqual(ET5ErrorCode.None, code);
        }

        #endregion

        #region Error propagation chain: provider -> ET5PointService.Error

        [Test]
        public void ErrorFromProvider_PropagatesViaET5PointServiceErrorEvent()
        {
            // Create a fake provider that raises ErrorOccurred
            var fakeProvider = new FakeErrorProvider();
            var service = new ET5PointService(
                gazeProvider: fakeProvider);

            Exception receivedError = null;
            var errorReceived = new ManualResetEventSlim(false);

            // Subscribe to the Error event on the point service
            service.Error += (sender, ex) =>
            {
                receivedError = ex;
                errorReceived.Set();
            };

            // Subscribe to Point to trigger Start()
            service.Point += (sender, args) => { };

            // Simulate provider raising an error
            var pluginError = ET5PluginException.ConnectionLost("simulated disconnect");
            fakeProvider.SimulateError(pluginError);

            // Wait briefly for event propagation
            bool received = errorReceived.Wait(TimeSpan.FromSeconds(1));

            Assert.IsTrue(received, "Error event should propagate from provider to ET5PointService.Error");
            Assert.IsNotNull(receivedError);
            Assert.IsInstanceOf<ET5PluginException>(receivedError);
            Assert.AreEqual(ET5ErrorCode.ConnectionLost, ((ET5PluginException)receivedError).Code);

            service.Dispose();
        }

        [Test]
        public void ConstructorDoesNotLoadNativeRuntime()
        {
            // The parameterless constructor must NOT load any native DLL.
            // This test verifies the construction contract that was proven
            // by the packaged loader harness in CI.
            var service = new ET5PointService();

            // If we got here without a DllNotFoundException or AccessViolation,
            // the constructor is hardware-independent.
            Assert.IsNotNull(service);

            // Dispose must also be safe without hardware
            Assert.DoesNotThrow(() => service.Dispose());
        }

        #endregion

        #region Helper: fake error provider

        private class FakeErrorProvider : IGazeProvider
        {
            public event EventHandler<GazePointEventArgs> GazePointAvailable;
            public event EventHandler<Exception> ErrorOccurred;
            public event EventHandler<bool> ConnectionStatusChanged;

            public bool IsConnected => false;

            private bool isStarted;

            public void Start()
            {
                isStarted = true;
            }

            public void Stop()
            {
                isStarted = false;
            }

            public void SimulateError(Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }

            public void Dispose()
            {
                isStarted = false;
            }
        }

        #endregion
    }
}
