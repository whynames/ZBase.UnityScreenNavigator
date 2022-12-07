using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public sealed class AnonymousSheetLifecycleEvent : ISheetLifecycleEvent
    {
        public event Action<Memory<object>> OnDidEnter;
        public event Action<Memory<object>> OnDidExit;

        public AnonymousSheetLifecycleEvent(
              Func<Memory<object>, UniTask> initialize = null
            , Func<Memory<object>, UniTask> onWillEnter = null, Action<Memory<object>> onDidEnter = null
            , Func<Memory<object>, UniTask> onWillExit = null, Action<Memory<object>> onDidExit = null
            , Func<UniTask> onCleanup = null
        )
        {
            if (initialize != null)
                OnInitialize.Add(initialize);

            if (onWillEnter != null)
                OnWillEnter.Add(onWillEnter);

            OnDidEnter = onDidEnter;

            if (onWillExit != null)
                OnWillExit.Add(onWillExit);

            OnDidExit = onDidExit;

            if (onCleanup != null)
                OnCleanup.Add(onCleanup);
        }

        public List<Func<Memory<object>, UniTask>> OnInitialize { get; } = new();

        public List<Func<Memory<object>, UniTask>> OnWillEnter { get; } = new();

        public List<Func<Memory<object>, UniTask>> OnWillExit { get; } = new();

        public List<Func<UniTask>> OnCleanup { get; } = new();

        async UniTask ISheetLifecycleEvent.Initialize(Memory<object> args)
        {
            foreach (var onInitialize in OnInitialize)
                await onInitialize.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.WillEnter(Memory<object> args)
        {
            foreach (var onWillEnter in OnWillEnter)
                await onWillEnter.Invoke(args);
        }

        void ISheetLifecycleEvent.DidEnter(Memory<object> args)
        {
            OnDidEnter?.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.WillExit(Memory<object> args)
        {
            foreach (var onWillExit in OnWillExit)
                await onWillExit.Invoke(args);
        }

        void ISheetLifecycleEvent.DidExit(Memory<object> args)
        {
            OnDidExit?.Invoke(args);
        }

        async UniTask ISheetLifecycleEvent.Cleanup()
        {
            foreach (var onCleanup in OnCleanup)
                await onCleanup.Invoke();
        }
    }
}