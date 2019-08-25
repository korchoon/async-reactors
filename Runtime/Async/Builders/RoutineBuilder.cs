// ----------------------------------------------------------------------------
// The MIT License
// Async Reactors framework https://github.com/korchoon/async-reactors
// Copyright (c) 2016-2019 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

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
        [UsedImplicitly] public Routine Task { get; }

        RoutineBuilder()
        {
            Task = new Routine();
            Task.Scope.Subscribe(BreakCurrent);
        }

        void BreakCurrent()
        {
            var i = _innerAwaiter;
            _innerAwaiter = null;
            if (i == null) return;

            i.Unsub();
            i.BreakInner();
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
            if (Task.Scope.Disposing)
                return;
         
            Task.Complete.Next();
        }

        [UsedImplicitly]
        public void SetException(Exception e)
        {
            Debug.LogException(e);
            SchPub.PubError.Next(e);
            Task.Complete.Next();
        }

        [UsedImplicitly]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            switch (awaiter)
            {
                case IBreakableAwaiter breakableAwaiter:
                    _innerAwaiter = breakableAwaiter;
                    awaiter.OnCompleted(_continuation);
                    break;
                case SelfScopeAwaiter selfScopeAwaiter:
                    selfScopeAwaiter.Value = Task.Scope;
                    awaiter.OnCompleted(_continuation);
                    break;
                case SelfDisposeAwaiter selfDisposeAwaiter:
                    selfDisposeAwaiter.Value = Task._Dispose;
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
        public void SetStateMachine(IAsyncStateMachine stateMachine) => _continuation = stateMachine.MoveNext;
    }
}