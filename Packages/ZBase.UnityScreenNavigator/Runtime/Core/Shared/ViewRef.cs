using ZBase.UnityScreenNavigator.Core.Views;

namespace ZBase.UnityScreenNavigator.Core
{
    public readonly struct ViewRef<T> where T : View
    {
        public readonly bool IgnorePoolingSetting;
        public readonly T View;
        public readonly string ResourcePath;

        public ViewRef(
              T view
            , string resourcePath
            , bool ignorePoolingSetting
        )
        {
            IgnorePoolingSetting = ignorePoolingSetting;
            View = view;
            ResourcePath = resourcePath;
        }

        public void Deconstruct(out T view, out string resourcePath)
        {
            view = View;
            resourcePath = ResourcePath;
        }

        public void Deconstruct(
              out T view
            , out string resourcePath
            , out bool ignorePoolingSetting
        )
        {
            view = View;
            resourcePath = ResourcePath;
            ignorePoolingSetting = IgnorePoolingSetting;
        }

        public static implicit operator ViewRef(ViewRef<T> value)
            => new ViewRef(value.View, value.ResourcePath, value.IgnorePoolingSetting);
    }

    public readonly struct ViewRef
    {
        public readonly bool IgnorePoolingSetting;
        public readonly View View;
        public readonly string ResourcePath;

        public ViewRef(
              View view
            , string resourcePath
            , bool ignorePoolingSetting
        )
        {
            IgnorePoolingSetting = ignorePoolingSetting;
            View = view;
            ResourcePath = resourcePath;
        }

        public void Deconstruct(out View view, out string resourcePath)
        {
            view = View;
            resourcePath = ResourcePath;
        }

        public void Deconstruct(
              out View view
            , out string resourcePath
            , out bool ignorePoolingSetting
        )
        {
            view = View;
            resourcePath = ResourcePath;
            ignorePoolingSetting = IgnorePoolingSetting;
        }
    }
}
