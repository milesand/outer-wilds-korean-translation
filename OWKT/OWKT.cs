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
        static OWKT self;

        private AssetBundle bundle;
        public AssetBundle Bundle
        {
            get
            {
                if (bundle == null)
                {
                    bundle = ModHelper.Assets.LoadBundle("Assets/owkt");
                }
                return bundle;
            }
        }

        private Font nanumBarunGothic;
        public Font NanumBarunGothic
        {
            get
            {
                if (nanumBarunGothic == null)
                {
                    nanumBarunGothic = Bundle.LoadAsset<Font>("Assets/Fonts/NanumBarunGothic.otf");
                }
                return nanumBarunGothic;
            }
        }

        private Font nanumBarunGothicDyn;
        public Font NanumBarunGothicDyn
        {
            get
            {
                if (nanumBarunGothicDyn == null)
                {
                    nanumBarunGothicDyn = Bundle.LoadAsset<Font>("Assets/Fonts/NanumBarunGothic_Dynamic.otf");
                }
                return nanumBarunGothicDyn;
            }
        }

        private Font d2Coding;
        public Font D2Coding
        {
            get
            {
                if (d2Coding == null)
                {
                    d2Coding = Bundle.LoadAsset<Font>("Assets/Fonts/D2Coding.ttf");
                }
                return d2Coding;
            }
        }

        private Font d2CodingDyn;
        public Font D2CodingDyn
        {
            get
            {
                if (d2CodingDyn == null)
                {
                    d2CodingDyn = Bundle.LoadAsset<Font>("Assets/Fonts/D2Coding_Dynamic.ttf");
                }
                return d2CodingDyn;
            }
        }

        private void Start()
        {
            self = this;
            ModHelper.HarmonyHelper.AddPrefix<TextTranslation>("SetLanguage", typeof(OWKT), nameof(OWKT.SetLanguage));
            ModHelper.HarmonyHelper.AddPrefix<TextTranslation>("_Translate", typeof(OWKT), nameof(OWKT._Translate));
            ModHelper.HarmonyHelper.AddPrefix<TextTranslation>("_Translate_ShipLog", typeof(OWKT), nameof(OWKT._Translate_ShipLog));
            ModHelper.HarmonyHelper.AddPrefix<TextTranslation>("_Translate_UI", typeof(OWKT), nameof(OWKT._Translate_UI));
            ModHelper.HarmonyHelper.AddPrefix<TextTranslation>("GetFont", typeof(OWKT), nameof(OWKT.GetFont));
            ModHelper.HarmonyHelper.AddPrefix<NomaiTranslatorProp>("InitializeFont", typeof(OWKT), nameof(OWKT.InitTranslatorFont));
            ModHelper.HarmonyHelper.AddPrefix<UIStyleManager>("GetShipLogFont", typeof(OWKT), nameof(OWKT.GetShipLogFont));
            ModHelper.HarmonyHelper.AddPostfix<GameOverController>("SetupGameOverScreen", typeof(OWKT), nameof(OWKT.SetGameOverScreenFont));
            ModHelper.HarmonyHelper.AddPostfix<HUDCanvas>("Start", typeof(OWKT), nameof(OWKT.FormatNotif));
            ModHelper.HarmonyHelper.AddPrefix<ItemTool>("UpdateState", typeof(OWKT), nameof(OWKT.ItemToolUpdateState));
            MethodInfo setPromptText = typeof(SingleInteractionVolume).GetMethod("SetPromptText", new Type[] { typeof(UITextType), typeof(string) });
            ModHelper.HarmonyHelper.AddPrefix(setPromptText, typeof(OWKT), nameof(OWKT.SetPromptTextCharacter));
        }

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
            TextAsset textAsset = self.Bundle.LoadAsset<TextAsset>("Assets/Translation.txt");
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

        private static bool _Translate(
            string key,
            TextTranslation __instance,
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
            __result = text;
            return false;
        }

        private static bool _Translate_ShipLog(
            string key,
            TextTranslation __instance,
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
            __result = text;
            return false;
        }

        private static bool _Translate_UI(
            int key,
            TextTranslation __instance,
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
            __result = text;
            return false;
        }

        private static bool GetFont(
            bool dynamicFont,
            ref Font __result)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            if (dynamicFont)
            {
                __result = OWKT.self.NanumBarunGothicDyn;
            }
            else
            {
                __result = OWKT.self.NanumBarunGothic;
            }
            return false;
        }

        private static bool InitTranslatorFont(
            ref Font ____fontInUse,
            ref Font ____dynamicFontInUse,
            ref float ____fontSpacingInUse,
            ref Text ____textField)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            ____fontInUse = OWKT.self.D2Coding;
            ____dynamicFontInUse = OWKT.self.D2CodingDyn;
            ____fontSpacingInUse = TextTranslation.GetDefaultFontSpacing();
            ____textField.font = ____fontInUse;
            ____textField.lineSpacing = ____fontSpacingInUse;
            return false;
        }

        private static bool GetShipLogFont(ref Font __result)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            __result = OWKT.self.D2Coding;
            return false;
        }
        private static bool GetShipLogCardFont(ref Font __result)
        {
            if (TextTranslation.Get().GetLanguage() != TextTranslation.Language.KOREAN)
            {
                return true;
            }

            __result = OWKT.self.D2Coding;
            return false;
        }

        private static void SetGameOverScreenFont(ref Text ____deathText)
        {
            ____deathText.font = TextTranslation.GetFont(false);
        }

        private static void FormatNotif(
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

        private static bool EndsWithFinalConsonant(string word)
        {
            if (word.Length == 0) { return false; }
            int n = (int)word[word.Length - 1];
            return 0xac00 <= n && n < 0xd7a4 && n % 28 != 16;

        }

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
