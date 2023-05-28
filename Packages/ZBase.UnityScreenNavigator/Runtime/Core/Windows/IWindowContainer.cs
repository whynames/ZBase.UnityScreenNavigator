using UnityEngine;

namespace ZBase.UnityScreenNavigator.Core.Views
{
    public interface IWindowContainer
    {
        string LayerName { get; }

        WindowContainerType LayerType { get; }

        IWindowContainerManager ContainerManager { get; }

        Canvas Canvas { get; }
    }
}