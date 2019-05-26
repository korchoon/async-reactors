using System;
using System.Runtime.CompilerServices;
using System.Security;
using JetBrains.Annotations;
using Lib.DataFlow;
using Utility.Asserts;
using Debug = UnityEngine.Debug;

namespace Lib.Async
{
    public class RoutineBuilder
    {
        Action _continuation;
        IBreakableAwaiter _innerAwaiter;
        [UsedImplicitly] public Routine Task { get; private set; }

        RoutineBuilder()
        {
            Task = new Routine(BreakCurrent);

            void BreakCurrent()
            {
                var i = _innerAwaiter;
                _innerAwaiter = null;
                i?.BreakInner();
            }
        }

        [UsedImplicitly]
        public static RoutineBuilder Create() => new RoutineBuilder();


        [UsedImplicitly]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            _continuation = stateMachine.MoveNext;
            _continuation.Invoke();
        }

        [UsedImplicitly]
        public void SetResult()
        {
            Task.Complete.Pub.Next();
        }

        [UsedImplicitly]
        public void SetException(Exception e)
        {
            Debug.LogException(e);
            SchPub.PubError.Next(e);
            Task.Complete.Pub.Next();
        }

        [UsedImplicitly]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            switch (awaiter)
            {
                case IBreakableAwaiter breakableAwaiter:
                    awaiter.OnCompleted(_continuation);
                    _innerAwaiter = breakableAwaiter;
                    break;
                case SelfScopeAwaiter selfScopeAwaiter:
                    selfScopeAwaiter.Value = Task._scope.Sub;
                    awaiter.OnCompleted(_continuation);
                    break;
                default:
                    Asr.Fail("passed unbreakable awaiter");
                    break;
            }
        }


        [SecuritySafeCritical, UsedImplicitly]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine =>
            AwaitOnCompleted(ref awaiter, ref stateMachine);


        [UsedImplicitly]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            _continuation = stateMachine.MoveNext;
        }
    }
}