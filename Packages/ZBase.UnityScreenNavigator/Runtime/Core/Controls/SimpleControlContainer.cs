using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Foundation.Collections;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    [RequireComponent(typeof(RectMask2D))]
    public sealed class SimpleControlContainer : ControlContainerBase
    {
        private static Dictionary<int, SimpleControlContainer> s_instanceCacheByTransform = new();
        private static Dictionary<string, SimpleControlContainer> s_instanceCacheByName = new();

        private readonly List<ISimpleControlContainerCallbackReceiver> _callbackReceivers = new();
        private readonly Dictionary<int, ViewRef<Control>> _controls = new();

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

        public void Deinitialize()
        {
            var controls = _controls;

            foreach (var controlRef in controls.Values)
            {
                ReturnToPool(controlRef.View, controlRef.ResourcePath, controlRef.PoolingPolicy);
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
        /// Register an instance of <typeparamref name="TControl"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Register<TControl>(ControlOptions options, params object[] args)
            where TControl : Control
        {
            RegisterAndForget<TControl>(options, args).Forget();
        }

        /// <summary>
        /// Register an instance of <see cref="Control"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Register(ControlOptions options, params object[] args)
        {
            RegisterAndForget<Control>(options, args).Forget();
        }

        /// <summary>
        /// Register an instance of <typeparamref name="TControl"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask<int> RegisterAsync<TControl>(ControlOptions options, params object[] args)
            where TControl : Control
        {
            return await RegisterAsyncInternal<TControl>(options, args);
        }

        /// <summary>
        /// Register an instance of <see cref="Control"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask<int> RegisterAsync(ControlOptions options, params object[] args)
        {
            return await RegisterAsyncInternal<Control>(options, args);
        }

        private async UniTaskVoid RegisterAndForget<TControl>(ControlOptions options, Memory<object> args)
            where TControl : Control
        {
            await RegisterAsyncInternal<TControl>(options, args);
        }

        private async UniTask<int> RegisterAsyncInternal<TControl>(ControlOptions options, Memory<object> args)
            where TControl : Control
        {
            var resourcePath = options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var (controlId, control) = await GetControlAsync<TControl>(options);

            options.onLoaded?.Invoke(controlId, control);

            await control.AfterLoadAsync((RectTransform)transform, args);

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
        /// Show an instance of <see cref="Control"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Show(int controlId, bool playAnimation, params object[] args)
        {
            ShowAndForget(controlId, playAnimation, args).Forget();
        }

        /// <summary>
        /// Show an instance of <see cref="Control"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask ShowAsync(int controlId, bool playAnimation, params object[] args)
        {
            await ShowAsyncInternal(controlId, playAnimation, args);
        }

        private async UniTaskVoid ShowAndForget(int controlId, bool playAnimation, Memory<object> args)
        {
            await ShowAsyncInternal(controlId, playAnimation, args);
        }

        private async UniTask ShowAsyncInternal(int controlId, bool playAnimation, Memory<object> args)
        {
            if (Settings.EnableInteractionInTransition == false)
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
            await enterControl.EnterAsync(playAnimation, null);

            // Postprocess
            enterControl.AfterEnter(args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterShow(enterControl, args);
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
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
            if (Settings.EnableInteractionInTransition == false)
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

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }
    }
}