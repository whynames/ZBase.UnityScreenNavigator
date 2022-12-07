using UnityEngine;

namespace ZBase.UnityScreenNavigator.Core.Views
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public sealed class GlobalContainerLayerManager : ContainerLayerManager
    {
        public static GlobalContainerLayerManager Root;

        protected override void Start()
        {
            base.Start();
            Root = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Root = null;
        }
    }
}