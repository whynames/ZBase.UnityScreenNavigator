using System;

namespace ZBase.UnityScreenNavigator.Core.Views
{
    public delegate void OnLoadCallback(View view, Memory<object> args);

    public readonly struct ViewOptions
    {
        public readonly bool loadAsync;
        public readonly bool playAnimation;
        public readonly PoolingPolicy poolingPolicy;
        public readonly string resourcePath;
        public readonly OnLoadCallback onLoaded;

        public ViewOptions(
            string resourcePath
            , bool playAnimation = true
            , OnLoadCallback onLoaded = null
            , bool loadAsync = true
            , PoolingPolicy poolingPolicy = PoolingPolicy.UseSettings
        )
        {
            this.loadAsync = loadAsync;
            this.playAnimation = playAnimation;
            this.poolingPolicy = poolingPolicy;
            this.resourcePath = resourcePath;
            this.onLoaded = onLoaded;
        }

        public static implicit operator ViewOptions(string resourcePath)
            => new(resourcePath);
    }
}