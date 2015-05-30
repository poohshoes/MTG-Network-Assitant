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
        WebClient client = new WebClient();
        KeyboardState keyboardState;
        KeyboardState lastKeyboardState;
        static Texture2D pixelTexture;

        public static Color tileColor = new Color(55, 55, 55);
        Color backgroundColor = new Color(35, 35, 35);
        Color textColor = Color.White;
        public static Color flareColor = new Color(33, 44, 188);
        public static Color flareHighlightColor = new Color(73, 84, 226);
        Color searchTextColor = Color.White;

        Renderer renderer;

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

            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            uint[] colorData = new uint[1];
            colorData[0] = 0xFFFFFFFF;
            pixelTexture.SetData<uint>(colorData);
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
            renderer = new Renderer(GraphicsDevice);

            Content.RootDirectory = "Data";

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
            GraphicsDevice.Clear(backgroundColor);

            UpdateAndDraw(gameTime, IMGUIPass.Draw);

            renderer.End();

            base.Draw(gameTime);
        }

        // For card selection.
        char shownLetter = (char)0;

        public int[,] manaBoxes = new int[4, 5];
        List<string> manaTextures = new List<string>() { "Mana\\greenMana.png", "Mana\\redMana.png", "Mana\\blueMana.png", "Mana\\blackMana.png", "Mana\\whiteMana.png" };
        int manaTextureSize = 30;
        int manaBoxWidthInMana = 5;
        int manaBoxHeightInMana = 5;

        public void UpdateAndDraw(GameTime gameTime, IMGUIPass pass)
        {
            // CARD CREATION
            bool mouseActionConsumed = false;
            Panel panel = new Panel(new Vector2(5, 0));
            panel.Font = font;
            int start = (int)'A';
            int end = (int)'Z';
            for (int i = start; i <= end; i++)
            {
                if (panel.DoClickableText(renderer, pass, input, ((char)i).ToString()))
                {
                    shownLetter = (char)i;
                    mouseActionConsumed = true;
                }
            }
            if (panel.DoClickableText(renderer, pass, input, "*"))
            {
                shownLetter = '*';
                mouseActionConsumed = true;
            }
            if (panel.DoClickableText(renderer, pass, input, "C"))
            {
                SpawnCounterAndSendNetworkMessage();
                mouseActionConsumed = true;
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
                            searchTextColor = textColor;
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
            panel.DoText(renderer, pass, searchText, searchTextColor);
            panel.Row();

            if (input.RightMouseEngaged() && shownLetter != (char)0)
            {
                shownLetter = (char)0;
                mouseActionConsumed = true;
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
                    Color starColor = Color.White;
                    if (!card.Starred)
                        starColor = Color.Black;
                    if (panel.DoClickableText(renderer, pass, input, "*", starColor))
                    {
                        card.Starred = !card.Starred;
                        SaveStarredCards();
                    }
                    if (panel.DoClickableText(renderer, pass, input, card.Name))
                    {
                        SpawnCardAndSendNetworkMessage(card.Name, card.TexturePath);
                        shownLetter = (char)0;
                        mouseActionConsumed = true;
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

                int manaBoxWidth = manaBoxWidthInMana * manaTextureSize;
                int totalManaBoxWidth = 2 * manaBoxWidth + manaTextureSize;
                int totalManaBoxHeight = manaTextureSize * manaBoxHeightInMana;

                float startX = graphics.PreferredBackBufferWidth - totalManaBoxWidth - manaTextureSize;
                Vector2[] refreshButtonPositions = new Vector2[2] { new Vector2(startX, 0), new Vector2(startX, graphics.PreferredBackBufferHeight - totalManaBoxHeight) };
                int[] avaliableManaIndex = new int[2] { manaBoxIndex_TopAvaliable, manaBoxIndex_BottomAvaliable };
                int[] usedManaIndex = new int[2] { manaBoxIndex_TopUsed, manaBoxIndex_BottomUsed };
                for(int i = 0; i <= 1; i++)
                {
                    // Refresh Button
                    Rectangle refreshButtonTextureRectangle = new Rectangle((int)refreshButtonPositions[i].X, (int)refreshButtonPositions[i].Y, (int)manaTextureSize, (int)manaTextureSize);
                    switch (pass)
                    {
                        case IMGUIPass.Draw:
                            Texture2D refreshManaTexture = Content.Load<Texture2D>("Mana\\refresh.png");
                            renderer.Draw(refreshManaTexture, refreshButtonTextureRectangle, Color.White);
                            break;
                        case IMGUIPass.Update:
                            if (input.LeftMouseDisengaged() && Game1.PointInRectangle(input.MousePosition, refreshButtonTextureRectangle))
                            {
                                RefreshMana(avaliableManaIndex[i]);
                                mouseActionConsumed = true;
                                network.SendRefreshMana(avaliableManaIndex[i]);
                            }
                            break;
                    }

                    Vector2 availiableManaBoxPosition = refreshButtonPositions[i] + new Vector2(manaTextureSize, 0);
                    Vector2 usedManaBoxPosition = availiableManaBoxPosition + new Vector2(manaBoxWidth, 0);

                    Vector2[] manaBoxPosition = new Vector2[2] { availiableManaBoxPosition, usedManaBoxPosition };
                    int[] myManaIndex = new int[2] { avaliableManaIndex[i], usedManaIndex[i] };
                    int[] swapManaIndex = new int[2] { usedManaIndex[i], avaliableManaIndex[i] };
                    Color[] textureColor = new Color[2] { Color.White, Color.Gray };
                    for(int j = 0; j <= 1; j++)
                    {
                        Vector2 currentPosition = new Vector2(manaBoxPosition[j].X, manaBoxPosition[j].Y);
                        int column = 0;
                        for (int manaType = 0; manaType < 5; manaType++)
                        {
                            Texture2D manaTexture = Content.Load<Texture2D>(manaTextures[manaType]);
                            for (int mana = 0; mana < manaBoxes[myManaIndex[j], manaType]; mana++)
                            {
                                Rectangle manaTextureRectangle = new Rectangle((int)currentPosition.X, (int)currentPosition.Y, (int)manaTextureSize, (int)manaTextureSize);
                                switch (pass)
                                {
                                    case IMGUIPass.Draw:
                                        renderer.Draw(manaTexture, manaTextureRectangle, textureColor[j]);
                                        break;
                                    case IMGUIPass.Update:
                                        if (Game1.PointInRectangle(input.MousePosition, manaTextureRectangle))
                                        {
                                            if (input.LeftMouseDisengaged())
                                            {
                                                manaBoxes[myManaIndex[j], manaType]--;
                                                manaBoxes[swapManaIndex[j], manaType]++;
                                                mouseActionConsumed = true;
                                                network.SendTransferMana(myManaIndex[j], swapManaIndex[j], manaType);
                                            }
                                            else if (input.RightMouseDisengaged())
                                            {
                                                manaBoxes[myManaIndex[j], manaType]--;
                                                mouseActionConsumed = true;
                                                network.SendRemoveMana(myManaIndex[j], manaType);
                                            }
                                        }
                                        break;
                                }
                                currentPosition.X += manaTextureSize;
                                column++;
                                if (column >= manaBoxWidthInMana)
                                {
                                    column = 0;
                                    currentPosition.X = manaBoxPosition[j].X;
                                    currentPosition.Y += manaTextureSize;
                                }
                            }
                        }
                    }

                    // Add Mana Buttons
                    Vector2 addManaBoxPosition = usedManaBoxPosition + new Vector2(manaBoxWidth, 0);
                    for (int manaType = 0; manaType < manaTextures.Count; manaType++)
                    {
                        Rectangle manaTextureRectangle = new Rectangle((int)addManaBoxPosition.X,
                                                                       (int)addManaBoxPosition.Y + manaType * manaTextureSize, 
                                                                       (int)manaTextureSize, 
                                                                       (int)manaTextureSize);
                        switch (pass)
                        {
                            case IMGUIPass.Draw:
                                Texture2D manaTexture = Content.Load<Texture2D>(manaTextures[manaType]);
                                renderer.Draw(manaTexture, manaTextureRectangle, Color.White);
                                break;
                            case IMGUIPass.Update:
                                if (input.LeftMouseDisengaged() && Game1.PointInRectangle(input.MousePosition, manaTextureRectangle))
                                {
                                    manaBoxes[avaliableManaIndex[i], manaType]++;
                                    mouseActionConsumed = true;
                                    network.SendCreateMana(avaliableManaIndex[i], manaType);
                                }
                                break;
                        }
                    }

                    // Blue Lines
                    if (pass == IMGUIPass.Draw)
                    {
                        Rectangle manaBoxRectangle = new Rectangle((int)availiableManaBoxPosition.X, (int)availiableManaBoxPosition.Y, totalManaBoxWidth, totalManaBoxHeight);
                        renderer.DrawRectangleOutline(2, flareColor, manaBoxRectangle);
                        renderer.DrawLine(2, flareColor, usedManaBoxPosition, usedManaBoxPosition + new Vector2(0, totalManaBoxHeight));
                        renderer.DrawLine(2, flareColor, addManaBoxPosition, addManaBoxPosition + new Vector2(0, totalManaBoxHeight));
                        renderer.DrawRectangle(manaBoxRectangle, tileColor);
                    }
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
                    }
                }
            }

            Entities.Sort(new EntityComparerReverse());
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
                            renderer.Draw(card.Texture, entity.Position, Color.White, rotation, card.GetHalf());
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
                            Rectangle textAreaRectangle = new Rectangle((int)entity.Position.X, 
                                                                        (int)entity.Position.Y + Counter.Buttonheight, 
                                                                        Counter.TextAreaWidth, 
                                                                        Counter.TextAreaHeight);
                            
                            // Top arrow
                            renderer.DrawLine(1, flareColor, new Vector2(textAreaRectangle.Left, textAreaRectangle.Top), topArrowPoint);
                            renderer.DrawLine(1, flareColor, topArrowPoint, new Vector2(textAreaRectangle.Right, textAreaRectangle.Top));

                            // Bottom arrow
                            renderer.DrawLine(1, flareColor, new Vector2(textAreaRectangle.Left, textAreaRectangle.Bottom), bottomArrowPoint);
                            renderer.DrawLine(1, flareColor, bottomArrowPoint, new Vector2(textAreaRectangle.Right, textAreaRectangle.Bottom));

                            string text = counter.Value.ToString();
                            SpriteFont fontToUse = font;
                            Vector2 centerOffset = (new Vector2(Counter.TextAreaWidth, Counter.TextAreaHeight) - fontToUse.MeasureString(text)) / 2f;
                            renderer.DrawString(fontToUse, text, new Vector2(textAreaRectangle.Left, textAreaRectangle.Top) + centerOffset, textColor);

                            renderer.DrawRectangle(textAreaRectangle, Game1.tileColor);
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
                                    }
                                }
                                else if(input.LeftMouseDisengaged())
                                {
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
                                    }
                                }
                            }
                        }
                        break;
                }
            }
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
    }
}
