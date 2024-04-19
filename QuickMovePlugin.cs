using Aki.Reflection.Patching;
using BepInEx;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DrakiaXYZ.QuickMoveToContainer
{
    [BepInPlugin("xyz.drakia.quickmovetocontainer", "DrakiaXYZ-QuickMoveToContainer", "1.0.0")]
    [BepInDependency("com.spt-aki.core", "3.8.0")]
    public class QuickMovePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
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
        public static void PatchPrefix(ref IEnumerable<LootItemClass> targets)
        {
            // Find the currently active container
            LootItemClass currentContainer = FindCurrentContainer();
            if (currentContainer == null)
            {
                return;
            }

            var newTargets = new List<LootItemClass>
            {
                currentContainer
            };
            newTargets.AddRange(targets);

            targets = newTargets;
        }

        private static LootItemClass FindCurrentContainer()
        {
            IList windowList = (IList)_windowListField.GetValue(ItemUiContext.Instance);
            if (windowList.Count == 0)
            {
                return null;
            }

            for (int i = windowList.Count - 1; i >= 0; i--)
            {
                var window = _windowContainerWindowField.GetValue(windowList[i]);
                if (window.GetType() == typeof(GridWindow))
                {
                    GridWindow gridWindow = (GridWindow)window;
                    return _windowLootItemField.GetValue(gridWindow) as LootItemClass;
                }
            }

            return null;
        }
    }
}
