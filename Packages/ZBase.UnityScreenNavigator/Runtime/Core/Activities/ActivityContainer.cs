using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace ZBase.UnityScreenNavigator.Core.Activities
{
    [RequireComponent(typeof(RectMask2D))]
    public class ActivityContainer : ContainerLayer
    {
        private static Dictionary<int, ActivityContainer> s_instanceCacheByTransform = new();
        private static Dictionary<string, ActivityContainer> s_instanceCacheByName = new();

        private readonly Dictionary<string, AssetLoadHandle<GameObject>> _preloadHandles = new();
        private readonly Dictionary<int, AssetLoadHandle<GameObject>> _assetLoadHandles = new();
        private readonly List<IActivityContainerCallbackReceiver> _callbackReceivers = new();
        private readonly Dictionary<string, Activity> _activities = new();

        private IAssetLoader _assetLoader;

        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_instanceCacheByTransform = new();
            s_instanceCacheByName = new();
        }

        /// <summary>
        /// Get the <see cref="ActivityContainer" /> that manages the screen to which <see cref="transform" /> belongs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="useCache">Use the previous result for the <see cref="transform" />.</param>
        /// <returns></returns>
        public static ActivityContainer Of(Transform transform, bool useCache = true)
        {
            return Of((RectTransform)transform, useCache);
        }

        /// <summary>
        /// Get the <see cref="ActivityContainer" /> that manages the screen to which <see cref="rectTransform" /> belongs.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="useCache">Use the previous result for the <see cref="rectTransform" />.</param>
        /// <returns></returns>
        public static ActivityContainer Of(RectTransform rectTransform, bool useCache = true)
        {
            var id = rectTransform.GetInstanceID();
            if (useCache && s_instanceCacheByTransform.TryGetValue(id, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<ActivityContainer>();

            if (container)
            {
                s_instanceCacheByTransform.Add(id, container);
                return container;
            }

            Debug.LogError($"Cannot find any parent {nameof(ActivityContainer)} component", rectTransform);
            return null;
        }

        /// <summary>
        /// Find the <see cref="ActivityContainer" /> of <see cref="containerName" />.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static ActivityContainer Find(string containerName)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            Debug.LogError($"Cannot find any {nameof(ActivityContainer)} by name `{containerName}`");
            return null;
        }

        /// <summary>
        /// Find the <see cref="ActivityContainer" /> of <see cref="containerName" />.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out ActivityContainer container)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
                return true;
            }

            Debug.LogError($"Cannot find any {nameof(ActivityContainer)} by name `{containerName}`");
            container = default;
            return false;
        }

        /// <summary>
        /// Create a new instance of <see cref="ActivityContainer"/> as a layer
        /// </summary>
        /// <param name="layerName"></param>
        /// <param name="layer"></param>
        /// <param name="layerType"></param>
        /// <returns></returns>
        public static async UniTask<ActivityContainer> CreateAsync(ContainerLayerConfig layerConfig, IContainerLayerManager manager)
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

            var container = root.GetOrAddComponent<ActivityContainer>();
            await container.InitializeAsync(layerConfig, manager);

            if (string.IsNullOrWhiteSpace(layerConfig.name) == false)
            {
                s_instanceCacheByName.Add(layerConfig.name, container);
            }

            return container;
        }

        /// <summary>
        /// By default, <see cref="IAssetLoader" /> in <see cref="UnityScreenNavigatorSettings" /> is used.
        /// If this property is set, it is used instead.
        /// </summary>
        public IAssetLoader AssetLoader
        {
            get => _assetLoader ?? UnityScreenNavigatorSettings.Instance.AssetLoader;
            set => _assetLoader = value ?? throw new ArgumentNullException(nameof(value));
        }

        public IReadOnlyDictionary<string, Activity> Activities => _activities;

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void AddCallbackReceiver(IActivityContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void RemoveCallbackReceiver(IActivityContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        public void Add(string resourcePath, Activity activity)
        {
            if (resourcePath == null)
                throw new ArgumentNullException(nameof(resourcePath));

            if (_activities.TryGetValue(resourcePath, out var otherActivity))
            {
                if (activity != otherActivity)
                {
                    Debug.LogWarning($"Another {nameof(Activity)} is existing for `{resourcePath}`", otherActivity);
                }

                return;
            }

            _activities.Add(resourcePath, activity);

            if (activity.TryGetTransform(out var transform))
                transform.AddChild(transform);
        }

        public bool Remove(string resourcePath)
        {
            if (resourcePath == null)
                throw new ArgumentNullException(nameof(resourcePath));

            if (_activities.TryGetValue(resourcePath, out var activity))
            {
                if (activity.TryGetTransform(out var transform))
                    transform.RemoveChild(transform);

                return _activities.Remove(resourcePath);
            }

            return false;
        }

        public bool TryGet(string resourcePath, out Activity activity)
        {
            return _activities.TryGetValue(resourcePath, out activity);
        }

        public bool TryGet<T>(string resourcePath, out T activity) where T : Activity
        {
            if (_activities.TryGetValue(resourcePath, out var otherActivity))
            {
                if (otherActivity is T activityT)
                {
                    activity = activityT;
                    return true;
                }
            }

            activity = default;
            return false;
        }

        protected override void OnDestroy()
        {
            foreach (var kv in _activities)
            {
                DestroyAndForget(kv.Value).Forget();
            }

            _activities.Clear();
        }

        protected virtual int GetChildIndex(Transform child)
        {
            Transform myTransform = transform;
            var count = myTransform.childCount;
            for (var i = count - 1; i >= 0; i--)
            {
                if (myTransform.GetChild(i).Equals(child))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Show an instance of <typeparamref name="TActivity"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Show<TActivity>(ActivityOptions options, params object[] args)
            where TActivity : Activity
        {
            ShowAndForget<TActivity>(options, args).Forget();
        }

        /// <summary>
        /// Show an instance of <see cref="Activity"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Show(ActivityOptions options, params object[] args)
        {
            ShowAndForget<Activity>(options, args).Forget();
        }

        /// <summary>
        /// Show an instance of <typeparamref name="TActivity"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask ShowAsync<TActivity>(ActivityOptions options, params object[] args)
            where TActivity : Activity
        {
            await ShowAsyncInternal<TActivity>(options, args);
        }

        /// <summary>
        /// Show an instance of <see cref="Activity"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask ShowAsync(ActivityOptions options, params object[] args)
        {
            await ShowAsyncInternal<Activity>(options, args);
        }

        private async UniTask ShowAndForget<TActivity>(ActivityOptions options, Memory<object> args)
            where TActivity : Activity
        {
            await ShowAsyncInternal<TActivity>(options, args);
        }

        private async UniTask ShowAsyncInternal<TActivity>(ActivityOptions options, Memory<object> args)
            where TActivity : Activity
        {
            var resourcePath = options.options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            if (_activities.TryGetValue(resourcePath, out var showingActivity))
            {
                Debug.LogWarning(
                    $"Cannot transition because the {typeof(TActivity).Name} at `{resourcePath}` is already showing."
                    , showingActivity
                );

                return;
            }
            
            if (UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition == false)
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

            var instance = Instantiate(assetLoadHandle.Result);
            
            if (instance.TryGetComponent<TActivity>(out var activity) == false)
            {
                Debug.LogError(
                    $"Cannot transition because the {typeof(TActivity).Name} component is not " +
                    $"attached to the specified resource `{resourcePath}`."
                    , instance
                );

                return;
            }

            var activityId = activity.GetInstanceID();
            activity.Identifier = string.Concat(gameObject.name, activityId.ToString());
            _assetLoadHandles.Add(activityId, assetLoadHandle);

            Add(resourcePath, activity);

            options.options.onLoaded?.Invoke(activity, args);

            await activity.AfterLoadAsync(RectTransform, args);

            activity.SetSortingLayer(options.sortingLayer, options.orderInLayer);

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeShow(activity, args);
            }

            await activity.BeforeEnterAsync(true, args);

            // Play Animation
            await activity.EnterAsync(true, options.options.playAnimation);

            // Postprocess
            activity.AfterEnter(true, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterShow(activity, args);
            }

            if (UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        /// <summary>
        /// Hide an instance of <see cref="Activity"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void Hide(string resourcePath, bool playAnimation = true, params object[] args)
        {
            HideAndForget(resourcePath, playAnimation, args).Forget();
        }

        private async UniTaskVoid HideAndForget(string resourcePath, bool playAnimation, params object[] args)
        {
            await HideAsync(resourcePath, playAnimation, args);
        }

        /// <summary>
        /// Hide an instance of <see cref="Activity"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask HideAsync(string resourcePath, bool playAnimation = true, params object[] args)
        {
            if (TryGet(resourcePath, out var activity) == false)
            {
                Debug.LogError(
                    $"Cannot transition because there is no activity loaded " +
                    $"on the stack by the resource path `{resourcePath}`"
                );

                return;
            }

            if (UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeHide(activity, args);
            }

            await activity.BeforeEnterAsync(false, args);

            // Play Animation
            await activity.EnterAsync(false, playAnimation);

            // End Transition
            Remove(resourcePath);

            // Postprocess
            activity.AfterEnter(false, args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterHide(activity, args);
            }

            // Unload unused Modal
            await activity.BeforeReleaseAsync();

            // Unload unused Activity
            DestroyAndForget(activity).Forget();

            if (UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition == false)
            {
                Interactable = true;
            }
        }

        private async UniTaskVoid DestroyAndForget(Activity activity)
        {
            Destroy(activity.gameObject);

            await UniTask.NextFrame();

            var activityId = activity.GetInstanceID();

            if (_assetLoadHandles.TryGetValue(activityId, out var loadHandle))
            {
                AssetLoader.Release(loadHandle.Id);
                _assetLoadHandles.Remove(activityId);
            }
        }

        /// <summary>
        /// Hide all instances of <see cref="Activity"/>.
        /// </summary>
        /// <remarks>Fire-and-forget</remarks>
        public void HideAll(bool playAnimation = true)
        {
            HideAllAndForget(playAnimation).Forget();
        }

        private async UniTaskVoid HideAllAndForget(bool playAnimation = true)
        {
            await HideAllAsync(playAnimation);
        }

        /// <summary>
        /// Hide all instances of <see cref="Activity"/>.
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async UniTask HideAllAsync(bool playAnimation = true)
        {
            var keys = Pool<List<string>>.Shared.Rent();
            keys.AddRange(_activities.Keys);

            var count = keys.Count;

            for (var i = 0; i < count; i++)
            {
                await HideAsync(keys[i], playAnimation);
            }

            Pool<List<string>>.Shared.Return(keys);
        }

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
            if (_preloadHandles.ContainsKey(resourcePath))
            {
                Debug.LogError($"The resource at `{resourcePath}` has already been preloaded.");
                return;
            }

            var assetLoadHandle = loadAsync
                ? AssetLoader.LoadAsync<GameObject>(resourcePath)
                : AssetLoader.Load<GameObject>(resourcePath);

            _preloadHandles.Add(resourcePath, assetLoadHandle);

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
            return _preloadHandles.ContainsKey(resourcePath);
        }

        public bool IsPreloaded(string resourcePath)
        {
            if (_preloadHandles.TryGetValue(resourcePath, out var handle) == false)
            {
                return false;
            }

            return handle.Status == AssetLoadStatus.Success;
        }

        public void ReleasePreloaded(string resourcePath)
        {
            if (_preloadHandles.TryGetValue(resourcePath, out var handle) == false)
            {
                Debug.LogError($"The resource at `{resourcePath}` is not preloaded.");
                return;
            }

            AssetLoader.Release(handle.Id);
        }
    }
}