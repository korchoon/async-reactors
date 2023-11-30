﻿using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Mk.Debugs;

namespace Mk.Routines {
#line hidden
    public class YieldAwaiter : IRoutine, ICriticalNotifyCompletion {
        internal static Pool<YieldAwaiter> Pool = new Pool<YieldAwaiter> (() => new YieldAwaiter (), awaiter => { }, onPop: a => a.Reset ());
        bool _updatedOnce;
        Action _continuation;

        public void Break () {
            if (!IsCompleted) {
                IsCompleted = true;
                Pool.Push (this);
            }
        }

        void Reset () {
            _updatedOnce = false;
            IsCompleted = false;
            _continuation = default;
            _guard = default;
        }

        public void UpdateParent () {
            if (Utils.TrySetNull (ref _continuation, out var c)) c.Invoke ();
        }

        public void Update () {
            if (!_updatedOnce) {
                _updatedOnce = true;
                return;
            }

            if (IsCompleted) return;
            this.BreakAndUpdateParent ();
        }

        #region async

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        [UsedImplicitly]
        public void GetResult () { }

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public YieldAwaiter GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public void OnCompleted (Action continuation) {
            if (IsCompleted) {
                continuation.Invoke ();
                return;
            }

            Asr.IsTrue (_continuation == null);
            _continuation = continuation;
        }

        [UsedImplicitly]
        public void UnsafeOnCompleted (Action continuation) => OnCompleted (continuation);

        #endregion
    }
#line default
}