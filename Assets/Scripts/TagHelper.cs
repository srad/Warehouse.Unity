using System;
using UnityEditor;

namespace DefaultNamespace
{
    public static class TagHelper
    {
        public static string AddTag(string tag)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset == null) || (asset.Length <= 0))
            {
                throw new Exception("TagManager not found");
            }

            var so = new SerializedObject(asset[0]);
            var tags = so.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; ++i)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return tag;
                }
            }

            tags.InsertArrayElementAtIndex(0);
            tags.GetArrayElementAtIndex(0).stringValue = tag;
            so.ApplyModifiedProperties();
            so.Update();

            return tag;
        }
    }
}