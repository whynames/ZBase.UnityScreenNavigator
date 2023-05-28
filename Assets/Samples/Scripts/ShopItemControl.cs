using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Controls;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;

namespace Demo.Scripts
{
    public class ShopItemControl : Control
    {
        [SerializeField] private Image _thumbnailImage;
        [SerializeField] private Button _thumbButton;
        [SerializeField] private RectTransform _locked;

        public Image ThumbnailImage => _thumbnailImage;

        public Button ThumbButton => _thumbButton;

        public RectTransform Locked => _locked;

        public override UniTask Initialize(Memory<object> args)
        {
            CanvasGroup.enabled = false;
            return base.Initialize(args);
        }

        public override void Deinitialize(Memory<object> args)
        {
            _thumbButton.onClick.RemoveAllListeners();
            _thumbnailImage.sprite = null;
            _locked.gameObject.SetActive(true);
        }
    }
}
