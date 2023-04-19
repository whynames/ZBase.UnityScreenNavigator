using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace ZBase.UnityScreenNavigator.Core.Views
{
    public abstract class ContainerLayer : Window, IContainerLayer
    {
        private readonly Dictionary<string, AssetLoadHandle<GameObject>> _preloadedResourceHandles = new();
        private readonly Dictionary<int, AssetLoadHandle<GameObject>> _assetLoadHandles = new();

        private IAssetLoader _assetLoader;

        public string LayerName { get; private set; }

        public ContainerLayerType LayerType { get; private set; }

        public IContainerLayerManager ContainerLayerManager { get; private set; }

        public Canvas Canvas { get; private set; }

        /// <summary>
        /// By default, <see cref="IAssetLoader" /> in <see cref="UnityScreenNavigatorSettings" /> is used.
        /// If this property is set, it is used instead.
        /// </summary>
        public IAssetLoader AssetLoader
        {
            get => _assetLoader ?? Settings.AssetLoader;
            set => _assetLoader = value ?? throw new ArgumentNullException(nameof(value));
        }

        protected ContainerLayerConfig Config { get; private set; }

        protected RectTransform PoolTransform { get; private set; }

        protected void Initialize(ContainerLayerConfig config, IContainerLayerManager manager)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Settings = UnityScreenNavigatorSettings.Instance;

            ContainerLayerManager = manager ?? throw new ArgumentNullException(nameof(manager));
            ContainerLayerManager.Add(this);

            LayerName = config.name;
            LayerType = config.layerType;
            
            var canvas = GetComponent<Canvas>();

            if (config.overrideSorting)
            {
                canvas.overrideSorting = true;
                canvas.sortingLayerID = config.sortingLayer.id;
                canvas.sortingOrder = config.orderInLayer;
            }

            Canvas = canvas;

            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        /// <summary>
        /// Preload a prefab of <see cref="Activity"/>.
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
        /// Preload a prefab of <see cref="Activity"/>.
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
                Debug.LogError($"The resource at `{resourcePath}` is not preloaded.");
                return;
            }

            AssetLoader.Release(handle.Id);
        }

        protected async UniTask<T> GetViewAsync<T>(string resourcePath, bool loadAsync)
            where T : View
        {
            var assetLoadHandle = loadAsync
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

            if (instance.TryGetComponent<T>(out var view) == false)
            {
                Debug.LogError(
                    $"Cannot find the {typeof(T).Name} component on the specified resource `{resourcePath}`."
                    , instance
                );

                return null;
            }

            view.Settings = Settings;

            var id = view.GetInstanceID();
            _assetLoadHandles.Add(id, assetLoadHandle);

            return view;
        }

        protected async UniTaskVoid DestroyAndForget(UIBehaviour view)
        {
            var id = view.GetInstanceID();

            Destroy(view.gameObject);

            await UniTask.NextFrame();

            if (_assetLoadHandles.TryGetValue(id, out var loadHandle))
            {
                AssetLoader.Release(loadHandle.Id);
                _assetLoadHandles.Remove(id);
            }
        }
    }
}