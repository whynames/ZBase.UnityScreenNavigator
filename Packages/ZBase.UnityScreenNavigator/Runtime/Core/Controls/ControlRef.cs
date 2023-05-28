namespace ZBase.UnityScreenNavigator.Core.Controls
{
    public readonly struct ControlRef<T> where T : Control
    {
        public readonly PoolingPolicy PoolingPolicy;
        public readonly T Control;
        public readonly string ResourcePath;

        public ControlRef(
              T control
            , string resourcePath
            , PoolingPolicy poolingPolicy
        )
        {
            PoolingPolicy = poolingPolicy;
            Control = control;
            ResourcePath = resourcePath;
        }

        public void Deconstruct(out T control, out string resourcePath)
        {
            control = Control;
            resourcePath = ResourcePath;
        }

        public void Deconstruct(
              out T control
            , out string resourcePath
            , out PoolingPolicy poolingPolicy
        )
        {
            control = Control;
            resourcePath = ResourcePath;
            poolingPolicy = PoolingPolicy;
        }

        public static implicit operator ControlRef(ControlRef<T> value)
            => new ControlRef(value.Control, value.ResourcePath, value.PoolingPolicy);
    }

    public readonly struct ControlRef
    {
        public readonly PoolingPolicy PoolingPolicy;
        public readonly Control Control;
        public readonly string ResourcePath;

        public ControlRef(
              Control control
            , string resourcePath
            , PoolingPolicy poolingPolicy
        )
        {
            PoolingPolicy = poolingPolicy;
            Control = control;
            ResourcePath = resourcePath;
        }

        public void Deconstruct(out Control control, out string resourcePath)
        {
            control = Control;
            resourcePath = ResourcePath;
        }

        public void Deconstruct(
              out Control control
            , out string resourcePath
            , out PoolingPolicy poolingPolicy
        )
        {
            control = Control;
            resourcePath = ResourcePath;
            poolingPolicy = PoolingPolicy;
        }
    }
}
