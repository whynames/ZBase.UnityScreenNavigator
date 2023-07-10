using UnityEditor;
using ZBase.UnityScreenNavigator.Core.Views;

namespace ZBase.UnityScreenNavigator.Editor.Core.Shared.Views
{
    [CustomEditor(typeof(View), editorForChildClasses: true)]
    public class ViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target is View view && view.DontAddCanvasGroupAutomatically == false)
            {
                var _ = view.CanvasGroup;
            }
        }
    }
}
