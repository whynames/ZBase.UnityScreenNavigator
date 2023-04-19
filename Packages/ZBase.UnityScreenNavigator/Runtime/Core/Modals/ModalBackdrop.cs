using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Foundation;

namespace ZBase.UnityScreenNavigator.Core.Modals
{
    public sealed class ModalBackdrop : UIBehaviour
    {
        [SerializeField] private ModalBackdropTransitionAnimationContainer _animationContainer;
        [SerializeField] private bool _closeModalWhenClicked;

        private CanvasGroup _canvasGroup;
        private RectTransform _parentTransform;
        private RectTransform _rectTransform;
        private Image _image;
        private float _originalAlpha;

        public UnityScreenNavigatorSettings Settings { get; set; }

        public ModalBackdropTransitionAnimationContainer AnimationContainer => _animationContainer;

        protected override void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();

            SetCloseModalOnClick(_closeModalWhenClicked);

            _image = GetComponent<Image>();
            _originalAlpha = _image ? _image.color.a : 1f;
        }

        public void Setup(
              RectTransform parentTransform
            , in float? alpha
            , in bool? closeModalWhenClick
        )
        {
            SetAlpha(alpha);
            SetCloseModalOnClick(closeModalWhenClick);

            _parentTransform = parentTransform;
            _rectTransform.FillParent(_parentTransform);
            _canvasGroup.interactable = _closeModalWhenClicked;

            gameObject.SetActive(false);
        }

        private void SetAlpha(in float? value)
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

        private void SetCloseModalOnClick(in bool? value)
        {
            if (value.HasValue)
            {
                _closeModalWhenClicked = value.Value;
            }

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

                button.onClick.AddListener(CloseModalOnClick);
            }
            else
            {
                if (TryGetComponent<Button>(out var button))
                {
                    button.onClick.RemoveListener(CloseModalOnClick);
                    Destroy(button);
                }
            }
        }

        private void CloseModalOnClick()
        {
            var modalContainer = ModalContainer.Of(transform);

            if (modalContainer.IsInTransition)
                return;

            modalContainer.Pop(true);
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
                return Settings.GetDefaultModalBackdropTransitionAnimation(enter);
            }

            return anim;
        }
    }
}