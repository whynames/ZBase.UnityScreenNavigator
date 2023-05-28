using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace ZBase.UnityScreenNavigator.Core
{
    [CreateAssetMenu(fileName = "WindowContainerSettings", menuName = "Screen Navigator/Settings/Window Container Settings", order = 0)]
    public class WindowContainerSettings : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("containerLayers")]
        private WindowContainerConfig[] containers;
        
        public ReadOnlyMemory<WindowContainerConfig> Containers
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => containers;
        }
    }
}