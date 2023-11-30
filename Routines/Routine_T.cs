﻿#if DEBUG
// #define MK_TRACE
#endif
using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Leopotam.EcsLite;
using Mk.Debugs;
using UnityEngine;

namespace Mk.Routines {
#if MK_TRACE
    [Serializable]
#endif
#line hidden
    [AsyncMethodBuilder (typeof (RoutineBuilder<>))]
    public sealed class Routine<T> : IRoutine<T>, ICriticalNotifyCompletion {
        [HideInInspector] public Action _Start;
        public string __Await;
        internal Rollback _rollback;
        [SerializeReference] internal IRoutine CurrentAwaiter;
        [SerializeReference] internal Option<AttachedRoutines> AttachedRoutines;
        Action _continuation;
        internal T _cached;
        internal bool _hasValue;
#if MK_TRACE
        EcsPackedEntityWithWorld _entity;
#endif
        internal Routine () {
#if MK_TRACE
            var newEntity = EditorLocator.World.NewEntity ();
            _entity = EditorLocator.World.PackEntityWithWorld (newEntity);
            EditorLocator.World.GetPool<__RoutineInfo> ().Add (newEntity).Value = this;
#endif
        }


        public void Break () {
            if (IsCompleted) return;

            _Start = null;
            IsCompleted = true;
            if (AttachedRoutines.TryGet (out var attachedRoutines)) {
                AttachedRoutines = default;
                for (var i = attachedRoutines.Routines.Count - 1; i >= 0; i--) attachedRoutines.Routines[i].Break ();
            }

            if (Utils.TrySetNull (ref CurrentAwaiter, out var buf)) buf.Break ();
            _rollback?.Dispose ();
#if MK_TRACE
            __Await = $"[Interrupted at] {__Await}";
#endif
        }

        void IRoutine.UpdateParent () {
            if (AttachedRoutines.TryGet (out var attachedRoutines))
                for (var i = 0; i < attachedRoutines.Routines.Count; i++)
                    attachedRoutines.Routines[i].UpdateParent ();

            if (Utils.TrySetNull (ref _continuation, out var c)) c.Invoke ();
        }

        public void Update () {
            if (IsCompleted) return;

            if (Utils.TrySetNull (ref _Start, out var s)) {
                s.Invoke ();
                return;
            }

            if (AttachedRoutines.TryGet (out var attachedRoutines))
                for (var i = 0; i < attachedRoutines.Routines.Count; i++)
                    attachedRoutines.Routines[i].Update ();

            CurrentAwaiter?.Update ();
        }

        public Option<T> TryGetResult () => _hasValue ? _cached : default;

        #region async

        [UsedImplicitly]
        public bool IsCompleted { get; private set; }

        CalledOnceGuard _guard;

        [UsedImplicitly]
        public Routine<T> GetAwaiter () {
            _guard.Assert ();
            return this;
        }

        [UsedImplicitly]
        public T GetResult () {
            Asr.IsTrue (_hasValue);
            return _cached;
        }

        public void OnCompleted (Action continuation) {
            if (IsCompleted) {
                continuation.Invoke ();
                return;
            }

            Asr.IsTrue (_continuation == null);
            _continuation = continuation;
        }

        public void UnsafeOnCompleted (Action continuation) => OnCompleted (continuation);

        #endregion
    }
#line default
}