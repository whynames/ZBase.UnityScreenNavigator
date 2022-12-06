using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Shared.Views;
using ZBase.UnityScreenNavigator.Core.Sheets;
using ZBase.UnityScreenNavigator.Foundation.Collections;
using ZBase.UnityScreenNavigator.Foundation.Coroutine;

namespace Demo.Scripts
{
    public class CharacterModal : Modal
    {
        private const int ImageCount = 3;
        [SerializeField] private SheetContainer _imageContainer;
        [SerializeField] private CharacterModalThumbnailGrid thumbnailGrid;
        [SerializeField] private Button _expandButton;

        public RectTransform CharacterImageRectTransform => (RectTransform) _imageContainer.transform;

        private readonly (int sheetId, CharacterModalImageSheet sheet)[] _imageSheets =
            new (int sheetId, CharacterModalImageSheet sheet)[ImageCount];

        private int _characterId;
        private int _selectedRank;

        public void Setup(int characterId)
        {
            _characterId = characterId;
        }

        public override async UniTask Initialize(Memory<object> args)
        {
            using var imageSheetHandles = new ValueList<AsyncProcessHandle>(4);

            for (var i = 0; i < ImageCount; i++)
            {
                var index = i;
                var options = new SheetOptions(
                    resourcePath: ResourceKey.CharacterModalImageSheetPrefab(),
                    onLoaded: (sheetId, sheet) => {
                        _imageSheets[index] = (sheetId, (CharacterModalImageSheet)sheet);
                    }
                );
                var handle = _imageContainer.Register(options);
                imageSheetHandles.Add(handle);
            }

            foreach (var handle in imageSheetHandles) await handle;

            _expandButton.onClick.AddListener(OnExpandButtonClicked);
        }

        public override async UniTask WillPushEnter(Memory<object> args)
        {
            for (var i = 0; i < ImageCount; i++)
            {
                _imageSheets[i].sheet.Setup(_characterId, i + 1);
            }

            await _imageContainer.Show(_imageSheets[0].sheetId, false);
            _selectedRank = 1;

            thumbnailGrid.Setup(_characterId);
            thumbnailGrid.ThumbnailClicked += x =>
            {
                if (_imageContainer.IsInTransition)
                {
                    return;
                }

                var targetSheet = _imageSheets[x];
                if (_imageContainer.ActiveSheet.Equals(targetSheet.sheet))
                {
                    return;
                }

                var sheetId = targetSheet.sheetId;
                _imageContainer.Show(sheetId, true);
                _selectedRank = x + 1;
            };
        }

        public override UniTask Cleanup()
        {
            _expandButton.onClick.RemoveListener(OnExpandButtonClicked);
            return UniTask.CompletedTask;
        }

        private void OnExpandButtonClicked()
        {
            var options = new WindowOptions(ResourceKey.CharacterImageModalPrefab(), true,
                onLoaded: (modal, args) =>
                {
                    var characterImageModal = (CharacterImageModal) modal;
                    characterImageModal.Setup(_characterId, _selectedRank);
                });
            ModalContainer.Find(ContainerKey.Modals)
                .Push(options);
        }
    }
}