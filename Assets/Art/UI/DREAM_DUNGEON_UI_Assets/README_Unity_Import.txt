DREAM DUNGEON — UI ASSET PACK
================================

Contents:
- Individual PNG crops from the generated Dream Dungeon UI reference sheet.
- Original_Asset_Sheet is included for reference.

Unity import:
1. Extract this folder into your Unity project's Assets folder, e.g. Assets/Art/UI/DreamDungeon/.
2. Select PNG files in Unity Project window.
3. Set Texture Type = Sprite (2D and UI).
4. Set Sprite Mode = Single.
5. Set Alpha Is Transparency = enabled.
6. Set Filter Mode = Bilinear for the illustrated UI. If your project uses pixel-perfect pixel art, try Point (no filter).
7. Apply.

Important:
- These are visual assets/crops, not functional UI prefabs.
- Some crops intentionally contain dark panel interiors/backgrounds as part of the design.
- Set UI Image components to preserve aspect ratio where needed.
- The source sheet is an AI-generated visual reference; inspect each crop and adjust slicing/layout before shipping.
- Text inside generated buttons/cards is baked into the image. For production localization, recreate button labels with Unity TextMeshPro over the button backgrounds.
