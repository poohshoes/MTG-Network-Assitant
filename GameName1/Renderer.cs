using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameName1
{
    class Renderer
    {
        enum DrawInfoType
        {
            SimpleTexture,
            Texture,
            String
        }

        class DrawInfo
        {
            public DrawInfoType Type;
        }

        class DrawInfoSimpleTexture : DrawInfo
        {
            public Texture2D Texture;
            public Color Color;
            public Rectangle Destination;

            public DrawInfoSimpleTexture(Texture2D texture, Rectangle destination, Color color)
            {
                Type = DrawInfoType.SimpleTexture;
                this.Texture = texture;
                this.Destination = destination;
                this.Color = color;
            }
        }

        class DrawInfoTexture : DrawInfo
        {
            public Texture2D Texture;
            public Vector2 Position;
            public Color Color;
            public float Rotation;
            public Vector2 Origin;
            public Vector2 Scale;

            public DrawInfoTexture(Texture2D texture, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale)
            {
                Type = DrawInfoType.Texture;
                this.Texture = texture;
                this.Position = position;
                this.Color = color;
                this.Rotation = rotation;
                this.Origin = origin;
                this.Scale = scale;
            }
        }

        class DrawInfoString : DrawInfo
        {
            public SpriteFont Font;
            public string Text;
            public Vector2 Position;
            public Color Color;

            public DrawInfoString(SpriteFont font, string text, Vector2 position, Color color)
            {
                Type = DrawInfoType.String;
                this.Font = font;
                this.Text = text;
                this.Position = position;
                this.Color = color;
            }
        }

        private SpriteBatch spriteBatch;
        private Texture2D pixelTexture;
        private Stack<DrawInfo> drawInfoStack;

        public Renderer(GraphicsDevice graphicsDevice)
        {
            this.spriteBatch = new SpriteBatch(graphicsDevice);

            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            Color[] pixelTextureData = new Color[1];
            pixelTexture.GetData<Color>(pixelTextureData);
            pixelTextureData[0] = Color.White;
            pixelTexture.SetData<Color>(pixelTextureData);

            drawInfoStack = new Stack<DrawInfo>();
        }

        internal void End()
        {
            spriteBatch.Begin();

            while (drawInfoStack.Count > 0)
            {
                DrawInfo drawInfo = drawInfoStack.Pop();
                switch (drawInfo.Type)
                {
                    case DrawInfoType.SimpleTexture:
                        DrawInfoSimpleTexture simpleTexture = (DrawInfoSimpleTexture)drawInfo;
                        spriteBatch.Draw(simpleTexture.Texture, simpleTexture.Destination, simpleTexture.Color);
                        break;
                    case DrawInfoType.Texture:
                        DrawInfoTexture drawTexture = (DrawInfoTexture)drawInfo;
                        spriteBatch.Draw(drawTexture.Texture, drawTexture.Position, null, drawTexture.Color, drawTexture.Rotation, drawTexture.Origin, drawTexture.Scale, SpriteEffects.None, 0f);
                        break;
                    case DrawInfoType.String:
                        DrawInfoString drawString = (DrawInfoString)drawInfo;
                        spriteBatch.DrawString(drawString.Font, drawString.Text, drawString.Position, drawString.Color);
                        break;
                }
            }

            spriteBatch.End();
        }

        internal void DrawRectangle(Rectangle destination, Color color)
        {
            drawInfoStack.Push(new DrawInfoSimpleTexture(pixelTexture, destination, color));
        }

        internal void DrawRectangleOutline(int lineThickness, Color color, Rectangle rectangle)
        {
            DrawRectangleOutline(lineThickness, color, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        internal void DrawRectangleOutline(int lineThickness, Color color, float left, float top, float right, float bottom)
        {
            DrawLine(lineThickness, color, new Vector2(left, top), new Vector2(right, top));
            DrawLine(lineThickness, color, new Vector2(right, top), new Vector2(right, bottom));
            DrawLine(lineThickness, color, new Vector2(right, bottom), new Vector2(left, bottom));
            DrawLine(lineThickness, color, new Vector2(left, bottom), new Vector2(left, top));
        }

        internal void DrawLine(float lineThickness, Color color, Vector2 point1, Vector2 point2)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);
            drawInfoStack.Push(new DrawInfoTexture(pixelTexture, point1, color, angle, Vector2.Zero, new Vector2(length, lineThickness)));
        }

        internal void DrawString(SpriteFont font, string text, Vector2 position, Color color)
        {
            drawInfoStack.Push(new DrawInfoString(font, text, position, color));
        }

        internal void Draw(Texture2D texture, Rectangle destination, Color color)
        {
            drawInfoStack.Push(new DrawInfoSimpleTexture(texture, destination, color));
        }
                            
        internal void Draw(Texture2D texture, Vector2 position, Color color, float rotation, Vector2 origin)
        {
            drawInfoStack.Push(new DrawInfoTexture(texture, position, color, rotation, origin, Vector2.One));
        }
    }
}
