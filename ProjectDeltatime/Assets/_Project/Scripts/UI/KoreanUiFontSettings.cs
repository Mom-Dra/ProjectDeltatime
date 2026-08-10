using TMPro;
using UnityEngine;

namespace Deltatime.UI
{
    /// <summary>
    /// Provides the Korean fonts shared by IMGUI, legacy TextMesh, and TMP UI.
    /// The asset is kept in Resources so every gameplay scene can resolve it
    /// without duplicating scene-level font references.
    /// </summary>
    public sealed class KoreanUiFontSettings : ScriptableObject
    {
        public const string ResourcesAssetName = "KoreanUiFontSettings";

        [SerializeField] private Font regularFont;
        [SerializeField] private Font boldFont;
        [SerializeField] private TMP_FontAsset textMeshProFont;

        public Font RegularFont => regularFont;
        public Font BoldFont => boldFont;
        public TMP_FontAsset TextMeshProFont => textMeshProFont;
        public bool IsConfigured =>
            regularFont != null && boldFont != null && textMeshProFont != null;

        public void Configure(
            Font regular,
            Font bold,
            TMP_FontAsset tmpFont)
        {
            regularFont = regular;
            boldFont = bold;
            textMeshProFont = tmpFont;
        }

        public static KoreanUiFontSettings Load()
        {
            return Resources.Load<KoreanUiFontSettings>(ResourcesAssetName);
        }
    }
}
