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
        [SerializeField] private RectTransform _unlocked;

        public Image ThumbnailImage => _thumbnailImage;

        public Button ThumbButton => _thumbButton;

        public RectTransform Locked => _locked;

        public RectTransform Unlocked => _unlocked;

        public override UniTask Initialize(Memory<object> args)
        {
            return base.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            _thumbButton.onClick.RemoveAllListeners();
            _thumbnailImage.sprite = null;
            _unlocked.gameObject.SetActive(false);
            _locked.gameObject.SetActive(true);

            return UniTask.CompletedTask;
        }
    }
}
