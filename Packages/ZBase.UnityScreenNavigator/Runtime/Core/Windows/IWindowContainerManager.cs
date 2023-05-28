using System.Collections.Generic;

namespace ZBase.UnityScreenNavigator.Core.Windows
{
    /// <summary>
    /// Manages layers of UI views.
    /// </summary>
    public interface IWindowContainerManager
    {
        IReadOnlyList<IWindowContainer> Containers { get; }

        void Add(IWindowContainer container);

        bool Remove(IWindowContainer container);

        T Find<T>() where T : IWindowContainer;

        T Find<T>(string containerName) where T : IWindowContainer;

        bool TryFind<T>(out T container) where T : IWindowContainer;

        bool TryFind<T>(string containerName, out T container) where T : IWindowContainer;

        void Clear();
    }
}