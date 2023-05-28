using System;
using UnityEngine;
using UnityEngine.Serialization;
using ZBase.UnityScreenNavigator.Core.Activities;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core.Windows;

namespace ZBase.UnityScreenNavigator.Core
{
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public class UnityScreenNavigatorLauncher : WindowContainerManager
    {
        [SerializeField]
        private UnityScreenNavigatorSettings unityScreenNavigatorSettings;

        [SerializeField, FormerlySerializedAs("containerLayerSettings")]
        private WindowContainerSettings windowContainerSettings;

        protected override void Awake()
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
        }

        protected override void Start()
        {
            var layers = windowContainerSettings.Containers.Span;

            foreach (var layer in layers)
            {
                switch (layer.containerType)
                {
                    case WindowContainerType.Modal:
                        ModalContainer.Create(layer, this, unityScreenNavigatorSettings);
                        break;

                    case WindowContainerType.Screen:
                        ScreenContainer.Create(layer, this, unityScreenNavigatorSettings);
                        break;

                    case WindowContainerType.Activity:
                        ActivityContainer.Create(layer, this, unityScreenNavigatorSettings);
                        break;
                }
            }
        }
    }
}