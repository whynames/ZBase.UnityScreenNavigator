using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Shared;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;
using ZBase.UnityScreenNavigator.Foundation.Collections;
using ZBase.UnityScreenNavigator.Foundation.Coroutine;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    [RequireComponent(typeof(RectMask2D))]
    public sealed class SheetContainer : UIBehaviour
    {
        private static readonly Dictionary<int, SheetContainer> s_instanceCacheByTransform = new();
        private static readonly Dictionary<string, SheetContainer> s_instanceCacheByName = new();

        [SerializeField] private string _name;

        private readonly Dictionary<int, AssetLoadHandle<GameObject>> _assetLoadHandles = new();
        private readonly List<ISheetContainerCallbackReceiver> _callbackReceivers = new();
        private readonly Dictionary<string, int> _sheetNameToId = new();
        private readonly Dictionary<int, Sheet> _sheets = new();

        private int? _activeSheetId;
        private CanvasGroup _canvasGroup;
        private IAssetLoader _assetLoader;

        /// <summary>
        ///     By default, <see cref="IAssetLoader" /> in <see cref="UnityScreenNavigatorSettings" /> is used.
        ///     If this property is set, it is used instead.
        /// </summary>
        public IAssetLoader AssetLoader
        {
            get => _assetLoader ?? UnityScreenNavigatorSettings.Instance.AssetLoader;
            set => _assetLoader = value ?? throw new ArgumentNullException(nameof(value));
        }

        public int? ActiveSheetId => _activeSheetId;

        public Sheet ActiveSheet
        {
            get
            {
                if (!_activeSheetId.HasValue)
                {
                    return null;
                }

                return _sheets[_activeSheetId.Value];
            }
        }

        /// <summary>
        ///     True if in transition.
        /// </summary>
        public bool IsInTransition { get; private set; }

        /// <summary>
        ///     Registered sheets.
        /// </summary>
        public IReadOnlyDictionary<int, Sheet> Sheets => _sheets;

        public bool Interactable
        {
            get => _canvasGroup.interactable;
            set => _canvasGroup.interactable = value;
        }

        protected override void Awake()
        {
            _callbackReceivers.AddRange(GetComponents<ISheetContainerCallbackReceiver>());

            if (!string.IsNullOrWhiteSpace(_name))
            {
                s_instanceCacheByName.Add(_name, this);
            }
            _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        }

        protected override void OnDestroy()
        {
            foreach (var sheet in _sheets.Values)
            {
                Destroy(sheet.gameObject);
            }

            foreach (var assetLoadHandle in _assetLoadHandles.Values)
            {
                AssetLoader.Release(assetLoadHandle.Id);
            }

            _assetLoadHandles.Clear();

            s_instanceCacheByName.Remove(_name);

            using var keysToRemove = new ValueList<int>(4);

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
        ///     Get the <see cref="SheetContainer" /> that manages the sheet to which <see cref="transform" /> belongs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="useCache">Use the previous result for the <see cref="transform" />.</param>
        /// <returns></returns>
        public static SheetContainer Of(Transform transform, bool useCache = true)
        {
            return Of((RectTransform)transform, useCache);
        }

        /// <summary>
        ///     Get the <see cref="SheetContainer" /> that manages the sheet to which <see cref="rectTransform" /> belongs.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="useCache">Use the previous result for the <see cref="rectTransform" />.</param>
        /// <returns></returns>
        public static SheetContainer Of(RectTransform rectTransform, bool useCache = true)
        {
            var hashCode = rectTransform.GetInstanceID();

            if (useCache && s_instanceCacheByTransform.TryGetValue(hashCode, out var container))
            {
                return container;
            }

            container = rectTransform.GetComponentInParent<SheetContainer>();
            if (container != null)
            {
                s_instanceCacheByTransform.Add(hashCode, container);
                return container;
            }

            return null;
        }

        /// <summary>
        ///     Find the <see cref="SheetContainer" /> of <see cref="containerName" />.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static SheetContainer Find(string containerName)
        {
            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                return instance;
            }

            return null;
        }

        /// <summary>
        ///     Find the <see cref="SheetContainer" /> of <see cref="containerName" />.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public static bool TryFind(string containerName, out SheetContainer container)
        {
            container = null;

            if (s_instanceCacheByName.TryGetValue(containerName, out var instance))
            {
                container = instance;
            }

            return container;
        }

        /// <summary>
        ///     Add a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void AddCallbackReceiver(ISheetContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        ///     Remove a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void RemoveCallbackReceiver(ISheetContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Remove(callbackReceiver);
        }

        /// <summary>
        ///     Show a sheet.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="playAnimation"></param>
        /// <returns></returns>
        public AsyncProcessHandle Show(string resourcePath, bool playAnimation, params object[] args)
        {
            return CoroutineManager.Run<Sheet>(ShowRoutine(resourcePath, playAnimation, args));
        }

        /// <summary>
        ///     Show a sheet.
        /// </summary>
        /// <param name="sheetId"></param>
        /// <param name="playAnimation"></param>
        /// <returns></returns>
        public AsyncProcessHandle Show(int sheetId, bool playAnimation, params object[] args)
        {
            return CoroutineManager.Run<Sheet>(ShowRoutine(sheetId, playAnimation, args));
        }

        /// <summary>
        ///     Hide a sheet.
        /// </summary>
        /// <param name="playAnimation"></param>
        public AsyncProcessHandle Hide(bool playAnimation)
        {
            return CoroutineManager.Run<Sheet>(HideRoutine(playAnimation));
        }

        /// <summary>
        ///     Register a sheet.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="onLoad"></param>
        /// <param name="loadAsync"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public AsyncProcessHandle Register<TSheet>(SheetOptions options, params object[] args)
            where TSheet : Sheet
        {
            return CoroutineManager.Run<Sheet>(RegisterRoutine<TSheet>(options, args));
        }

        /// <summary>
        ///     Register a sheet.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="onLoad"></param>
        /// <param name="loadAsync"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public AsyncProcessHandle Register(SheetOptions options, params object[] args)
        {
            return CoroutineManager.Run<Sheet>(RegisterRoutine<Sheet>(options, args));
        }

        private IEnumerator RegisterRoutine<TSheet>(SheetOptions options, Memory<object> args)
            where TSheet : Sheet
        {
            var resourcePath = options.resourcePath;

            if (resourcePath == null)
            {
                throw new ArgumentNullException(nameof(resourcePath));
            }

            var assetLoadHandle = options.loadAsync
                ? AssetLoader.LoadAsync<GameObject>(resourcePath)
                : AssetLoader.Load<GameObject>(resourcePath);

            while (!assetLoadHandle.IsDone)
            {
                yield return null;
            }

            if (assetLoadHandle.Status == AssetLoadStatus.Failed)
            {
                throw assetLoadHandle.OperationException;
            }

            var instance = Instantiate(assetLoadHandle.Result);
            var sheet = instance.GetOrAddComponent<TSheet>();

            if (sheet == null)
            {
                throw new InvalidOperationException(
                    $"Cannot register because the \"{typeof(TSheet).Name}\" component is not attached to the specified resource \"{resourcePath}\".");
            }

            var sheetId = sheet.GetInstanceID();
            _sheets.Add(sheetId, sheet);
            _sheetNameToId[resourcePath] = sheetId;
            _assetLoadHandles.Add(sheetId, assetLoadHandle);

            options.onLoaded?.Invoke(sheetId, sheet);

            var afterLoadHandle = sheet.AfterLoad((RectTransform)transform, args);

            while (!afterLoadHandle.IsTerminated)
            {
                yield return null;
            }

            yield return sheetId;
        }

        private IEnumerator ShowRoutine(string resourcePath, bool playAnimation, Memory<object> args)
        {
            var sheetId = _sheetNameToId[resourcePath];
            yield return ShowRoutine(sheetId, playAnimation, args);
        }

        private IEnumerator ShowRoutine(int sheetId, bool playAnimation, Memory<object> args)
        {
            if (IsInTransition)
            {
                throw new InvalidOperationException(
                    "Cannot transition because there is a sheet already in transition.");
            }

            if (_activeSheetId.HasValue && _activeSheetId.Value.Equals(sheetId))
            {
                throw new InvalidOperationException(
                    $"Cannot transition because the sheet {sheetId} is already active.");
            }

            IsInTransition = true;
            
            if (!UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition)
            {
                Interactable = false;
            }

            var enterSheet = _sheets[sheetId];
            var exitSheet = _activeSheetId.HasValue ? _sheets[_activeSheetId.Value] : null;

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeShow(enterSheet, exitSheet);
            }

            using var preprocessHandles = new ValueList<AsyncProcessHandle>(2);
            if (exitSheet != null)
            {
                preprocessHandles.Add(exitSheet.BeforeExit(args));
            }

            preprocessHandles.Add(enterSheet.BeforeEnter(args));

            foreach (var coroutineHandle in preprocessHandles)
            {
                while (!coroutineHandle.IsTerminated)
                {
                    yield return null;
                }
            }

            // Play Animation
            using var animationHandles = new ValueList<AsyncProcessHandle>(2);
            if (exitSheet != null)
            {
                animationHandles.Add(exitSheet.Exit(playAnimation, enterSheet));
            }

            animationHandles.Add(enterSheet.Enter(playAnimation, exitSheet));

            foreach (var handle in animationHandles)
            {
                while (!handle.IsTerminated)
                {
                    yield return null;
                }
            }

            // End Transition
            _activeSheetId = sheetId;
            IsInTransition = false;

            // Postprocess
            if (exitSheet != null)
            {
                exitSheet.AfterExit(args);
            }

            enterSheet.AfterEnter(args);

            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterShow(enterSheet, exitSheet);
            }
            
            if (!UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition)
            {
                Interactable = true;
            }
        }

        private IEnumerator HideRoutine(bool playAnimation)
        {
            if (IsInTransition)
            {
                throw new InvalidOperationException(
                    "Cannot transition because there is a sheet already in transition.");
            }

            if (!_activeSheetId.HasValue)
            {
                throw new InvalidOperationException(
                    "Cannot transition because there is no active sheet.");
            }

            IsInTransition = true;
            
            if (!UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition)
            {
                Interactable = false;
            }

            var exitSheet = _sheets[_activeSheetId.Value];

            // Preprocess
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.BeforeHide(exitSheet);
            }

            var preprocessHandle = exitSheet.BeforeExit(null);
            while (!preprocessHandle.IsTerminated)
            {
                yield return preprocessHandle;
            }

            // Play Animation
            var animationHandle = exitSheet.Exit(playAnimation, null);
            while (!animationHandle.IsTerminated)
            {
                yield return null;
            }

            // End Transition
            _activeSheetId = null;
            IsInTransition = false;

            // Postprocess
            exitSheet.AfterExit(null);
            foreach (var callbackReceiver in _callbackReceivers)
            {
                callbackReceiver.AfterHide(exitSheet);
            }
            
            if (!UnityScreenNavigatorSettings.Instance.EnableInteractionInTransition)
            {
                Interactable = true;
            }
        }
    }
}