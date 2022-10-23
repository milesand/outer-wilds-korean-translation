using HarmonyLib;
using OWML.ModHelper;
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
        [HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation._Translate))]
        public static bool Translate(
            string key,
            ref string __result,
            TextTranslation.Language ___m_language,
            TextTranslation.TranslationTable ___m_table)
        {
            if (___m_language != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            if (___m_table == null)
            {
                Debug.LogError("TextTranslation not initialized");
                __result = key;
                return false;
            }
            string text = ___m_table.Get(key);
            if (text == null)
            {
                Debug.LogError("String \"" + key + "\" not found in table for language " + TextTranslation.s_langFolder[(int)___m_language]);
                __result = key;
                return false;
            }
            text = text.Replace("\\\\n", "\n");
            // In the base game, OW replaces spaces(U+0020) in text to U+3000 here when the language is set to Korean,
            // which doesn't make much sense, given that basic space character is prevalent in Korean, and U+3000 is not.
            // Also U+3000 is supposed look like a full-width space, but that's not how it looks in-game, it just looks like a regular space.
            // Also if it actually looked like a full-width space in-game, it would look *ugly*.
            // Therefore, we're just skipping that conversion entirely.
            __result = text;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation._Translate_ShipLog))]
        public static bool Translate_ShipLog(
            string key,
            ref string __result,
            TextTranslation.Language ___m_language,
            TextTranslation.TranslationTable ___m_table)
        {
            if (___m_language != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            if (___m_table == null)
            {
                Debug.LogError("TextTranslation not initialized");
                __result = key;
                return false;
            }
            string text = ___m_table.GetShipLog(key);
            if (text == null)
            {
                Debug.LogError("String \"" + key + "\" not found in ShipLog table for language " + TextTranslation.s_langFolder[(int)___m_language]);
                __result = key;
                return false;
            }
            text = text.Replace("\\\\n", "\n");
            // Something about space and U+3000. See comment in Translate method.
            __result = text;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation._Translate_UI))]
        public static bool Translate_UI(
            int key,
            ref string __result,
            TextTranslation.Language ___m_language,
            TextTranslation.TranslationTable ___m_table)
        {
            if (___m_language != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            if (___m_table == null)
            {
                Debug.LogError("TextTranslation not initialized");
                __result = key.ToString();
                return false;
            }
            string text = ___m_table.Get_UI(key);
            if (text == null)
            {
                Debug.LogWarning(string.Concat(new object[]
                {
                    "UI String #",
                    key,
                    " not found in table for language ",
                    TextTranslation.s_langFolder[(int)___m_language]
                }));
                __result = key.ToString();
                return false;
            }
            text = text.Replace("\\\\n", "\n");
            // Something about space and U+3000. See comment in Translate method.
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
        [HarmonyPatch(typeof(GameOverController), nameof(GameOverController.SetupGameOverScreen))]
        // In base game there's a bug where game uses default font for gameover text.
        // Latin messages are displayed just fine, but anything else that requires glyphs not included in said font
        // will display broken messages. This is also true for Korean, so this fixes that.
        public static void SetGameOverScreenFont(ref Text ____deathText)
        {
            ____deathText.font = TextTranslation.GetFont(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDCanvas), nameof(HUDCanvas.Start))]
        // Some notification include extra data, and the way base game formats it doesn't look quite nice in Korean.
        // This patch fixex that.
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
