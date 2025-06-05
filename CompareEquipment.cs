using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Erenshor_CompareEquipment
{
    [BepInPlugin(ModGUID, ModDescription, ModVersion)]
    public class CompareEquipment : BaseUnityPlugin
    {
        internal const string ModName = "CompareEquipment";
        internal const string ModVersion = "1.2.2";
        internal const string ModDescription = "Compare Equipment";
        internal const string Author = "Brad522";
        private const string ModGUID = Author + "." + ModName;

        private readonly Harmony harmony = new Harmony(ModGUID);

        private GameObject gameUI;
        private GameObject playerInvUI;
        private GameObject inspectSimUI;
        private ItemIcon dragIcon;
        private static RectTransform canvasRect;
        private static RectTransform positionDummy;
        private bool uiInitialized;

        private static float halfScaledWindowWidth;
        private static float minValidX;
        private static Resolution curResolution;

        public static GameObject clonedItemInfo;
        public static ItemCompareWindow ItemCompareWindow;
        public static ItemSlotData curItemLook;
        public static ItemSlotData curItemEquip;
        public static List<ItemSlotData> equipSlots;
        public static Vector3 compareWindowPos;

        private ConfigEntry<KeyboardShortcut> switchKey;

        //For separating the new UI components into their own GameObject - Here just in case using existing UI causes issues
        //private static GameObject cwUI;

        public void Awake()
        {
            harmony.PatchAll();
            uiInitialized = false;

            // Initialize the variables that are used to clamp the windows to the bounds of the screen.
            float scaleX = Screen.width / 1920f;
            float scaleY = Screen.height / 1080f;
            float scaleFactor = Mathf.Min(scaleX, scaleY);
            float windowWidth = 350f;
            float scaledWindowWidth = windowWidth * scaleFactor;
            halfScaledWindowWidth = scaledWindowWidth / 2f;
            curResolution = Screen.currentResolution;
            minValidX = 5f;

            switchKey = Config.Bind(
                "General",
                "Hotkey",
                new KeyboardShortcut(KeyCode.F),
                "Hotkey used to switch the slot you are comparing with.");

            Logger.LogMessage("CompareEquipment loaded successfully!");

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

        private void OnDestroy()
        {
            // Unpatch all methods when the mod is disabled or unloaded.
            harmony.UnpatchAll(ModGUID);

            // Destroy GameObjects or Components created by the mod.
            if (clonedItemInfo != null)
                GameObject.Destroy(clonedItemInfo);
            if (ItemCompareWindow != null)
                GameObject.Destroy(ItemCompareWindow);
            if (positionDummy != null)
                GameObject.Destroy(positionDummy.gameObject);
            if (equipSlots != null)
                equipSlots.Clear();

            // Set references to null
            gameUI = null;
            playerInvUI = null;
            inspectSimUI = null;
            dragIcon = null;
            canvasRect = null;
            positionDummy = null;
            clonedItemInfo = null;
            ItemCompareWindow = null;
            curItemLook = null;
            curItemEquip = null;
            equipSlots = null;
            switchKey = null;

            // Reset the static variables to their default values.
            compareWindowPos = default;
            uiInitialized = false;
            curResolution = default;
            halfScaledWindowWidth = 0f;
            minValidX = 0f;
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

            // Handles automatic closing of the windows.
            HandleClosingWindows();

            // Allow switching of the comparison between primary/secondary slots via key input.
            HandleCompareWindowSwitch();
        }

        // Function to check that all UI references exist.
        private bool CheckUIElementsInit()
        {
            return gameUI != null &&
                clonedItemInfo != null &&
                ItemCompareWindow != null &&
                playerInvUI != null &&
                inspectSimUI != null &&
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
                GameObject originalItemInfo = FindChild(gameUI.transform, "ItemInfo");
                if (originalItemInfo != null)
                    clonedItemInfo = Instantiate(originalItemInfo, gameUI.transform);
            }

            // Create and initalize the ItemCompareWindow if needed.
            if (ItemCompareWindow == null && clonedItemInfo != null)
                //ItemCompareWindow = gameUI.AddComponent<ItemCompareWindow>();
                gameUI.AddComponent<ItemCompareWindow>();

            if (ItemCompareWindow != null)
                InitItemCompareWindow();

            // Cache reference to the PlayerInv GameObject.
            if (playerInvUI == null)
                playerInvUI = FindChild(gameUI.transform, "UIElements/InvPar/PlayerInv");

            // Cache reference to the InspectSim GameObject.
            if (inspectSimUI == null)
                inspectSimUI = FindChild(gameUI.transform, "UIElements/SimInspectPar/InspectSim");

            // Cache reference to the component used during item dragging
            if (dragIcon == null && playerInvUI != null)
                dragIcon = FindAndGet<ItemIcon>(playerInvUI.transform, "Mouse Slot/ItemIcon (2)");

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
                // Cache transforms and positions
                Transform compareTransform = ItemCompareWindow.ParentWindow.transform;
                Transform itemInfoTransform = GameData.ItemInfoWindow.ParentWindow.transform;

                Vector2 comparePos = compareTransform.position;
                Vector2 itemInfoPos = itemInfoTransform.position;

                // Calculate the minimum valid X position for the compare window and amount to push the windows by.
                float leftMostX = comparePos.x - halfScaledWindowWidth;
                float pushAmount = Mathf.Max(0, minValidX - leftMostX);
                Vector2 pushVector = new Vector2(pushAmount, 0f);
                Vector2 offset = new Vector2(200f, 100f);

                // Push the item info window.
                itemInfoTransform.position = itemInfoPos + pushVector;

                // Push the compare window and update compareWindowPos for switch usage.
                Vector2 newComparePos = comparePos + pushVector;
                compareTransform.position = newComparePos;

                compareWindowPos = newComparePos + offset;
            }
        }

        // Handles closing the windows in certain conditions.
        private void HandleClosingWindows()
        {
            // Only proceed if the UI is initialized and the compare window is open.
            if (!uiInitialized)
                return;

            if (!ItemCompareWindow.isWindowActive() && !GameData.ItemInfoWindow.isWindowActive())
                return;

            // Check if the player's inventory window is closed or the inventory window is destroyed.
            bool isInventoryClosed = playerInvUI == null || !playerInvUI.activeSelf;

            // Check if the inspect sim window is closed or the inventory window is destroyed.
            bool isSimInventoryClosed = inspectSimUI == null || !inspectSimUI.activeSelf;

            // Check if an item is currently being dragged.
            bool isDragging = dragIcon != null && dragIcon.dragging;

            // Logic to determine if the windows should be closed.
            bool shouldCloseCompare = isInventoryClosed || isDragging;
            bool shouldCloseInfo = isSimInventoryClosed && shouldCloseCompare;

            // Close the compare window under either condition.
            if (shouldCloseCompare && ItemCompareWindow.isWindowActive())
                ItemCompareWindow.CloseItemWindow();

            // Close the ItemInfoWindow if the Sim inventory is closed.
            if (shouldCloseInfo && GameData.ItemInfoWindow.isWindowActive())
                GameData.ItemInfoWindow.CloseItemWindow();
        }

        // Handles switching of the compare window between the Primary and Secondary slots.
        private void HandleCompareWindowSwitch()
        {
            // Ensure UI is ready, compare window is active, and key was pressed.
            if (!uiInitialized || !ItemCompareWindow.isWindowActive() || !switchKey.Value.IsDown())
                return;

            // Check required variables and make sure we're not looking at an empty slot.
            if (curItemLook == null || curItemEquip == null || curItemLook.MyItem == GameData.PlayerInv.Empty)
                return;

            // Only allow switching for items that can go in the Ring, Bracer slots and items that can go in both Primary and Secondary slots.
            if (curItemLook.MyItem.RequiredSlot != Item.SlotType.PrimaryOrSecondary && 
                curItemLook.MyItem.RequiredSlot != Item.SlotType.Ring &&
                curItemLook.MyItem.RequiredSlot != Item.SlotType.Bracer)
                return;

            // Ensures the current item being compared is in a valid slot.
            // Just an extra safety check in case things ever become out of sync.
            if (curItemEquip.ThisSlotType != Item.SlotType.Primary &&
                curItemEquip.ThisSlotType != Item.SlotType.Secondary &&
                curItemEquip.ThisSlotType != Item.SlotType.Ring &&
                curItemEquip.ThisSlotType != Item.SlotType.Bracer)
                return;

            // Get the equipped item in the opposite slot depending on the current slot type.
            if (curItemEquip.ThisSlotType == Item.SlotType.Bracer || curItemEquip.ThisSlotType == Item.SlotType.Ring)
                curItemEquip = GetEquippedItem(curItemEquip.ThisSlotType, true);
            else
                curItemEquip = GetEquippedItem(GetAlternateSlotType(curItemEquip.ThisSlotType), false);

            // Abort if the result is invalid or opposite slot is empty.
            if (curItemEquip == null || curItemEquip.MyItem == GameData.PlayerInv.Empty)
                return;

            // Update the compare window with the item from the opposite slot.
            ItemCompareWindow.CloseItemWindow();
            ItemCompareWindow.DisplayItem(curItemEquip.MyItem, compareWindowPos, curItemEquip.Quant);
        }

        // Checks if the compare window is out of bounds and needs to be clamped. 
        private static bool CheckOutOfBounds()
        {
            const float epsilon = 0.01f; // Small tolerance to avoid floating point precision issues

            return CompareEquipment.ItemCompareWindow.ParentWindow.transform.position.x -
                CompareEquipment.halfScaledWindowWidth < CompareEquipment.minValidX - epsilon;
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
            ItemCompareWindow.itemPrice = FindAndGet<TextMeshProUGUI>(transform, "Text (TMP) (6)");

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

        // Gets the data of the equipped item in the same equipment slot of the item we are currently looking at.
        public static ItemSlotData GetEquippedItem(Item.SlotType slotNeeded, bool switchRingWrist)
        {
            foreach (ItemSlotData equipSlot in equipSlots)
            {
                var slotType = equipSlot.ThisSlotType;

                if (switchRingWrist && equipSlot == curItemEquip)
                    continue;

                if (slotType == slotNeeded)
                    return equipSlot;

                if (slotNeeded == Item.SlotType.PrimaryOrSecondary &&
                    (slotType == Item.SlotType.Primary || slotType == Item.SlotType.Secondary) &&
                    equipSlot.MyItem != GameData.PlayerInv.Empty)
                    return equipSlot;
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
        class PlayerItemOnPointerEnterPatch
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

                // Set the item we are looking at to the one we are hovering over.
                CompareEquipment.curItemLook = new ItemSlotData(__instance);

                // Set offset values for the item info window and compare window.
                float mouseY = Input.mousePosition.y;
                int yOffset = mouseY > (float)(Screen.height / 2) ? -225 : 225;
                int xOffset = -250;
                int compareXOffset = xOffset - 347;

                CompareEquipment.DisplayItemInfoWindow(new Vector2((float)xOffset, (float)yOffset), mouseY);

                if (!GameData.InspectSim.InspectWindow.activeSelf)
                    CompareEquipment.DisplayCompareWindow(new Vector2((float)compareXOffset, (float)yOffset), mouseY);
                else
                    CompareEquipment.DisplayCompareWindow(new Vector2((float)compareXOffset, (float)yOffset), mouseY, true);

                // Only included here incase there are any equipment items that have quests attached. Otherwise not needed.
                CompareEquipment.HandleItemQuests();

                // Skips the original function since we handled everything it would normally do.
                return false;
            }
        }

        private static void DisplayItemInfoWindow(Vector2 offset, float mouseY)
        {
            Vector2 windowPosition = CompareEquipment.CalculatePosition(CompareEquipment.curItemLook.transform, offset, mouseY);
            if (windowPosition == Vector2.zero)
                return;

            GameData.ItemInfoWindow.DisplayItem(CompareEquipment.curItemLook.MyItem, windowPosition, CompareEquipment.curItemLook.Quant);
        }

        private static void DisplayCompareWindow(Vector2 offset, float mouseY)
        {
            DisplayCompareWindow(offset, mouseY, false);
        }

        private static void DisplayCompareWindow(Vector2 offset, float mouseY, bool inspectSim)
        {
            // If we aren't inspecting a sim, we need to set equipSlots to the player's equipment slots.
            if (!inspectSim)
                CompareEquipment.equipSlots = GameData.PlayerInv.EquipmentSlots
                    .Select(slot => new ItemSlotData(slot))
                    .ToList();
            else // Else set equipSlots to the sim's equipment slots.
                CompareEquipment.equipSlots = GameData.InspectSim.Who.MyEquipment
                    .Select(slot => new ItemSlotData(slot))
                    .ToList();

            // If not inspecting a sim don't compare the item against itself if we're hovering over an equipped item.
            if (!inspectSim)
                if (CompareEquipment.equipSlots.Any(slot => ReferenceEquals(slot.Source, CompareEquipment.curItemLook.Source)))
                    return;

            // Get the equipped item that matches the required slot of the hovered item.
            CompareEquipment.curItemEquip = CompareEquipment.GetEquippedItem(CompareEquipment.curItemLook.MyItem.RequiredSlot, false);

            // Proceed only if a valid equipped item exists in that slot.
            if (CompareEquipment.curItemEquip != null && CompareEquipment.curItemEquip.MyItem != GameData.PlayerInv.Empty)
            {
                Vector2 compareWindowPosition = CompareEquipment.CalculatePosition(CompareEquipment.curItemLook.transform, offset, mouseY);
                if (compareWindowPosition == Vector2.zero)
                    return;

                CompareEquipment.compareWindowPos = compareWindowPosition;

                // If the compare window is initialized, display the item. If not the coroutine will wait a frame at a time
                // until the window is initialized.
                if (CompareEquipment.ItemCompareWindow != null)
                    CompareEquipment.ItemCompareWindow.DisplayItem(CompareEquipment.curItemEquip.MyItem, CompareEquipment.compareWindowPos, CompareEquipment.curItemEquip.Quant);
                else
                    CoroutineRunner.Run(WaitForCompareWindow(CompareEquipment.curItemEquip.MyItem, CompareEquipment.compareWindowPos, CompareEquipment.curItemEquip.Quant));
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
        class PlayerItemOnPointerExitPatch
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

        // Display the item info window when hovering over an item in the sim inventory.
        [HarmonyPatch(typeof(SimItemDisplay))]
        [HarmonyPatch("OnPointerEnter")]
        class SimItemOnPointerEnterPatch
        {
            static bool Prefix(SimItemDisplay __instance)
            {
                if (__instance.MyItem == GameData.PlayerInv.Empty)
                    return false;

                GameData.PlayerAud.PlayOneShot(GameData.Misc.Click, 0.05f * GameData.SFXVol);

                // Set offset values for the item info window and compare window.
                float mouseY = Input.mousePosition.y;
                int yOffset = mouseY > (float)(Screen.height / 2) ? -225 : 225;
                int xOffset = -250;
                int compareXOffset = xOffset - 347;

                // Set the item we are looking at to the one we are hovering over.
                CompareEquipment.curItemLook = new ItemSlotData(__instance);

                CompareEquipment.DisplayItemInfoWindow(new Vector2((float)xOffset, (float)yOffset), mouseY);
             
                return false;
            }
        }

        // Closes the item info window when the mouse exits the sim inventory item.
        [HarmonyPatch(typeof(SimItemDisplay))]
        [HarmonyPatch("OnPointerExit")]
        class SimItemOnPointerExitPatch
        {
            static void Postfix(SimItemDisplay __instance)
            {
                if (GameData.ItemInfoWindow.isWindowActive())
                    GameData.ItemInfoWindow.CloseItemWindow();
            }
        }

        [HarmonyPatch(typeof(ItemInfoWindow))]
        [HarmonyPatch("DisplayItem")]
        class ItemInfoWindowDisplayItemPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld &&
                        codes[i].operand is FieldInfo field &&
                        field.Name == "ItemInfoWindow" &&
                        field.DeclaringType == typeof(GameData))
                    {
                        codes[i] = new CodeInstruction(OpCodes.Ldarg_0)
                        {
                            labels = codes[i].labels
                        };
                    }
                }

                return codes.AsEnumerable();
            }
        }

        // Wrapper class for item slot data. Allows for uniform access to item data regardless of the source type.
        public class ItemSlotData
        {
            public object Source { get; }
            public Item MyItem { get; }
            public Item.SlotType ThisSlotType { get; }
            public int Quant { get; }
            public Transform transform => 
                (Source as ItemIcon)?.transform ??
                (Source as SimItemDisplay)?.transform;
            public bool VendorSlot => (Source as ItemIcon)?.VendorSlot ?? false;
            public bool LootSlot => (Source as ItemIcon)?.LootSlot ?? false;

            public ItemSlotData(ItemIcon icon)
            {
                Source = icon;
                MyItem = icon.MyItem;
                ThisSlotType = icon.ThisSlotType;
                Quant = icon.Quantity;
            }

            public ItemSlotData(SimInvSlot slot)
            {
                Source = slot;
                MyItem = slot.MyItem;
                ThisSlotType = slot.ThisSlotType;
                Quant = slot.Quant;
            }

            public ItemSlotData(SimItemDisplay slot)
            {
                Source = slot;
                MyItem = slot.MyItem;
                ThisSlotType = slot.Slot;
                Quant = slot.ItemLvl;
            }
        }
    }
}
