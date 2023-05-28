using System;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    public sealed class AnonymousControlContainerCallbackReceiver : IControlContainerCallbackReceiver
    {
        public event Action<Control, Control, Memory<object>> OnBeforeShow;
        public event Action<Control, Control, Memory<object>> OnAfterShow;
        public event Action<Control, Memory<object>> OnBeforeHide;
        public event Action<Control, Memory<object>> OnAfterHide;

        public AnonymousControlContainerCallbackReceiver(
              Action<Control, Control, Memory<object>> onBeforeShow = null
            , Action<Control, Control, Memory<object>> onAfterShow = null
            , Action<Control, Memory<object>> onBeforeHide = null
            , Action<Control, Memory<object>> onAfterHide = null
        )
        {
            OnBeforeShow = onBeforeShow;
            OnAfterShow = onAfterShow;
            OnBeforeHide = onBeforeHide;
            OnAfterHide = onAfterHide;
        }

        void IControlContainerCallbackReceiver.BeforeShow(Control enterControl, Control exitControl, Memory<object> args)
        {
            OnBeforeShow?.Invoke(enterControl, exitControl, args);
        }

        void IControlContainerCallbackReceiver.AfterShow(Control enterControl, Control exitControl, Memory<object> args)
        {
            OnAfterShow?.Invoke(enterControl, exitControl, args);
        }

        void IControlContainerCallbackReceiver.BeforeHide(Control exitControl, Memory<object> args)
        {
            OnBeforeHide?.Invoke(exitControl, args);
        }

        void IControlContainerCallbackReceiver.AfterHide(Control exitControl, Memory<object> args)
        {
            OnAfterHide?.Invoke(exitControl, args);
        }
    }
}