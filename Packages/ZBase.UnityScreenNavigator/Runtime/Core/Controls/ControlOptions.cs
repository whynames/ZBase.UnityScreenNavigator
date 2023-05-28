using ZBase.UnityScreenNavigator.Core.Views;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    public readonly struct ControlOptions
    {
        public readonly bool loadAsync;
        public readonly string resourcePath;
        public readonly PoolingPolicy poolingPolicy;
        public readonly ControlLoadedAction onLoaded;

        public ControlOptions(
              string resourcePath
            , ControlLoadedAction onLoaded = null
            , bool loadAsync = true
            , PoolingPolicy poolingPolicy = PoolingPolicy.UseSettings
        )
        {
            this.loadAsync = loadAsync;
            this.resourcePath = resourcePath;
            this.onLoaded = onLoaded;
            this.poolingPolicy = poolingPolicy;
        }

        public ViewOptions AsViewOptions()
            => new(resourcePath, false, null, loadAsync, poolingPolicy);

        public static implicit operator ControlOptions(string resourcePath)
            => new(resourcePath);
    }
}