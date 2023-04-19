using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;
using ZBase.UnityScreenNavigator.Foundation.Collections;

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    [RequireComponent(typeof(RectMask2D))]
    public sealed class ModalContainer : ContainerLayer
    {
        private static Dictionary<int, ModalContainer> s_instanceCacheByTransform = new();
        private static Dictionary<string, ModalContainer> s_instanceCacheByName = new();

        [SerializeField] private ModalBackdrop _overrideBackdropPrefab;

        private readonly Dictionary<int, AssetLoadHandle<GameObject>> _assetLoadHandles = new();
        private readonly List<ModalBackdrop> _backdrops = new();
        private readonly List<IModalContainerCallbackReceiver> _callbackReceivers = new();
        private readonly List<ViewRef<Modal>> _modals = new();
        private readonly Dictionary<string, AssetLoadHandle<GameObject>> _preloadedResourceHandles = new();

        private ModalBackdrop _backdropPrefab;
        private IAssetLoader _assetLoader;
        private bool _disableBackdrop;

        /// <summary>
        /// By default, <see cref="IAssetLoader" /> in <see cref="UnityScreenNavigatorSettings" /> is used.
        /// If this property is set, it is used instead.
        /// </summary>
        public IAssetLoader AssetLoader
        {
            get => _assetLoader ?? Settings.AssetLoader;
            set => _assetLoader = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// True if in transition.
        /// </summary>
        public bool IsInTransition { get; private set; }

        /// <summary>
        /// Stacked modals.
        /// </summary>
        public IReadOnlyList<ViewRef<Modal>> Modals => _modals;

        /// <summary>
        /// Stacked backdrops.
        /// </summary>
        public IReadOnlyList<ModalBackdrop> Backdrops => _backdrops;

        public ViewRef<Modal> Current => _modals[^1];

        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instanceCacheByTransform = new();
            s_instanceCacheByName = new();
        }

        protected override void Awake()
        {
            _callbackReceivers.AddRange(GetComponents<IModalContainerCallbackReceiver>());

            _backdropPrefab = _overrideBackdropPrefab
                ? _overrideBackdropPrefab
                : Settings.ModalBackdropPrefab;

            _disableBackdrop = Settings.DisableModalBackdrop;
        }

        protected override void OnDestroy()
        {
            var modals = _modals;
            var count = modals.Count;
            var assetLoadHandles = _assetLoadHandles;

            for (var i = 0; i < count; i++)
            {
                var modal = modals[i].View;
                var modalId = modal.GetInstanceID();

                Destroy(modal.gameObject);

                if (assetLoadHandles.TryGetValue(modalId, out var assetLoadHandle))
                {
                    AssetLoader.Release(assetLoadHandle.Id);
                }
            }

            modals.Clear();
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

        /// <summary>
        /// Get the <see cref="ModalContainer" /> that manages the modal to which <see cref="transform" /> belongs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="useCache">Use the previous result for the <see cref="transform" />.</param>
        /// <returns></returns>
        public static ModalContainer Of(Transform transform, bool useCache = true)
        {
            return Of((RectTransform)transform, useCache);
        }

        /// <summary>
        /// Get the <see cref="ModalContainer" /> that manages the modal to which <paramref name="rectTransform"/> belongs.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="useCache">Use the previous result for the <paramref name="rectTransform"/>.</param>
        /// <returns></returns>
        public static ModalContainer Of(RectTransform rectTransform, bool useCache = true)
        {
            var id = rectTransform.GetInstanceID();

            if (useCache && s_instanceCacheByTransform.TryGetValue(id, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<ModalContainer>();

            if (container)
            {
                s_instanceCacheByTransform.Add(id, container);
                return container;
            }

            Debug.LogError($"Cannot find any parent {nameof(ModalContainer)} component", rectTransform);
            return null;
        }

        /// <summary>
        /// Find the <see cref="ModalContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static ModalContainer Find(string containerName)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            Debug.LogError($"Cannot find any {nameof(ModalContainer)} by name `{containerName}`");
            return null;
        }

        /// <summary>
        /// Find the <see cref="ModalContainer" /> of <paramref name="containerName"/>.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out ModalContainer container)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
                return true;
            }

            Debug.LogError($"Cannot find any {nameof(ModalContainer)} by name `{containerName}`");
            container = default;
            return false;
        }

        /// <summary>
        /// Create a new <see cref="ModalContainer" /> as a layer.
        /// </summary>
        public static async UniTask<ModalContainer> CreateAsync(ContainerLayerConfig layerConfig, IContainerLayerManager manager)
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

            var container = root.AddComponent<ModalContainer>();
            await container.InitializeAsync(layerConfig, manager);

            s_instanceCacheByName.Add(container.LayerName, container);
            return container;
        }

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void AddCallbackReceiver(IModalContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void RemoveCallbackReceiver(IModalContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        /// <summary>
        /// Searches through the <see cref="Modals"/> stack
        /// and returns the index of the Modal loaded from <paramref name="resourcePath"/>
        /// that has been recently pushed into this container if any.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="index">
        /// Return a value greater or equal to 0 if there is
        /// a Modal loaded from this <paramref name="resourcePath"/>.
        /// </param>
        /// <returns>
        /// True if there is a Modal loaded from this <paramref name="resourcePath"/>.
        /// </returns>
        public bool FindIndexOfRecentlyPushed(string resourcePath, out int index)
        {
            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var modals = _modals;

            for (var i = modals.Count - 1; i >= 0; i--)
            {
                if (string.Equals(resourcePath, modals[i].ResourcePath))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Searches through the <see cref="Modals"/> stack
        /// and destroys the Modal loaded from <paramref name="resourcePath"/>
        /// that has been recently pushed into this container if any.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="ignoreFront">Do not destroy if the modal is in the front.</param>
        /// <returns>
        /// True if there is a Modal loaded from this <paramref name="resourcePath"/>.
        /// </returns>
        public void DestroyRecentlyPushed(string resourcePath, bool ignoreFront = true)
        {
            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var frontIndex = _modals.Count - 1;

            if (FindIndexOfRecentlyPushed(resourcePath, out var index) == false)
            {
                return;
            }

            if (ignoreFront && frontIndex == index)
            {
                return;
            }

            var modal = _modals[index].View;
            _modals.RemoveAt(index);

            ModalBackdrop backdrop = null;

            if (_disableBackdrop == false)
            {
                backdrop = _backdrops[index];
                _backdrops.RemoveAt(index);
            }

            DestroyAndForget(modal, backdrop).Forget();
        }

        /// <summary>
        /// Bring an instance of <see cref="Modal"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the modal is already in the front.</param>
        /// <remarks>Fire-and-forget</remarks>
        public void BringToFront(ModalOptions options, bool ignoreFront, params object[] args)
        {
            BringToFrontAndForget(options, ignoreFront, args).Forget();
        }

        /// <summary>
        /// Bring an instance of <see cref="Modal"/> to the front.
        /// </summary>
        /// <param name="ignoreFront">Ignore if the modal is already in the front.</param>
        /// <remarks>Asynchronous</remarks>
        public async UniTask BringToFrontAsync(ModalOptions options, bool ignoreFront, params object[] args)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        private async UniTaskVoid BringToFrontAndForget(ModalOptions options, bool ignoreFront, Memory<object> args)
        {
            await BringToFrontAsyncInternal(options, ignoreFront, args);
        }

        private async UniTask BringToFrontAsyncInternal(ModalOptions options, bool ignoreFront, Memory<object> args)
        {
            var resourcePath = options.options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var frontIndex = _modals.Count - 1;

            if (FindIndexOfRecentlyPushed(resourcePath, out var index) == false)
            {
                return;
            }

            if (ignoreFront && frontIndex == index)
            {
                return;
            }

            var enterModal = _modals[index].View;
            enterModal.Settings = Settings;

            var modalId = enterModal.GetInstanceID();
            _modals.RemoveAt(index);
            RectTransform.RemoveChild(enterModal.transform);

            ModalBackdrop backdrop = null;

            if (_disableBackdrop == false)
            {
                backdrop = _backdrops[index];
                _backdrops.RemoveAt(index);
                RectTransform.RemoveChild(backdrop.transform);

                backdrop.Setup(RectTransform, options.backdropAlpha, options.closeWhenClickOnBackdrop);
                backdrop.Settings = Settings;

                _backdrops.Add(backdrop);
            }

            options.options.onLoaded?.Invoke(enterModal, args);

            await enterModal.AfterLoadAsync(RectTransform, args);

            var exitModal = _modals.Count == 0 ? null : _modals[^1].View;

            if (exitModal)
            {
                exitModal.Settings = Settings;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePush(enterModal, exitModal, args);
            }

            if (exitModal)
            {
                await exitModal.BeforeExitAsync(true, args);
            }

            await enterModal.BeforeEnterAsync(true, args);

            // Play Animation

            if (backdrop)
            {
                await backdrop.EnterAsync(options.options.playAnimation);
            }

            if (exitModal)
            {
                await exitModal.ExitAsync(true, options.options.playAnimation, enterModal);
            }

            await enterModal.EnterAsync(true, options.options.playAnimation, exitModal);

            // End Transition
            _modals.Add(new ViewRef<Modal>(enterModal, resourcePath));
            IsInTransition = false;

            // Postprocess
            if (exitModal)
            {
                exitModal.AfterExit(true, args);
            }

            enterModal.AfterEnter(true, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPush(enterModal, exitModal, args);
            }

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TModal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Push<TModal>(ModalOptions options, params object[] args)
            where TModal : Modal
        {
            PushAndForget<TModal>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Push(ModalOptions options, params object[] args)
        {
            PushAndForget<Modal>(options, args).Forget();
        }

        /// <summary>
        /// Push an instance of <typeparamref name="TModal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PushAsync<TModal>(ModalOptions options, params object[] args)
            where TModal : Modal
        {
            await PushAsyncInternal<TModal>(options, args);
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PushAsync(ModalOptions options, params object[] args)
        {
            await PushAsyncInternal<Modal>(options, args);
        }

        private async UniTaskVoid PushAndForget<TModal>(ModalOptions options, Memory<object> args)
            where TModal : Modal
        {
            await PushAsyncInternal<TModal>(options, args);
        }

        private async UniTask PushAsyncInternal<TModal>(ModalOptions options, Memory<object> args)
            where TModal : Modal
        {
            var resourcePath = options.options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            if (IsInTransition)
            {
                Debug.LogWarning("Cannot transition because there is a modal already in transition.");
                return;
            }

            IsInTransition = true;
            
            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

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

            ModalBackdrop backdrop = null;

            if (_disableBackdrop == false)
            {
                backdrop = Instantiate(_backdropPrefab);
                backdrop.Setup(RectTransform, options.backdropAlpha, options.closeWhenClickOnBackdrop);
                backdrop.Settings = Settings;

                _backdrops.Add(backdrop);
            }

            var instance = Instantiate(assetLoadHandle.Result);

            if (instance.TryGetComponent<TModal>(out var enterModal) == false)
            {
                Debug.LogError(
                    $"Cannot transition because {typeof(TModal).Name} component is not " +
                    $"attached to the specified resource `{resourcePath}`."
                    , instance
                );

                return;
            }

            enterModal.Settings = Settings;

            var modalId = enterModal.GetInstanceID();
            _assetLoadHandles.Add(modalId, assetLoadHandle);
            options.options.onLoaded?.Invoke(enterModal, args);

            await enterModal.AfterLoadAsync(RectTransform, args);

            var exitModal = _modals.Count == 0 ? null : _modals[^1].View;

            if (exitModal)
            {
                exitModal.Settings = Settings;
            }    

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePush(enterModal, exitModal, args);
            }

            if (exitModal)
            {
                await exitModal.BeforeExitAsync(true, args);
            }

            await enterModal.BeforeEnterAsync(true, args);

            // Play Animation

            if (backdrop)
            {
                await backdrop.EnterAsync(options.options.playAnimation);
            }

            if (exitModal)
            {
                await exitModal.ExitAsync(true, options.options.playAnimation, enterModal);
            }

            await enterModal.EnterAsync(true, options.options.playAnimation, exitModal);

            // End Transition
            _modals.Add(new ViewRef<Modal>(enterModal, resourcePath));
            IsInTransition = false;

            // Postprocess
            if (exitModal)
            {
                exitModal.AfterExit(true, args);
            }

            enterModal.AfterEnter(true, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPush(enterModal, exitModal, args);
            }
            
            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Push an instance of <see cref="Modal"/>.
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
        /// Push an instance of <see cref="Modal"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask PopAsync(bool playAnimation, params object[] args)
        {
            if (_modals.Count == 0)
            {
                Debug.LogError("Cannot transition because there is no modal loaded on the stack.");
                return;
            }

            if (IsInTransition)
            {
                Debug.LogWarning("Cannot transition because there is a modal already in transition.");
                return;
            }

            IsInTransition = true;
            
            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var lastModalIndex = _modals.Count - 1;
            var exitModal = _modals[lastModalIndex].View;
            exitModal.Settings = Settings;

            var enterModal = _modals.Count == 1 ? null : _modals[^2].View;

            if (enterModal)
            {
                enterModal.Settings = Settings;
            }

            ModalBackdrop backdrop = null;

            if (_disableBackdrop == false)
            {
                var lastBackdropIndex = _backdrops.Count - 1;
                backdrop = _backdrops[lastBackdropIndex];
                backdrop.Settings = Settings;
                _backdrops.RemoveAt(lastBackdropIndex);
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforePop(enterModal, exitModal, args);
            }

            await exitModal.BeforeExitAsync(false, args);

            if (enterModal != null)
            {
                await enterModal.BeforeEnterAsync(false, args);
            }

            // Play Animation
            await exitModal.ExitAsync(false, playAnimation, enterModal);

            if (enterModal != null)
            {
                await enterModal.EnterAsync(false, playAnimation, exitModal);
            }

            if (backdrop)
            {
                await backdrop.ExitAsync(playAnimation);
            }

            // End Transition
            _modals.RemoveAt(lastModalIndex);
            IsInTransition = false;

            // Postprocess
            exitModal.AfterExit(false, args);

            if (enterModal != null)
            {
                enterModal.AfterEnter(false, args);
            }

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterPop(enterModal, exitModal, args);
            }

            // Unload unused Modal
            await exitModal.BeforeReleaseAsync();

            DestroyAndForget(exitModal, backdrop).Forget();

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        private async UniTaskVoid DestroyAndForget(Modal modal, ModalBackdrop backdrop)
        {
            var modalId = modal.GetInstanceID();

            Destroy(modal.gameObject);

            if (backdrop)
            {
                Destroy(backdrop.gameObject);
            }

            await UniTask.NextFrame();

            if (_assetLoadHandles.TryGetValue(modalId, out var loadHandle))
            {
                AssetLoader.Release(loadHandle.Id);
                _assetLoadHandles.Remove(modalId);
            }
        }

        /// <summary>
        /// Preload a prefab of <see cref="Modal"/>.
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
        /// Preload a prefab of <see cref="Modal"/>.
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
            if (!_preloadedResourceHandles.TryGetValue(resourcePath, out var handle))
            {
                return false;
            }

            return handle.Status == AssetLoadStatus.Success;
        }

        public void ReleasePreloaded(string resourcePath)
        {
            if (_preloadedResourceHandles.TryGetValue(resourcePath, out var handle) == false)
            {
                Debug.LogError($"The resource at `{resourcePath}` is not preloaded.");
                return;
            }

            AssetLoader.Release(handle.Id);
        }
    }
}