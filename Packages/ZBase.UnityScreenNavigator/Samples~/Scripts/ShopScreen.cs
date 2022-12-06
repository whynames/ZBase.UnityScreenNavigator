using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Sheets;
using ZBase.UnityScreenNavigator.Foundation.Coroutine;

namespace Demo.Scripts
{
    public class ShopScreen : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        private const int ItemGridSheetCount = 3;

        [SerializeField] private SheetContainer _itemGridContainer;
        [SerializeField] private Button[] _itemGridButtons;

        private readonly int[] _itemGridSheetIds = new int[ItemGridSheetCount];

        public override async UniTask Initialize(Memory<object> args)
        {
            var registerHandles = new AsyncProcessHandle[ItemGridSheetCount];
            for (var i = 0; i < ItemGridSheetCount; i++)
            {
                var index = i;
                var options = new SheetOptions(
                    resourcePath: ResourceKey.ShopItemGridSheetPrefab(),
                    onLoaded: (sheetId, sheet) => {
                        var id = sheetId;
                        _itemGridSheetIds[index] = id;
                        var shopItemGrid = (ShopItemGridSheet)sheet;
                        shopItemGrid.Setup(index, GetCharacterId(index));
                    }
                );
                registerHandles[i] = _itemGridContainer.Register(options);
            }

            for (var i = 0; i < ItemGridSheetCount; i++)
            {
                var handle = registerHandles[i];
                while (!handle.IsTerminated)
                {
                    await UniTask.Yield();
                }
                
                var sheetId = _itemGridSheetIds[i];

                async void ShowSheet()
                {
                    if (_itemGridContainer.IsInTransition)
                    {
                        return;
                    }

                    if (_itemGridContainer.ActiveSheetId == sheetId)
                    {
                        // This sheet is already displayed.
                        return;
                    }

                    await _itemGridContainer.Show(sheetId, true);
                }

                _itemGridButtons[i].onClick.AddListener( ShowSheet);
            }

            await _itemGridContainer.Show(_itemGridSheetIds[0], false);
        }
        
        private int GetCharacterId(int index)
        {
            switch (index)
            {
                case 0:
                    return 3;
                case 1:
                    return 4;
                case 2:
                    return 5;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}