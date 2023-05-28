using System;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    public interface IControlContainerCallbackReceiver
    {
        void BeforeShow(Control enterControl, Control exitControl, Memory<object> args);

        void AfterShow(Control enterControl, Control exitControl, Memory<object> args);

        void BeforeHide(Control exitControl, Memory<object> args);

        void AfterHide(Control exitControl, Memory<object> args);
    }
}