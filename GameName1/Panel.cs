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
        public Renderer Renderer;

        public float ScrollWidth = 6;
        public Vector2 ScrollAreaStartPosition;
        public bool IsScrollArea = false;
        public int ScrollValue = 0;
        public int RightmostScrollX;

        public Panel(Vector2 position, Renderer renderer)
        {
            Position = position;
            startX = position.X;
            Renderer = renderer;
        }

        internal void Row()
        {
            Position.Y += Font.LineSpacing;
            Position.X = startX;
            if (IsScrollArea)
            {
                Position.X += ScrollWidth;
            }
        }

        internal bool DoClickableText(IMGUIPass cardCreationPass, Input input, string text)
        {
            return DoClickableText(cardCreationPass, input, text, Color.White);
        }

        internal bool DoClickableText(IMGUIPass cardCreationPass, Input input, string text, Color textColor)
        {
            bool result = false;
            int horisontalPadding = 5;
            int buttonWidth = (int)Font.MeasureString(text).X + 2 * horisontalPadding;

            Rectangle buttonRectangle = new Rectangle((int)Position.X, (int)Position.Y + ScrollValue, buttonWidth, Font.LineSpacing);

            int rightmostX = buttonRectangle.Right;
            if (IsScrollArea && rightmostX >= RightmostScrollX)
            {
                RightmostScrollX = rightmostX;
            }
            switch (cardCreationPass)
            {
                case IMGUIPass.Draw:
                {
                    Vector2 textPosition = new Vector2(buttonRectangle.Left + horisontalPadding, buttonRectangle.Top);
                    Renderer.DrawString(Font, text, textPosition, textColor);
                    if (Game1.PointInRectangle(input.MousePosition, buttonRectangle))
                    {
                        Renderer.DrawRectangleOutline(2, Game1.flareHighlightColor, buttonRectangle);
                    }
                    if (!IsScrollArea)
                    {
                        Renderer.DrawRectangle(buttonRectangle, Game1.tileColor);
                    }
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

        internal void DoText(IMGUIPass cardCreationPass, string text, Color textColor)
        {
            switch (cardCreationPass)
            {
                case IMGUIPass.Draw:
                    {
                        Renderer.DrawString(Font, text, Position, textColor);
                        Position.X += Font.MeasureString(text).X;
                    }
                    break;
                case IMGUIPass.Update:
                    {
                    }
                    break;
            }
        }

        internal void VerticalScroll(IMGUIPass pass, int scrollValue)
        {
            ScrollAreaStartPosition = Position;
            Position.X += ScrollWidth;
            IsScrollArea = true;
            ScrollValue = scrollValue;
        }

        internal int EndScroll(IMGUIPass pass, Input input, int scrollValue)
        {
            IsScrollArea = false;
            
            if (pass == IMGUIPass.Draw)
            {
                // Draw Scroll Bar
                float x = ScrollAreaStartPosition.X + ScrollWidth / 2;
                float scrollYStart = ScrollAreaStartPosition.Y;
                float scrollYEnd = Math.Min(Position.Y, Game1.Graphics.PreferredBackBufferHeight);
                float scrollLength = scrollYEnd - scrollYStart;

                float firstItemY = ScrollAreaStartPosition.Y + scrollValue;
                float lastItemY = Position.Y + scrollValue;
                float totalLength = lastItemY - firstItemY;
                float distanceBeforeScreen = -scrollValue;
                float distanceAfterScreen = (Position.Y + scrollValue) - Game1.Graphics.PreferredBackBufferHeight;
                float highlightStartPercent = 0f;
                float highlightEndPercent = 1f;
                if (totalLength != 0f)
                {
                    highlightStartPercent = distanceBeforeScreen / totalLength;
                    if (distanceAfterScreen > 0f)
                    {
                        highlightEndPercent = 1 - (distanceAfterScreen / totalLength);
                    }
                }

                float highlightYStart = scrollYStart + (highlightStartPercent * scrollLength);
                float highlightYEnd = scrollYStart + (highlightEndPercent * scrollLength);
                if (highlightStartPercent != 0f || highlightEndPercent != 1f)
                {
                    Renderer.DrawLine(2, Game1.flareHighlightColor, new Vector2(x, highlightYStart), new Vector2(x, highlightYEnd));
                }
                Renderer.DrawLine(2, Game1.backgroundColor, new Vector2(x, scrollYStart), new Vector2(x, scrollYEnd));

                // Draw Background
                Rectangle outputScrollArea = new Rectangle((int)ScrollAreaStartPosition.X, (int)ScrollAreaStartPosition.Y,
                    (int)(RightmostScrollX - ScrollAreaStartPosition.X), (int)(Position.Y - ScrollAreaStartPosition.Y));
                Renderer.DrawRectangle(outputScrollArea, Game1.tileColor);
            }

            // Get new scroll value.
            int newScrollValue = scrollValue;
            if (pass == IMGUIPass.Update)
            {
                Vector2 mousePosition = input.MousePosition;
                if (ScrollAreaStartPosition.X <= input.MousePosition.X &&
                    input.MousePosition.X <= RightmostScrollX &&
                    ScrollAreaStartPosition.Y <= input.MousePosition.Y &&
                    input.MousePosition.Y <= Position.Y)
                {
                    newScrollValue += (input.MouseState.ScrollWheelValue - input.LastMouseState.ScrollWheelValue) / 20;
                    int maxScroll = (int)-(Position.Y - Game1.Graphics.PreferredBackBufferHeight);
                    if (newScrollValue < maxScroll)
                    {
                        newScrollValue = maxScroll;
                    }
                    if (newScrollValue > 0)
                    {
                        newScrollValue = 0;
                    }
                }
            }
            return newScrollValue;
        }
    }
}
