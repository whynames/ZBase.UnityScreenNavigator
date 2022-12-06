using System;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Activities;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core.Shared;
using ZBase.UnityScreenNavigator.Core.Shared.Views;
using ZBase.UnityScreenNavigator.Foundation;

namespace Demo.Scripts
{
    public class Launcher : MonoBehaviour
    {
        [SerializeField]
        private ContainerLayerSettings containerLayerSettings;

        private GlobalContainerLayerManager _globalContainerLayerManager;

        private void Awake()
        {
            if (containerLayerSettings == null)
                throw new ArgumentNullException(nameof(containerLayerSettings));

            _globalContainerLayerManager = this.GetOrAddComponent<GlobalContainerLayerManager>();
        }

        private async void Start()
        {
            var layers = containerLayerSettings.GetContainerLayers();
            var manager = _globalContainerLayerManager;

            foreach (var layer in layers)
            {
                switch (layer.layerType)
                {
                    case ContainerLayerType.Modal:
                        await ModalContainer.CreateAsync(layer, manager);
                        break;

                    case ContainerLayerType.Screen:
                        await ScreenContainer.CreateAsync(layer, manager);
                        break;

                    case ContainerLayerType.Activity:
                        await ActivityContainer.CreateAsync(layer, manager);
                        break;
                }
            }

            var options = new WindowOptions(ResourceKey.TopPagePrefab(), false, loadAsync: false);
            await manager.Find<ScreenContainer>(ContainerKey.Screens).PushAsync(options);
        }
    }
}