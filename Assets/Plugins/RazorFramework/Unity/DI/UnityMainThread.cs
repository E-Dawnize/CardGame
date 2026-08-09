using System.Threading;
using UnityEngine;

namespace RazorFramework.Unity.DI
{
    internal static class UnityMainThread
    {
        private static int _initialized;
        private static int _threadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Volatile.Write(ref _threadId, 0);
            Volatile.Write(ref _initialized, 0);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureRuntimeThread()
        {
            CaptureCurrentThread();
        }

        internal static void EnsureCurrent()
        {
            var initialized = Volatile.Read(ref _initialized);
            var expectedThread = Volatile.Read(ref _threadId);
            if (initialized != 1 ||
                Thread.CurrentThread.ManagedThreadId != expectedThread)
            {
                throw new UnityInjectionException(
                    UnityInjectionErrorCode.WrongThread,
                    "Unity object injection requires the Unity main thread " +
                    "captured during runtime initialization.");
            }
        }

        internal static void InitializeForTests()
        {
            CaptureCurrentThread();
        }

        private static void CaptureCurrentThread()
        {
            var currentThread = Thread.CurrentThread.ManagedThreadId;
            if (Interlocked.CompareExchange(
                    ref _threadId,
                    currentThread,
                    0) != 0 &&
                Volatile.Read(ref _threadId) != currentThread)
            {
                throw new UnityInjectionException(
                    UnityInjectionErrorCode.WrongThread,
                    "The Unity main-thread identity cannot be replaced " +
                    "from another thread.");
            }

            Volatile.Write(ref _initialized, 1);
        }
    }
}
