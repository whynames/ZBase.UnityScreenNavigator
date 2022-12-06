using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public sealed class AnonymousModalLifecycleEvent : IModalLifecycleEvent
    {
        public event Action<Memory<object>> OnDidPushEnter;
        public event Action<Memory<object>> OnDidPushExit;
        public event Action<Memory<object>> OnDidPopEnter;
        public event Action<Memory<object>> OnDidPopExit;

        public AnonymousModalLifecycleEvent(
              Func<Memory<object>, UniTask> initialize = null
            , Func<Memory<object>, UniTask> onWillPushEnter = null, Action<Memory<object>> onDidPushEnter = null
            , Func<Memory<object>, UniTask> onWillPushExit = null, Action<Memory<object>> onDidPushExit = null
            , Func<Memory<object>, UniTask> onWillPopEnter = null, Action<Memory<object>> onDidPopEnter = null
            , Func<Memory<object>, UniTask> onWillPopExit = null, Action<Memory<object>> onDidPopExit = null
            , Func<UniTask> onCleanup = null
        )
        {
            if (initialize != null)
                OnInitialize.Add(initialize);

            if (onWillPushEnter != null)
                OnWillPushEnter.Add(onWillPushEnter);

            OnDidPushEnter = onDidPushEnter;

            if (onWillPushExit != null)
                OnWillPushExit.Add(onWillPushExit);

            OnDidPushExit = onDidPushExit;

            if (onWillPopEnter != null)
                OnWillPopEnter.Add(onWillPopEnter);

            OnDidPopEnter = onDidPopEnter;

            if (onWillPopExit != null)
                OnWillPopExit.Add(onWillPopExit);

            OnDidPopExit = onDidPopExit;

            if (onCleanup != null)
                OnCleanup.Add(onCleanup);
        }

        public List<Func<Memory<object>, UniTask>> OnInitialize { get; } = new();

        public List<Func<Memory<object>, UniTask>> OnWillPushEnter { get; } = new();

        public List<Func<Memory<object>, UniTask>> OnWillPushExit { get; } = new();

        public List<Func<Memory<object>, UniTask>> OnWillPopEnter { get; } = new();

        public List<Func<Memory<object>, UniTask>> OnWillPopExit { get; } = new();

        public List<Func<UniTask>> OnCleanup { get; } = new();

        async UniTask IModalLifecycleEvent.Initialize(Memory<object> args)
        {
            foreach (var onInitialize in OnInitialize)
                await onInitialize.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.WillPushEnter(Memory<object> args)
        {
            foreach (var onWillPushEnter in OnWillPushEnter)
                await onWillPushEnter.Invoke(args);
        }

        void IModalLifecycleEvent.DidPushEnter(Memory<object> args)
        {
            OnDidPushEnter?.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.WillPushExit(Memory<object> args)
        {
            foreach (var onWillPushExit in OnWillPushExit)
                await onWillPushExit.Invoke(args);
        }

        void IModalLifecycleEvent.DidPushExit(Memory<object> args)
        {
            OnDidPushExit?.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.WillPopEnter(Memory<object> args)
        {
            foreach (var onWillPopEnter in OnWillPopEnter)
                await onWillPopEnter.Invoke(args);
        }

        void IModalLifecycleEvent.DidPopEnter(Memory<object> args)
        {
            OnDidPopEnter?.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.WillPopExit(Memory<object> args)
        {
            foreach (var onWillPopExit in OnWillPopExit)
                await onWillPopExit.Invoke(args);
        }

        void IModalLifecycleEvent.DidPopExit(Memory<object> args)
        {
            OnDidPopExit?.Invoke(args);
        }

        async UniTask IModalLifecycleEvent.Cleanup()
        {
            foreach (var onCleanup in OnCleanup)
                await onCleanup.Invoke();
        }
    }
}