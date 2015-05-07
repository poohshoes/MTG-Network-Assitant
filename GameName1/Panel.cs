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

        internal bool DoButton(CardCreationPass cardCreationPass, Input input, SpriteBatch spriteBatch, string text)
        {
            return DoButton(cardCreationPass, input, spriteBatch, text, Color.Black);
        }

        internal bool DoButton(CardCreationPass cardCreationPass, Input input, SpriteBatch spriteBatch, string text, Color textColor)
        {
            bool result = false;
            int buttonWidth = (int)Font.MeasureString(text).X + 10;
            Rectangle buttonRectangle = new Rectangle((int)Position.X, (int)Position.Y, buttonWidth, Font.LineSpacing);
            switch (cardCreationPass)
            {
                case CardCreationPass.Draw:
                {
                    Color color = Color.Wheat;
                    if (Game1.PointInRectangle(input.MousePosition, buttonRectangle))
                        color = Color.Yellow;
                    spriteBatch.Draw(Game1.PixelTexture, buttonRectangle, null, color);
                    spriteBatch.DrawString(Font, text, Position, textColor);
                }
                break;
                case CardCreationPass.Update:
                {
                    if (input.LeftMouseDisengaged() && Game1.PointInRectangle(input.MousePosition, buttonRectangle))
                        result = true;
                }
                break;
            }
                        
            Position.X += buttonWidth;

            return result;
        }

        internal void DoText(CardCreationPass cardCreationPass, SpriteBatch spriteBatch, string text, Color textColor)
        {
            switch (cardCreationPass)
            {
                case CardCreationPass.Draw:
                    {
                        spriteBatch.DrawString(Font, text, Position, textColor);
                        Position.X += Font.MeasureString(text).X;
                    }
                    break;
                case CardCreationPass.Update:
                    {
                    }
                    break;
            }
        }
    }
}
