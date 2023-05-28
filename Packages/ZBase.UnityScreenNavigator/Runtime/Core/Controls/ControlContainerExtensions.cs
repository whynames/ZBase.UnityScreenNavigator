using System;

namespace ZBase.UnityScreenNavigator.Core.Controls
{
    public static class ControlContainerExtensions
    {
        /// <summary>
        /// Add callbacks.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="onBeforeShow"></param>
        /// <param name="onAfterShow"></param>
        /// <param name="onBeforeHide"></param>
        /// <param name="onAfterHide"></param>
        public static void AddCallbackReceiver(this ControlContainerBase self
            , Action<Control, Control, Memory<object>> onBeforeShow = null
            , Action<Control, Control, Memory<object>> onAfterShow = null
            , Action<Control, Memory<object>> onBeforeHide = null
            , Action<Control, Memory<object>> onAfterHide = null
        )
        {
            var callbackReceiver = new AnonymousControlContainerCallbackReceiver(
                onBeforeShow, onAfterShow, onBeforeHide, onAfterHide
            );

            self.AddCallbackReceiver(callbackReceiver);
        }
    }
}