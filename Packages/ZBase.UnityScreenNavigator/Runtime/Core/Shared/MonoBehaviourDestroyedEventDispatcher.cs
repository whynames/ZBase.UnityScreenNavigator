using System;
using UnityEngine;

namespace ZBase.UnityScreenNavigator.Core.Shared
{
    public class MonoBehaviourDestroyedEventDispatcher : MonoBehaviour
    {
        public void OnDestroy()
        {
            OnDispatch?.Invoke();
        }

        public event Action OnDispatch;
    }
}