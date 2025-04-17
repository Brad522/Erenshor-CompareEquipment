using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Erenshor_CompareEquipment
{
    // Token: 0x02000100 RID: 256
    public class ItemCompareWindow : MonoBehaviour
    {
        // Token: 0x060005FA RID: 1530 RVA: 0x00062F6F File Offset: 0x0006116F
        public void Awake()
        {
            CompareEquipment.ItemCompareWindow = this;
        }

        // Token: 0x060005FB RID: 1531 RVA: 0x00062F78 File Offset: 0x00061178
        public void DisplayItem(Item item, Vector2 slotLoc, int _quantity)
        {
            int num = 1;
            if (!this.ParentWindow.activeSelf)
            {
                if (item.TeachSpell == null && item.TeachSkill == null)
                {
                    this.ReqLvl.SetActive(false);
                }
                this.ParentWindow.SetActive(true);
                this.Usable.text = "";
                if (item.Classes.Count > 0)
                {
                    if (item.Classes.Contains(GameData.ClassDB.Arcanist))
                    {
                        TextMeshProUGUI usable = this.Usable;
                        usable.text += " Arcanist ";
                    }
                    if (item.Classes.Contains(GameData.ClassDB.Duelist))
                    {
                        TextMeshProUGUI usable2 = this.Usable;
                        usable2.text += " Duelist ";
                    }
                    if (item.Classes.Contains(GameData.ClassDB.Druid))
                    {
                        TextMeshProUGUI usable3 = this.Usable;
                        usable3.text += " Druid ";
                    }
                    if (item.Classes.Contains(GameData.ClassDB.Warrior))
                    {
                        TextMeshProUGUI usable4 = this.Usable;
                        usable4.text += " Paladin ";
                    }
                }
                else
                {
                    this.Usable.text = "";
                }
                if (slotLoc.y > 0f)
                {
                    num = -1;
                }
                this.ItemIcon.sprite = item.ItemIcon;
                this.ItemName.text = item.ItemName;
                if (!item.Template)
                {
                    this.Lore.verticalAlignment = VerticalAlignmentOptions.Middle;
                    this.Lore.text = item.Lore.ToString();
                }
                else
                {
                    this.Lore.verticalAlignment = VerticalAlignmentOptions.Middle;
                    this.Lore.text = "Ingredients:\n\n";
                    foreach (Item item2 in item.TemplateIngredients)
                    {
                        TextMeshProUGUI lore = this.Lore;
                        lore.text = lore.text + item2.ItemName + "\n";
                    }
                    TextMeshProUGUI lore2 = this.Lore;
                    lore2.text += "\n<color=grey>Note: Ingredients MUST be exact quantities\n\nUse CTRL + CLICK to separate stacks.</color>";
                }
                this.ParentWindow.transform.position = slotLoc + new Vector2(-200f, (float)(100 * num));
                if (item.RequiredSlot == Item.SlotType.General)
                {
                    this.ItemName.color = this.NormalText;
                }
                if (item.Aura)
                {
                    this.ItemName.color = this.NormalText;
                }
                if (item.Aura == null && item.TeachSpell == null && item.TeachSkill == null && item.RequiredSlot != Item.SlotType.General)
                {
                    if (_quantity <= 1)
                    {
                        this.ItemName.color = this.NormalText;
                    }
                    if (_quantity == 2)
                    {
                        this.ItemName.color = this.BlessedText;
                    }
                    if (_quantity == 3)
                    {
                        this.ItemName.color = this.GodlyText;
                    }
                    this.Lore.verticalAlignment = VerticalAlignmentOptions.Bottom;
                    this.ReqLvl.SetActive(false);
                    CompareEquipment.ItemCompareWindow.StatTextParent.SetActive(true);
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(false);
                    if (item.WeaponDmg == 0 && item.WeaponDly == 0f)
                    {
                        this.DMGtxt.gameObject.SetActive(false);
                        this.DelTXT.gameObject.SetActive(false);
                        this.DMGNum.gameObject.SetActive(false);
                        this.DelNum.gameObject.SetActive(false);
                    }
                    else
                    {
                        this.DMGtxt.gameObject.SetActive(true);
                        this.DelTXT.gameObject.SetActive(true);
                        this.DMGNum.gameObject.SetActive(true);
                        this.DelNum.gameObject.SetActive(true);
                    }
                    this.Str.text = item.CalcStat(item.Str, _quantity).ToString();
                    this.End.text = item.CalcStat(item.End, _quantity).ToString();
                    this.Dex.text = item.CalcStat(item.Dex, _quantity).ToString();
                    this.Agi.text = item.CalcStat(item.Agi, _quantity).ToString();
                    this.Int.text = item.CalcStat(item.Int, _quantity).ToString();
                    this.Wis.text = item.CalcStat(item.Wis, _quantity).ToString();
                    this.Cha.text = item.CalcStat(item.Cha, _quantity).ToString();
                    this.Res.text = item.CalcRes(item.Res, _quantity).ToString();
                    this.MR.text = item.CalcStat(item.MR, _quantity).ToString();
                    this.PR.text = item.CalcStat(item.PR, _quantity).ToString();
                    this.VR.text = item.CalcStat(item.VR, _quantity).ToString();
                    this.ER.text = item.CalcStat(item.ER, _quantity).ToString();
                    this.AC.text = item.CalcACHPMC(item.AC, _quantity).ToString();
                    this.HP.text = item.CalcACHPMC(item.HP, _quantity).ToString();
                    this.Mana.text = item.CalcACHPMC(item.Mana, _quantity).ToString();
                    this.DMGNum.text = item.CalcDmg(item.WeaponDmg, _quantity).ToString();
                    this.DelNum.text = item.WeaponDly.ToString() + " sec";
                    this.Slot.text = "Slot: " + item.RequiredSlot.ToString();
                    if (item.RequiredSlot == Item.SlotType.PrimaryOrSecondary)
                    {
                        this.Slot.text = "Primary or Secondary";
                    }
                    if (item.ThisWeaponType == Item.WeaponType.TwoHandMelee || item.ThisWeaponType == Item.WeaponType.TwoHandStaff)
                    {
                        TextMeshProUGUI slot = this.Slot;
                        slot.text += " - 2-Handed";
                    }
                    if (item.Relic)
                    {
                        TextMeshProUGUI slot2 = this.Slot;
                        slot2.text += " - Relic Item";
                    }
                }
                else if (item.RequiredSlot == Item.SlotType.General)
                {
                    CompareEquipment.ItemCompareWindow.StatTextParent.SetActive(false);
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().text = "";
                }
                else
                {
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(true);
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().text = "";
                }
                if (item.ItemEffectOnClick != null)
                {
                    this.Lore.verticalAlignment = VerticalAlignmentOptions.Bottom;
                    this.ReqLvl.SetActive(false);
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().text = "";
                    this.ItemEffect.SetActive(true);
                    this.ClickSpell.text = "Activatable: " + item.ItemEffectOnClick.SpellName;
                    this.ClickDesc.text = item.ItemEffectOnClick.SpellDesc;
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(true);
                    if (item.Disposable)
                    {
                        TextMeshProUGUI component = this.OtherTextParent.GetComponent<TextMeshProUGUI>();
                        component.text += "Item Consumed Upon Use.\n\n";
                    }
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().text = "Right click or assign to hotkey to use.";
                }
                else if (item.WornEffect != null)
                {
                    this.Lore.verticalAlignment = VerticalAlignmentOptions.Bottom;
                    this.ReqLvl.SetActive(false);
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().text = "";
                    this.ItemEffect.SetActive(true);
                    this.ClickSpell.text = "Worn Effect: " + item.WornEffect.SpellName;
                    this.ClickDesc.text = item.WornEffect.SpellDesc;
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(true);
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().text = "Effect applied once item is equipped.";
                }
                else if (item.WeaponProcOnHit != null)
                {
                    this.ItemEffect.SetActive(true);
                    if (!item.Shield && (item.RequiredSlot == Item.SlotType.Primary || item.RequiredSlot == Item.SlotType.Secondary || item.RequiredSlot == Item.SlotType.PrimaryOrSecondary))
                    {
                        this.ClickSpell.text = item.WeaponProcChance.ToString() + "% chance on ATTACK: \n" + item.WeaponProcOnHit.SpellName;
                    }
                    if (item.Shield)
                    {
                        this.ClickSpell.text = item.WeaponProcChance.ToString() + "% chance on BASH: \n" + item.WeaponProcOnHit.SpellName;
                    }
                    if (item.RequiredSlot == Item.SlotType.Bracer)
                    {
                        this.ClickSpell.text = item.WeaponProcChance.ToString() + "% chance on CAST: \n" + item.WeaponProcOnHit.SpellName;
                    }
                    this.ClickDesc.text = item.WeaponProcOnHit.SpellDesc;
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(true);
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().text = "";
                }
                else
                {
                    this.ItemEffect.SetActive(false);
                    this.ClickSpell.text = "";
                    this.ClickDesc.text = "";
                }
                if (item.WeaponDmg != 0)
                {
                    float num2 = 1f;
                    if (item.ThisWeaponType == Item.WeaponType.TwoHandMelee || item.ThisWeaponType == Item.WeaponType.TwoHandStaff)
                    {
                        num2 = 1.5f;
                    }
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(true);
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().faceColor = Color.green;
                    float num3 = ((float)(item.WeaponDmg + (_quantity - 1)) * (float)GameData.PlayerStats.Level * 0.8f * 0.9f + ((float)GameData.PlayerStats.dexBonus() / 40f + (float)GameData.PlayerStats.strBonus() / 40f) + 5f + 2f * (float)GameData.PlayerStats.Level) / item.WeaponDly;
                    num3 *= num2;
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().text = "Base DPS: " + Mathf.RoundToInt(num3).ToString();
                }
                else
                {
                    this.OtherTextParent.GetComponent<TextMeshProUGUI>().faceColor = Color.white;
                }
                if (item.TeachSpell != null)
                {
                    this.Lore.verticalAlignment = VerticalAlignmentOptions.Bottom;
                    CompareEquipment.ItemCompareWindow.StatTextParent.SetActive(false);
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(true);
                    Spell teachSpell = item.TeachSpell;
                    CompareEquipment.ItemCompareWindow.ReqLvl.SetActive(true);
                    this.ReqLvl.GetComponent<TextMeshProUGUI>().text = string.Concat(new string[]
                    {
                    "Required Level: ",
                    teachSpell.RequiredLevel.ToString(),
                    "\n\nMana Cost: ",
                    teachSpell.ManaCost.ToString(),
                    "\nSpell Type: ",
                    teachSpell.Type.ToString(),
                    "\n\n",
                    teachSpell.SpellDesc
                    });
                }
                else if (item.Aura != null)
                {
                    CompareEquipment.ItemCompareWindow.StatTextParent.SetActive(false);
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(true);
                    this.Lore.verticalAlignment = VerticalAlignmentOptions.Middle;
                    this.Lore.text = "<color=#16EC00>Aura Item</color>\nAuras effect entire party\nAuras of same type do not stack\n\n<color=#16EC00>" + item.Aura.SpellName + "</color>\n" + item.Aura.SpellDesc;
                }
                if (item.TeachSkill != null)
                {
                    this.Lore.verticalAlignment = VerticalAlignmentOptions.Bottom;
                    CompareEquipment.ItemCompareWindow.StatTextParent.SetActive(false);
                    CompareEquipment.ItemCompareWindow.OtherTextParent.SetActive(true);
                    CompareEquipment.ItemCompareWindow.ReqLvl.SetActive(true);
                    Skill teachSkill = item.TeachSkill;
                    string text = "Required Level: \n";
                    if (teachSkill.DuelistRequiredLevel != 0)
                    {
                        text = text + "Duelist: " + teachSkill.DuelistRequiredLevel.ToString() + "\n";
                    }
                    if (teachSkill.DruidRequiredLevel != 0)
                    {
                        text = text + "Druid: " + teachSkill.DruidRequiredLevel.ToString() + "\n";
                    }
                    if (teachSkill.ArcanistRequiredLevel != 0)
                    {
                        text = text + "Arcanist: " + teachSkill.ArcanistRequiredLevel.ToString() + "\n";
                    }
                    if (teachSkill.PaladinRequiredLevel != 0)
                    {
                        text = text + "Paladin: " + teachSkill.PaladinRequiredLevel.ToString() + "\n";
                    }
                    this.ReqLvl.GetComponent<TextMeshProUGUI>().text = string.Concat(new string[]
                    {
                    text,
                    "\n\nSkill Type: ",
                    teachSkill.TypeOfSkill.ToString(),
                    "\n\n",
                    teachSkill.SkillDesc
                    });
                    if (!teachSkill.SimPlayersAutolearn)
                    {
                        this.Lore.text = "<color=yellow>SimPlayers DO NOT automatically learn this skill!\nHand this book to them to allow them to use it.</color>";
                    }
                }
                if (item.RequiredSlot == Item.SlotType.General && item.ItemEffectOnClick == null)
                {
                    this.StatTextParent.SetActive(false);
                }
                bool template = item.Template;
            }
        }

        // Token: 0x060005FC RID: 1532 RVA: 0x00063D2C File Offset: 0x00061F2C
        public void CloseItemWindow()
        {
            if (this.ParentWindow.activeSelf)
            {
                this.ParentWindow.SetActive(false);
            }
        }

        // Token: 0x060005FD RID: 1533 RVA: 0x00063D47 File Offset: 0x00061F47
        public bool isWindowActive()
        {
            return this.ParentWindow.activeSelf;
        }

        // Token: 0x04000A96 RID: 2710
        public TextMeshProUGUI ItemName;

        // Token: 0x04000A97 RID: 2711
        public TextMeshProUGUI Str;

        // Token: 0x04000A98 RID: 2712
        public TextMeshProUGUI End;

        // Token: 0x04000A99 RID: 2713
        public TextMeshProUGUI Dex;

        // Token: 0x04000A9A RID: 2714
        public TextMeshProUGUI Agi;

        // Token: 0x04000A9B RID: 2715
        public TextMeshProUGUI Int;

        // Token: 0x04000A9C RID: 2716
        public TextMeshProUGUI Wis;

        // Token: 0x04000A9D RID: 2717
        public TextMeshProUGUI Cha;

        // Token: 0x04000A9E RID: 2718
        public TextMeshProUGUI MR;

        // Token: 0x04000A9F RID: 2719
        public TextMeshProUGUI PR;

        // Token: 0x04000AA0 RID: 2720
        public TextMeshProUGUI ER;

        // Token: 0x04000AA1 RID: 2721
        public TextMeshProUGUI VR;

        // Token: 0x04000AA2 RID: 2722
        public TextMeshProUGUI HP;

        // Token: 0x04000AA3 RID: 2723
        public TextMeshProUGUI Mana;

        // Token: 0x04000AA4 RID: 2724
        public TextMeshProUGUI AC;

        // Token: 0x04000AA5 RID: 2725
        public TextMeshProUGUI Lore;

        // Token: 0x04000AA6 RID: 2726
        public TextMeshProUGUI DMGtxt;

        // Token: 0x04000AA7 RID: 2727
        public TextMeshProUGUI DMGNum;

        // Token: 0x04000AA8 RID: 2728
        public TextMeshProUGUI DelTXT;

        // Token: 0x04000AA9 RID: 2729
        public TextMeshProUGUI DelNum;

        // Token: 0x04000AAA RID: 2730
        public TextMeshProUGUI Slot;

        // Token: 0x04000AAB RID: 2731
        public TextMeshProUGUI Usable;

        // Token: 0x04000AAC RID: 2732
        public TextMeshProUGUI Res;

        // Token: 0x04000AAD RID: 2733
        public GameObject ParentWindow;

        // Token: 0x04000AAE RID: 2734
        public Image ItemIcon;

        // Token: 0x04000AAF RID: 2735
        public GameObject StatTextParent;

        // Token: 0x04000AB0 RID: 2736
        public GameObject OtherTextParent;

        // Token: 0x04000AB1 RID: 2737
        public GameObject ItemEffect;

        // Token: 0x04000AB2 RID: 2738
        public TextMeshProUGUI ClickSpell;

        // Token: 0x04000AB3 RID: 2739
        public TextMeshProUGUI ClickDesc;

        // Token: 0x04000AB4 RID: 2740
        public GameObject ReqLvl;

        // Token: 0x04000AB5 RID: 2741
        public Color Normal;

        // Token: 0x04000AB6 RID: 2742
        public Color Blessed;

        // Token: 0x04000AB7 RID: 2743
        public Color Legendary;

        // Token: 0x04000AB8 RID: 2744
        public Color NormalText;

        // Token: 0x04000AB9 RID: 2745
        public Color BlessedText;

        // Token: 0x04000ABA RID: 2746
        public Color GodlyText;

        // Token: 0x04000ABB RID: 2747
        public Image Banner;
    }

}
