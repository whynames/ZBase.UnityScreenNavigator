using UnityEngine;

namespace ZBase.UnityScreenNavigator.Core.Windows
{
    public interface IWindowContainer
    {
        string LayerName { get; }

        WindowContainerType LayerType { get; }

        IWindowContainerManager ContainerManager { get; }

        Canvas Canvas { get; }
    }
}