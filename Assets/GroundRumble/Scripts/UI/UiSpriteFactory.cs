using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Sprites blancos generados en tiempo de ejecucion para las barras de UI.
    /// </summary>
    public static class UiSpriteFactory
    {
        private static Sprite whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    whiteSprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    whiteSprite.name = "UI_White";
                }

                return whiteSprite;
            }
        }
    }
}
