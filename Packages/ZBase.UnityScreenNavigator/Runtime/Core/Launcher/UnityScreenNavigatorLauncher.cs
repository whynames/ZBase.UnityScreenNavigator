using System;
using UnityEngine;
using UnityEngine.Serialization;
using ZBase.UnityScreenNavigator.Core.Activities;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core.Windows;
using ZBase.UnityScreenNavigator.Foundation;

namespace ZBase.UnityScreenNavigator.Core
{
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public class UnityScreenNavigatorLauncher : MonoBehaviour
    {
        [SerializeField]
        private UnityScreenNavigatorSettings unityScreenNavigatorSettings;

        [SerializeField, FormerlySerializedAs("containerLayerSettings")]
        private WindowContainerSettings windowContainerSettings;

        protected GlobalWindowContainerManager WindowContainerManager { get; private set; }

        protected virtual void Awake()
        {
            if (unityScreenNavigatorSettings == false)
            {
                throw new NullReferenceException(nameof(unityScreenNavigatorSettings));
            }

            if (windowContainerSettings == false)
            {
                throw new NullReferenceException(nameof(windowContainerSettings));
            }

            UnityScreenNavigatorSettings.DefaultSettings = unityScreenNavigatorSettings;
            WindowContainerManager = this.GetOrAddComponent<GlobalWindowContainerManager>();
        }

        protected virtual void Start()
        {
            var layers = windowContainerSettings.Containers.Span;
            var manager = WindowContainerManager;

            foreach (var layer in layers)
            {
                switch (layer.containerType)
                {
                    case WindowContainerType.Modal:
                        ModalContainer.Create(layer, manager, unityScreenNavigatorSettings);
                        break;

                    case WindowContainerType.Screen:
                        ScreenContainer.Create(layer, manager, unityScreenNavigatorSettings);
                        break;

                    case WindowContainerType.Activity:
                        ActivityContainer.Create(layer, manager, unityScreenNavigatorSettings);
                        break;
                }
            }
        }
    }
}