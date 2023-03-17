using ZBase.UnityScreenNavigator.Core.Views;

namespace ZBase.UnityScreenNavigator.Core
{
    public readonly struct ViewRef<T> where T : View
    {
        public readonly T View;
        public readonly string ResourcePath;

        public ViewRef(T view, string resourcePath)
        {
            View = view;
            ResourcePath = resourcePath;
        }

        public void Deconstruct(out T view, out string resourcePath)
        {
            view = View;
            resourcePath = ResourcePath;
        }

        public static implicit operator ViewRef<T>((T, string) value)
            => new ViewRef<T>(value.Item1, value.Item2);

        public static implicit operator (T, string)(ViewRef<T> value)
            => (value.View, value.ResourcePath);
    }
}
