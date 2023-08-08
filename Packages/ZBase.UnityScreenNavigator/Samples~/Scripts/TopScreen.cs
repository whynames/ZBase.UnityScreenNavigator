using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Activities;
using ZBase.UnityScreenNavigator.Core.Screens;

namespace Demo.Scripts
{
    public class TopScreen : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        [SerializeField] private Button _button;

        protected override void Start()
        {
            _button.onClick.AddListener(OnClick);
        }

        protected override void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
            }
        }

        private void OnClick()
        {
            ActivityContainer.Find(ContainerKey.Activities).Show(ResourceKey.LoadingActivity());
            ScreenContainer.Of(transform).Push(ResourceKey.HomeScreenPrefab());
        }
    }
}
