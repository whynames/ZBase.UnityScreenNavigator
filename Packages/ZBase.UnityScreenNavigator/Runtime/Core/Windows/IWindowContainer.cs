using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace ZBase.UnityScreenNavigator.Core.Windows
{
    public interface IWindowContainer
    {
        string LayerName { get; }

        WindowContainerType LayerType { get; }

        IWindowContainerManager ContainerManager { get; }

        Canvas Canvas { get; }

        IAssetLoader AssetLoader { get; set; }

        void Initialize(WindowContainerConfig config, IWindowContainerManager manager, UnityScreenNavigatorSettings settings);

        bool ContainsInPool(string resourcePath);

        int CountInPool(string resourcePath);

        void KeepInPool(string resourcePath, int amount);

        UniTask KeepInPoolAsync(string resourcePath, int amount);

        void Preload(string resourcePath, bool loadAsync = true, int amount = 1);

        UniTask PreloadAsync(string resourcePath, bool loadAsync = true, int amount = 1);
    }
}