using System;
using Cysharp.Threading.Tasks;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    public interface ISheetLifecycleEvent
    {
        UniTask Initialize(Memory<object> args);

        UniTask WillEnter(Memory<object> args);

        void DidEnter(Memory<object> args);

        UniTask WillExit(Memory<object> args);

        void DidExit(Memory<object> args);

        UniTask Cleanup();
    }
}