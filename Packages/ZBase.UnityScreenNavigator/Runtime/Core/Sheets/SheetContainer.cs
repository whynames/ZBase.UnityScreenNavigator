using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Controls;
using ZBase.UnityScreenNavigator.Foundation.Collections;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    [RequireComponent(typeof(RectMask2D))]
    public sealed class SheetContainer : ControlContainerBase
    {
        private static Dictionary<int, SheetContainer> s_instanceCacheByTransform = new();
        private static Dictionary<string, SheetContainer> s_instanceCacheByName = new();

        private readonly List<ISheetContainerCallbackReceiver> _callbackReceivers = new();
        private readonly Dictionary<int, ControlRef<Sheet>> _sheets = new();
        private int? _activeSheetId;

        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instanceCacheByTransform = new();
            s_instanceCacheByName = new();
        }

        public int? ActiveSheetId => _activeSheetId;

        public Sheet ActiveSheet
        {
            get
            {
                if (ActiveSheetId.HasValue == false)
                {
                    return null;
                }

                return _sheets[ActiveSheetId.Value].Control;
            }
        }

        /// <summary>
        /// True if in transition.
        /// </summary>
        public bool IsInTransition { get; private set; }

        protected override void Awake()
        {
            s_instanceCacheByName[ContainerName] = this;

            base.Awake();

            _callbackReceivers.AddRange(GetComponents<ISheetContainerCallbackReceiver>());
        }

        public void Deinitialize()
        {
            _activeSheetId = null;
            IsInTransition = false;

            var controls = _sheets;

            foreach (var controlRef in controls.Values)
            {
                ReturnToPool(controlRef.Control, controlRef.ResourcePath, controlRef.PoolingPolicy);
            }

            controls.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            var controls = _sheets;

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
        /// Get the <see cref="SheetContainer" /> that manages the sheet to which <paramref name="transform"/> belongs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="transform"/>.</param>
        /// <returns></returns>
        public static SheetContainer Of(Transform transform, bool useCache = true)
        {
            return Of((RectTransform)transform, useCache);
        }

        /// <summary>
        /// Get the <see cref="SheetContainer" /> that manages the sheet to which <paramref name="rectTransform"/> belongs.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="rectTransform"/>.</param>
        /// <returns></returns>
        public static SheetContainer Of(RectTransform rectTransform, bool useCache = true)
        {
            var id = rectTransform.GetInstanceID();

            if (useCache && s_instanceCacheByTransform.TryGetValue(id, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<SheetContainer>();

            if (container)
            {
                s_instanceCacheByTransform.Add(id, container);
                return container;
            }

            Debug.LogError($"Cannot find any parent {nameof(SheetContainer)} component", rectTransform);
            return null;
        }

        /// <summary>
        /// Find the <see cref="SheetContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static SheetContainer Find(string containerName)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            Debug.LogError($"Cannot find any {nameof(SheetContainer)} by name `{containerName}`");
            return null;
        }

        /// <summary>
        /// Find the <see cref="SheetContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out SheetContainer container)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
                return true;
            }

            Debug.LogError($"Cannot find any {nameof(SheetContainer)} by name `{containerName}`");
            container = default;
            return false;
        }

        #endregion

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void AddCallbackReceiver(ISheetContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void RemoveCallbackReceiver(ISheetContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        /// <summary>
        /// Register an instance of <typeparamref name="TSheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Register<TSheet>(ControlOptions options, params object[] args)
            where TSheet : Sheet
        {
            RegisterAndForget<TSheet>(options, args).Forget();
        }

        /// <summary>
        /// Register an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Register(ControlOptions options, params object[] args)
        {
            RegisterAndForget<Sheet>(options, args).Forget();
        }

        /// <summary>
        /// Register an instance of <typeparamref name="TSheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask<int> RegisterAsync<TSheet>(ControlOptions options, params object[] args)
            where TSheet : Sheet
        {
            return await RegisterAsyncInternal<TSheet>(options, args);
        }

        /// <summary>
        /// Register an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask<int> RegisterAsync(ControlOptions options, params object[] args)
        {
            return await RegisterAsyncInternal<Sheet>(options, args);
        }

        private async UniTaskVoid RegisterAndForget<TSheet>(ControlOptions options, Memory<object> args)
            where TSheet : Sheet
        {
            await RegisterAsyncInternal<TSheet>(options, args);
        }

        private async UniTask<int> RegisterAsyncInternal<TSheet>(ControlOptions options, Memory<object> args)
            where TSheet : Sheet
        {
            var resourcePath = options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var (sheetId, sheet) = await GetSheetAsync<TSheet>(options);

            options.onLoaded?.Invoke(sheetId, sheet);

            await sheet.AfterLoadAsync((RectTransform)transform, args);

            return sheetId;
        }

        private async UniTask<(int, TSheet)> GetSheetAsync<TSheet>(ControlOptions options)
            where TSheet : Sheet
        {
            var sheet = await GetViewAsync<TSheet>(options.AsViewOptions());
            var sheetId = sheet.GetInstanceID();

            _sheets[sheetId] = new ControlRef<Sheet>(sheet, options.resourcePath, options.poolingPolicy);

            return (sheetId, sheet);
        }

        /// <summary>
        /// Show an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Show(int sheetId, bool playAnimation, params object[] args)
        {
            ShowAndForget(sheetId, playAnimation, args).Forget();
        }

        /// <summary>
        /// Show an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask ShowAsync(int sheetId, bool playAnimation, params object[] args)
        {
            await ShowAsyncInternal(sheetId, playAnimation, args);
        }

        private async UniTaskVoid ShowAndForget(int sheetId, bool playAnimation, Memory<object> args)
        {
            await ShowAsyncInternal(sheetId, playAnimation, args);
        }

        private async UniTask ShowAsyncInternal(int sheetId, bool playAnimation, Memory<object> args)
        {
            if (IsInTransition)
            {
                Debug.LogError("Cannot transition because there is a sheet already in transition.");
                return;
            }

            if (ActiveSheetId.HasValue && ActiveSheetId.Value.Equals(sheetId))
            {
                Debug.LogWarning($"Cannot transition because the sheet {sheetId} is already active.");
                return;
            }

            IsInTransition = true;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var enterSheet = _sheets[sheetId].Control;
            enterSheet.Settings = Settings;

            ControlRef<Sheet>? exitSheetRef = ActiveSheetId.HasValue ? _sheets[ActiveSheetId.Value] : null;
            var exitSheet = exitSheetRef.HasValue ? exitSheetRef.Value.Control : null;

            if (exitSheet)
            {
                exitSheet.Settings = Settings;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeShow(enterSheet, exitSheet, args);
            }

            if (exitSheet)
            {
                await exitSheet.BeforeExitAsync(args);
            }

            await enterSheet.BeforeEnterAsync(args);

            // Play Animation
            if (exitSheet)
            {
                await exitSheet.ExitAsync(playAnimation, enterSheet);
            }

            await enterSheet.EnterAsync(playAnimation, exitSheet);

            // End Transition
            _activeSheetId = sheetId;
            IsInTransition = false;

            // Postprocess
            if (exitSheet)
            {
                exitSheet.AfterExit(args);
            }

            enterSheet.AfterEnter(args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterShow(enterSheet, exitSheet, args);
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Hide an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Hide(bool playAnimation, params object[] args)
        {
            HideAndForget(playAnimation, args).Forget();
        }

        private async UniTaskVoid HideAndForget(bool playAnimation, params object[] args)
        {
            await HideAsync(playAnimation, args);
        }

        /// <summary>
        /// Hide an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask HideAsync(bool playAnimation, params object[] args)
        {
            if (IsInTransition)
            {
                Debug.LogError("Cannot transition because there is a sheet already in transition.");
                return;
            }

            if (ActiveSheetId.HasValue == false)
            {
                Debug.LogWarning("Cannot transition because there is no active sheet.");
                return;
            }

            IsInTransition = true;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var exitSheetRef = _sheets[ActiveSheetId.Value];
            var exitSheet = exitSheetRef.Control;
            exitSheet.Settings = Settings;

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeHide(exitSheet, args);
            }

            await exitSheet.BeforeExitAsync(args);

            // Play Animation
            await exitSheet.ExitAsync(playAnimation, null);

            // End Transition
            _activeSheetId = null;
            IsInTransition = false;

            // Postprocess
            exitSheet.AfterExit(args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterHide(exitSheet, args);
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }
    }
}