using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Controls;
using ZBase.UnityScreenNavigator.Foundation;

namespace ZBase.UnityScreenNavigator.Core.Sheets
{
    [DisallowMultipleComponent]
    public class Sheet : Control
    {
        [SerializeField]
        private int _renderingOrder;

        protected override void OnAfterLoad(RectTransform parentTransform)
        {
            RectTransform.FillParent(Parent);

            // Set order of rendering.
            var siblingIndex = 0;

            for (var i = 0; i < Parent.childCount; i++)
            {
                var child = Parent.GetChild(i);
                var childControl = child.GetComponent<Sheet>();
                siblingIndex = i;

                if (_renderingOrder >= childControl._renderingOrder)
                {
                    continue;
                }

                break;
            }

            RectTransform.SetSiblingIndex(siblingIndex);
        }

        protected override void OnBeforeEnter()
        {
            RectTransform.FillParent(Parent);
        }

        protected override void OnBeforeExit()
        {
            RectTransform.FillParent(Parent);
        }

        protected override ITransitionAnimation GetAnimation(bool enter, Control partner)
        {
            var partnerIdentifier = partner == true ? partner.Identifier : string.Empty;
            var anim = AnimationContainer.GetAnimation(enter, partnerIdentifier);

            if (anim == null)
            {
                return Settings.GetDefaultSheetTransitionAnimation(enter);
            }

            return anim;
        }
    }
}