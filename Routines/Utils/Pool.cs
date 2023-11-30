﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mk.Debugs;
using Mk;

namespace Mk.Routines {
    
    class Pool<T> : IDisposable where T : class {
        static Action<T> Empty { get; } = _ => { };
        Func<T> _ctor;
        internal Stack<T> _stack;

        // todo place asserts on app quit
        Action<T> _onPush;
        Action<T> _onPop;
        Action<T> _destroy;


        public Pool(Func<T> ctor, Action<T> onPush, Action<T> onDestroy = null, Action<T> onPop = null) {
            _ctor = ctor;
            _onPush = onPush;
            _onPop = onPop;
#if !M_DISABLE_POOLING
            _stack = new Stack<T>();
            _destroy = onDestroy ?? Empty;
#endif
        }

        public T Pop() {
            Pop(out var res);
            return res;
        }
#if false

        public T Pop(IScope scope) {
            Pop(out var res);
            var t = Disposable.Pool.Pop();
            // _disposables.scope.Add();
            return res;
        }

        class Disposable : IDisposable {
            // public static Pool<Disposable> Pool = new Pool<Disposable>(() => new Disposable(), disposable => disposable.Reset());
            readonly Pool<T> _pool;
            public T Target;

            public Disposable(Pool<T> pool) {
                _pool = pool;
            }

            public void Reset() => Target = default;

            public void Dispose() {
                _pool.PushRef(ref Target);
                Pool.Push(this);
            }
        }
#endif


        public void Pop(out T res) {
#if M_DISABLE_POOLING
            return _ctor.Invoke();
#else
            if (_stack.Count == 0) {
                res = _ctor();
                Asr.IsNotNull(res);
            }
            else
                res = _stack.Pop();

            _onPop?.Invoke(res);
#endif
        }

        public void PushRef(ref T element) {
#if !M_DISABLE_POOLING
            Asr.IsFalse(_stack.Count > 0 && ReferenceEquals(_stack.Peek(), element),
                "Internal error. Trying to release object that is already released to pool. ");

            _stack.Push(element);
#endif
            _onPush.Invoke(element);
            element = null;
        }

        public void Push(T element) {
#if !M_DISABLE_POOLING
            Asr.IsFalse(_stack.Count > 0 && ReferenceEquals(_stack.Peek(), element),
                "Internal error. Trying to release object that is already released to pool. ");

            _stack.Push(element);
#endif
            _onPush.Invoke(element);
        }

        public void Dispose() {
#if !M_DISABLE_POOLING
            while (_stack.Count > 0) {
                var t = _stack.Pop();
                _destroy.Invoke(t);
            }
#endif
        }

        [MustUseReturnValue]
        public _Scope GetScoped(out T tmp) {
            Pop(out tmp);
            return new _Scope(this, ref tmp);
        }

        public struct _Scope : IDisposable {
            Pool<T> _pool;
            T _val;

            internal _Scope(Pool<T> pool, ref T val) {
                _pool = pool;
                _val = val;
            }

            public void Dispose() {
                _pool.PushRef(ref _val);
            }
        }
    }
}