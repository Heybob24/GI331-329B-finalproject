using UnityEngine;

namespace DreamDungeon
{
    public static class UIThemeManager
    {
        private static DreamDungeonUITheme cachedTheme;

        public static DreamDungeonUITheme Theme
        {
            get
            {
                if (cachedTheme == null)
                {
                    cachedTheme = Resources.Load<DreamDungeonUITheme>(DreamDungeonUITheme.ResourcesPath);
                    if (cachedTheme == null)
                    {
                        cachedTheme = DreamDungeonUITheme.CreateRuntimeDefault();
                    }
                }
                return cachedTheme;
            }
        }

        public static DreamDungeonUIAssetDatabase Assets => DreamDungeonUIAssetDatabase.Load();
    }
}
