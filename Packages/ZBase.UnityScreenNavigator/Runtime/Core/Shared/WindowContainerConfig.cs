using System;
using UnityEngine.Serialization;
using ZBase.UnityScreenNavigator.Foundation;

namespace ZBase.UnityScreenNavigator.Core
{
    [Serializable]
    public class WindowContainerConfig
    {
        public string name;

        [FormerlySerializedAs("layerType")]
        public WindowContainerType containerType;

        public bool overrideSorting;

        [ShowIf(nameof(overrideSorting))]
        public SortingLayerId sortingLayer;

        [ShowIf(nameof(overrideSorting))]
        public int orderInLayer = 0;
    }
}
