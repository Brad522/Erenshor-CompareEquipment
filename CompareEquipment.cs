using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Erenshor_CompareEquipment
{
    [BepInPlugin(ModGUID, ModDescription, ModVersion)]
    public class CompareEquipment : BaseUnityPlugin
    {
        internal const string ModName = "CompareEquipment";
        internal const string ModVersion = "1.0.0";
        internal const string ModDescription = "Compare Equipment";
        internal const string Author = "Brad522";
        private const string ModGUID = Author + "." + ModName;

        private readonly Harmony harmony = new Harmony(ModGUID);

        internal static ManualLogSource Log;

        private GameObject gameUI;
        private GameObject playerInv;
        private ItemIcon dragIcon;
        private bool uiInitialized;

        public static GameObject clonedItemInfo;
        public static ItemCompareWindow ItemCompareWindow;
        public static ItemIcon curItemLook;
        public static ItemIcon curItemEquip;
        public static Vector3 compareWindowPos;

        //For separating the new UI components into their own GameObject - Here just in case using existing UI causes issues
        //private static GameObject cwUI;

        public void Awake()
        {
            harmony.PatchAll();
            uiInitialized = false;

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
                dragIcon != null;
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
                if(originalItemInfo != null)
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

            // Final check to confirm all required UI references are set.
            uiInitialized = CheckUIElementsInit();
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

                // Grabs the y offset we will be using for our window positions based on the position of the mouse cursor.
                int offset = Input.mousePosition.y > (float)(Screen.height / 2) ? -200 : 200;

                // Store the item we are looking at for use later if we need to switch between the Primary and Secondary slots.
                CompareEquipment.curItemLook = __instance;

                DisplayItemInfoWindow(offset);
                DisplayCompareWindow(offset);

                // Only included here incase there are any equipment items that have quests attached. Otherwise not needed.
                HandleItemQuests();

                // Skips the original function since we handled everything it would normally do.
                return false;
            }
        }

        private static void DisplayItemInfoWindow(int offset)
        {
            // Checking whether the item we are displaying is in your inventory or in a vendor/loot window.
            if (CompareEquipment.curItemLook.ThisSlotType == Item.SlotType.General && !CompareEquipment.curItemLook.VendorSlot && !CompareEquipment.curItemLook.LootSlot)
                GameData.ItemInfoWindow.DisplayItem(CompareEquipment.curItemLook.MyItem, CompareEquipment.curItemLook.transform.position + new Vector3(-200f, (float)offset, 0f), CompareEquipment.curItemLook.Quantity);
            else
                GameData.ItemInfoWindow.DisplayItem(CompareEquipment.curItemLook.MyItem, CompareEquipment.curItemLook.transform.parent.position + new Vector3(-200f, (float)offset, 0f), CompareEquipment.curItemLook.Quantity);
        }

        private static void DisplayCompareWindow(int offset)
        {
            // Don't compare against itself if we're hovering over an equipped item.
            if (GameData.PlayerInv.EquipmentSlots.Contains(CompareEquipment.curItemLook))
                return;

            // Get the equipped item that matches the required slot of the hovered item.
            CompareEquipment.curItemEquip = GetEquippedItem(CompareEquipment.curItemLook.MyItem.RequiredSlot);

            // Proceed only if a valid equipped item exists in that slot.
            if (CompareEquipment.curItemEquip != null && CompareEquipment.curItemEquip.MyItem != GameData.PlayerInv.Empty)
            {
                // Determine the position for the compare window based on the item's origin (Player Inventory vs Vender/Loot)
                // Store the result for use later for Primary/Secondary switching.
                if (CompareEquipment.curItemLook.ThisSlotType != Item.SlotType.General && !CompareEquipment.curItemLook.VendorSlot && !CompareEquipment.curItemLook.LootSlot)
                    CompareEquipment.compareWindowPos = CompareEquipment.curItemLook.transform.position + new Vector3(-666f, (float)offset, 0f);
                else
                    CompareEquipment.compareWindowPos = CompareEquipment.curItemLook.transform.parent.position + new Vector3(-666f, (float)offset, 0f);

                // If the compare window is initialized, display the item. If not the coroutine will wait a frame at a time
                // until the window is initialized.
                if (ItemCompareWindow != null)
                    ItemCompareWindow.DisplayItem(CompareEquipment.curItemEquip.MyItem, CompareEquipment.compareWindowPos, CompareEquipment.curItemEquip.Quantity);
                else
                    CoroutineRunner.Run(WaitForCompareWindow(CompareEquipment.curItemEquip.MyItem, CompareEquipment.compareWindowPos, CompareEquipment.curItemEquip.Quantity));
            }
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

            ItemCompareWindow.DisplayItem(item, position, quantity);
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

                if (ItemCompareWindow.isWindowActive())
                    ItemCompareWindow.CloseItemWindow();

                return false;
            }
        }
    }
}
