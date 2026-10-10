using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SPMod.Tool
{
    public class CustomTextController : MonoBehaviour
    {
        public TextMeshProUGUI? CustomText;
        public TextMeshProUGUI? CustomText3;
        public PlayerAvatar? Player;
        private Camera? Camera;
        void Start()
        {
            Camera = Camera.main;
        }
        void Update()
        {
            if (Player != null && Camera != null)
            {
                try
                {
                    Vector3 screenPos = Camera.WorldToScreenPoint(Player.transform.position + new Vector3(1.5f, -0.55f, 0));
                    if (CustomText != null)
                    {
                        CustomText.rectTransform.position = screenPos;
                        string text = "";

                        if (StarSephiriaMod.Otter3_AddCrit.TryGetValue(Player, out Timer time2))
                        {
                            text += string.Format(Tools.GetLocalization("UI_Buff_CritRateBonus"), time2.time * (1f - time2.Ratio));
                        }
                        if (StarSephiriaMod.BrownRabbit2Hit.TryGetValue(Player.Name, out Timer time))
                        {
                            text += string.Format(Tools.GetLocalization("UI_Buff_PassiveCooldown"), time.time * (1f - time.Ratio));
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.WinterSmithsDraftblade, WeaponIDs.SilenceofEverfrost, WeaponIDs.FragmentofNastrond)
                            && WeaponPatch.DraftbladeAttack.TryGetValue(Player, out Timer tim2))
                        {
                            text += string.Format(Tools.GetLocalization("UI_Buff_DraftbladeEffect"), tim2.time * (1f - tim2.Ratio));
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.SilenceofEverfrost))
                        {
                            if (WeaponPatch.SilenceofEverfrostAttack.TryGetValue(Player, out int Stack2))
                                text += string.Format(Tools.GetLocalization("UI_Buff_EverfrostSpecialAttack"), Stack2);
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.Minimalism, WeaponIDs.TheSecond, WeaponIDs.BlackIronHalberd))
                        {
                            if (WeaponPatch.MinimalismDashAttack.TryGetValue(Player.Name, out int Stack3))
                                text += string.Format(Tools.GetLocalization("UI_Buff_MinimalismStack"), Stack3);
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.Heidi) && WeaponPatch.HeidiDashAttack.TryGetValue(Player.Name, out int HeidiDash))
                        {
                            text += string.Format(Tools.GetLocalization("UI_Buff_HeidiResidualHeat"), HeidiDash);
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.RuadhsleaSpear))
                        {
                            if (WeaponPatch.RuadhsleaSpearTime.TryGetValue(Player.Name, out Timer Buff) &&
                                WeaponPatch.RuadhsleaSpearBuff.TryGetValue(Player.Name, out int BuffStack) && BuffStack > 0)
                            {
                                text += string.Format(Tools.GetLocalization("UI_Buff_RuadhsleaDamageStack"), BuffStack);
                                text += string.Format(Tools.GetLocalization("UI_Buff_RuadhsleaDamageTime"), (1f - Buff.Ratio) * Buff.time);
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.YuisShortDagger))
                        {
                            if (WeaponPatch.YuisShortDaggerMagic.TryGetValue(Player.Name, out int Value))
                            {
                                text += string.Format(Tools.GetLocalization("UI_Buff_YuisShortDaggerMagic"), Value);
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.FiaolasJudgmentStaff))
                        {
                            if (WeaponPatch.FiaolasJudgmentStaffBuff.TryGetValue(Player, out Timer Buff) && Buff.Ratio < 1f)
                            {
                                text += string.Format(Tools.GetLocalization("UI_Buff_FiaolasJudgmentStaff"), (1f - Buff.Ratio) * Buff.time);
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.TomeOfMimicry))
                        {
                            if (WeaponPatch.TomeOfMimicryEffectTime.TryGetValue(Player.Name, out Timer Buff))
                            {
                                text += string.Format(Tools.GetLocalization("UI_Buff_PassiveCooldown"), (1f - Buff.Ratio) * Buff.time);
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.FrozenSmelt))
                        {
                            if (WeaponPatch.FrozenSmeltEffect.TryGetValue(Player, out int Buff))
                            {
                                text += string.Format(Tools.GetLocalization("UI_Buff_FrozenSmelt"), Buff);
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.LightningsWrath))
                        {
                            if (WeaponPatch.LightningsWrathEffect.TryGetValue(Player, out int Buff))
                            {
                                text += string.Format(Tools.GetLocalization("UI_Buff_LightningsWrath"), Buff);
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.GlacialBlade))
                        {
                            if (WeaponPatch.GlacialBladeEffect.TryGetValue(Player, out Timer Buff) && Buff.Ratio < 1f)
                            {
                                text += string.Format(Tools.GetLocalization("UI_Buff_GlacialBlade"), (1f - Buff.Ratio) * Buff.time);
                            }
                        }
                        if (Player.IsWeaponUseNet(WeaponIDs.ScaleOfTheAstralSea))
                        {
                            int Value = Player.GetCustomStatUnsafe(TextFile.ScaleOfTheAstralSeaEffect);
                            if (Value > 0) text += string.Format(Tools.GetLocalization("UI_Buff_ScaleOfTheAstralSea"), Value);
                        }

                        CustomText.text = text;
                        CustomText.enabled = !string.IsNullOrEmpty(text);
                    }
                    if (CustomText3 != null)
                    {
                        if (StarSephiriaPatches.DisplayCustomStats && Player)
                        {
                            string AllCustomStats = "";
                            AllCustomStats += $"玩家: {Player.Name} (按小键盘↑↓←→控制位置)";
                            foreach (KeyValuePair<string, int> Value in Player.customStats)
                            {
                                if (Value.Value == 0 && Player.GetCustomStatUnsafe(Value.Key) == 0) continue;

                                if (StarSephiriaMod._displayNames.TryGetValue(Value.Key, out string value))
                                {
                                    int OrigValue = Value.Value;
                                    int NewValue = Player.GetCustomStatUnsafe(Value.Key);
                                    if (OrigValue == NewValue)
                                        AllCustomStats += $"\n属性—{value} 数值：{Player.GetCustomStatUnsafe(Value.Key)}";
                                    else
                                        AllCustomStats += $"\n属性—{value} 原始值：{Value.Value} 最终值：{Player.GetCustomStatUnsafe(Value.Key)}";
                                }
                                else if (!No.Contains(Value.Key))
                                {
                                    No.Add(Value.Key);
                                    StarSephiriaMod.LogError($"{Value.Key} 不在数据列表中");
                                }
                                else
                                {
                                    int OrigValue = Value.Value;
                                    int NewValue = Player.GetCustomStatUnsafe(Value.Key);
                                    if (OrigValue == NewValue)
                                        AllCustomStats += $"\n属性—{Value.Key} 数值：{Player.GetCustomStatUnsafe(Value.Key)}";
                                    else
                                        AllCustomStats += $"\n属性—{Value.Key} 原始值：{Value.Value} 最终值：{Player.GetCustomStatUnsafe(Value.Key)}";
                                }
                            }
                            float screenLeftX = Screen.width * 0.35f;
                            float screenCenterY = Screen.height * 0.85f;
                            if (Keyboard.current != null)
                            {
                                if (Keyboard.current.upArrowKey.isPressed)
                                {
                                    YOffset += 3f;
                                }
                                if (Keyboard.current.downArrowKey.isPressed)
                                {
                                    YOffset -= 3f;
                                }
                                if (Keyboard.current.rightArrowKey.isPressed)
                                {
                                    XOffset += 3f;
                                }
                                if (Keyboard.current.leftArrowKey.isPressed)
                                {
                                    XOffset -= 3f;
                                }
                            }
                            CustomText3.rectTransform.position = new Vector3(screenLeftX + XOffset, screenCenterY + YOffset, 0);
                            CustomText3.text = AllCustomStats;
                            CustomText3.enabled = true;
                        }
                        else
                        {
                            XOffset = 0;
                            YOffset = 0;
                            CustomText3.enabled = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    StarSephiriaMod.LogError(ex.Message);
                }
            }
        }
        private float XOffset = 0;
        private float YOffset = 0;
        private readonly List<string> No = new List<string>();
    }
}