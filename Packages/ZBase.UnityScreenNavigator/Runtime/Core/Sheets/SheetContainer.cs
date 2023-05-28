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
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
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

            if (ActiveControlId.HasValue && ActiveControlId.Value.Equals(sheetId))
            {
                Debug.LogWarning($"Cannot transition because the sheet {sheetId} is already active.");
                return;
            }

            IsInTransition = true;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var enterSheet = Controls[sheetId].Control;
            enterSheet.Settings = Settings;

            ControlRef<Control>? exitSheetRef = ActiveControlId.HasValue ? Controls[ActiveControlId.Value] : null;
            var exitSheet = exitSheetRef.HasValue ? exitSheetRef.Value.Control : null;

            if (exitSheet)
            {
                exitSheet.Settings = Settings;
            }

            // Preprocess
            foreach (var callbackReceiver in CallbackReceivers)
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
            ActiveControlId = sheetId;
            IsInTransition = false;

            // Postprocess
            if (exitSheet)
            {
                exitSheet.AfterExit(args);
            }

            enterSheet.AfterEnter(args);

            foreach (var callbackReceiver in CallbackReceivers)
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

            if (ActiveControlId.HasValue == false)
            {
                Debug.LogWarning("Cannot transition because there is no active sheet.");
                return;
            }

            IsInTransition = true;

            if (Settings.EnableInteractionInTransition == false)
            {
                Interactable = false;
            }

            var exitSheetRef = Controls[ActiveControlId.Value];
            var exitSheet = exitSheetRef.Control;
            exitSheet.Settings = Settings;

            // Preprocess
            foreach (var callbackReceiver in CallbackReceivers)
            {
                callbackReceiver.BeforeHide(exitSheet, args);
            }

            await exitSheet.BeforeExitAsync(args);

            // Play Animation
            await exitSheet.ExitAsync(playAnimation, null);

            // End Transition
            ActiveControlId = null;
            IsInTransition = false;

            // Postprocess
            exitSheet.AfterExit(args);

            foreach (var callbackReceiver in CallbackReceivers)
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