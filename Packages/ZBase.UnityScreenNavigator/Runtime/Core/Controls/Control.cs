using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.PriorityCollection;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    [DisallowMultipleComponent]
    public class Control : View, IControlLifecycleEvent
    {
        [SerializeField]
        private ControlTransitionAnimationContainer _animationContainer = new();

        private readonly UniquePriorityList<IControlLifecycleEvent> _lifecycleEvents = new();
        private Progress<float> _transitionProgressReporter;

        private Progress<float> TransitionProgressReporter
        {
            get => _transitionProgressReporter ??= new Progress<float>(SetTransitionProgress);
        }

        public ControlTransitionAnimationContainer AnimationContainer => _animationContainer;

        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Return the transition animation type currently playing.
        /// If not in transition, return null.
        /// </summary>
        public ControlTransitionAnimationType? TransitionAnimationType { get; private set; }

        /// <summary>
        /// Progress of the transition animation.
        /// </summary>
        public float TransitionAnimationProgress { get; private set; }

        /// <summary>
        /// Event when the transition animation progress changes.
        /// </summary>
        public event Action<float> TransitionAnimationProgressChanged;

        /// <inheritdoc/>
        public virtual UniTask Initialize(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual UniTask WillEnter(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidEnter(Memory<object> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask WillExit(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void DidExit(Memory<object> args)
        {
        }

        /// <inheritdoc/>
        public virtual UniTask Cleanup()
        {
            return UniTask.CompletedTask;
        }

        public void AddLifecycleEvent(IControlLifecycleEvent lifecycleEvent, int priority = 0)
        {
            _lifecycleEvents.Add(lifecycleEvent, priority);
        }

        public void RemoveLifecycleEvent(IControlLifecycleEvent lifecycleEvent)
        {
            _lifecycleEvents.Remove(lifecycleEvent);
        }

        internal async UniTask AfterLoadAsync(RectTransform parentTransform, Memory<object> args)
        {
            _lifecycleEvents.Add(this, 0);
            SetIdentifer();

            Parent = parentTransform;
            OnAfterLoad(parentTransform);
            gameObject.SetActive(false);

            var tasks = _lifecycleEvents.Select(x => x.Initialize(args));
            await WaitForAsync(tasks);
        }

        protected virtual void OnAfterLoad(RectTransform parentTransform) { }

        internal async UniTask BeforeEnterAsync(Memory<object> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = ControlTransitionAnimationType.Enter;
            gameObject.SetActive(true);
            OnBeforeEnter();
            SetTransitionProgress(0.0f);
            Alpha = 0.0f;

            var tasks = _lifecycleEvents.Select(x => x.WillEnter(args));
            await WaitForAsync(tasks);
        }

        protected virtual void OnBeforeEnter() { }

        internal async UniTask EnterAsync(bool playAnimation, Control partnerControl)
        {
            Alpha = 1.0f;

            if (playAnimation)
            {
                var anim = GetAnimation(true, partnerControl);

                if (partnerControl)
                {
                    anim.SetPartner(partnerControl.RectTransform);
                }

                anim.Setup(RectTransform);

                await anim.PlayAsync(TransitionProgressReporter);
            }

            RectTransform.FillParent(Parent);
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

        internal async UniTask BeforeExitAsync(Memory<object> args)
        {
            IsTransitioning = true;
            TransitionAnimationType = ControlTransitionAnimationType.Exit;
            gameObject.SetActive(true);
            OnBeforeExit();
            SetTransitionProgress(0.0f);

            Alpha = 1.0f;

            var tasks = _lifecycleEvents.Select(x => x.WillExit(args));
            await WaitForAsync(tasks);
        }

        protected virtual void OnBeforeExit() { }

        internal async UniTask ExitAsync(bool playAnimation, Control partnerControl)
        {
            if (playAnimation)
            {
                var anim = GetAnimation(false, partnerControl);

                if (partnerControl)
                {
                    anim.SetPartner(partnerControl.RectTransform);
                }

                anim.Setup(RectTransform);

                await anim.PlayAsync(TransitionProgressReporter);
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

        internal async UniTask BeforeReleaseAsync()
        {
            var tasks = _lifecycleEvents.Select(x => x.Cleanup());
            await WaitForAsync(tasks);
        }

        private void SetTransitionProgress(float progress)
        {
            TransitionAnimationProgress = progress;
            TransitionAnimationProgressChanged?.Invoke(progress);
        }

        protected virtual ITransitionAnimation GetAnimation(bool enter, Control partner)
        {
            var partnerIdentifier = partner == true ? partner.Identifier : string.Empty;
            var anim = _animationContainer.GetAnimation(enter, partnerIdentifier);

            if (anim == null)
            {
                return Settings.GetDefaultControlTransitionAnimation(enter);
            }

            return anim;
        }
    }
}