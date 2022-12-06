using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Shared;
using ZBase.UnityScreenNavigator.Foundation;

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public sealed class ModalBackdrop : MonoBehaviour
    {
        [SerializeField] private ModalBackdropTransitionAnimationContainer _animationContainer;
        [SerializeField] private bool _closeModalWhenClicked;

        private CanvasGroup _canvasGroup;
        private RectTransform _parentTransform;
        private RectTransform _rectTransform;
        private Image _image;
        private float _originalAlpha;

        public ModalBackdropTransitionAnimationContainer AnimationContainer => _animationContainer;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();

            if (_closeModalWhenClicked)
            {
                if (TryGetComponent<Image>(out var image) == false)
                {
                    image = gameObject.AddComponent<Image>();
                    image.color = Color.clear;
                }

                if (TryGetComponent<Button>(out var button) == false)
                {
                    button = gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.None;
                }

                button.onClick.AddListener(() => {
                    var modalContainer = ModalContainer.Of(transform);
                    if (modalContainer.IsInTransition)
                        return;
                    modalContainer.Pop(true);
                });
            }

            _image = GetComponent<Image>();
            _originalAlpha = _image ? _image.color.a : 1f;
        }

        public void Setup(RectTransform parentTransform, float? alpha)
        {
            SetAlpha(alpha);

            _parentTransform = parentTransform;
            _rectTransform.FillParent(_parentTransform);
            _canvasGroup.interactable = _closeModalWhenClicked;

            gameObject.SetActive(false);
        }

        private void SetAlpha(float? value)
        {
            var image = _image;

            if (image == false)
            {
                return;
            }

            var alpha = _originalAlpha;

            if (value.HasValue)
            {
                alpha = value.Value;
            }

            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        internal async UniTask EnterAsync(bool playAnimation)
        {
            gameObject.SetActive(true);
            _rectTransform.FillParent(_parentTransform);
            _canvasGroup.alpha = 1f;

            if (playAnimation)
            {
                var anim = GetAnimation(true);
                anim.Setup(_rectTransform);
                
                await anim.PlayAsync();
            }

            _rectTransform.FillParent(_parentTransform);
        }

        internal async UniTask ExitAsync(bool playAnimation)
        {
            gameObject.SetActive(true);
            _rectTransform.FillParent(_parentTransform);
            _canvasGroup.alpha = 1f;

            if (playAnimation)
            {
                var anim = GetAnimation(false);
                anim.Setup(_rectTransform);

                await anim.PlayAsync();
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private ITransitionAnimation GetAnimation(bool enter)
        {
            var anim = _animationContainer.GetAnimation(enter);

            if (anim == null)
            {
                return UnityScreenNavigatorSettings.Instance.GetDefaultModalBackdropTransitionAnimation(enter);
            }

            return anim;
        }
    }
}