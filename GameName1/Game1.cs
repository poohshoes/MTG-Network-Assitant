#region Using Statements
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

    public enum IMGUIPass
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
        KeyboardState keyboardState;
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

            keyboardState = Keyboard.GetState();

            if (network == null)
            {
                network = new Network(settings);

                //// Note(ian): Comment this out for release.
                //if (network.IsServer())
                //    SetWindowPosition(0, 0);
                //else
                //    SetWindowPosition(1920, 0);

                if (network.IsServer())
                    NetworkIDCounter = 0;
                else
                    NetworkIDCounter = 1000000;
            }
            network.Update(gameTime, this);

            lastDragMessageSentSeconds += gameTime.ElapsedGameTime.TotalSeconds;

            UpdateAndDraw(gameTime, IMGUIPass.Update);

            lastKeyboardState = keyboardState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);

            spriteBatch.Begin();
            
            UpdateAndDraw(gameTime, IMGUIPass.Draw);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        // For card selection.
        char shownLetter = (char)0;

        public int[,] manaBoxes = new int[4, 5];
        List<string> manaTextures = new List<string>() { "Mana\\greenMana.png", "Mana\\redMana.png", "Mana\\blueMana.png", "Mana\\blackMana.png", "Mana\\whiteMana.png" };
        int manaTextureSize = 30;
        int manaBoxWidthInMana = 5;

        public void UpdateAndDraw(GameTime gameTime, IMGUIPass pass)
        {
            bool mouseActionConsumedDebug = true;

            // CARD CREATION
            bool mouseActionConsumed = false;
            Panel panel = new Panel(new Vector2(5, 0));
            panel.Font = font;
            int start = (int)'A';
            int end = (int)'Z';
            for (int i = start; i <= end; i++)
            {
                if (panel.DoButton(pass, input, spriteBatch, ((char)i).ToString()))
                {
                    shownLetter = (char)i;
                    mouseActionConsumed = true;
                    if (mouseActionConsumedDebug)
                    {
                        Console.WriteLine("MAC: Letter Selected");
                    }
                }
            }
            if (panel.DoButton(pass, input, spriteBatch, "*"))
            {
                shownLetter = '*';
                mouseActionConsumed = true;
                if (mouseActionConsumedDebug)
                {
                    Console.WriteLine("MAC: * Selected");
                }
            }
            if (panel.DoButton(pass, input, spriteBatch, "C"))
            {
                SpawnCounterAndSendNetworkMessage();
                mouseActionConsumed = true;
                if (mouseActionConsumedDebug)
                {
                    Console.WriteLine("MAC: Counter Selected");
                }
            }

            // TYPED CARD RETREIVAL
            if (pass == IMGUIPass.Update)
            {
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
            }
            panel.DoText(pass, spriteBatch, searchText, searchTextColor);
            panel.Row();

            if (input.RightMouseEngaged() && shownLetter != (char)0)
            {
                shownLetter = (char)0;
                mouseActionConsumed = true;
                if (mouseActionConsumedDebug)
                {
                    Console.WriteLine("MAC: Card List Closed");
                }
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
                    if (panel.DoButton(pass, input, spriteBatch, "*", starColor))
                    {
                        card.Starred = !card.Starred;
                        SaveStarredCards();
                    }
                    if (panel.DoButton(pass, input, spriteBatch, card.Name))
                    {
                        SpawnCardAndSendNetworkMessage(card.Name, card.TexturePath);
                        shownLetter = (char)0;
                        mouseActionConsumed = true;
                        if (mouseActionConsumedDebug)
                        {
                            Console.WriteLine("MAC: Card Spawned");
                        }
                    }
                    panel.Row();
                }
            }

            // MANA BOXES
            if (!mouseActionConsumed || pass == IMGUIPass.Draw)
            {
                int manaBoxIndex_TopAvaliable = 0;
                int manaBoxIndex_TopUsed = 1;
                int manaBoxIndex_BottomAvaliable = 2;
                int manaBoxIndex_BottomUsed = 3;

                int manaBoxHeightInMana = 5;
                
                int manaBoxWidth = manaBoxWidthInMana * manaTextureSize;
                int totalManaBoxWidth = 2 * manaBoxWidth + manaTextureSize;

                int startX = graphics.PreferredBackBufferWidth - totalManaBoxWidth;
                mouseActionConsumed |= DoManaBox(pass, new Vector2(startX, 0), manaBoxIndex_TopAvaliable, manaBoxIndex_TopUsed, Color.White);
                mouseActionConsumed |= DoManaBox(pass, new Vector2(startX + manaBoxWidth, 0), manaBoxIndex_TopUsed, manaBoxIndex_TopAvaliable, Color.Gray);
                mouseActionConsumed |= DoAddManaButtons(pass, new Vector2(startX + (2 * manaBoxWidth), 0), manaBoxIndex_TopAvaliable);
                mouseActionConsumed |= DoRefreshManaButton(pass, new Vector2(startX - manaTextureSize, 0), manaBoxIndex_TopAvaliable);

                int startY = graphics.PreferredBackBufferHeight - (manaTextureSize * manaBoxHeightInMana);
                mouseActionConsumed |= DoManaBox(pass, new Vector2(startX, startY), manaBoxIndex_BottomAvaliable, manaBoxIndex_BottomUsed, Color.White);
                mouseActionConsumed |= DoManaBox(pass, new Vector2(startX + manaBoxWidth, startY), manaBoxIndex_BottomUsed, manaBoxIndex_BottomAvaliable, Color.Gray);
                mouseActionConsumed |= DoAddManaButtons(pass, new Vector2(startX + (2 * manaBoxWidth), startY), manaBoxIndex_BottomAvaliable);
                mouseActionConsumed |= DoRefreshManaButton(pass, new Vector2(startX - manaTextureSize, startY), manaBoxIndex_BottomAvaliable);

                if (mouseActionConsumedDebug && mouseActionConsumed)
                {
                    Console.WriteLine("MAC: Mana Box Action");
                }
            }

            // CARDS AND COUNTERS
            // Move And Drop Drag Target
            if (pass == IMGUIPass.Update && !mouseActionConsumed)
            {
                if (dragTarget != null)
                {
                    dragTarget.Position += input.MousePosition - input.LastMousePosition;

                    if (lastDragMessageSentSeconds > 0.2)
                    {
                        network.SendEntityMovedMessage(dragTarget.NetworkID, dragTarget.Position);
                        lastDragMessageSentSeconds = 0;
                    }

                    if (input.LeftMouseDisengaged())
                    {
                        network.SendEntityMovedMessage(dragTarget.NetworkID, dragTarget.Position);
                        lastDragMessageSentSeconds = 0;
                        dragTarget = null;
                        mouseActionConsumed = true;
                        if (mouseActionConsumedDebug)
                        {
                            Console.WriteLine("MAC: Entity Drag Stopped");
                        }
                    }
                }
            }

            Entities.Sort(new EntityComparer());
            foreach (Entity entity in Entities)
            {
                switch (entity.Type)
                {
                    case EntityType.Card:
                        if (pass == IMGUIPass.Draw)
                        {
                            Card card = (Card)entity.TypeSpecificClass;
                            float rotation = 0;
                            if (card.Tapped)
                                rotation = (float)Math.PI / 2f;
                            spriteBatch.Draw(card.Texture, entity.Position, null, Color.White, rotation, card.GetHalf(), 1f, SpriteEffects.None, 0);
                        }
                        else // Update Pass
                        {
                            // Start Drag
                            if (input.LeftMouseEngaged() && dragTarget == null && !mouseActionConsumed)
                            {
                                if (PointInRectangle(input.MousePosition, entity.GetBounds()))
                                {
                                    dragTarget = entity;
                                    dragTarget.Depth = GetNextHeighestDepth();
                                    mouseActionConsumed = true;
                                    if (mouseActionConsumedDebug)
                                    {
                                        Console.WriteLine("MAC: Card Drag Started");
                                    }
                                }
                            }

                            // Tap
                            if (input.RightMouseDisengaged() && !mouseActionConsumed)
                            {
                                if (PointInRectangle(input.MousePosition, entity.GetBounds()))
                                {
                                    Card card = (Card)entity.TypeSpecificClass;
                                    card.Tapped = !card.Tapped;
                                    network.SendEntityTappedMessage(entity.NetworkID);
                                    mouseActionConsumed = true;
                                    if (mouseActionConsumedDebug)
                                    {
                                        Console.WriteLine("MAC: Card Tapped");
                                    }
                                }
                            }
                        }
                        break;
                    case EntityType.Counter:
                        Counter counter = (Counter)entity.TypeSpecificClass;
                        if (pass == IMGUIPass.Draw)
                        {
                            Vector2 topArrowPoint = entity.Position + new Vector2(Counter.TextAreaWidth / 2f, 0);
                            Vector2 bottomArrowPoint = topArrowPoint + new Vector2(0, 2 * Counter.Buttonheight + Counter.TextAreaHeight);
                            float top = entity.Position.Y + Counter.Buttonheight;
                            float bottom = top + Counter.TextAreaHeight;
                            float left = entity.Position.X;
                            float right = left + Counter.TextAreaWidth;

                            // Text area box
                            DrawLine(3, Color.SkyBlue, new Vector2(left, top), new Vector2(right, top));
                            DrawLine(3, Color.SkyBlue, new Vector2(right, top), new Vector2(right, bottom));
                            DrawLine(3, Color.SkyBlue, new Vector2(right, bottom), new Vector2(left, bottom));
                            DrawLine(3, Color.SkyBlue, new Vector2(left, bottom), new Vector2(left, top));

                            // Top arrow
                            DrawLine(3, Color.SkyBlue, new Vector2(left, top), topArrowPoint);
                            DrawLine(3, Color.SkyBlue, topArrowPoint, new Vector2(right, top));

                            // Bottom arrow
                            DrawLine(3, Color.SkyBlue, new Vector2(left, bottom), bottomArrowPoint);
                            DrawLine(3, Color.SkyBlue, bottomArrowPoint, new Vector2(right, bottom));

                            string text = counter.Value.ToString();
                            SpriteFont fontToUse = font;
                            Vector2 centerOffset = (new Vector2(Counter.TextAreaWidth, Counter.TextAreaHeight) - fontToUse.MeasureString(text)) / 2f;
                            spriteBatch.DrawString(fontToUse, text, new Vector2(left, top) + centerOffset, Color.Blue);

                            // Test code
                            Vector2 startOfBottomTriangle = entity.Position + new Vector2(0, Counter.Buttonheight + Counter.TextAreaHeight);
                            if (PointInTriangle(input.MousePosition,
                                    entity.Position + new Vector2(0, Counter.Buttonheight),
                                    entity.Position + new Vector2(Counter.TextAreaWidth / 2, 0),
                                    entity.Position + new Vector2(Counter.TextAreaWidth, Counter.Buttonheight)))
                            {
                                spriteBatch.DrawString(fontToUse, "LOVE ME", entity.Position, Color.Red);
                            }
                            else if (PointInTriangle(input.MousePosition,
                                    startOfBottomTriangle + new Vector2(0, 0),
                                    startOfBottomTriangle + new Vector2(Counter.TextAreaWidth / 2, Counter.Buttonheight),
                                    startOfBottomTriangle + new Vector2(Counter.TextAreaWidth, 0)))
                            {
                                spriteBatch.DrawString(fontToUse, "DAT CLICK", startOfBottomTriangle, Color.Red);
                            }
                        }
                        else // Update Pass
                        {
                            if(dragTarget == null && !mouseActionConsumed)
                            {
                                if (input.LeftMouseEngaged())
                                {
                                    if (PointInRectangle(input.MousePosition,
                                        new Rectangle((int)entity.Position.X, (int)entity.Position.Y + Counter.Buttonheight,
                                            Counter.TextAreaHeight, Counter.TextAreaHeight)))
                                    {
                                        dragTarget = entity;
                                        dragTarget.Depth = GetNextHeighestDepth();
                                        mouseActionConsumed = true;
                                        if (mouseActionConsumedDebug)
                                        {
                                            Console.WriteLine("MAC: Counter Drag Started");
                                        }
                                    }
                                }
                                else if(input.LeftMouseDisengaged())
                                {
                                    Console.WriteLine("Checking Counter Buttons");
                                    Vector2 startOfBottomTriangle = entity.Position + new Vector2(0, Counter.Buttonheight + Counter.TextAreaHeight);
                                    int counterChange = 0;
                                    if (PointInTriangle(input.MousePosition,
                                            entity.Position + new Vector2(0, Counter.Buttonheight),
                                            entity.Position + new Vector2(Counter.TextAreaWidth / 2, 0),
                                            entity.Position + new Vector2(Counter.TextAreaWidth, Counter.Buttonheight)))
                                    {
                                        counterChange = 1;
                                    }
                                    else if (PointInTriangle(input.MousePosition,
                                            startOfBottomTriangle + new Vector2(0, 0),
                                            startOfBottomTriangle + new Vector2(Counter.TextAreaWidth / 2, Counter.Buttonheight),
                                            startOfBottomTriangle + new Vector2(Counter.TextAreaWidth, 0)))
                                    {
                                        counterChange = -1;
                                    }
                                    if (counterChange != 0)
                                    {
                                        counter.Value += counterChange;
                                        network.SendCounterChangeMessage(entity.NetworkID, counter.Value);
                                        mouseActionConsumed = true;
                                        if (mouseActionConsumedDebug)
                                        {
                                            Console.WriteLine("MAC: Counter Changed");
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            //if (!mouseActionConsumed && mouseActionConsumedDebug && 
            //    (input.LeftMouseDisengaged() || input.LeftMouseEngaged() || input.RightMouseDisengaged() || input.RightMouseEngaged()))
            //{
            //    Console.WriteLine("MAC: No Mouse Action");
            //}
        }

        // TODO(ian): inline this?
        private bool DoManaBox(IMGUIPass cardCreationPass, Vector2 topLeft, int manaBoxIndex, int manaBoxSwapIndex, Color color)
        {
            bool consumesMouseAction = false;
            Vector2 currentPosition = new Vector2(topLeft.X, topLeft.Y);
            int column = 0;
            for (int manaType = 0; manaType < 5; manaType++)
            {
                Texture2D manaTexture = Content.Load<Texture2D>(manaTextures[manaType]);
                for (int mana = 0; mana < manaBoxes[manaBoxIndex, manaType]; mana++)
                {
                    Rectangle textureRectangle = new Rectangle((int)currentPosition.X, (int)currentPosition.Y, (int)manaTextureSize, (int)manaTextureSize);
                    switch (cardCreationPass)
                    {
                        case IMGUIPass.Draw:
                            spriteBatch.Draw(manaTexture, textureRectangle, color);
                            break;
                        case IMGUIPass.Update:
                            if (Game1.PointInRectangle(input.MousePosition, textureRectangle))
                            {
                                if (input.LeftMouseDisengaged())
                                {
                                    manaBoxes[manaBoxIndex, manaType]--;
                                    manaBoxes[manaBoxSwapIndex, manaType]++;
                                    consumesMouseAction = true;
                                    network.SendTransferMana(manaBoxIndex, manaBoxSwapIndex, manaType);
                                }
                                else if (input.RightMouseDisengaged())
                                {
                                    manaBoxes[manaBoxIndex, manaType]--;
                                    consumesMouseAction = true;
                                    network.SendRemoveMana(manaBoxIndex, manaType);
                                }
                            }
                            break;
                    }
                    currentPosition.X += manaTextureSize;
                    column++;
                    if (column >= manaBoxWidthInMana)
                    {
                        column = 0;
                        currentPosition.X = topLeft.X;
                        currentPosition.Y += manaTextureSize;
                    }
                }
            }
            return consumesMouseAction;
        }

        // TODO(ian): inline this?
        private bool DoAddManaButtons(IMGUIPass cardCreationPass, Vector2 topLeft, int manaBoxIndex)
        {
            bool consumesMouseAction = false;
            for (int manaType = 0; manaType < manaTextures.Count; manaType++)
            {
                Rectangle textureRectangle = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)manaTextureSize, (int)manaTextureSize);
                switch (cardCreationPass)
                {
                    case IMGUIPass.Draw:
                        Texture2D manaTexture = Content.Load<Texture2D>(manaTextures[manaType]);
                        spriteBatch.Draw(manaTexture, textureRectangle, Color.White);
                        break;
                    case IMGUIPass.Update:
                        if (input.LeftMouseDisengaged() && Game1.PointInRectangle(input.MousePosition, textureRectangle))
                        {
                            manaBoxes[manaBoxIndex, manaType]++;
                            consumesMouseAction = true;
                            network.SendCreateMana(manaBoxIndex, manaType);
                        }
                        break;
                }
                topLeft.Y += manaTextureSize;
            }
            return consumesMouseAction;
        }

        // TODO(ian): inline this?
        private bool DoRefreshManaButton(IMGUIPass cardCreationPass, Vector2 topLeft, int manaBoxIndex)
        {
            bool consumesMouseAction = false;
            Rectangle textureRectangle = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)manaTextureSize, (int)manaTextureSize);
            switch (cardCreationPass)
            {
                case IMGUIPass.Draw:
                    Texture2D refreshManaTexture = Content.Load<Texture2D>("Mana\\refresh.png");
                    spriteBatch.Draw(refreshManaTexture, textureRectangle, Color.White);
                    break;
                case IMGUIPass.Update:
                    if (input.LeftMouseDisengaged() && Game1.PointInRectangle(input.MousePosition, textureRectangle))
                    {
                        RefreshMana(manaBoxIndex);
                        consumesMouseAction = true;
                        network.SendRefreshMana(manaBoxIndex);
                    }
                    break;
            }
            return consumesMouseAction;
        }

        public void RefreshMana(int manaBoxIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                manaBoxes[manaBoxIndex, i] += manaBoxes[manaBoxIndex + 1, i];
                manaBoxes[manaBoxIndex + 1, i] = 0;
            }
        }

        public bool TryGetFile(string cardName)
        {
            string outputFileName = cardName + ".jpg";
            string dataName = Path.GetFileNameWithoutExtension(outputFileName);
            if (cardData.Exists(x => x.Name == dataName))
            {
                return false;
            }

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
                client.DownloadFile(downloadLink, "Data\\" + outputFileName);
                CardData data = new CardData();
                data.TexturePath = Path.GetFileName(outputFileName);
                data.Name = dataName;
                cardData.Add(data);
                return true;
            }
        }

        public static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var s = p0.Y * p2.X - p0.X * p2.Y + (p2.Y - p0.Y) * p.X + (p0.X - p2.X) * p.Y;
            var t = p0.X * p1.Y - p0.Y * p1.X + (p0.Y - p1.Y) * p.X + (p1.X - p0.X) * p.Y;

            if ((s < 0) != (t < 0))
                return false;

            var A = -p1.Y * p2.X + p0.Y * (p2.X - p1.X) + p0.X * (p1.Y - p2.Y) + p1.X * p2.Y;
            if (A < 0.0)
            {
                s = -s;
                t = -t;
                A = -A;
            }
            return s > 0 && t > 0 && (s + t) < A;
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

        private void SetWindowPosition(int x, int y)
        {
            Type type = typeof(GameWindow).Assembly.GetType("Microsoft.Xna.Framework.OpenTKGameWindow");
            System.Reflection.FieldInfo field = type.GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            OpenTK.INativeWindow window = (OpenTK.INativeWindow)field.GetValue(this.Window);
            window.X = x;
            window.Y = y;
        }

        private void SaveStarredCards()
        {
            List<string> starredCardNames = cardData.Where(x => x.Starred).Select(x => x.Name).ToList();
            File.WriteAllLines("starred.txt", starredCardNames);
        }

        private void SpawnCardAndSendNetworkMessage(string cardName, string texturePath)
        {
            Entity card = SpawnCard(texturePath, NetworkIDCounter);
            network.SendCardCreateMessage(NetworkIDCounter, texturePath, card.Position);
            NetworkIDCounter++;
        }

        public Entity SpawnCard(string texturePath, int networkID)
        {
            if (!File.Exists(Path.Combine(Content.RootDirectory, texturePath)))
            {
                string cardName = Path.GetFileNameWithoutExtension(texturePath);
                if (!TryGetFile(cardName))
                {
                    throw new Exception("Couldn't aquire card " + texturePath);
                }
            }

            Entity entity = new Entity();
            entity.Type = EntityType.Card;
            entity.NetworkID = networkID;
            entity.Position = input.MousePosition;
            Card card = new Card();
            entity.TypeSpecificClass = card;
            card.Texture = Content.Load<Texture2D>(texturePath);
            Entities.Add(entity);
            entity.Depth = GetNextHeighestDepth();
            return entity;
        }

        private void SpawnCounterAndSendNetworkMessage()
        {
            Entity entity = SpawnCounter(NetworkIDCounter);
            network.SendCounterCreateMessage(NetworkIDCounter, entity.Position);
            NetworkIDCounter++;
        }

        public Entity SpawnCounter(int networkID)
        {
            Entity entity = new Entity();
            entity.Type = EntityType.Counter;
            entity.NetworkID = networkID;
            entity.Position = input.MousePosition;
            Counter counter = new Counter();
            entity.TypeSpecificClass = counter;
            Entities.Add(entity);
            entity.Depth = GetNextHeighestDepth();
            return entity;
        }

        internal void DrawLine(float width, Color color, Vector2 point1, Vector2 point2)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);

            Texture2D pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            uint[] colorData = new uint[1];
            colorData[0] = 0xFFFFFFFF;
            pixelTexture.SetData<uint>(colorData);

            spriteBatch.Draw(pixelTexture, point1, null, color, angle, Vector2.Zero, new Vector2(length, width), SpriteEffects.None, 0f);
        }
    }
}
