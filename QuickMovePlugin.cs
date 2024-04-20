using Aki.Reflection.Patching;
using BepInEx;
using DrakiaXYZ.QuickMoveToContainer.Helpers;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DrakiaXYZ.QuickMoveToContainer
{
    [BepInPlugin("xyz.drakia.quickmovetocontainer", "DrakiaXYZ-QuickMoveToContainer", "1.0.2")]
    [BepInDependency("com.spt-aki.core", "3.8.0")]
    public class QuickMovePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Settings.Init(Config);

            new QuickFindPatch().Enable();
        }
    }

    public class QuickFindPatch : ModulePatch
    {
        private static FieldInfo _windowListField;
        private static FieldInfo _windowLootItemField;
        private static FieldInfo _windowContainerWindowField;

        protected override MethodBase GetTargetMethod()
        {
            _windowListField = AccessTools.Field(typeof(ItemUiContext), "list_0");
            _windowLootItemField = AccessTools.GetDeclaredFields(typeof(GridWindow)).Single(x => x.FieldType == typeof(LootItemClass));

            Type windowContainerType = AccessTools.FirstInner(typeof(ItemUiContext), x => x.GetField("WindowType") != null);
            _windowContainerWindowField = AccessTools.Field(windowContainerType, "Window");

            return typeof(InteractionsHandlerClass).GetMethod(nameof(InteractionsHandlerClass.QuickFindAppropriatePlace));
        }

        [PatchPrefix]
        public static void PatchPrefix(Item item, ref IEnumerable<LootItemClass> targets, InteractionsHandlerClass.EMoveItemOrder order)
        {
            // If `order` doesn't have `MoveToAnotherSide` set, don't do anything
            if (!order.HasFlag(InteractionsHandlerClass.EMoveItemOrder.MoveToAnotherSide))
            {
                return;
            }

            // Find the currently active container
            var itemContainer = item.Parent.Container;
            List<LootItemClass> targetContainers = FindTargetContainers(itemContainer);
            if (targetContainers.Count == 0)
            {
                return;
            }

            var newTargets = new List<LootItemClass>();
            newTargets.AddRange(targetContainers);
            newTargets.AddRange(targets);

            targets = newTargets;
        }

        private static List<LootItemClass> FindTargetContainers(EFT.InventoryLogic.IContainer itemContainer)
        {
            var gridWindowList = new List<LootItemClass>();

            IList openWindowList = (IList)_windowListField.GetValue(ItemUiContext.Instance);
            for (int i = openWindowList.Count - 1; i >= 0; i--)
            {
                var window = _windowContainerWindowField.GetValue(openWindowList[i]);
                if (window.GetType() == typeof(GridWindow))
                {
                    GridWindow gridWindow = (GridWindow)window;
                    LootItemClass windowLootItem = _windowLootItemField.GetValue(gridWindow) as LootItemClass;

                    // Skip if the gridWindow contains the container the item is coming from
                    if (Enumerable.Contains(windowLootItem.Containers, itemContainer))
                    {
                        continue;
                    }

                    gridWindowList.Add(windowLootItem);

                    // If we're only checking the topmost container, exit here
                    if (!Settings.AllOpenContainers.Value)
                    {
                        break;
                    }
                }
            }

            return gridWindowList;
        }
    }
}
