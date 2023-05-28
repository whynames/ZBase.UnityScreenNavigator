using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Foundation.Collections;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    public sealed class SimpleControlContainer : ControlContainerBase
    {
        private static Dictionary<int, SimpleControlContainer> s_instanceCacheByTransform = new();
        private static Dictionary<string, SimpleControlContainer> s_instanceCacheByName = new();

        [SerializeField] private RectTransform _content;
        [SerializeField] private bool _disableInteractionInTransition;

        private readonly List<ISimpleControlContainerCallbackReceiver> _callbackReceivers = new();
        private readonly Dictionary<int, ViewRef<Control>> _controls = new();

        public RectTransform Content
        {
            get
            {
                if (_content == false)
                    _content = RectTransform;

                return _content;
            }
        }

        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instanceCacheByTransform = new();
            s_instanceCacheByName = new();
        }

        protected override void Awake()
        {
            s_instanceCacheByName[ContainerName] = this;

            base.Awake();

            _callbackReceivers.AddRange(GetComponents<ISimpleControlContainerCallbackReceiver>());
        }

        public void Deinitialize(params object[] args)
        {
            var controls = _controls;

            foreach (var controlRef in controls.Values)
            {
                controlRef.View.Deinitialize(args);
                DestroyAndForget(controlRef);
            }

            controls.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            var controls = _controls;

            foreach (var controlRef in controls.Values)
            {
                var (control, resourcePath) = controlRef;
                DestroyAndForget(control, resourcePath, PoolingPolicy.DisablePooling).Forget();
            }

            controls.Clear();

            s_instanceCacheByName.Remove(ContainerName);

            using var keysToRemove = new PooledList<int>(s_instanceCacheByTransform.Count);

            foreach (var cache in s_instanceCacheByTransform)
            {
                if (Equals(cache.Value))
                {
                    keysToRemove.Add(cache.Key);
                }
            }

            foreach (var keyToRemove in keysToRemove)
            {
                s_instanceCacheByTransform.Remove(keyToRemove);
            }
        }

        #region STATIC_METHODS

        /// <summary>
        /// Get the <see cref="SimpleControlContainer" /> that manages the control to which <paramref name="transform"/> belongs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="transform"/>.</param>
        /// <returns></returns>
        public static SimpleControlContainer Of(Transform transform, bool useCache = true)
        {
            return Of((RectTransform)transform, useCache);
        }

        /// <summary>
        /// Get the <see cref="SimpleControlContainer" /> that manages the control to which <paramref name="rectTransform"/> belongs.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="rectTransform"/>.</param>
        /// <returns></returns>
        public static SimpleControlContainer Of(RectTransform rectTransform, bool useCache = true)
        {
            var id = rectTransform.GetInstanceID();

            if (useCache && s_instanceCacheByTransform.TryGetValue(id, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<SimpleControlContainer>();

            if (container)
            {
                s_instanceCacheByTransform.Add(id, container);
                return container;
            }

            Debug.LogError($"Cannot find any parent {nameof(SimpleControlContainer)} component", rectTransform);
            return null;
        }

        /// <summary>
        /// Find the <see cref="SimpleControlContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static SimpleControlContainer Find(string containerName)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            Debug.LogError($"Cannot find any {nameof(SimpleControlContainer)} by name `{containerName}`");
            return null;
        }

        /// <summary>
        /// Find the <see cref="SimpleControlContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out SimpleControlContainer container)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
                return true;
            }

            Debug.LogError($"Cannot find any {nameof(SimpleControlContainer)} by name `{containerName}`");
            container = default;
            return false;
        }

        #endregion

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void AddCallbackReceiver(ISimpleControlContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void RemoveCallbackReceiver(ISimpleControlContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        /// <summary>
        /// Show an instance of <see cref="Control"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Show<TControl>(ControlOptions options, params object[] args)
            where TControl : Control
        {
            ShowAndForget<TControl>(options, args).Forget();
        }

        public void Show(ControlOptions options, params object[] args)
        {
            ShowAndForget<Control>(options, args).Forget();
        }

        /// <summary>
        /// Show an instance of <see cref="Control"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask<int> ShowAsync<TControl>(ControlOptions options, params object[] args)
            where TControl : Control
        {
            return await ShowAsyncInternal<TControl>(options, args);
        }

        public async UniTask<int> ShowAsync(ControlOptions options, params object[] args)
        {
            return await ShowAsyncInternal<Control>(options, args);
        }

        private async UniTaskVoid ShowAndForget<TControl>(ControlOptions options, Memory<object> args)
            where TControl : Control
        {
            await ShowAsyncInternal<TControl>(options, args);
        }

        private async UniTask<int> ShowAsyncInternal<TControl>(ControlOptions options, Memory<object> args)
            where TControl : Control
        {
            var resourcePath = options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var (controlId, control) = await GetControlAsync<TControl>(options);

            options.onLoaded?.Invoke(controlId, control, args);

            await control.AfterLoadAsync(Content, args);

            if (_disableInteractionInTransition)
            {
                Interactable = false;
            }

            var enterControl = _controls[controlId].View;
            enterControl.Settings = Settings;

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeShow(enterControl, args);
            }

            await enterControl.BeforeEnterAsync(args);

            // Play Animation
            await enterControl.EnterAsync(options.playAnimation, null);

            // Postprocess
            enterControl.AfterEnter(args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterShow(enterControl, args);
            }

            if (_disableInteractionInTransition)
            {
                Interactable = true;
            }

            return controlId;
        }

        private async UniTask<(int, TControl)> GetControlAsync<TControl>(ControlOptions options)
            where TControl : Control
        {
            var control = await GetViewAsync<TControl>(options.AsViewOptions());
            var controlId = control.GetInstanceID();

            _controls[controlId] = new ViewRef<Control>(control, options.resourcePath, options.poolingPolicy);

            return (controlId, control);
        }

        /// <summary>
        /// Hide an instance of <see cref="Control"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Hide(int controlId, bool playAnimation, params object[] args)
        {
            HideAndForget(controlId, playAnimation, args).Forget();
        }

        private async UniTaskVoid HideAndForget(int controlId, bool playAnimation, params object[] args)
        {
            await HideAsync(controlId, playAnimation, args);
        }

        /// <summary>
        /// Hide an instance of <see cref="Control"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask HideAsync(int controlId, bool playAnimation, params object[] args)
        {
            if (_disableInteractionInTransition)
            {
                Interactable = false;
            }

            var exitControlRef = _controls[controlId];
            var exitControl = exitControlRef.View;
            exitControl.Settings = Settings;

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeHide(exitControl, args);
            }

            await exitControl.BeforeExitAsync(args);

            // Play Animation
            await exitControl.ExitAsync(playAnimation, null);

            // Postprocess
            exitControl.AfterExit(args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterHide(exitControl, args);
            }

            if (_disableInteractionInTransition)
            {
                Interactable = true;
            }
        }
    }
}