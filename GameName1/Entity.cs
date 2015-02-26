using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameName1
{
    public class Entity
    {
        public int NetworkID;
        public Vector2 Position;
        public Texture2D Texture;
        public bool Tapped;
        public int Depth;

        internal Rectangle GetBounds()
        {
            Vector2 half = GetHalf();
            return new Rectangle((int)(Position.X - half.X), (int)(Position.Y - half.Y), Texture.Width, Texture.Height);
        }

        internal Vector2 GetHalf()
        {
            return new Vector2(Texture.Width, Texture.Height) / 2f;
        }
    }

    class EntityComparer : IComparer<Entity>
    {
        public int Compare(Entity one, Entity two)
        {
            return one.Depth - two.Depth;
        }
    }
}
