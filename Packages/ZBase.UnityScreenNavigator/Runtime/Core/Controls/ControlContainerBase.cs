using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Views;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    [RequireComponent(typeof(RectMask2D))]
    public abstract class ControlContainerBase : ViewContainerBase
    {
        [SerializeField] private string _name;
        [SerializeField] private UnityScreenNavigatorSettings _settings;

        private readonly List<IControlContainerCallbackReceiver> _callbackReceivers = new();

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

        protected IReadOnlyList<IControlContainerCallbackReceiver> CallbackReceivers => _callbackReceivers;

        protected override void Awake()
        {
            var _ = CanvasGroup;

            _callbackReceivers.AddRange(GetComponents<IControlContainerCallbackReceiver>());

            InitializePool();
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
    }
}