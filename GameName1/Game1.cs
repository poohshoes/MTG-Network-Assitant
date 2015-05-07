﻿#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net;
#endregion

namespace GameName1
{
    public class CardData
    {
        public string Name;
        public string TexturePath;
        public bool Starred = false;
    }

    public enum CardCreationPass
    {
        Draw, Update
    }

    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        public static Texture2D PixelTexture;
        SpriteBatch spriteBatch;
        Network network;

        Input input;
        Settings settings;

        Entity dragTarget = null;
        public List<Entity> Entities;

        double lastDragMessageSentSeconds = 0;

        List<CardData> cardData;

        SpriteFont font;
        int NetworkIDCounter;

        string searchText = "";
        Color searchTextColor = Color.Black;
        WebClient client = new WebClient();
        KeyboardState lastKeyboardState;

        int topDepth = 0;
        public int GetNextHeighestDepth()
        {
            return topDepth++;
        }

        public Game1()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            Entities = new List<Entity>();
            settings = new Settings();
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = settings.width;
            graphics.PreferredBackBufferHeight = settings.height;
            graphics.ApplyChanges();

            input = new Input();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Content.RootDirectory = "Data";

            PixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            Color[] pixelTextureData = new Color[1];
            PixelTexture.GetData<Color>(pixelTextureData);
            pixelTextureData[0] = Color.White;
            PixelTexture.SetData<Color>(pixelTextureData);

            font = Content.Load<SpriteFont>("SegoeUIMono12");

            cardData = new List<CardData>();

            foreach (string cardPath in Directory.GetFiles("Data", "*.jpg"))
            {
                CardData data = new CardData();
                data.TexturePath = Path.GetFileName(cardPath);
                data.Name = Path.GetFileNameWithoutExtension(cardPath);
                cardData.Add(data);
            }

            if (File.Exists("starred.txt"))
            {
                foreach (string starredCardName in File.ReadAllLines("starred.txt"))
                {
                    CardData data = cardData.FirstOrDefault(x => x.Name == starredCardName);
                    if (data != null)
                        data.Starred = true;
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            input.Update();

            if (Debugger.IsAttached && input.KeyboardState.IsKeyDown(Keys.Escape))
                Exit();

            if (network == null)
            {
                network = new Network(settings);

                // Note(ian): Comment this out for release.
                if (network.IsServer())
                    SetWindowPosition(0, 0);
                else
                    SetWindowPosition(1920, 0);

                if (network.IsServer())
                    NetworkIDCounter = 0;
                else
                    NetworkIDCounter = 1000000;
            }
            network.Update(gameTime, this);

            bool mouseInteracted = false;
            mouseInteracted = CardCreation(CardCreationPass.Update);

            lastDragMessageSentSeconds += gameTime.ElapsedGameTime.TotalSeconds;

            if (!mouseInteracted)
            {
                if (dragTarget != null)
                {
                    dragTarget.Position += input.MousePosition - input.LastMousePosition;

                    if (lastDragMessageSentSeconds > 0.2)
                    {
                        network.SendEntityMovedMessage(dragTarget.NetworkID, dragTarget.Position);
                        lastDragMessageSentSeconds = 0;
                    }
                }

                if (input.LeftMouseEngaged())
                {
                    if (dragTarget == null)
                    {
                        foreach (Entity entity in Entities)
                        {
                            if (PointInRectangle(input.MousePosition, entity.GetBounds()))
                            {
                                dragTarget = entity;
                                dragTarget.Depth = GetNextHeighestDepth();
                            }
                        }
                    }
                }
                else if (input.LeftMouseDisengaged())
                {
                    if (dragTarget != null)
                    {
                        network.SendEntityMovedMessage(dragTarget.NetworkID, dragTarget.Position);
                        lastDragMessageSentSeconds = 0;
                    }
                    dragTarget = null;
                }

                if (input.RightMouseDisengaged())
                {
                    foreach (Entity entity in Entities)
                    {
                        if (PointInRectangle(input.MousePosition, entity.GetBounds()))
                        {
                            entity.Tapped = !entity.Tapped;
                            network.SendEntityTappedMessage(entity.NetworkID);
                        }
                    }
                }
            }

            KeyboardState keyboardState = Keyboard.GetState();
            if (lastKeyboardState != null)
            {
                for (int i = 65; i <= 90; i++)
                {
                    if (keyboardState.IsKeyDown((Keys)i) && lastKeyboardState.IsKeyUp((Keys)i))
                    {
                        int key = i;
                        if (!keyboardState.IsKeyDown(Keys.LeftShift) && !keyboardState.IsKeyDown(Keys.RightShift))
                        {
                            key += 32;
                        }
                        searchText += (char)key;
                        searchTextColor = Color.Black;
                    }
                }
                if (keyboardState.IsKeyDown(Keys.Space) && lastKeyboardState.IsKeyUp(Keys.Space))
                {
                    searchText += " ";
                }
                if (keyboardState.IsKeyDown(Keys.OemComma) && lastKeyboardState.IsKeyUp(Keys.OemComma))
                {
                    searchText += ",";
                }
                if (keyboardState.IsKeyDown(Keys.OemMinus) && lastKeyboardState.IsKeyUp(Keys.OemMinus))
                {
                    searchText += "-";
                }
                if (keyboardState.IsKeyDown(Keys.OemQuotes) && lastKeyboardState.IsKeyUp(Keys.OemQuotes))
                {
                    searchText += "'";
                }
                if (searchText.Length > 0 && keyboardState.IsKeyDown(Keys.Back) && lastKeyboardState.IsKeyUp(Keys.Back))
                {
                    searchText = searchText.Substring(0, searchText.Length - 1);
                }
                if (keyboardState.IsKeyDown(Keys.Enter) && lastKeyboardState.IsKeyUp(Keys.Enter))
                {
                    if (!TryGetFile(searchText))
                    {
                        searchTextColor = Color.Red;
                    }
                    else
                    {
                        searchText = "";
                    }
                }
            }
            lastKeyboardState = keyboardState;

            base.Update(gameTime);
        }

        public bool TryGetFile(string cardName)
        {
            string source = client.DownloadString("https://deckbox.org/mtg/" + cardName);
            string match = "id='card_image' src='";
            int matchIndex = source.IndexOf(match);
            if (matchIndex == -1)
            {
                return false;
            }
            else
            {
                int cardID = matchIndex + match.Length;
                int cardIDEnd = source.IndexOf("'", cardID);
                string downloadLink = "https://deckbox.org" + source.Substring(cardID, cardIDEnd - cardID);
                string outputFileName = cardName + ".jpg";
                client.DownloadFile(downloadLink, "Data\\" + outputFileName);
                CardData data = new CardData();
                data.TexturePath = Path.GetFileName(outputFileName);
                data.Name = Path.GetFileNameWithoutExtension(outputFileName);
                cardData.Add(data);
                return true;
            }
        }

        public static bool PointInRectangle(Vector2 point, Rectangle rectangle)
        {
            return PointInRectangle(point, rectangle.Left, rectangle.Right, rectangle.Top, rectangle.Bottom);
        }

        public static bool PointInRectangle(Vector2 point, int left, int right, int top, int bottom)
        {
            return point.X >= left &&
                point.X < right &&
                point.Y >= top &&
                point.Y < bottom;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);

            spriteBatch.Begin();

            Entities.Sort(new EntityComparer());
            foreach (Entity entity in Entities)
            {
                float rotation = 0;
                if (entity.Tapped)
                    rotation = (float)Math.PI / 2f;
                spriteBatch.Draw(entity.Texture, entity.Position, null, Color.White, rotation, entity.GetHalf(), 1f, SpriteEffects.None, 0);
            }

            CardCreation(CardCreationPass.Draw);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void SetWindowPosition(int x, int y)
        {
            Type type = typeof(GameWindow).Assembly.GetType("Microsoft.Xna.Framework.OpenTKGameWindow");
            System.Reflection.FieldInfo field = type.GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            OpenTK.INativeWindow window = (OpenTK.INativeWindow)field.GetValue(this.Window);
            window.X = x;
            window.Y = y;
        }

        char shownLetter = (char)0;
        // Returns true if it consumes the mouse action.
        private bool CardCreation(CardCreationPass cardCreationPass)
        {
            bool consumesMouseAction = false;
            Panel panel = new Panel(new Vector2(5, 0));
            panel.Font = font;
            int start = (int)'A';
            int end = (int)'Z';
            for (int i = start; i <= end; i++)
            {
                if (panel.DoButton(cardCreationPass, input, spriteBatch, ((char)i).ToString()))
                {
                    shownLetter = (char)i;
                    consumesMouseAction = true;
                }
            }
            if (panel.DoButton(cardCreationPass, input, spriteBatch, "*"))
            {
                shownLetter = '*';
            }
            panel.DoText(cardCreationPass, spriteBatch, searchText, searchTextColor);
            panel.Row();

            if (input.RightMouseEngaged())
            {
                shownLetter = (char)0;
                consumesMouseAction = true;
            }

            if (shownLetter != (char)0)
            {
                List<CardData> cardsToShow;
                if (shownLetter == '*')
                    cardsToShow = cardData.Where(x => x.Starred).ToList();
                else
                    cardsToShow = cardData.Where(x => x.Name.ToUpper()[0] == shownLetter).ToList();

                foreach (CardData card in cardsToShow)
                {
                    Color starColor = Color.Black;
                    if (!card.Starred)
                        starColor = Color.White;
                    if (panel.DoButton(cardCreationPass, input, spriteBatch, "*", starColor))
                    {
                        card.Starred = !card.Starred;
                        SaveStarredCards();
                    }
                    if (panel.DoButton(cardCreationPass, input, spriteBatch, card.Name))
                    {
                        SpawnCardAndSendNetworkMessage(card.Name, card.TexturePath);
                        shownLetter = (char)0;
                        consumesMouseAction = true;
                    }
                    panel.Row();
                }
            }
            return consumesMouseAction;
        }

        private void SaveStarredCards()
        {
            List<string> starredCardNames = cardData.Where(x => x.Starred).Select(x => x.Name).ToList();
            File.WriteAllLines("starred.txt", starredCardNames);
        }
        
        private void SpawnCardAndSendNetworkMessage(string cardName, string texturePath)
        {
            Entity card = SpawnCard(texturePath, NetworkIDCounter);
            network.SendEntityCreateMessage(NetworkIDCounter, texturePath, card.Position);
            NetworkIDCounter++;
        }

        public Entity SpawnCard(string texturePath, int networkID)
        {
            if(!File.Exists(Path.Combine(Content.RootDirectory, texturePath)))
            {
                string cardName = Path.GetFileNameWithoutExtension(texturePath);
                if(!TryGetFile(cardName))
                {
                    throw new Exception("Couldn't aquire card " + texturePath);
                }
            }

            Entity card = new Entity();
            card.NetworkID = networkID;
            card.Position = input.MousePosition;
            card.Texture = Content.Load<Texture2D>(texturePath);
            Entities.Add(card);
            card.Depth = GetNextHeighestDepth();
            return card;
        }
        
        //internal void DrawLine(float width, Color color, Vector2 point1, Vector2 point2)
        //{
        //    float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
        //    float length = Vector2.Distance(point1, point2);

        //    spriteBatch.Draw(pixelTexture, point1, null, color, angle, Vector2.Zero, new Vector2(length, width), SpriteEffects.None, 0f);
        //}
    }
}
