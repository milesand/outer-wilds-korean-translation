using HarmonyLib;
using OWML.ModHelper;
using OWML.Common;
using System;
using System.Reflection;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace OWKT
{
    public class OWKT : ModBehaviour
    {
        // singleton OWKT instance.
        public static OWKT Instance;

        public readonly Lazy<AssetBundle> Bundle = new Lazy<AssetBundle>(() =>
        {
            return Instance.ModHelper.Assets.LoadBundle("assets/owkt");
        });

        // NanumBarunGothic Font(not monospace) that has been loaded, static and dynamic.
        public readonly Lazy<Font> NanumBarunGothic = new Lazy<Font>(() =>
        {
            return Instance.Bundle.Value.LoadAsset<Font>("Assets/Fonts/NanumBarunGothic.otf");
        });
        public readonly Lazy<Font> NanumBarunGothicDyn = new Lazy<Font>(() =>
        {
            return Instance.Bundle.Value.LoadAsset<Font>("Assets/Fonts/NanumBarunGothic_Dynamic.otf");
        });

        // D2Coding Font(monospace) that has been loaded, static and dynamic.
        public readonly Lazy<Font> D2Coding = new Lazy<Font>(() =>
        {
            return Instance.Bundle.Value.LoadAsset<Font>("Assets/Fonts/D2Coding.ttf");
        });
        public readonly Lazy<Font> D2CodingDyn = new Lazy<Font>(() =>
        {
            return Instance.Bundle.Value.LoadAsset<Font>("Assets/Fonts/D2Coding_Dynamic.ttf");
        });

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation.SetLanguage))]
        // Hijack SetLanguage to load mod's translation asset instead.
        //
        // This would be a lot cleaner if I could just replace the asset itself, or correctly patch Resources.Load<TextAsset>,
        // But apparently I can do neither due to former functionality being unavailable in OWML, and the latter because it seems
        // pretty much impossible to just select Resources.Load<TextAsset> without selecting Resources.Load or
        // every instance of Resources.Load<T> and what not.
        //
        // Also, this works, so I'm just leaving this be.
        private static bool SetLanguage(
            TextTranslation.Language lang,
            TextTranslation __instance,
            ref TextTranslation.Language ___m_language,
            ref TextTranslation.TranslationTable ___m_table)
        {
            if (lang != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            ___m_language = lang;
            ___m_table = null;
            TextAsset textAsset = OWKT.Instance.Bundle.Value.LoadAsset<TextAsset>("Assets/Translation.txt");
            if (null == textAsset)
            {
                Debug.LogError("Unable to load text translation file for language " + TextTranslation.s_langFolder[(int)___m_language]);
                return false;
            }
            string xml = OWUtilities.RemoveByteOrderMark(textAsset);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            XmlNode xmlNode = xmlDocument.SelectSingleNode("TranslationTable_XML");
            XmlNodeList xmlNodeList = xmlNode.SelectNodes("entry");
            TextTranslation.TranslationTable_XML translationTable_XML = new TextTranslation.TranslationTable_XML();
            foreach (object obj in xmlNodeList)
            {
                XmlNode xmlNode2 = (XmlNode)obj;
                translationTable_XML.table.Add(new TextTranslation.TranslationTableEntry(xmlNode2.SelectSingleNode("key").InnerText, xmlNode2.SelectSingleNode("value").InnerText));
            }
            foreach (object obj2 in xmlNode.SelectSingleNode("table_shipLog").SelectNodes("TranslationTableEntry"))
            {
                XmlNode xmlNode3 = (XmlNode)obj2;
                translationTable_XML.table_shipLog.Add(new TextTranslation.TranslationTableEntry(xmlNode3.SelectSingleNode("key").InnerText, xmlNode3.SelectSingleNode("value").InnerText));
            }
            foreach (object obj3 in xmlNode.SelectSingleNode("table_ui").SelectNodes("TranslationTableEntryUI"))
            {
                XmlNode xmlNode4 = (XmlNode)obj3;
                translationTable_XML.table_ui.Add(new TextTranslation.TranslationTableEntryUI(int.Parse(xmlNode4.SelectSingleNode("key").InnerText), xmlNode4.SelectSingleNode("value").InnerText));
            }
            ___m_table = new TextTranslation.TranslationTable(translationTable_XML);
            Resources.UnloadAsset(textAsset);
            var onLanguageChangedDelegate = (MulticastDelegate)__instance.GetType().GetField("OnLanguageChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            if (onLanguageChangedDelegate != null)
            {
                onLanguageChangedDelegate.DynamicInvoke();
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation.ProcessCustomWhitespace))]
        public static bool Translate(
            string inputString,
            TextTranslation __instance,
            ref string __result,
            bool ___m_ignoreManualLineBreaks)
        {
            if (__instance.GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            string text;
            if (__instance.m_ignoreManualLineBreaks)
            {
                text = inputString.Replace("\\\\N\\\\n", " ");
                text = text.Replace("\\\\n\\\\N", " ");
                text = text.Replace("\\\\n", " ");
                text = text.Replace("\\\\N", " ");
            }
            else if (PlayerData.IsUILargeTextSize())
            {
                text = inputString.Replace("\\\\N\\\\n", "\n");
                text = text.Replace("\\\\n\\\\N", "\n");
                text = text.Replace("\\\\N", "\n");
                text = text.Replace("\\\\n", " ");
            }
            else
            {
                text = inputString.Replace("\\\\N\\\\n", "\n");
                text = text.Replace("\\\\n\\\\N", "\n");
                text = text.Replace("\\\\n", "\n");
                text = text.Replace("\\\\N", " ");
            }
            // Base game replaces all spaces with two spaces here if korean. Why?
            // Used to be U+3000 in previous versions.
            __result = text;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation.GetFont))]
        // Korean font included in base game lacks some glyphs.
        // This patch hijacks font loading to load a font with all required glyphs instead.
        public static bool GetFont(
            bool dynamicFont,
            ref Font __result)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            if (dynamicFont)
            {
                __result = OWKT.Instance.NanumBarunGothicDyn.Value;
            }
            else
            {
                __result = OWKT.Instance.NanumBarunGothic.Value;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation.GetGameOverFont))]
        public static bool GetGameOverFont(ref Font __result)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            __result = OWKT.Instance.NanumBarunGothicDyn.Value;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NomaiTranslatorProp), nameof(NomaiTranslatorProp.InitializeFont))]
        // Make Translator use D2Coding instead of NanumBarunGothic.
        // While not strictly necessary, this fixes an issue with a certain easter egg that depends on translator font
        // being monospace to be correctly displayed.
        public static bool InitTranslatorFont(
            ref Font ____fontInUse,
            ref Font ____dynamicFontInUse,
            ref float ____fontSpacingInUse,
            ref Text ____textField)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            ____fontInUse = OWKT.Instance.D2Coding.Value;
            ____dynamicFontInUse = OWKT.Instance.D2CodingDyn.Value;
            ____fontSpacingInUse = TextTranslation.GetDefaultFontSpacing();
            ____textField.font = ____fontInUse;
            ____textField.lineSpacing = ____fontSpacingInUse;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStyleManager), nameof(UIStyleManager.GetShipLogFont))]
        // Make Ship log use D2Coding font.
        public static bool GetShipLogFont(ref Font __result)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            __result = OWKT.Instance.D2Coding.Value;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStyleManager), nameof(UIStyleManager.GetShipLogCardFont))]
        // Make Ship log card use D2Coding font.
        public static bool GetShipLogCardFont(ref Font __result)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            __result = OWKT.Instance.D2Coding.Value;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDCanvas), nameof(HUDCanvas.Start))]
        // Some notification include extra data, and the way base game formats it doesn't look quite nice in Korean.
        // This patch fixes that.
        public static void FormatNotif(
            ref NotificationData ____lowFuelNotif,
            ref NotificationData ____critOxygenNotif,
            ref NotificationData ____lowOxygenNotif,
            PlayerResources ____playerResources)

        {
            if (TextTranslation.Get().GetLanguage() == TextTranslation.Language.KOREAN)
            {
                ____lowFuelNotif = new NotificationData(NotificationTarget.Player, UITextLibrary.GetString(UITextType.NotificationFuelLow).Replace("<Fuel>", ____playerResources.GetLowFuel().ToString()), 3f, true);
                ____critOxygenNotif = new NotificationData(NotificationTarget.Player, UITextLibrary.GetString(UITextType.NotificationO2Sec).Replace("<Sec>", Mathf.RoundToInt(____playerResources.GetCriticalOxygenInSeconds()).ToString()), 3f, true);
                ____lowOxygenNotif = new NotificationData(NotificationTarget.Player, UITextLibrary.GetString(UITextType.NotificationO2Min).Replace("<Min>", Mathf.RoundToInt(____playerResources.GetLowOxygenInSeconds() / 60f).ToString()), 3f, true);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDCanvas), nameof(HUDCanvas.DoResizeAction))]
        public static void FormatNotif2(
            ref NotificationData ____lowFuelNotif,
            ref NotificationData ____critOxygenNotif,
            ref NotificationData ____lowOxygenNotif,
            PlayerResources ____playerResources)
        {
            FormatNotif(ref ____lowFuelNotif, ref ____critOxygenNotif, ref ____lowOxygenNotif, ____playerResources);
        }

        // Check whether the last letter of given word is Korean, and if it is, whether it has a final consonant(batchim; 받침).
        // returns true if it ends with a Korean letter with final consonant, false otherwise.
        public static bool EndsWithFinalConsonant(string word)
        {
            if (word.Length == 0) { return false; }
            char codepoint = word[word.Length - 1];
            const int FINAL_CONSONANT_CYCLE_LEN = 28;
            return '가' <= codepoint && codepoint <= '힣' // Korean range check
                && ((int)codepoint % FINAL_CONSONANT_CYCLE_LEN != (int)'가' % FINAL_CONSONANT_CYCLE_LEN); // Final consonant check

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemTool), nameof(ItemTool.UpdateState))]
        // Again, this patch changes base game prompt formatting for Korean so that it looks nice.
        private static bool ItemToolUpdateState(
            ItemTool.PromptState newState,
            string itemName,
            ref ItemTool.PromptState ____promptState,
            ref ScreenPrompt ____messageOnlyPrompt,
            ref ScreenPrompt ____cancelButtonPrompt,
            ref ScreenPrompt ____interactButtonPrompt)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            if (____promptState == newState)
            {
                return false;
            }
            ____promptState = newState;
            string text = string.Empty;
            string text2 = string.Empty;
            string empty = string.Empty;
            switch (____promptState)
            {
                case ItemTool.PromptState.PICK_UP:
                    text2 = itemName + UITextLibrary.GetString(UITextType.ItemPickUpPrompt);
                    break;
                case ItemTool.PromptState.DROP:
                    text2 = itemName + UITextLibrary.GetString(UITextType.ItemDropPrompt);
                    break;
                case ItemTool.PromptState.UNSOCKET:
                    text2 = itemName + UITextLibrary.GetString(UITextType.ItemRemovePrompt);
                    break;
                case ItemTool.PromptState.SOCKET:
                    text2 = itemName + UITextLibrary.GetString(UITextType.ItemInsertPrompt);
                    break;
                case ItemTool.PromptState.WRONG_SOCKET_TYPE:
                    string iga = EndsWithFinalConsonant(itemName) ? "이" : "가";
                    text = itemName + UITextLibrary.GetString(UITextType.ItemNotFitPrompt).Replace("<I/Ga>", iga);
                    break;
                case ItemTool.PromptState.CANNOT_HOLD_MORE:
                    string ulrul = EndsWithFinalConsonant(itemName) ? "을" : "를";
                    text = UITextLibrary.GetString(UITextType.ItemAlreadyHoldingPrompt).Replace("<Item>", itemName).Replace("<Ul/Rul>", ulrul);
                    break;
                case ItemTool.PromptState.GIVE:
                    text2 = itemName + " " + UITextLibrary.GetString(UITextType.GivePrompt);
                    break;
                case ItemTool.PromptState.TAKE:
                    text2 = itemName + " " + UITextLibrary.GetString(UITextType.TakePrompt);
                    break;
                default:
                    break;
            }
            if (text == string.Empty)
            {
                ____messageOnlyPrompt.SetVisibility(false);
            }
            else
            {
                ____messageOnlyPrompt.SetVisibility(true);
                ____messageOnlyPrompt.SetText(text);
            }
            if (empty == string.Empty)
            {
                ____cancelButtonPrompt.SetVisibility(false);
            }
            else
            {
                ____cancelButtonPrompt.SetVisibility(true);
                ____cancelButtonPrompt.SetText(empty);
            }
            if (text2 == string.Empty)
            {
                ____interactButtonPrompt.SetVisibility(false);
            }
            else
            {
                ____interactButtonPrompt.SetVisibility(true);
                ____interactButtonPrompt.SetText(text2);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SingleInteractionVolume), nameof(SingleInteractionVolume.SetPromptText), new Type[] { typeof(UITextType), typeof(string) })]
        // Yet another text format fix, this time for talking with people.
        private static bool SetPromptTextCharacter(
            UITextType promptID,
            string _characterName,
            ref ScreenPrompt ____screenPrompt,
            ref ScreenPrompt ____noCommandIconPrompt)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }
            string wagwa = EndsWithFinalConsonant(_characterName) ? "과" : "와";
            string prompt = UITextLibrary.GetString(promptID).Replace("<Wa/Gwa>", wagwa);
            ____screenPrompt.SetText("<CMD> " + _characterName + prompt);
            ____noCommandIconPrompt.SetText(_characterName + prompt);
            return false;
        }
    }
}
