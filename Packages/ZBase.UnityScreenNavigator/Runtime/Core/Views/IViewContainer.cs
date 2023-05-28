using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace ZBase.UnityScreenNavigator.Core.Views
{
    public interface IViewContainer
    {
        IAssetLoader AssetLoader { get; set; }

        bool ContainsInPool(string resourcePath);

        int CountInPool(string resourcePath);

        void KeepInPool(string resourcePath, int amount);

        UniTask KeepInPoolAsync(string resourcePath, int amount);

        void Preload(string resourcePath, bool loadAsync = true, int amount = 1);

        UniTask PreloadAsync(string resourcePath, bool loadAsync = true, int amount = 1);
    }
}