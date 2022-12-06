using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Shared;
using ZBase.UnityScreenNavigator.Core.Shared.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.Animation;
using ZBase.UnityScreenNavigator.Foundation.Coroutine;
using ZBase.UnityScreenNavigator.Foundation.PriorityCollection;
using System.Linq;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    [DisallowMultipleComponent]
    public class Sheet : View, ISheetLifecycleEvent
    {
        [SerializeField]
        private int _renderingOrder;

        [SerializeField]
        private SheetTransitionAnimationContainer _animationContainer = new();

        private readonly PriorityList<ISheetLifecycleEvent> _lifecycleEvents = new();
        private Progress<float> _transitionProgressReporter;

        private Progress<float> TransitionProgressReporter
        {
            get
            {
                if (_transitionProgressReporter == null)
                    _transitionProgressReporter = new Progress<float>(SetTransitionProgress);
                return _transitionProgressReporter;
            }
        }

        public SheetTransitionAnimationContainer AnimationContainer => _animationContainer;

        public bool IsTransitioning { get; private set; }

        /// <summary>
        ///     Return the transition animation type currently playing.
        ///     If not in transition, return null.
        /// </summary>
        public SheetTransitionAnimationType? TransitionAnimationType { get; private set; }

        /// <summary>
        ///     Progress of the transition animation.
        /// </summary>
        public float TransitionAnimationProgress { get; private set; }

        /// <summary>
        ///     Event when the transition animation progress changes.
        /// </summary>
        public event Action<float> TransitionAnimationProgressChanged;

        public virtual UniTask Initialize(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask WillEnter(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        public virtual void DidEnter(Memory<object> args)
        {
        }

        public virtual UniTask WillExit(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        public virtual void DidExit(Memory<object> args)
        {
        }

        public virtual UniTask Cleanup()
        {
            return UniTask.CompletedTask;
        }

        public void AddLifecycleEvent(ISheetLifecycleEvent lifecycleEvent, int priority = 0)
        {
            _lifecycleEvents.Add(lifecycleEvent, priority);
        }

        public void RemoveLifecycleEvent(ISheetLifecycleEvent lifecycleEvent)
        {
            _lifecycleEvents.Remove(lifecycleEvent);
        }

        internal AsyncProcessHandle AfterLoad(RectTransform parentTransform, Memory<object> args)
        {
            _lifecycleEvents.Add(this, 0);
            SetIdentifer();

            Parent = parentTransform;
            RectTransform.FillParent((RectTransform)Parent);

            // Set order of rendering.
            var siblingIndex = 0;
            for (var i = 0; i < Parent.childCount; i++)
            {
                var child = Parent.GetChild(i);
                var childPage = child.GetComponent<Sheet>();
                siblingIndex = i;
                if (_renderingOrder >= childPage._renderingOrder)
                {
                    continue;
                }

                break;
            }

            RectTransform.SetSiblingIndex(siblingIndex);
            gameObject.SetActive(false);

            return CoroutineManager.Run<Sheet>(
                CreateCoroutine(_lifecycleEvents.Select(x => x.Initialize(args)))
            );
        }

        internal AsyncProcessHandle BeforeEnter(Memory<object> args)
        {
            return CoroutineManager.Run<Sheet>(BeforeEnterRoutine(args));
        }

        private IEnumerator BeforeEnterRoutine(Memory<object> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = SheetTransitionAnimationType.Enter;
            gameObject.SetActive(true);
            RectTransform.FillParent((RectTransform)Parent);
            SetTransitionProgress(0.0f);

            Alpha = 0.0f;

            var handle = CoroutineManager.Run<Sheet>(
                CreateCoroutine(_lifecycleEvents.Select(x => x.WillEnter(args)))
            );

            while (!handle.IsTerminated)
            {
                yield return null;
            }
        }

        internal AsyncProcessHandle Enter(bool playAnimation, Sheet partnerSheet)
        {
            return CoroutineManager.Run<Sheet>(EnterRoutine(playAnimation, partnerSheet));
        }

        private IEnumerator EnterRoutine(bool playAnimation, Sheet partnerSheet)
        {
            Alpha = 1.0f;

            if (playAnimation)
            {
                var anim = _animationContainer.GetAnimation(true, partnerSheet?.Identifier);
                if (anim == null)
                {
                    anim = UnityScreenNavigatorSettings.Instance.GetDefaultSheetTransitionAnimation(true);
                }

                anim.SetPartner(partnerSheet?.transform as RectTransform);
                anim.Setup(RectTransform);
                yield return CoroutineManager.Run<Sheet>(anim.CreatePlayRoutine(TransitionProgressReporter));
            }

            RectTransform.FillParent((RectTransform)Parent);
        }

        internal void AfterEnter(Memory<object> args)
        {
            foreach (var lifecycleEvent in _lifecycleEvents)
            {
                lifecycleEvent.DidEnter(args);
            }

            IsTransitioning = false;
            TransitionAnimationType = null;
        }

        internal AsyncProcessHandle BeforeExit(Memory<object> args)
        {
            return CoroutineManager.Run<Sheet>(BeforeExitRoutine(args));
        }

        private IEnumerator BeforeExitRoutine(Memory<object> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = SheetTransitionAnimationType.Exit;
            gameObject.SetActive(true);
            RectTransform.FillParent((RectTransform)Parent);
            SetTransitionProgress(0.0f);

            Alpha = 1.0f;

            var handle = CoroutineManager.Run<Sheet>(CreateCoroutine(_lifecycleEvents.Select(x => x.WillExit(args))));
            while (!handle.IsTerminated)
            {
                yield return null;
            }
        }

        internal AsyncProcessHandle Exit(bool playAnimation, Sheet partnerSheet)
        {
            return CoroutineManager.Run<Sheet>(ExitRoutine(playAnimation, partnerSheet));
        }

        private IEnumerator ExitRoutine(bool playAnimation, Sheet partnerSheet)
        {
            if (playAnimation)
            {
                var anim = _animationContainer.GetAnimation(false, partnerSheet?.Identifier);
                if (anim == null)
                {
                    anim = UnityScreenNavigatorSettings.Instance.GetDefaultSheetTransitionAnimation(false);
                }

                anim.SetPartner(partnerSheet?.transform as RectTransform);
                anim.Setup(RectTransform);
                yield return CoroutineManager.Run<Sheet>(anim.CreatePlayRoutine(TransitionProgressReporter));
            }

            Alpha = 0.0f;
            SetTransitionProgress(1.0f);
        }

        internal void AfterExit(Memory<object> args)
        {
            foreach (var lifecycleEvent in _lifecycleEvents)
            {
                lifecycleEvent.DidExit(args);
            }

            gameObject.SetActive(false);
        }

        internal AsyncProcessHandle BeforeRelease()
        {
            return CoroutineManager.Run<Sheet>(CreateCoroutine(_lifecycleEvents.Select(x => x.Cleanup())));
        }

        private IEnumerator CreateCoroutine(IEnumerable<UniTask> targets)
        {
            foreach (var target in targets)
            {
                var handle = CoroutineManager.Run<Sheet>(CreateCoroutine(target));
                if (!handle.IsTerminated)
                {
                    yield return handle;
                }
            }
        }

        private IEnumerator CreateCoroutine(UniTask target)
        {
            async void WaitTaskAndCallback(UniTask task, Action callback)
            {
                await task;
                callback?.Invoke();
            }
            
            var isCompleted = false;
            WaitTaskAndCallback(target, () =>
            {
                isCompleted = true;
            });
            return new WaitUntil(() => isCompleted);
        }

        private void SetTransitionProgress(float progress)
        {
            TransitionAnimationProgress = progress;
            TransitionAnimationProgressChanged?.Invoke(progress);
        }
    }
}