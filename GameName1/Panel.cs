using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameName1
{
    class Panel
    {
        public float startX;
        public Vector2 Position;
        public SpriteFont Font;

        public Panel(Vector2 position)
        {
            Position = position;
            startX = position.X;
        }

        internal void Row()
        {
            Position.Y += Font.LineSpacing;
            Position.X = startX;
        }

        internal bool DoClickableText(IMGUIPass cardCreationPass, Input input, SpriteBatch spriteBatch, string text)
        {
            return DoClickableText(cardCreationPass, input, spriteBatch, text, Color.White);
        }

        internal bool DoClickableText(IMGUIPass cardCreationPass, Input input, SpriteBatch spriteBatch, string text, Color textColor)
        {
            bool result = false;
            int horisontalPadding = 5;
            int buttonWidth = (int)Font.MeasureString(text).X + 2 * horisontalPadding;
            Rectangle buttonRectangle = new Rectangle((int)Position.X, (int)Position.Y, buttonWidth, Font.LineSpacing);
            switch (cardCreationPass)
            {
                case IMGUIPass.Draw:
                {
                    spriteBatch.Draw(Game1.PixelTexture, buttonRectangle, null, Game1.tileColor);
                    if (Game1.PointInRectangle(input.MousePosition, buttonRectangle))
                    {
                        Game1.DrawRectangle(spriteBatch, 2, Game1.flareHighlightColor, buttonRectangle);
                    }
                    Vector2 textPosition = new Vector2(Position.X + horisontalPadding, Position.Y);
                    spriteBatch.DrawString(Font, text, textPosition, textColor);
                }
                break;
                case IMGUIPass.Update:
                {
                    if (input.LeftMouseDisengaged() && Game1.PointInRectangle(input.MousePosition, buttonRectangle))
                        result = true;
                }
                break;
            }
                        
            Position.X += buttonWidth;

            return result;
        }

        internal void DoText(IMGUIPass cardCreationPass, SpriteBatch spriteBatch, string text, Color textColor)
        {
            switch (cardCreationPass)
            {
                case IMGUIPass.Draw:
                    {
                        spriteBatch.DrawString(Font, text, Position, textColor);
                        Position.X += Font.MeasureString(text).X;
                    }
                    break;
                case IMGUIPass.Update:
                    {
                    }
                    break;
            }
        }
    }
}
