using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;
using ZBase.UnityScreenNavigator.Foundation.Collections;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    [RequireComponent(typeof(RectMask2D))]
    public sealed class SheetContainer : ViewContainer, IViewContainer
    {
        private static Dictionary<int, SheetContainer> s_instanceCacheByTransform = new();
        private static Dictionary<string, SheetContainer> s_instanceCacheByName = new();

        [SerializeField] private string _name;
        [SerializeField] UnityScreenNavigatorSettings _settings;

        private readonly Dictionary<string, AssetLoadHandle<GameObject>> _resourcePathToHandle = new();
        private readonly Dictionary<string, Queue<Sheet>> _resourcePathToPool = new();
        private readonly List<ISheetContainerCallbackReceiver> _callbackReceivers = new();
        private readonly Dictionary<int, SheetRef<Sheet>> _sheets = new();

        private int? _activeSheetId;

        public override UnityScreenNavigatorSettings Settings
        {
            get
            {
                if (_settings == false)
                {
                    _settings = UnityScreenNavigatorSettings.DefaultSettings;
                }

                return _settings;
            }

            set
            {
                if (value == false)
                    throw new ArgumentNullException(nameof(value));

                _settings = value;
            }
        }

        public int? ActiveSheetId => _activeSheetId;

        public Sheet ActiveSheet
        {
            get
            {
                if (_activeSheetId.HasValue == false)
                {
                    return null;
                }

                return _sheets[_activeSheetId.Value].Sheet;
            }
        }

        /// <summary>
        /// True if in transition.
        /// </summary>
        public bool IsInTransition { get; private set; }

        /// <summary>
        /// Registered sheets.
        /// </summary>
        public IReadOnlyDictionary<int, SheetRef<Sheet>> Sheets => _sheets;

        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instanceCacheByTransform = new();
            s_instanceCacheByName = new();
        }

        protected override void Awake()
        {
            s_instanceCacheByName[_name] = this;

            var _ = CanvasGroup;

            _callbackReceivers.AddRange(GetComponents<ISheetContainerCallbackReceiver>());

            InitializePool();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            var sheets = _sheets;

            foreach (var sheetRef in sheets.Values)
            {
                var (sheet, resourcePath) = sheetRef;
                DestroyAndForget(sheet, resourcePath, PoolingPolicy.DisablePooling).Forget();
            }

            sheets.Clear();
            s_instanceCacheByName.Remove(_name);

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

        public void Deinitialize()
        {
            _activeSheetId = null;
            IsInTransition = false;

            var sheets = _sheets;

            foreach (var sheetRef in sheets.Values)
            {
                ReturnToPool(sheetRef.Sheet, sheetRef.ResourcePath, sheetRef.PoolingPolicy);
            }

            sheets.Clear();
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
        public void Register<TSheet>(SheetOptions options, params object[] args)
            where TSheet : Sheet
        {
            RegisterAndForget<TSheet>(options, args).Forget();
        }

        /// <summary>
        /// Register an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Register(SheetOptions options, params object[] args)
        {
            RegisterAndForget<Sheet>(options, args).Forget();
        }

        /// <summary>
        /// Register an instance of <typeparamref name="TSheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask<int> RegisterAsync<TSheet>(SheetOptions options, params object[] args)
            where TSheet : Sheet
        {
            return await RegisterAsyncInternal<TSheet>(options, args);
        }

        /// <summary>
        /// Register an instance of <see cref="Sheet"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask<int> RegisterAsync(SheetOptions options, params object[] args)
        {
            return await RegisterAsyncInternal<Sheet>(options, args);
        }

        private async UniTaskVoid RegisterAndForget<TSheet>(SheetOptions options, Memory<object> args)
            where TSheet : Sheet
        {
            await RegisterAsyncInternal<TSheet>(options, args);
        }

        private async UniTask<int> RegisterAsyncInternal<TSheet>(SheetOptions options, Memory<object> args)
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

            if (_activeSheetId.HasValue && _activeSheetId.Value.Equals(sheetId))
            {
                Debug.LogWarning($"Cannot transition because the sheet {sheetId} is already active.");
                return;
            }

            IsInTransition = true;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var enterSheet = _sheets[sheetId].Sheet;
            enterSheet.Settings = Settings;

            SheetRef<Sheet>? exitSheetRef = _activeSheetId.HasValue ? _sheets[_activeSheetId.Value] : null;
            var exitSheet = exitSheetRef.HasValue ? exitSheetRef.Value.Sheet : null;

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

            if (_activeSheetId.HasValue == false)
            {
                Debug.LogWarning("Cannot transition because there is no active sheet.");
                return;
            }

            IsInTransition = true;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var exitSheetRef = _sheets[_activeSheetId.Value];
            var exitSheet = exitSheetRef.Sheet;
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

        private async UniTask<(int, T)> GetSheetAsync<T>(SheetOptions options)
            where T : Sheet
        {
            var resourcePath = options.resourcePath;

            if (GetFromPool<T>(resourcePath, options.poolingPolicy, out var existSheet))
            {
                existSheet.Settings = Settings;

                var existSheetId = existSheet.GetInstanceID();
                _sheets[existSheetId] = new SheetRef<Sheet>(existSheet, resourcePath, options.poolingPolicy);

                return (existSheetId, existSheet);
            }

            AssetLoadHandle<GameObject> assetLoadHandle;
            var handleInMap = false;

            if (_resourcePathToHandle.TryGetValue(resourcePath, out var handle))
            {
                assetLoadHandle = handle;
                handleInMap = true;
            }
            else
            {
                assetLoadHandle = options.loadAsync
                    ? AssetLoader.LoadAsync<GameObject>(resourcePath)
                    : AssetLoader.Load<GameObject>(resourcePath);
            }

            while (assetLoadHandle.IsDone == false)
            {
                await UniTask.NextFrame();
            }

            if (assetLoadHandle.Status == AssetLoadStatus.Failed)
            {
                throw assetLoadHandle.OperationException;
            }

            var instance = Instantiate(assetLoadHandle.Result);

            if (instance.TryGetComponent<T>(out var sheet) == false)
            {
                Debug.LogError(
                    $"Cannot find the {typeof(T).Name} component on the specified resource `{resourcePath}`."
                    , instance
                );

                return (default, null);
            }

            sheet.Settings = Settings;

            var sheetId = sheet.GetInstanceID();
            _sheets[sheetId] = new SheetRef<Sheet>(sheet, resourcePath, options.poolingPolicy);

            if (handleInMap == false)
            {
                _resourcePathToHandle[resourcePath] = assetLoadHandle;
            }

            return (sheetId, sheet);
        }
    }
}