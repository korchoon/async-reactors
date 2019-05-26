using System;
using System.ComponentModel;
using JetBrains.Annotations;
using Lib.Async;
using Lib.DataFlow;
using Lib.Timers;
using UnityEngine;
using UnityEngine.Events;

namespace Lib
{
    public static class ReactExperimental
    {
        [MustUseReturnValue]
        // single-next, single-onnext 
        internal static (IPub<T> pub, ISub<T> sub) PubSub11<T>(this IScope scope)
        {
            var subject = new Pub1Sub1<T>(scope);
            return (subject, subject);
        }

        [MustUseReturnValue]
        // single-next, single-onnext 
        internal static (IPub pub, ISub sub) PubSub11(this IScope scope)
        {
            var subject = new Pub1Sub1(scope);
            return (subject, subject);
        }


        public static CachedSubject<T> FromEventCached<T>(Action<Action<T>> sub, Action<Action<T>> unsub, IScope scope)
        {
            var s = new CachedSubject<T>(scope);
            sub.Invoke(Action);
            scope.OnDispose(() => unsub.Invoke(Action));

            return s;

            void Action(T msg) => s.Next(msg);
        }

        public static ISub<T> FromEvent<T>(Action<Action<T>> sub, Action<Action<T>> unsub, IScope scope)
        {
            var (pub, res) = React.PubSub<T>(scope);
            sub.Invoke(Action);
            scope.OnDispose(() => unsub.Invoke(Action));

            return res;

            void Action(T msg) => pub.Next(msg);
        }

        public static void DelayedAction(this float sec, Action continuation, IScope scope)
        {
            var scopeAwaiter = sec.GetAwaiter();
            scopeAwaiter.UnsafeOnCompleted(continuation);
        }

        public static Func<ISub<T>> FromRoutine<T>(this Func<Routine<T>> callback, IScope scope)
        {
            return DynamicMethod;

            ISub<T> DynamicMethod()
            {
                var routine = callback.Invoke();
                var (p, s) = scope.PubSub<T>();

                scope.OnDispose(routine.BreakInnerFromOuter.Pub.Next);
                var aw = routine.GetAwaiter();
                routine.Scope.Sub.OnDispose(() =>
                {
                    try
                    {
                        var r = aw.GetResult();
                        p.Next(r);
                    }
                    catch (Exception _)
                    {
//                        Debug.Log(e); // todo testcase
                    }
                });
                return s;
            }
        }

        [MustUseReturnValue]
        public static (IDisposable disposable, IScope scope) ScopeTuple()
        {
            var res = new ScopeStack();
            return (res, res);
        }

        public static void FromAction(out IPub pub, Action t, IScope sd) => pub = FromAction(t, sd);
        public static void FromAction<T>(out IPub<T> pub, Action<T> t, IScope sd) => pub = FromAction(t, sd);

        public static IPub FromAction(this Action t, IScope sd)
        {
            var (pub1, sub1) = React.PubSub(sd);
            sub1.OnNext(t.Invoke, sd);
            return pub1;
        }

        public static IPub<T> FromAction<T>(this Action<T> t, IScope sd)
        {
            var (pub1, sub1) = React.PubSub<T>(sd);
            sub1.OnNext(t.Invoke, sd);
            return pub1;
        }

        public static IPub Wrap(this IPub pub, Action<IPub> proxy, IScope sd)
        {
            var (pub1, sub1) = React.PubSub(sd);
            sub1.OnNext(Pub, sd);
            return pub1;

            void Pub() => proxy.Invoke(pub);
        }

        public static IPub<T> Wrap<T>(this IPub<T> pub, Action<T, IPub<T>> proxy, IScope sd)
        {
            var (pub1, sub1) = React.PubSub<T>(sd);
            sub1.OnNext(Pub, sd);
            return pub1;

            void Pub(T msg) => proxy.Invoke(msg, pub);
        }

        [MustUseReturnValue]
        internal static IDisposeWith<Exception> ErrScope(out IErrorScope<Exception> scope)
        {
            return new CatchStack(out scope);
        }

        public static ISub<T> ToSub<T>(Func<IPub<T>, Routine> ctor, IScope scope)
        {
            IPub<T> pub;
            ISub<T> sub;
            (pub, sub) = React.PubSub<T>(scope);
            var res = sub;
            ctor.Invoke(pub).DisposeOn(scope);
            return res;
        }

        public static ISub ToSub(this Routine r, IScope scope)
        {
            var (pub, sub) = React.PubSub(scope);

            // will implicitly dispose r
            Local().DisposeOn(scope);
            return sub;

            async Routine Local()
            {
                try
                {
                    await r;
                }
                finally
                {
                    pub.Next();
                }
            }
        }

        public static void Every(float timeout, Action action, IScope scope)
        {
            Inner().DisposeOn(scope);

            async Routine Inner()
            {
                while (true)
                {
                    await timeout;
                    action();
                }
            }
        }

        public static T1 DisposeOn<T1>(this T1 dispose, IScope scope) where T1 : IDisposable
        {
            scope.OnDispose(dispose.Dispose);
            return dispose;
        }
    }
}