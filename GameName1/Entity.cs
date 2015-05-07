using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameName1
{
    public enum EntityType
    {
        Card,
        Counter
    }

    public class Entity
    {
        public int NetworkID;
        public Vector2 Position;
        public int Depth;
        public EntityType Type;
        public object TypeSpecificClass;

        internal Rectangle GetBounds()
        {
            switch (Type)
            {
                case EntityType.Card:
                    Card card = (Card)TypeSpecificClass;
                    int width = card.Texture.Width;
                    int height = card.Texture.Height;
                    if (card.Tapped)
                    {
                        int swap = width;
                        width = height;
                        height = swap;
                    }
                    Vector2 half = new Vector2(width, height) / 2f;
                    return new Rectangle((int)(Position.X - half.X), (int)(Position.Y - half.Y), width, height);
                case EntityType.Counter:
                    return new Rectangle((int)(Position.X), (int)(Position.Y), Counter.TextAreaWidth, Counter.TextAreaHeight + (2 * Counter.Buttonheight));
                default:
                    throw new Exception("The compiler thinks that is a code paths that doesn't return a value : p");
            }
        }
    }

    public class Card
    {
        public Texture2D Texture;
        public bool Tapped;

        internal Vector2 GetHalf()
        {
            return new Vector2(Texture.Width, Texture.Height) / 2f;
        }
    }

    public class Counter
    {
        public static int TextAreaWidth = 40;
        public static int TextAreaHeight = 40;
        public static int Buttonheight = 20;

        public int Value;
    }

    class EntityComparer : IComparer<Entity>
    {
        public int Compare(Entity one, Entity two)
        {
            return one.Depth - two.Depth;
        }
    }
}
