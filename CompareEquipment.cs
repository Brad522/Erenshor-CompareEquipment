using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Runtime.Remoting.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Erenshor_CompareEquipment
{
    [BepInPlugin(ModGUID, ModDescription, ModVersion)]
    public class CompareEquipment : BaseUnityPlugin
    {
        internal const string ModName = "CompareEquipment";
        internal const string ModVersion = "1.1.0";
        internal const string ModDescription = "Compare Equipment";
        internal const string Author = "Brad522";
        private const string ModGUID = Author + "." + ModName;

        private readonly Harmony harmony = new Harmony(ModGUID);

        private GameObject gameUI;
        private GameObject playerInv;
        private ItemIcon dragIcon;
        private static RectTransform canvasRect;
        private static RectTransform positionDummy;
        private bool uiInitialized;

        private static float halfScaledWindowWidth;
        private static float minValidX;
        private static Resolution curResolution;

        //public static ManualLogSource Log;

        public static GameObject clonedItemInfo;
        public static ItemCompareWindow ItemCompareWindow;
        public static ItemIcon curItemLook;
        public static ItemIcon curItemEquip;
        public static Vector3 compareWindowPos;
        private float modifiedItemInfoPosX;

        //For separating the new UI components into their own GameObject - Here just in case using existing UI causes issues
        //private static GameObject cwUI;

        public void Awake()
        {
            harmony.PatchAll();
            uiInitialized = false;

            //Log = Logger;

            // Initialize the variables that are used to clamp the windows to the bounds of the screen.
            float scaleX = Screen.width / 1920f;
            float scaleY = Screen.height / 1080f;
            float scaleFactor = Mathf.Min(scaleX, scaleY);
            float windowWidth = 350f;
            float scaledWindowWidth = windowWidth * scaleFactor;
            halfScaledWindowWidth = scaledWindowWidth / 2f;
            curResolution = Screen.currentResolution;
            minValidX = 5f;

            //More stuff for separating the new UI components into their own GameObject - Here just in case using existing UI causes issues
            //cwUI = new GameObject("CompareEquipment");
            //DontDestroyOnLoad(cwUI);
            //cwUI.AddComponent<Canvas>();
            //cwUI.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            //CanvasScaler cScaler = cwUI.AddComponent<CanvasScaler>();
            //cScaler.referenceResolution = new Vector2((float)1920, (float)1080);
            //cScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            //cScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        // Ensures required UI elements are initialized and handles compare window updates.
        private void Update()
        {
            // If UI was initialized but references are now invalid (e.g., after returning to main menu).
            if (uiInitialized && !CheckUIElementsInit())
                uiInitialized = false;

            // Attempt to initialize UI if it hasn't been done yet.
            if (!uiInitialized)
                TryInitializeUIReferences();

            // Handles clamping the windows to the bounds of the screen.
            ClampWindows();

            // Handles automatic closing of the compare window.
            HandleCompareWindow();

            // Allow switching of the comparison between primary/secondary slots via key input.
            HandleCompareWindowSwitch();
        }

        // Function to check that all UI references exist.
        private bool CheckUIElementsInit()
        {
            return gameUI != null &&
                clonedItemInfo != null &&
                ItemCompareWindow != null &&
                playerInv != null &&
                dragIcon != null &&
                canvasRect != null &&
                positionDummy != null;
        }

        // Attempts to cache necessary UI references and initialize custom UI components.
        private void TryInitializeUIReferences()
        {
            // Cache the main UI GameObject to avoid calling GameObject.Find every frame.
            if (gameUI == null)
            {
                gameUI = GameObject.Find("UI");
                if (gameUI == null)
                    return; // UI not yet loaded, abort further initialization.
            }

            // Clone the ItemInfo window if it hasn't been cloned yet.
            if (clonedItemInfo == null)
            {
                GameObject originalItemInfo = GameObject.Find("ItemInfo");
                if (originalItemInfo != null)
                    clonedItemInfo = Instantiate(GameObject.Find("ItemInfo"), gameUI.transform);
            }

            // Create and initalize the ItemCompareWindow if needed.
            if (ItemCompareWindow == null && clonedItemInfo != null)
                gameUI.AddComponent<ItemCompareWindow>();

            if (ItemCompareWindow != null)
                InitItemCompareWindow();

            // Cache reference to the PlayerInv GameObject.
            if (playerInv == null)
                playerInv = GameObject.Find("PlayerInv");

            // Cache reference to the component used during item dragging
            if (dragIcon == null && playerInv != null)
            {
                Transform mouseSlot = playerInv.transform.Find("Mouse Slot");
                if (mouseSlot != null)
                {
                    Transform itemIcon = mouseSlot.Find("ItemIcon (2)");
                    if (itemIcon != null)
                        dragIcon = itemIcon.GetComponent<ItemIcon>();
                }
            }

            // Cache reference to the canvas RectTransform.
            if (canvasRect == null)
                canvasRect = gameUI.GetComponent<Canvas>().GetComponent<RectTransform>();

            // Create a dummy RectTransform for positioning the windows.
            if (positionDummy == null)
            {
                GameObject dummyObj = new GameObject("PositionDummy");
                dummyObj.transform.SetParent(gameUI.transform);

                RectTransform rt = dummyObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = Vector2.zero;

                dummyObj.SetActive(false);

                positionDummy = rt;
            }

            // Final check to confirm all required UI references are set.
            uiInitialized = CheckUIElementsInit();
        }

        // Clamps the compare window to the bounds of the screen.
        private void ClampWindows()
        {
            // Only proceed if the UI is initialized, the compare window is active and the compare window is out of bounds.
            if (!uiInitialized || !ItemCompareWindow.isWindowActive())
                return;

            // Check if the resolution was changed and update the halfScaledWindowWidth accordingly.
            if (curResolution.width != Screen.currentResolution.width || curResolution.height != Screen.currentResolution.height)
            {
                curResolution = Screen.currentResolution;
                float scaleX = Screen.width / 1920f;
                float scaleY = Screen.height / 1080f;
                float scaleFactor = Mathf.Min(scaleX, scaleY);
                float windowWidth = 350f;
                float scaledWindowWidth = windowWidth * scaleFactor;
                halfScaledWindowWidth = scaledWindowWidth / 2f;
            }

            // Only proceed if the compare window is out of bounds.
            if (CheckOutOfBounds())
            {
                // Calculate the minimum valid X position for the compare window and amount to push the windows by.
                float leftMostX = ItemCompareWindow.ParentWindow.transform.position.x - halfScaledWindowWidth;
                float pushAmount = Mathf.Max(0, minValidX - leftMostX);

                // Only push the ItemInfoWindow if the previously modified position is different from the current position.
                float curItemInfoWindowPosX = GameData.ItemInfoWindow.ParentWindow.transform.position.x;
                if (!Mathf.Approximately(curItemInfoWindowPosX,modifiedItemInfoPosX))
                {
                    modifiedItemInfoPosX = GameData.ItemInfoWindow.ParentWindow.transform.position.x + pushAmount;
                    GameData.ItemInfoWindow.ParentWindow.transform.position = new Vector2(modifiedItemInfoPosX, GameData.ItemInfoWindow.ParentWindow.transform.position.y);
                    
                }

                // Push the CompareEquipment window to the right.
                ItemCompareWindow.ParentWindow.transform.position = new Vector2(ItemCompareWindow.ParentWindow.transform.position.x + pushAmount, CompareEquipment.ItemCompareWindow.ParentWindow.transform.position.y);
            }
        }

        // Handles closing the compare window on certain conditions.
        private void HandleCompareWindow()
        {
            // Only proceed if the UI is initialized and the compare window is open.
            if (!uiInitialized || !ItemCompareWindow.isWindowActive())
                return;

            // Check if the player's inventory window is closed or the inventory object is destroyed.
            bool isInventoryClosed = playerInv == null || !playerInv.activeSelf;

            // Check if an item is currently being dragged.
            bool isDragging = dragIcon != null && dragIcon.dragging;

            // Close the compare window under either condition.
            if (isInventoryClosed || isDragging)
                ItemCompareWindow.CloseItemWindow();
        }

        // Handles switching of the compare window between the Primary and Secondary slots.
        private void HandleCompareWindowSwitch()
        {
            // Ensure UI is ready, compare window is active, and key was pressed.
            if (!uiInitialized || !ItemCompareWindow.isWindowActive() || !Input.GetKeyDown(KeyCode.F))
                return;

            // Check required variables and make sure we're not looking at an empty slot.
            if (curItemLook == null || curItemEquip == null || curItemLook == GameData.PlayerInv.Empty)
                return;

            // Only allow switching for items that can go in either Primary or Secondary slots.
            if (curItemLook.MyItem.RequiredSlot != Item.SlotType.PrimaryOrSecondary)
                return;

            // Ensure the current item being compared is for a valid slot.
            if (curItemEquip.ThisSlotType != Item.SlotType.Primary && curItemEquip.ThisSlotType != Item.SlotType.Secondary)
                return;

            // Switch the equipped item variable to the item in the opposite slot.
            curItemEquip = GetEquippedItem(GetAlternateSlotType(curItemEquip.ThisSlotType));

            // Abort if the result is invalid or opposite slot is empty.
            if (curItemEquip == null || curItemEquip.MyItem == GameData.PlayerInv.Empty)
                return;

            // Update the compare window with the item from the opposite slot.
            ItemCompareWindow.CloseItemWindow();
            ItemCompareWindow.DisplayItem(curItemEquip.MyItem, compareWindowPos, curItemEquip.Quantity);
        }

        // Checks if the compare window is out of bounds and needs to be clamped. 
        private static bool CheckOutOfBounds()
        {
            if (CompareEquipment.ItemCompareWindow.ParentWindow.transform.position.x - CompareEquipment.halfScaledWindowWidth < CompareEquipment.minValidX)
                return true;
            else
                return false;
        }

        /// Initializes fields of the new ItemCompareWindow component to the appropriate GameObjects / Components of
        /// the cloned ItemInfo window.
        private void InitItemCompareWindow()
        {
            clonedItemInfo.SetActive(false);

            var transform = clonedItemInfo.transform;
            ItemCompareWindow.ParentWindow = clonedItemInfo;
            ItemCompareWindow.OtherTextParent = FindChild(transform, "OtherText");
            ItemCompareWindow.StatTextParent = FindChild(transform, "EquipmentInfo");
            ItemCompareWindow.ItemEffect = FindChild(transform, "OnClick");
            ItemCompareWindow.ReqLvl = FindChild(transform, "ReqLvl");
            ItemCompareWindow.Banner = FindAndGet<Image>(transform, "BG");
            ItemCompareWindow.ItemIcon = FindAndGet<Image>(transform, "Icon");
            ItemCompareWindow.ItemName = FindAndGet<TextMeshProUGUI>(transform, "Title");
            ItemCompareWindow.Lore = FindAndGet<TextMeshProUGUI>(transform, "Lore");
            ItemCompareWindow.Usable = FindAndGet<TextMeshProUGUI>(transform, "UsableBy");

            var effectTransform = ItemCompareWindow.ItemEffect.transform.Find("Image (1)");
            ItemCompareWindow.ClickDesc = FindAndGet<TextMeshProUGUI>(effectTransform, "SpellDesc");
            ItemCompareWindow.ClickSpell = FindAndGet<TextMeshProUGUI>(effectTransform, "SpellName");

            var stat = ItemCompareWindow.StatTextParent.transform;
            ItemCompareWindow.AC = FindAndGet<TextMeshProUGUI>(stat, "ArmorStat");
            ItemCompareWindow.Agi = FindAndGet<TextMeshProUGUI>(stat, "AgiStat");
            ItemCompareWindow.Cha = FindAndGet<TextMeshProUGUI>(stat, "ChaStat");
            ItemCompareWindow.DelNum = FindAndGet<TextMeshProUGUI>(stat, "DelStat");
            ItemCompareWindow.DelTXT = FindAndGet<TextMeshProUGUI>(stat, "Delay");
            ItemCompareWindow.Dex = FindAndGet<TextMeshProUGUI>(stat, "DexStat");
            ItemCompareWindow.DMGNum = FindAndGet<TextMeshProUGUI>(stat, "DmgStat");
            ItemCompareWindow.DMGtxt = FindAndGet<TextMeshProUGUI>(stat, "Damage");
            ItemCompareWindow.End = FindAndGet<TextMeshProUGUI>(stat, "EndStat");
            ItemCompareWindow.ER = FindAndGet<TextMeshProUGUI>(stat, "ERStat");
            ItemCompareWindow.HP = FindAndGet<TextMeshProUGUI>(stat, "HealthStat");
            ItemCompareWindow.Int = FindAndGet<TextMeshProUGUI>(stat, "IntStat");
            ItemCompareWindow.Mana = FindAndGet<TextMeshProUGUI>(stat, "ManaStat");
            ItemCompareWindow.MR = FindAndGet<TextMeshProUGUI>(stat, "MRStat");
            ItemCompareWindow.PR = FindAndGet<TextMeshProUGUI>(stat, "PRStat");
            ItemCompareWindow.Res = FindAndGet<TextMeshProUGUI>(stat, "ResStat");
            ItemCompareWindow.Slot = FindAndGet<TextMeshProUGUI>(stat, "SlotType");
            ItemCompareWindow.Str = FindAndGet<TextMeshProUGUI>(stat, "StrStat");
            ItemCompareWindow.VR = FindAndGet<TextMeshProUGUI>(stat, "VRStat");
            ItemCompareWindow.Wis = FindAndGet<TextMeshProUGUI>(stat, "WisStat");

            ItemCompareWindow.GodlyText = new Color(0.9894f, 0.533f, 1f, 1f);
            ItemCompareWindow.Legendary = new Color(1f, 0f, 0.7107f, 1f);
            ItemCompareWindow.Blessed = new Color(0f, 0.5583f, 1f, 1f);
            ItemCompareWindow.BlessedText = new Color(0f, 0.9802f, 1f, 1f);
            ItemCompareWindow.Normal = Color.white;
            ItemCompareWindow.NormalText = Color.white;
        }

        // Helper functions to make the InitItemCompareWindow function cleaner.
        private T FindAndGet<T>(Transform parent, string path) where T : Component
        {
            var child = parent.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        private GameObject FindChild(Transform parent, string path)
        {
            var child = parent.Find(path);
            return child?.gameObject;
        }

        // Gets the ItemIcon of the equipped item in the same equipment slot of the item we are currently looking at.
        public static ItemIcon GetEquippedItem(Item.SlotType slotNeeded)
        {
            foreach (ItemIcon itemIcon in GameData.PlayerInv.EquipmentSlots)
            {
                var slotType = itemIcon.ThisSlotType;

                if (slotType == slotNeeded)
                    return itemIcon;

                if (slotNeeded == Item.SlotType.PrimaryOrSecondary &&
                    (slotType == Item.SlotType.Primary || slotType == Item.SlotType.Secondary) &&
                    itemIcon.MyItem != GameData.PlayerInv.Empty)
                    return itemIcon;
            }
            return null;
        }

        // Returns the opposite hand equipment slot.
        private Item.SlotType GetAlternateSlotType(Item.SlotType curSlotType)
        {
            switch (curSlotType)
            {
                case Item.SlotType.Primary:
                    return Item.SlotType.Secondary;
                case Item.SlotType.Secondary:
                    return Item.SlotType.Primary;
                default:
                    return curSlotType;
            }
        }

        /// Complete refactor of the original OnPointerEnter function as with additional coding it was hard to read.
        /// Functionally the same. This version of the function only runs if you are hovering over an item
        /// that can be equipped.
        [HarmonyPatch(typeof(ItemIcon))]
        [HarmonyPatch("OnPointerEnter")]
        class OnPointerEnterPatch
        {
            static bool Prefix(ItemIcon __instance)
            {
                // Check if the item can be equipped, if it can't run the original function.
                if (__instance.MyItem.RequiredSlot == Item.SlotType.General)
                    return true;

                GameData.HighlightedItem = __instance;
                __instance.transform.localScale = Vector3.one * 1.33f;

                if (__instance.MyItem == GameData.PlayerInv.Empty || __instance.dragging)
                    return false;

                GameData.PlayerAud.PlayOneShot(GameData.Misc.Click, 0.05f * GameData.SFXVol);

                // Store the item we are looking at for use later if we need to switch between the Primary and Secondary slots.
                CompareEquipment.curItemLook = __instance;

                // Set offset values for the item info window and compare window.
                float mouseY = Input.mousePosition.y;
                int yOffset = mouseY > (float)(Screen.height / 2) ? -225 : 225;
                int xOffset = -250;
                int compareXOffset = xOffset - 347;

                DisplayItemInfoWindow(new Vector2((float)xOffset, (float)yOffset), mouseY);
                DisplayCompareWindow(new Vector2((float)compareXOffset, (float)yOffset), mouseY);

                // Only included here incase there are any equipment items that have quests attached. Otherwise not needed.
                HandleItemQuests();

                // Skips the original function since we handled everything it would normally do.
                return false;
            }
        }

        private static void DisplayItemInfoWindow(Vector2 offset, float mouseY)
        {
            Vector2 windowPosition = CalculatePosition(CompareEquipment.curItemLook.transform, offset, mouseY);
            if (windowPosition == Vector2.zero)
                return;

            GameData.ItemInfoWindow.DisplayItem(CompareEquipment.curItemLook.MyItem, windowPosition, CompareEquipment.curItemLook.Quantity);
        }

        private static void DisplayCompareWindow(Vector2 offset, float mouseY)
        {
            // Don't compare against itself if we're hovering over an equipped item.
            if (GameData.PlayerInv.EquipmentSlots.Contains(CompareEquipment.curItemLook))
                return;

            // Get the equipped item that matches the required slot of the hovered item.
            CompareEquipment.curItemEquip = GetEquippedItem(CompareEquipment.curItemLook.MyItem.RequiredSlot);

            // Proceed only if a valid equipped item exists in that slot.
            if (CompareEquipment.curItemEquip != null && CompareEquipment.curItemEquip.MyItem != GameData.PlayerInv.Empty)
            {
                Vector2 compareWindowPosition = CalculatePosition(CompareEquipment.curItemLook.transform, offset, mouseY);
                if (compareWindowPosition == Vector2.zero)
                    return;

                CompareEquipment.compareWindowPos = compareWindowPosition;

                // If the compare window is initialized, display the item. If not the coroutine will wait a frame at a time
                // until the window is initialized.
                if (CompareEquipment.ItemCompareWindow != null)
                    CompareEquipment.ItemCompareWindow.DisplayItem(CompareEquipment.curItemEquip.MyItem, CompareEquipment.compareWindowPos, CompareEquipment.curItemEquip.Quantity);
                else
                    CoroutineRunner.Run(WaitForCompareWindow(CompareEquipment.curItemEquip.MyItem, CompareEquipment.compareWindowPos, CompareEquipment.curItemEquip.Quantity));
            }
        }

        // Calculates the correct position to place the window with respect to resolution and aspect ratio
        private static Vector2 CalculatePosition(Transform itemTransform, Vector2 offset, float mouseY)
        {
            // Determines starting position of the window based on if the item is in the player's inventory or not.
            Vector3 itemPosition = (CompareEquipment.curItemLook.ThisSlotType != Item.SlotType.General &&
                    !CompareEquipment.curItemLook.VendorSlot &&
                    !CompareEquipment.curItemLook.LootSlot)
                    ? itemTransform.position
                    : itemTransform.parent.position;

            if (CompareEquipment.canvasRect == null)
                return Vector2.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(CompareEquipment.canvasRect, (Vector2)itemPosition, null, out Vector2 localPoint);

            // Counteract the offset implemented in the DisplayItem() function of the base game.
            int num = mouseY > (Screen.height * 0.5f) ? -1 : 1;
            Vector2 baseGameOffset = new Vector2(-200f, 100f * num);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(CompareEquipment.canvasRect, (Vector2)itemPosition + baseGameOffset, null, out Vector2 offsetLocalPoint);

            Vector2 baseOffsetInCanvasSpace = offsetLocalPoint - localPoint;

            // Set the dummy window position with anchoredPosition which stays consistent regardless of resolution or aspect ratio
            CompareEquipment.positionDummy.anchoredPosition = localPoint - baseOffsetInCanvasSpace + offset;

            // return the transform.position of the dummy window for use by DisplayItem()
            return positionDummy.transform.position;
        }

        // Same logic as in the normal function just moved to a separate function
        private static void HandleItemQuests()
        {
            if (CompareEquipment.curItemLook.MyItem.AssignQuestOnRead != null &&
                (CompareEquipment.curItemLook.MyItem.AssignQuestOnRead.repeatable ||
                !GameData.CompletedQuests.Contains(CompareEquipment.curItemLook.MyItem.AssignQuestOnRead.DBName)))
            {
                GameData.AssignQuest(CompareEquipment.curItemLook.MyItem.AssignQuestOnRead.DBName);
            }
            if (CompareEquipment.curItemLook.MyItem.CompleteOnRead != null)
            {
                GameData.FinishQuest(CompareEquipment.curItemLook.MyItem.CompleteOnRead.DBName);
            }
        }

        // Waits for a frame at a time while ItemCompareWindow isn't initialized then calls for the window to be displayed.
        private static IEnumerator WaitForCompareWindow(Item item, Vector3 position, int quantity)
        {
            while (ItemCompareWindow == null)
            {
                yield return null;
            }

            CompareEquipment.ItemCompareWindow.DisplayItem(item, position, quantity);
        }

        /// If the item's required slot isn't general then close the iteminfowindow and itemcomparewindow if they are open.
        /// Otherwise run normally.
        [HarmonyPatch(typeof(ItemIcon))]
        [HarmonyPatch("OnPointerExit")]
        class OnPointerExitPatch
        {
            static bool Prefix(ItemIcon __instance)
            {
                if (__instance.MyItem.RequiredSlot == Item.SlotType.General)
                    return true;

                GameData.HighlightedItem = null;

                if (GameData.ItemInfoWindow.isWindowActive())
                    GameData.ItemInfoWindow.CloseItemWindow();

                if (CompareEquipment.ItemCompareWindow.isWindowActive())
                    CompareEquipment.ItemCompareWindow.CloseItemWindow();

                return false;
            }
        }
    }
}
