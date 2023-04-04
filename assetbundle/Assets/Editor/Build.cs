using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Build {
    static void BuildAssetBundle() {
        TextAsset translationAsset = (TextAsset)AssetDatabase.LoadAssetAtPath("Assets/Translation.txt", typeof(TextAsset));

        // Deduplicate characters.
        string translationStr = translationAsset.ToString();
        HashSet<char> charsSet = new HashSet<char>();
        for (int i = 0; i < translationStr.Length; i++) {
            charsSet.Add(translationStr[i]);
        }
        List<char> charsList = new List<char>(charsSet);
        charsList.Sort();
        string charsStr = string.Join("", charsList);

        foreach (string path in new string[] { "Assets/Fonts/NanumBarunGothic.otf", "Assets/Fonts/D2Coding.ttf"} ) {
            TrueTypeFontImporter staticFontImporter = (TrueTypeFontImporter)AssetImporter.GetAtPath(path);
            staticFontImporter.customCharacters = charsStr;
        }
        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
        buildMap[0].assetBundleName = "owkt";
        buildMap[0].assetNames = new string[] {"Assets/Translation.txt", "Assets/Fonts/NanumBarunGothic.otf", "Assets/Fonts/NanumBarunGothic_Dynamic.otf", "Assets/Fonts/D2Coding.ttf", "Assets/Fonts/D2Coding_Dynamic.ttf"};
        BuildPipeline.BuildAssetBundles("Assets/AssetBundle", buildMap, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }
}