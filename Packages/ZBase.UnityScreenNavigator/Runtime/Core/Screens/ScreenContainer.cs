using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;
using ZBase.UnityScreenNavigator.Foundation.Collections;

namespace ZBase.UnityScreenNavigator.Core.Screens
{
    [RequireComponent(typeof(RectMask2D))]
    public sealed class ScreenContainer : ContainerLayer
    {
        private static Dictionary<int, ScreenContainer> s_instanceCacheByTransform = new();
        private static Dictionary<string, ScreenContainer> s_instanceCacheByName = new();

        private readonly Dictionary<int, AssetLoadHandle<GameObject>> _assetLoadHandles = new();
        private readonly List<IScreenContainerCallbackReceiver> _callbackReceivers = new();
        private readonly List<ViewRef<Screen>> _screens = new();
        private readonly Dictionary<string, AssetLoadHandle<GameObject>> _preloadedResourceHandles = new();

        private bool _isActiveScreenStacked;
        private IAssetLoader _assetLoader;

        /// <summary>
        /// By default, <see cref="IAssetLoader" /> in <see cref="UnityScreenNavigatorSettings" /> is used.
        /// If this property is set, it is used instead.
        /// </summary>
        public IAssetLoader AssetLoader
        {
            get => _assetLoader ?? UnityScreenNavigatorSettings.Instance.AssetLoader;
            set => _assetLoader = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// True if in transition.
        /// </summary>
        public bool IsInTransition { get; private set; }

        /// <summary>
        /// Stacked screens.
        /// </summary>
        public IReadOnlyList<ViewRef<Screen>> Screens => _screens;

        public ViewRef<Screen> Current => _screens[^1];

        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instanceCacheByTransform = new();
            s_instanceCacheByName = new();
        }

        protected override void Awake()
        {
            _callbackReceivers.AddRange(GetComponents<IScreenContainerCallbackReceiver>());
        }

        protected override void OnDestroy()
        {
            var screens = _screens;
            var count = screens.Count;
            var assetLoadHandles = _assetLoadHandles;

            for (var i = 0; i < count; i++)
            {
                var screen = screens[i].View;
                var screenId = screen.GetInstanceID();

                Destroy(screen.gameObject);

                if (assetLoadHandles.TryGetValue(screenId, out var assetLoadHandle))
                {
                    AssetLoader.Release(assetLoadHandle.Id);
                }
            }

            screens.Clear();
            assetLoadHandles.Clear();
            s_instanceCacheByName.Remove(LayerName);

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
        /// Get the <see cref="ScreenContainer" /> that manages the screen to which <paramref name="transform"/> belongs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="transform"/>.</param>
        /// <returns></returns>
        public static ScreenContainer Of(Transform transform, bool useCache = true)
        {
            return Of((RectTransform)transform, useCache);
        }

        /// <summary>
        /// Get the <see cref="ScreenContainer" /> that manages the screen to which <paramref name="rectTransform"/> belongs.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="rectTransform"/>.</param>
        /// <returns></returns>
        public static ScreenContainer Of(RectTransform rectTransform, bool useCache = true)
        {
            var id = rectTransform.GetInstanceID();

            if (useCache && s_instanceCacheByTransform.TryGetValue(id, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<ScreenContainer>();

            if (container)
            {
                s_instanceCacheByTransform.Add(id, container);
                return container;
            }

            Debug.LogError($"Cannot find any parent {nameof(ScreenContainer)} component", rectTransform);
            return null;
        }

        /// <summary>
        /// Find the <see cref="ScreenContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static ScreenContainer Find(string containerName)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            Debug.LogError($"Cannot find any {nameof(ScreenContainer)} by name `{containerName}`");
            return null;
        }

        /// <summary>
        /// Find the <see cref="ScreenContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out ScreenContainer container)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
                return true;
            }

            Debug.LogError($"Cannot find any {nameof(ScreenContainer)} by name `{containerName}`");
            container = default;
            return false;
        }

        /// <summary>
        /// Create a new <see cref="ScreenContainer"/> as a layer.
        /// </summary>
        public static async UniTask<ScreenContainer> CreateAsync(ContainerLayerConfig layerConfig, IContainerLayerManager manager)
        {
            var root = new GameObject(
                  layerConfig.name
                , typeof(Canvas)
                , typeof(GraphicRaycaster)
                , typeof(CanvasGroup)
            );

            var rectTransform = root.GetOrAddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localPosition = Vector3.zero;

            var container = root.AddComponent<ScreenContainer>();
            await container.InitializeAsync(layerConfig, manager);

            s_instanceCacheByName.Add(container.LayerName, container);
            return container;
        }

        #endregion

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        public void AddCallbackReceiver(IScreenContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        public void RemoveCallbackReceiver(IScreenContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        /// <summary>
        /// Searches through the <see cref="Screens"/> stack
        /// and returns the index of the Screen loaded from <paramref name="resourcePath"/>
        /// that has been recently pushed into this container if any.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="index">
        /// Return a value greater or equal to 0 if there is
        /// a Screen loaded from this <paramref name="resourcePath"/>.
        /// </param>
        /// <returns>
        /// True if there is a Screen loaded from this <paramref name="resourcePath"/>.
        /// </returns>
        public bool FindIndexOfRecentlyPushed(string resourcePath, out int index)
        {
            var screens = _screens;

            for (var i = screens.Count - 1; i >= 0; i--)
            {
                if (string.Equals(resourcePath, screens[i].ResourcePath))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Searches through the <see cref="Screens"/> stack
        /// and destroys the Screen loaded from <paramref name="resourcePath"/>
        /// that has been recently pushed into this container if any.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="screen">
        /// Returns the Screen loaded from this <paramref name="resourcePath"/> after it is removed.
        /// </param>
        /// <returns>
        /// True if there is a Screen loaded from this <paramref name="resourcePath"/>.
        /// </returns>
        public void DestroyRecentlyPushed(string resourcePath)
        {
            if (FindIndexOfRecentlyPushed(resourcePath, out var index) == false)
            {
                return;
            }

            var screen = _screens[index].View;
            var screenId = screen.GetInstanceID();
            _screens.RemoveAt(index);

            Destroy(screen.gameObject);

            if (_assetLoadHandles.TryGetValue(screenId, out var loadHandle))
            {
                AssetLoader.Release(loadHandle.Id);
                _assetLoadHandles.Remove(screenId);
            }
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TScreen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Push<TScreen>(ScreenOptions options, params object[] args)
            where TScreen : Screen
        {
            PushAndForget<TScreen>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Push(ScreenOptions options, params object[] args)
        {
            PushAndForget<Screen>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TScreen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PushAsync<TScreen>(ScreenOptions options, params object[] args)
            where TScreen : Screen
        {
            await PushAsyncInternal<TScreen>(options, args);
        }

        /// <summary>
        /// Push an instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PushAsync(ScreenOptions options, params object[] args)
        {
            await PushAsyncInternal<Screen>(options, args);
        }

        private async UniTaskVoid PushAndForget<TScreen>(ScreenOptions options, Memory<object> args)
            where TScreen : Screen
        {
            await PushAsyncInternal<Screen>(options, args);
        }

        private async UniTask PushAsyncInternal<TScreen>(ScreenOptions options, Memory<object> args)
            where TScreen : Screen
        {
            var resourcePath = options.options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            if (IsInTransition)
            {
                Debug.LogError($"Cannot transition because there is a screen already in transition.");
                return;
            }

            IsInTransition = true;
            
            if (UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            // Setup
            var assetLoadHandle = options.options.loadAsync
                ? AssetLoader.LoadAsync<GameObject>(resourcePath)
                : AssetLoader.Load<GameObject>(resourcePath);

            while (assetLoadHandle.IsDone == false)
            {
                await UniTask.NextFrame();
            }

            if (assetLoadHandle.Status == AssetLoadStatus.Failed)
            {
                throw assetLoadHandle.OperationException;
            }

            var instance = Instantiate(assetLoadHandle.Result);

            if (instance.TryGetComponent<TScreen>(out var enterScreen) == false)
            {
                Debug.LogError(
                    $"Cannot transition because {typeof(TScreen).Name} component is not " +
                    $"attached to the specified resource `{resourcePath}`."
                    , instance
                );

                return;
            }

            var screenId = enterScreen.GetInstanceID();
            _assetLoadHandles.Add(screenId, assetLoadHandle);
            options.options.onLoaded?.Invoke(enterScreen, args);

            await enterScreen.AfterLoadAsync(RectTransform, args);

            var exitScreen = _screens.Count == 0 ? null : _screens[^1].View;
            var exitScreenId = exitScreen == null ? (int?) null : exitScreen.GetInstanceID();

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePush(enterScreen, exitScreen, args);
            }

            if (exitScreen)
            {
                await exitScreen.BeforeExitAsync(true, args);
            }

            await enterScreen.BeforeEnterAsync(true, args);

            // Play Animations
            if (exitScreen)
            {
                await exitScreen.ExitAsync(true, options.options.playAnimation, enterScreen);
            }

            await enterScreen.EnterAsync(true, options.options.playAnimation, exitScreen);

            // End Transition
            if (_isActiveScreenStacked == false && exitScreenId.HasValue)
            {
                _screens.RemoveAt(_screens.Count - 1);
            }

            _screens.Add(new ViewRef<Screen>(enterScreen, resourcePath));
            IsInTransition = false;

            // Postprocess
            if (exitScreen)
            {
                exitScreen.AfterExit(true, args);
            }

            enterScreen.AfterEnter(true, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPush(enterScreen, exitScreen, args);
            }

            // Unload unused Screen
            if (_isActiveScreenStacked == false && exitScreenId.HasValue)
            {
                await exitScreen.BeforeReleaseAsync();

                DestroyAndForget(exitScreen, exitScreenId.Value).Forget();
            }

            _isActiveScreenStacked = options.stack;
            
            if (UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        private async UniTaskVoid DestroyAndForget(Screen screen, int screenId)
        {
            Destroy(screen.gameObject);

            await UniTask.NextFrame();

            if (_assetLoadHandles.TryGetValue(screenId, out var handle))
            {
                AssetLoader.Release(handle.Id);
                _assetLoadHandles.Remove(screenId);
            }
        }

        /// <summary>
        /// Pop current instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Pop(bool playAnimation, params object[] args)
        {
            PopAndForget(playAnimation, args).Forget();
        }

        private async UniTaskVoid PopAndForget(bool playAnimation, params object[] args)
        {
            await PopAsync(playAnimation, args);
        }

        /// <summary>
        /// Pop current instance of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PopAsync(bool playAnimation, params object[] args)
        {
            if (_screens.Count == 0)
            {
                Debug.LogError("Cannot transition because there is no screen loaded on the stack.");
                return;
            }

            if (IsInTransition)
            {
                Debug.LogWarning("Cannot transition because there is a screen already in transition.");
                return;
            }

            IsInTransition = true;
            
            if (UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var lastScreen = _screens.Count - 1;
            var exitScreen = _screens[lastScreen].View;
            var exitScreenId = exitScreen.GetInstanceID();
            var enterScreen = _screens.Count == 1 ? null : _screens[^2].View;

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePop(enterScreen, exitScreen, args);
            }

            await exitScreen.BeforeExitAsync(false, args);

            if (enterScreen)
            {
                await enterScreen.BeforeEnterAsync(false, args);
            }

            // Play Animations
            await exitScreen.ExitAsync(false, playAnimation, enterScreen);

            if (enterScreen)
            {
                await enterScreen.EnterAsync(false, playAnimation, exitScreen);
            }

            // End Transition
            _screens.RemoveAt(lastScreen);
            IsInTransition = false;

            // Postprocess
            exitScreen.AfterExit(false, args);

            if (enterScreen)
            {
                enterScreen.AfterEnter(false, args);
            }

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPop(enterScreen, exitScreen, args);
            }

            // Unload unused Screen
            await exitScreen.BeforeReleaseAsync();

            DestroyAndForget(exitScreen, exitScreenId).Forget();

            _isActiveScreenStacked = true;
            
            if (UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Preload a prefab of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Preload(string resourcePath, bool loadAsync = true)
        {
            PreloadAndForget(resourcePath, loadAsync).Forget();
        }

        private async UniTaskVoid PreloadAndForget(string resourcePath, bool loadAsync = true)
        {
            await PreloadAsync(resourcePath, loadAsync);
        }

        /// <summary>
        /// Preload a prefab of <see cref="Screen"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PreloadAsync(string resourcePath, bool loadAsync = true)
        {
            if (_preloadedResourceHandles.ContainsKey(resourcePath))
            {
                Debug.LogError($"The resource at `{resourcePath}` has already been preloaded.");
                return;
            }

            var assetLoadHandle = loadAsync
                ? AssetLoader.LoadAsync<GameObject>(resourcePath)
                : AssetLoader.Load<GameObject>(resourcePath);

            _preloadedResourceHandles.Add(resourcePath, assetLoadHandle);

            if (assetLoadHandle.IsDone == false)
            {
                await assetLoadHandle.Task;
            }

            if (assetLoadHandle.Status == AssetLoadStatus.Failed)
            {
                throw assetLoadHandle.OperationException;
            }
        }

        public bool IsPreloadRequested(string resourcePath)
        {
            return _preloadedResourceHandles.ContainsKey(resourcePath);
        }

        public bool IsPreloaded(string resourcePath)
        {
            if (_preloadedResourceHandles.TryGetValue(resourcePath, out var handle) == false)
            {
                return false;
            }

            return handle.Status == AssetLoadStatus.Success;
        }

        public void ReleasePreloaded(string resourcePath)
        {
            if (_preloadedResourceHandles.TryGetValue(resourcePath, out var handle) == false)
            {
                Debug.LogError($"The resource {resourcePath} is not preloaded.");
                return;
            }

            _preloadedResourceHandles.Remove(resourcePath);
            AssetLoader.Release(handle.Id);
        }
    }
}