using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    public sealed class AnonymousScreenLifecycleEvent : IScreenLifecycleEvent
    {
        public event Action<Memory<object>> OnDidPushEnter;
        public event Action<Memory<object>> OnDidPushExit;
        public event Action<Memory<object>> OnDidPopEnter;
        public event Action<Memory<object>> OnDidPopExit;

        public AnonymousScreenLifecycleEvent(
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

        async UniTask IScreenLifecycleEvent.Initialize(Memory<object> args)
        {
            foreach (var onInitialize in OnInitialize)
                await onInitialize.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.WillPushEnter(Memory<object> args)
        {
            foreach (var onWillPushEnter in OnWillPushEnter)
                await onWillPushEnter.Invoke(args);
        }

        void IScreenLifecycleEvent.DidPushEnter(Memory<object> args)
        {
            OnDidPushEnter?.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.WillPushExit(Memory<object> args)
        {
            foreach (var onWillPushExit in OnWillPushExit)
                await onWillPushExit.Invoke(args);
        }

        void IScreenLifecycleEvent.DidPushExit(Memory<object> args)
        {
            OnDidPushExit?.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.WillPopEnter(Memory<object> args)
        {
            foreach (var onWillPopEnter in OnWillPopEnter)
                await onWillPopEnter.Invoke(args);
        }

        void IScreenLifecycleEvent.DidPopEnter(Memory<object> args)
        {
            OnDidPopEnter?.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.WillPopExit(Memory<object> args)
        {
            foreach (var onWillPopExit in OnWillPopExit)
                await onWillPopExit.Invoke(args);
        }

        void IScreenLifecycleEvent.DidPopExit(Memory<object> args)
        {
            OnDidPopExit?.Invoke(args);
        }

        async UniTask IScreenLifecycleEvent.Cleanup()
        {
            foreach (var onCleanup in OnCleanup)
                await onCleanup.Invoke();
        }
    }
}