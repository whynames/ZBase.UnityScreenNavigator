using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Views;

namespace ZBase.UnityScreenNavigator.Core.Windows
{
    public interface IWindowContainer : IViewContainer
    {
        string LayerName { get; }

        WindowContainerType LayerType { get; }

        IWindowContainerManager ContainerManager { get; }

        Canvas Canvas { get; }
    }
}