using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core;
using ZBase.UnityScreenNavigator.Core.Views;
using ZBase.UnityScreenNavigator.Core.Controls;
using ZBase.UnityScreenNavigator.Core.Sheets;

namespace Demo.Scripts
{
    public class ShopItemGridSheet : Sheet
    {
        [SerializeField] private SimpleControlContainer _controlContainer;

        private const int ItemCount = 8;
        private int _characterId;
        private ShopItemControl _firstShopItemControl;

        public void Setup(int index, int characterId)
        {
            Identifier = $"{nameof(ShopItemGridSheet)}{index}";
            _characterId = characterId;
            SetupTransitionAnimations(index);
        }

        public override UniTask Initialize(Memory<object> args)
        {
            var shopItemKey = ResourceKey.ShopItemControlPrefab();
            _controlContainer.Preload(shopItemKey, true, ItemCount);

            return UniTask.CompletedTask;
        }

        public override async UniTask WillEnter(Memory<object> args)
        {
            var shopItemKey = ResourceKey.ShopItemControlPrefab();

            for (var i = 0; i < ItemCount; i++)
            {
                ControlOptions options;

                if (i == 0)
                {
                    options = new ControlOptions(shopItemKey, false, onLoaded: OnFirstShopItemLoaded);
                    await _controlContainer.ShowAsync(options);
                }
                else
                {
                    options = new ControlOptions(shopItemKey, false);
                    _controlContainer.Show(options);
                }
            }
        }

        private void OnFirstShopItemLoaded(int controlId, Control control, Memory<object> args)
        {
            _firstShopItemControl = control.GetComponent<ShopItemControl>();
            
            var spriteKey = ResourceKey.CharacterThumbnailSprite(_characterId, 1);
            var sprite = DemoAssetLoader.AssetLoader.Load<Sprite>(spriteKey).Result;

            _firstShopItemControl.ThumbnailImage.sprite = sprite;
            _firstShopItemControl.ThumbButton.onClick.RemoveListener(OnFirstThumbButtonClicked);
            _firstShopItemControl.ThumbButton.onClick.AddListener(OnFirstThumbButtonClicked);

            _firstShopItemControl.Locked.gameObject.SetActive(false);
            _firstShopItemControl.Unlocked.gameObject.SetActive(true);
        }

        public override void DidExit(Memory<object> args)
        {
            _controlContainer.Cleanup();
        }

        public override async UniTask Cleanup(Memory<object> args)
        {
            await _controlContainer.CleanupAsync(args);
        }

        private void SetupTransitionAnimations(int index)
        {
            string beforeSheetIdentifierRegex;
            if (index == 0)
            {
                beforeSheetIdentifierRegex = string.Empty;
            }
            else if (index == 1)
            {
                beforeSheetIdentifierRegex = $"{nameof(ShopItemGridSheet)}0";
            }
            else
            {
                beforeSheetIdentifierRegex = $"{nameof(ShopItemGridSheet)}[0-{index - 1}]";
            }

            var afterSheetIdentifierRegex = $"{nameof(ShopItemGridSheet)}[{index + 1}-9]";
            var toLeftExitAnim = SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Center,
                afterAlignment: SheetAlignment.Left, beforeAlpha: 1.0f, afterAlpha: 0.0f);
            var toRightExitAnim = SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Center,
                afterAlignment: SheetAlignment.Right, beforeAlpha: 1.0f, afterAlpha: 0.0f);
            var fromRightEnterAnim = SimpleTransitionAnimationObject.CreateInstance(
                beforeAlignment: SheetAlignment.Right,
                afterAlignment: SheetAlignment.Center, beforeAlpha: 0.0f, afterAlpha: 1.0f);
            var fromLeftEnterAnim = SimpleTransitionAnimationObject.CreateInstance(beforeAlignment: SheetAlignment.Left,
                afterAlignment: SheetAlignment.Center, beforeAlpha: 0.0f, afterAlpha: 1.0f);

            if (!string.IsNullOrEmpty(beforeSheetIdentifierRegex))
            {
                var enterAnimation1 = new ControlTransitionAnimationContainer.TransitionAnimation {
                    PartnerControlIdentifierRegex = beforeSheetIdentifierRegex,
                    AssetType = AnimationAssetType.ScriptableObject,
                    AnimationObject = fromRightEnterAnim
                };

                AnimationContainer.EnterAnimations.Add(enterAnimation1);
            }

            var enterAnimation2 = new ControlTransitionAnimationContainer.TransitionAnimation {
                PartnerControlIdentifierRegex = afterSheetIdentifierRegex,
                AssetType = AnimationAssetType.ScriptableObject,
                AnimationObject = fromLeftEnterAnim
            };

            AnimationContainer.EnterAnimations.Add(enterAnimation2);

            if (!string.IsNullOrEmpty(beforeSheetIdentifierRegex))
            {
                var exitAnimation1 = new ControlTransitionAnimationContainer.TransitionAnimation {
                    PartnerControlIdentifierRegex = beforeSheetIdentifierRegex,
                    AssetType = AnimationAssetType.ScriptableObject,
                    AnimationObject = toRightExitAnim
                };

                AnimationContainer.ExitAnimations.Add(exitAnimation1);
            }

            var exitAnimation2 = new ControlTransitionAnimationContainer.TransitionAnimation {
                PartnerControlIdentifierRegex = afterSheetIdentifierRegex,
                AssetType = AnimationAssetType.ScriptableObject,
                AnimationObject = toLeftExitAnim
            };

            AnimationContainer.ExitAnimations.Add(exitAnimation2);
        }

        private void OnFirstThumbButtonClicked()
        {
            var modalContainer = ModalContainer.Find(ContainerKey.Modals);
            var options = new ViewOptions(
                  ResourceKey.CharacterModalPrefab()
                , playAnimation: true
                , poolingPolicy: PoolingPolicy.DisablePooling
                , onLoaded: (modal, args) => {
                    var characterModal = (CharacterModal) modal;
                    characterModal.Setup(_characterId);
                }
            );

            modalContainer.Push(options);
        }
    }
}