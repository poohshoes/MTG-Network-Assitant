using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameName1
{
    class Input
    {
        public KeyboardState KeyboardState;
        public KeyboardState LastKeyboardState;
        
        public MouseState MouseState;
        public MouseState LastMouseState;
        public Vector2 MousePosition;
        public Vector2 LastMousePosition;

        // do mouse left pressed here for use in do button!

        public void Update()
        {
            LastKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();

            LastMouseState = MouseState;
            MouseState = Mouse.GetState();
            MousePosition = new Vector2(MouseState.X, MouseState.Y);
            LastMousePosition = new Vector2(LastMouseState.X, LastMouseState.Y);
        }

        public bool LeftMouseEngaged()
        {
            return MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released;
        }

        public bool LeftMouseDisengaged()
        {
            return MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed;
        }

        public bool RightMouseEngaged()
        {
            return MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released;
        }

        public bool RightMouseDisengaged()
        {
            return MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed;
        }

        public bool KeyPressed(Keys key)
        {
            return LastKeyboardState != null && LastKeyboardState.IsKeyUp(key) && KeyboardState.IsKeyDown(key);
        }
    }
}
