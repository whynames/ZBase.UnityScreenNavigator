using ZBase.UnityScreenNavigator.Core.Screens;
using ZBase.UnityScreenNavigator.Core;
using ZBase.UnityScreenNavigator.Core.Views;
using Cysharp.Threading.Tasks;

namespace Demo.Scripts
{
    public class Launcher : UnityScreenNavigatorLauncher
    {
        protected override void Start()
        {
            base.Start();
            ShowTopPage().Forget();
        }

        private async UniTaskVoid ShowTopPage()
        {
            var options = new WindowOptions(ResourceKey.TopPagePrefab(), false, loadAsync: false);
            await GlobalContainerLayerManager.Find<ScreenContainer>(ContainerKey.Screens).PushAsync(options);
        }
    }
}