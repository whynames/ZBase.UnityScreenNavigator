using ZBase.UnityScreenNavigator.Foundation.AssetLoaders;

namespace Demo.Scripts
{
    public class DemoAddressablesLoader : AssetLoaderObject
    {
        private readonly AddressableAssetLoader _loader = new();

        public override AssetLoadHandle<T> Load<T>(string key)
        {
            return _loader.Load<T>(GetResourceKey(key));
        }

        public override AssetLoadHandle<T> LoadAsync<T>(string key)
        {
            return _loader.LoadAsync<T>(GetResourceKey(key));
        }

        public override void Release(AssetLoadHandleId handleId)
        {
            _loader.Release(handleId);
        }

        private string GetResourceKey(string key)
        {
            return $"prefab_demo_{key}";
        }
    }
}