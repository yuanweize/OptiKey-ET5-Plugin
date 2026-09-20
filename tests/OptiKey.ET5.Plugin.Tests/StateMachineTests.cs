using System.Collections.Generic;
using NUnit.Framework;
using OptiKey.ET5.Plugin.State;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class StateMachineTests
    {
        [Test]
        public void InitialState_IsCreated()
        {
            var sm = new GazeServiceStateMachine();
            Assert.AreEqual(GazeServiceState.Created, sm.CurrentState);
            Assert.IsFalse(sm.IsRunning);
        }

        [Test]
        public void ValidTransitions_Succeed()
        {
            var sm = new GazeServiceStateMachine();

            Assert.IsTrue(sm.TryTransition(GazeServiceState.Starting));
            Assert.AreEqual(GazeServiceState.Starting, sm.CurrentState);
            Assert.IsTrue(sm.IsRunning);

            Assert.IsTrue(sm.TryTransition(GazeServiceState.Connected));
            Assert.AreEqual(GazeServiceState.Connected, sm.CurrentState);

            Assert.IsTrue(sm.TryTransition(GazeServiceState.Reconnecting));
            Assert.AreEqual(GazeServiceState.Reconnecting, sm.CurrentState);

            Assert.IsTrue(sm.TryTransition(GazeServiceState.Connected));
            Assert.AreEqual(GazeServiceState.Connected, sm.CurrentState);

            Assert.IsTrue(sm.TryTransition(GazeServiceState.Stopping));
            Assert.AreEqual(GazeServiceState.Stopping, sm.CurrentState);

            Assert.IsTrue(sm.TryTransition(GazeServiceState.Stopped));
            Assert.AreEqual(GazeServiceState.Stopped, sm.CurrentState);
            Assert.IsFalse(sm.IsRunning);

            Assert.IsTrue(sm.TryTransition(GazeServiceState.Starting));
            Assert.AreEqual(GazeServiceState.Starting, sm.CurrentState);
        }

        [Test]
        public void InvalidTransitions_AreRejected()
        {
            var sm = new GazeServiceStateMachine();

            // Cannot jump directly from Created to Connected
            Assert.IsFalse(sm.TryTransition(GazeServiceState.Connected));
            Assert.AreEqual(GazeServiceState.Created, sm.CurrentState);

            // Cannot jump directly from Created to Reconnecting
            Assert.IsFalse(sm.TryTransition(GazeServiceState.Reconnecting));
            Assert.AreEqual(GazeServiceState.Created, sm.CurrentState);
        }

        [Test]
        public void DisposedIsTerminal_CannotTransitionOut()
        {
            var sm = new GazeServiceStateMachine();
            sm.TryTransition(GazeServiceState.Starting);
            sm.TryTransition(GazeServiceState.Disposed);

            Assert.AreEqual(GazeServiceState.Disposed, sm.CurrentState);
            Assert.IsFalse(sm.TryTransition(GazeServiceState.Created));
            Assert.IsFalse(sm.TryTransition(GazeServiceState.Starting));
            Assert.IsFalse(sm.TryTransition(GazeServiceState.Connected));
            Assert.AreEqual(GazeServiceState.Disposed, sm.CurrentState);
        }

        [Test]
        public void ErrorEvents_AreEmittedDuringTransitions()
        {
            var sm = new GazeServiceStateMachine();
            sm.TryTransition(GazeServiceState.Starting);

            GazeServiceError capturedError = null;
            sm.ErrorOccurred += (s, e) => capturedError = e;

            var err = new GazeServiceError("USB_DISCONNECT", "Hardware detached");
            sm.TryTransition(GazeServiceState.Reconnecting, err);

            Assert.IsNotNull(capturedError);
            Assert.AreEqual("USB_DISCONNECT", capturedError.ErrorCode);
            Assert.AreEqual(err, sm.LastError);
        }
    }
}
