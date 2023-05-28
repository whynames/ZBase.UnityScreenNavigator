using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    [RequireComponent(typeof(RectMask2D))]
    public abstract class ControlContainerBase : ViewContainerBase, IViewContainer
    {
        [SerializeField] private string _name;
        [SerializeField] private UnityScreenNavigatorSettings _settings;

        private readonly List<IControlContainerCallbackReceiver> _callbackReceivers = new();
        private readonly Dictionary<int, ControlRef<Control>> _controls = new();

        public string ContainerName => _name;

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

        public int? ActiveControlId { get; protected set; }

        public Control ActiveControl
        {
            get
            {
                if (ActiveControlId.HasValue == false)
                {
                    return null;
                }

                return _controls[ActiveControlId.Value].Control;
            }
        }

        /// <summary>
        /// True if in transition.
        /// </summary>
        public bool IsInTransition { get; protected set; }

        protected IReadOnlyList<IControlContainerCallbackReceiver> CallbackReceivers => _callbackReceivers;

        protected IReadOnlyDictionary<int, ControlRef<Control>> Controls => _controls;

        protected override void Awake()
        {
            var _ = CanvasGroup;

            _callbackReceivers.AddRange(GetComponents<IControlContainerCallbackReceiver>());

            InitializePool();
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
        }

        public virtual void Deinitialize()
        {
            ActiveControlId = null;
            IsInTransition = false;

            var controls = _controls;

            foreach (var controlRef in controls.Values)
            {
                ReturnToPool(controlRef.Control, controlRef.ResourcePath, controlRef.PoolingPolicy);
            }

            controls.Clear();
        }

        /// <summary>
        /// Add a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void AddCallbackReceiver(IControlContainerCallbackReceiver callbackReceiver)
        {
            _callbackReceivers.Add(callbackReceiver);
        }

        /// <summary>
        /// Remove a callback receiver.
        /// </summary>
        /// <param name="callbackReceiver"></param>
        public void RemoveCallbackReceiver(IControlContainerCallbackReceiver callbackReceiver)
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

        private async UniTask<(int, T)> GetControlAsync<T>(ControlOptions options)
            where T : Control
        {
            var control = await GetViewAsync<T>(options.AsViewOptions());
            var controlId = control.GetInstanceID();
            _controls[controlId] = new ControlRef<Control>(control, options.resourcePath, options.poolingPolicy);

            return (controlId, control);
        }
    }
}