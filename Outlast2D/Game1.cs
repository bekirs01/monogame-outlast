// Ana oyun: yükleme, güncelleme, çizim. Oda büyütme, harita açma, minimap burada.
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpriteFontPlus;

namespace Outlast2D;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixelTexture;
    private Texture2D[] _kenneyFloors;
    private Texture2D[] _kenneyWalls;
    private Texture2D _kenneyDoorClosed;
    private Texture2D _kenneyCrate;
    private Texture2D _playerAtlas;
    private TileMap _tileMap;
    private Player _player;
    private bool[,] _chestOpened;
    private int _animChestX = -1;
    private int _animChestY = -1;
    private float _chestAnimT;
    private const float ChestAnimDurationSeconds = 0.42f;
    private int _keysCollected;
    private int _keyFloatGx;
    private int _keyFloatGy;
    private float _keyFloatT = -1f;
    private const float KeyFloatDurationSeconds = 0.55f;
    private bool _gameWon;
    private bool _mainMenu = true;
    /// <summary>0 = Basit (küçük harita), 1 = Zor (büyük harita).</summary>
    private int _menuIndex;
    private bool _menuUpWasDown;
    private bool _menuDownWasDown;
    private bool _menuEnterWasDown;
    private bool _menuKey1WasDown;
    private bool _menuKey2WasDown;
    private MouseState _menuPrevMouse;
    private Rectangle _menuEasyRect;
    private Rectangle _menuHardRect;
    private SpriteFont _menuFont = default!;

    private const string MenuRuTitle = "Выберите сложность";
    private const string MenuRuEasyName = "Лёгкий";
    private const string MenuRuEasyDesc = "Уровень: низкий · компактная карта";
    private const string MenuRuHardName = "Сложный";
    private const string MenuRuHardDesc = "Уровень: высокий · просторная карта";
    private int _revealMarkerTileX;
    private int _revealMarkerTileY;
    private RenderTarget2D _fullMapTarget;
    /// <summary>Çarpımsal maske: merkez beyaz (harita görünsün), kenar siyah — küçük dairesel fener.</summary>
    private Texture2D _flashlightMultiplyRadial;
    private BlendState _multiplyBlend = null!;
    /// <summary>Tam ekran çarpım maskesi (dışı siyah); küçük daire buraya çizilir, sonra tüm ekran harita ile çarpılır.</summary>
    private RenderTarget2D _flashlightMaskScreen = null!;

    private float _mapRevealTimer;
    /// <summary>Orta odadaki işarete basılınca tüm 9 oda birlikte görünsün diye süre (saniye).</summary>
    private const float MapRevealDurationSeconds = 4f;
    private bool _wasOnRevealMarkerLastFrame;

    private static readonly Color RevealMarkerColor = new Color(255, 230, 80);

    private static readonly Color MiniMapBg = new Color(30, 30, 35);
    private static readonly Color MiniMapCell = new Color(55, 55, 60);
    /// <summary>Çıkış kapısının bulunduğu oda — hedef bu yeşil karedir.</summary>
    private static readonly Color MiniMapExit = new Color(40, 215, 95);
    private static readonly Color MiniMapExitBorder = new Color(180, 255, 130);
    private static readonly Color MiniMapPlayer = new Color(220, 60, 60);

    private int _windowedW;
    private int _windowedH;
    private bool _f11WasDown;
    private bool _altEnterWasDown;
    private bool _escapeWasDown;
    private bool _gamepadBackWasDown;
    private MouseState _playMousePrev;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.HardwareModeSwitch = false;
    }

    protected override void Initialize()
    {
        Window.Title = "Outlast 2D";
        Console.WriteLine("[Game1] Initialize...");
        Console.Out.Flush();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        Console.WriteLine("[Game1] LoadContent...");
        Console.Out.Flush();

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        string fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "NotoSans-Regular.ttf");
        if (!File.Exists(fontPath))
            throw new FileNotFoundException("Не найден файл шрифта меню: " + fontPath);
        byte[] fontBytes = File.ReadAllBytes(fontPath);
        // Rus menü metni + «·»: tam Kiril bloğu yerine tipik harf aralığı (atlas taşmasını önler).
        var fontBake = TtfFontBaker.Bake(
            fontBytes,
            24,
            2048,
            2048,
            new CharacterRange[]
            {
                CharacterRange.BasicLatin,
                new CharacterRange('\u00B7', '\u00B7'), // ·
                new CharacterRange('\u0410', '\u044F'), // А–я (Kiril temel)
                new CharacterRange('\u0401', '\u0401'), // Ё
                new CharacterRange('\u0451', '\u0451'), // ё
            });
        _menuFont = fontBake.CreateSpriteFont(GraphicsDevice);

        string kenneyDir = Path.Combine(AppContext.BaseDirectory, "Content", "sprites", "KenneyScribble");

        _kenneyFloors = new Texture2D[9];
        _kenneyWalls = new Texture2D[9];
        for (int i = 0; i < 9; i++)
        {
            var (floorBase, wallBase) = KenneyRoomThemes.RoomPairs[i];
            _kenneyFloors[i] = LoadTexture(Path.Combine(kenneyDir, KenneyRoomThemes.FileName(floorBase)));
            _kenneyWalls[i] = LoadTexture(Path.Combine(kenneyDir, KenneyRoomThemes.FileName(wallBase)));
        }

        _kenneyDoorClosed = LoadTexture(Path.Combine(kenneyDir, "door_closed.png"));
        _kenneyCrate = LoadTexture(Path.Combine(kenneyDir, "crate.png"));

        string playerAtlasPath = Path.Combine(AppContext.BaseDirectory, "Content", "sprites", "0x72_DungeonTilesetII_v1.7.png");
        if (!File.Exists(playerAtlasPath))
            throw new FileNotFoundException("Oyuncu sprite atlası bulunamadı. Beklenen yol: " + playerAtlasPath);
        using (var fs = File.OpenRead(playerAtlasPath))
            _playerAtlas = Texture2D.FromStream(GraphicsDevice, fs);

        _flashlightMultiplyRadial = CreateFlashlightMultiplyRadialTexture(GraphicsDevice, 1024);
        _multiplyBlend = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.DestinationAlpha,
            AlphaDestinationBlend = Blend.Zero,
            AlphaBlendFunction = BlendFunction.Add
        };

        const int tileSizePixels = 24;
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        int roomPx = MapData.TilesPerRoomSide * tileSizePixels;
        int maxSide = Math.Max(640, Math.Min(display.Width, display.Height) * 9 / 10);
        int roomsFit = Math.Max(1, maxSide / roomPx);
        _windowedW = roomsFit * roomPx;
        _windowedH = _windowedW;

        _graphics.PreferredBackBufferWidth = display.Width;
        _graphics.PreferredBackBufferHeight = display.Height;
        _graphics.IsFullScreen = true;
        _graphics.ApplyChanges();

        Console.WriteLine("[Game1] Меню: мышь — выбор зоны, клик — старт. ↑/↓ или 1/2, Enter. F11 / Alt+Enter — окно.");
        Console.Out.Flush();
    }

    private void BeginGameplay(MapDifficulty difficulty)
    {
        _fullMapTarget?.Dispose();
        _fullMapTarget = null;

        const int tileSizePixels = 24;
        var built = MapData.CreateDungeonMap(tileSizePixels, difficulty);
        _tileMap = built.TileMap;
        _revealMarkerTileX = built.RevealMarkerGridX;
        _revealMarkerTileY = built.RevealMarkerGridY;

        _fullMapTarget = new RenderTarget2D(
            GraphicsDevice,
            _tileMap.WidthPixels,
            _tileMap.HeightPixels,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);

        _chestOpened = new bool[_tileMap.WidthInTiles, _tileMap.HeightInTiles];
        _player = new Player(built.StartGridX, built.StartGridY);
        _keysCollected = 0;
        _gameWon = false;
        _animChestX = -1;
        _animChestY = -1;
        _chestAnimT = 0f;
        _keyFloatT = -1f;
        _mapRevealTimer = 0f;
        _wasOnRevealMarkerLastFrame = false;

        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        int roomPx = _tileMap.TilesPerRoomSide * tileSizePixels;
        int maxSide = Math.Max(640, Math.Min(display.Width, display.Height) * 9 / 10);
        int roomsFit = Math.Max(1, maxSide / roomPx);
        _windowedW = roomsFit * roomPx;
        _windowedH = _windowedW;

        _mainMenu = false;
        _playMousePrev = default;
        Window.Title = difficulty == MapDifficulty.Hard ? "Outlast 2D — сложный" : "Outlast 2D — лёгкий";
    }

    private void EnsureFlashlightMaskScreen(int w, int h)
    {
        if (_flashlightMaskScreen != null && _flashlightMaskScreen.Width == w && _flashlightMaskScreen.Height == h)
            return;
        _flashlightMaskScreen?.Dispose();
        _flashlightMaskScreen = new RenderTarget2D(
            GraphicsDevice,
            w,
            h,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
    }

    private void ReturnToMainMenu()
    {
        _fullMapTarget?.Dispose();
        _fullMapTarget = null;
        _flashlightMaskScreen?.Dispose();
        _flashlightMaskScreen = null;
        _mainMenu = true;
        _gameWon = false;
        _mapRevealTimer = 0f;
        Window.Title = "Outlast 2D";
    }

    private Texture2D LoadTexture(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Kenney dosyası bulunamadı: " + path);
        using var fs = File.OpenRead(path);
        return Texture2D.FromStream(GraphicsDevice, fs);
    }

    protected override void UnloadContent()
    {
        _playerAtlas?.Dispose();
        foreach (var t in _kenneyFloors ?? Array.Empty<Texture2D>())
            t?.Dispose();
        foreach (var t in _kenneyWalls ?? Array.Empty<Texture2D>())
            t?.Dispose();
        _kenneyDoorClosed?.Dispose();
        _kenneyCrate?.Dispose();
        _fullMapTarget?.Dispose();
        _flashlightMaskScreen?.Dispose();
        _flashlightMultiplyRadial?.Dispose();
        _multiplyBlend?.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState kb = Keyboard.GetState();
        MouseState mouse = Mouse.GetState();

        bool esc = kb.IsKeyDown(Keys.Escape);
        if (esc && !_escapeWasDown)
        {
            if (_mainMenu)
                Exit();
            else
                ReturnToMainMenu();
        }
        _escapeWasDown = esc;

        var gp = GamePad.GetState(PlayerIndex.One);
        if (gp.Buttons.Back == ButtonState.Pressed && !_gamepadBackWasDown && !_mainMenu)
            ReturnToMainMenu();
        _gamepadBackWasDown = gp.Buttons.Back == ButtonState.Pressed;

        float dt = Math.Max((float)gameTime.ElapsedGameTime.TotalSeconds, 1e-6f);

        if (_mainMenu)
        {
            var pp = GraphicsDevice.PresentationParameters;
            UpdateMainMenu(kb, mouse, pp.BackBufferWidth, pp.BackBufferHeight);
            bool f11Menu = kb.IsKeyDown(Keys.F11);
            if (f11Menu && !_f11WasDown)
                ToggleFullscreen();
            _f11WasDown = f11Menu;
            bool altEnterMenu = (kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt)) && kb.IsKeyDown(Keys.Enter);
            if (altEnterMenu && !_altEnterWasDown)
                ToggleFullscreen();
            _altEnterWasDown = altEnterMenu;
            base.Update(gameTime);
            return;
        }

        if (!_gameWon)
        {
            _player.Update(kb, _tileMap, dt, _keysCollected);
            UpdateChestKeysAndWin(dt);
        }

        if (_keyFloatT >= 0f)
        {
            _keyFloatT += dt;
            if (_keyFloatT >= KeyFloatDurationSeconds)
                _keyFloatT = -1f;
        }

        bool onRevealMarker = _player.GridX == _revealMarkerTileX && _player.GridY == _revealMarkerTileY;
        if (onRevealMarker && !_wasOnRevealMarkerLastFrame)
            _mapRevealTimer = MapRevealDurationSeconds;
        _wasOnRevealMarkerLastFrame = onRevealMarker;

        if (_mapRevealTimer > 0f)
            _mapRevealTimer -= dt;
        if (_mapRevealTimer < 0f)
            _mapRevealTimer = 0f;

        bool f11 = kb.IsKeyDown(Keys.F11);
        if (f11 && !_f11WasDown)
            ToggleFullscreen();
        _f11WasDown = f11;

        bool altEnter = (kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt)) && kb.IsKeyDown(Keys.Enter);
        if (altEnter && !_altEnterWasDown)
            ToggleFullscreen();
        _altEnterWasDown = altEnter;

        var pp2 = GraphicsDevice.PresentationParameters;
        Rectangle backR = GetBackButtonRect(pp2.BackBufferWidth, pp2.BackBufferHeight);
        MapMouseToBackBuffer(mouse, out int mx, out int my);
        // Basma anında tetikle (release beklemek trackpad’de kaçırılabiliyor); önceki kare Released idi.
        bool backPress = mouse.LeftButton == ButtonState.Pressed
            && _playMousePrev.LeftButton == ButtonState.Released;
        if (backPress && backR.Contains(mx, my))
            ReturnToMainMenu();
        _playMousePrev = mouse;

        base.Update(gameTime);
    }

    private void SyncMainMenuLayout(int screenW, int screenH)
    {
        int bandH = screenH / 3;
        _menuEasyRect = new Rectangle(0, screenH / 2 - bandH - 20, screenW, bandH);
        _menuHardRect = new Rectangle(0, screenH / 2 + 20, screenW, bandH);
    }

    private void UpdateMainMenu(KeyboardState kb, MouseState mouse, int screenW, int screenH)
    {
        SyncMainMenuLayout(screenW, screenH);

        if (_menuEasyRect.Contains(mouse.X, mouse.Y))
            _menuIndex = 0;
        else if (_menuHardRect.Contains(mouse.X, mouse.Y))
            _menuIndex = 1;

        bool click = mouse.LeftButton == ButtonState.Released
            && _menuPrevMouse.LeftButton == ButtonState.Pressed;
        if (click)
        {
            if (_menuEasyRect.Contains(mouse.X, mouse.Y))
                BeginGameplay(MapDifficulty.Easy);
            else if (_menuHardRect.Contains(mouse.X, mouse.Y))
                BeginGameplay(MapDifficulty.Hard);
        }

        _menuPrevMouse = mouse;

        bool up = kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W);
        bool down = kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S);
        if (up && !_menuUpWasDown)
            _menuIndex = Math.Max(0, _menuIndex - 1);
        if (down && !_menuDownWasDown)
            _menuIndex = Math.Min(1, _menuIndex + 1);
        _menuUpWasDown = up;
        _menuDownWasDown = down;

        bool k1 = kb.IsKeyDown(Keys.D1) || kb.IsKeyDown(Keys.NumPad1);
        bool k2 = kb.IsKeyDown(Keys.D2) || kb.IsKeyDown(Keys.NumPad2);
        if (k1 && !_menuKey1WasDown)
            _menuIndex = 0;
        if (k2 && !_menuKey2WasDown)
            _menuIndex = 1;
        _menuKey1WasDown = k1;
        _menuKey2WasDown = k2;

        bool enter = kb.IsKeyDown(Keys.Enter);
        if (enter && !_menuEnterWasDown)
            BeginGameplay(_menuIndex == 0 ? MapDifficulty.Easy : MapDifficulty.Hard);

        _menuEnterWasDown = enter;
    }

    private void DrawMenuBandText(Rectangle band, string line1, string line2, Color c1, Color c2)
    {
        if (_menuFont == null)
            return;

        Vector2 m1 = _menuFont.MeasureString(line1);
        Vector2 m2 = _menuFont.MeasureString(line2);
        float gap = 8f;
        float totalH = m1.Y + m2.Y + gap;
        float y = band.Y + (band.Height - totalH) * 0.5f;
        float x1 = band.X + (band.Width - m1.X) * 0.5f;
        float x2 = band.X + (band.Width - m2.X) * 0.5f;
        _spriteBatch.DrawString(_menuFont, line1, new Vector2(x1, y), c1);
        _spriteBatch.DrawString(_menuFont, line2, new Vector2(x2, y + m1.Y + gap), c2);
    }

    private void UpdateChestKeysAndWin(float dt)
    {
        int x = _player.GridX;
        int y = _player.GridY;

        bool onChest = _tileMap.GetTileId(x, y) == 3 && !_chestOpened[x, y];
        if (onChest)
        {
            if (_animChestX != x || _animChestY != y)
            {
                _animChestX = x;
                _animChestY = y;
                _chestAnimT = 0f;
            }

            _chestAnimT += dt / ChestAnimDurationSeconds;
            if (_chestAnimT >= 1f)
            {
                _chestOpened[x, y] = true;
                _keysCollected++;
                if (_keysCollected > DungeonAtlasSprites.KeysToWin)
                    _keysCollected = DungeonAtlasSprites.KeysToWin;
                _keyFloatGx = x;
                _keyFloatGy = y;
                _keyFloatT = 0f;
                _chestAnimT = 0f;
                _animChestX = -1;
                _animChestY = -1;
            }
        }
        else
        {
            if (_animChestX >= 0 && _animChestY >= 0 && !_chestOpened[_animChestX, _animChestY])
                _chestAnimT = 0f;
            _animChestX = -1;
            _animChestY = -1;
        }

        if (_keysCollected >= DungeonAtlasSprites.KeysToWin
            && _tileMap.GetTileId(x, y) == 4)
        {
            _gameWon = true;
            Window.Title = "Outlast 2D — KAZANDIN!";
            Console.WriteLine("KAZANDIN! Üç anahtar ile çıkışa ulaştın.");
            Console.Out.Flush();
        }
    }

    private void ToggleFullscreen()
    {
        if (_graphics.IsFullScreen)
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = _windowedW;
            _graphics.PreferredBackBufferHeight = _windowedH;
        }
        else
        {
            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = mode.Width;
            _graphics.PreferredBackBufferHeight = mode.Height;
            _graphics.IsFullScreen = true;
        }

        _graphics.ApplyChanges();
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_mainMenu)
        {
            int menuW = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int menuH = GraphicsDevice.PresentationParameters.BackBufferHeight;
            GraphicsDevice.Clear(new Color(14, 12, 20));
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawMainMenu(menuW, menuH);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        int t = _tileMap.TileSizePixels;
        int mapW = _tileMap.WidthPixels;
        int mapH = _tileMap.HeightPixels;

        GraphicsDevice.SetRenderTarget(_fullMapTarget);
        GraphicsDevice.Clear(new Color(18, 14, 22));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _tileMap.Draw(
            _spriteBatch,
            _kenneyFloors,
            _kenneyWalls,
            _kenneyDoorClosed,
            _playerAtlas,
            _kenneyCrate,
            _chestOpened,
            _animChestX,
            _animChestY,
            _chestAnimT,
            _keysCollected);
        DrawRevealMarkerDot(t);
        _player.Draw(_spriteBatch, _playerAtlas, t);
        DrawKeyFloatPickup(t);
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        // Fener modu: tam ekran siyah; oyuncu ekrandaki konumunda küçük daire içi görünür (çarpımsal maske).
        if (_mapRevealTimer > 0f)
            GraphicsDevice.Clear(new Color(22, 16, 28));
        else
            GraphicsDevice.Clear(Color.Black);

        int sw = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int sh = GraphicsDevice.PresentationParameters.BackBufferHeight;

        if (_mapRevealTimer > 0f)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            var src = new Rectangle(0, 0, mapW, mapH);
            ComputeScaledDest(src.Width, src.Height, sw, sh, out Rectangle dest);
            _spriteBatch.Draw(_fullMapTarget, dest, src, Color.White);
            DrawMiniMapHint();
            DrawKeysHud(sw, sh);
            DrawBackToMapButton(sw, sh);
            if (_gameWon)
                DrawWinOverlay(sw, sh);
            _spriteBatch.End();
        }
        else
        {
            // Tek oda tam ekrana ölçeklenir; daire maskesi tam ekran RT’ye yazılır (dışı 0), sonra harita ile çarpılır — aksi halde küçük dikdörtgen dışı harita kalırdı.
            ComputeFlashlightSourceRect(mapW, mapH, t, out Rectangle flashlightSrc);
            ComputeScaledDest(flashlightSrc.Width, flashlightSrc.Height, sw, sh, out Rectangle dest);

            float pCx = (_player.GridX + 0.5f) * t;
            float pCy = (_player.GridY + 0.5f) * t;
            MapWorldPixelsToScreen(pCx, pCy, flashlightSrc, dest, out float scrX, out float scrY);

            float tilesW = flashlightSrc.Width / (float)t;
            float pxPerTile = dest.Width / tilesW;
            const float flashlightDiameterTiles = 9f;
            float diameter = flashlightDiameterTiles * pxPerTile;
            float radius = Math.Clamp(diameter * 0.5f, 40f, Math.Min(sw, sh) * 0.48f);
            int d = Math.Max(8, (int)Math.Ceiling(radius * 2f));
            var maskDest = new Rectangle(
                (int)Math.Round(scrX - d * 0.5f),
                (int)Math.Round(scrY - d * 0.5f),
                d,
                d);

            EnsureFlashlightMaskScreen(sw, sh);
            GraphicsDevice.SetRenderTarget(_flashlightMaskScreen);
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            _spriteBatch.Draw(_flashlightMultiplyRadial, maskDest, Color.White);
            _spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);

            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_fullMapTarget, dest, flashlightSrc, Color.White);
            _spriteBatch.End();

            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                _multiplyBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);
            _spriteBatch.Draw(
                _flashlightMaskScreen,
                new Rectangle(0, 0, sw, sh),
                new Rectangle(0, 0, sw, sh),
                Color.White);
            _spriteBatch.End();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawMiniMapHint();
            DrawKeysHud(sw, sh);
            DrawBackToMapButton(sw, sh);
            if (_gameWon)
                DrawWinOverlay(sw, sh);
            _spriteBatch.End();
        }

        base.Draw(gameTime);
    }

    private void DrawKeyFloatPickup(int tileSizePixels)
    {
        if (_keyFloatT < 0f || _keyFloatT > KeyFloatDurationSeconds)
            return;

        float u = _keyFloatT / KeyFloatDurationSeconds;
        int cx = _keyFloatGx * tileSizePixels + tileSizePixels / 2;
        int cy = _keyFloatGy * tileSizePixels + tileSizePixels / 2;
        int offY = (int)(-30f * u);
        byte a = (byte)(255 * (1f - u * u));
        int sz = Math.Max(12, (int)(tileSizePixels * 0.55f));
        var dest = new Rectangle(cx - sz / 2, cy + offY - sz / 2, sz, sz);
        var color = new Color((byte)255, (byte)245, (byte)180, a);
        _spriteBatch.Draw(_playerAtlas, dest, DungeonAtlasSprites.CoinKey, color);
    }

    private void DrawKeysHud(int screenW, int screenH)
    {
        int pad = 10;
        int box = 18;
        int gap = 5;
        int n = DungeonAtlasSprites.KeysToWin;
        int startX = screenW - pad - n * box - (n - 1) * gap;
        for (int i = 0; i < n; i++)
        {
            var outline = new Rectangle(startX + i * (box + gap), pad, box, box);
            _spriteBatch.Draw(_pixelTexture, outline, new Color(28, 26, 34, 230));
            if (_keysCollected > i)
            {
                int inset = 4;
                var inner = new Rectangle(outline.X + inset, outline.Y + inset, box - 2 * inset, box - 2 * inset);
                _spriteBatch.Draw(_pixelTexture, inner, new Color(210, 175, 55));
                _spriteBatch.Draw(_playerAtlas, inner, DungeonAtlasSprites.CoinKey, Color.White);
            }
        }
    }

    private void DrawWinOverlay(int screenW, int screenH)
    {
        var full = new Rectangle(0, 0, screenW, screenH);
        _spriteBatch.Draw(_pixelTexture, full, new Color(25, 90, 45, 100));
    }

    private void DrawMainMenu(int screenW, int screenH)
    {
        SyncMainMenuLayout(screenW, screenH);

        _spriteBatch.Draw(_pixelTexture, _menuEasyRect, new Color(35, 55, 40, 220));
        _spriteBatch.Draw(_pixelTexture, _menuHardRect, new Color(55, 35, 40, 220));

        if (_menuIndex == 0)
            _spriteBatch.Draw(_pixelTexture, new Rectangle(_menuEasyRect.X + 8, _menuEasyRect.Y + 8, _menuEasyRect.Width - 16, _menuEasyRect.Height - 16), new Color(120, 200, 130, 80));
        else
            _spriteBatch.Draw(_pixelTexture, new Rectangle(_menuHardRect.X + 8, _menuHardRect.Y + 8, _menuHardRect.Width - 16, _menuHardRect.Height - 16), new Color(200, 120, 100, 80));

        int barW = Math.Min(420, screenW - 40);
        int barH = 8;
        int bx = (screenW - barW) / 2;
        int by = 24;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bx, by, barW, barH), new Color(50, 48, 58));
        int seg = (barW - 4) / 2;
        if (_menuIndex == 0)
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bx + 2, by + 1, seg - 2, barH - 2), new Color(180, 200, 120));
        else
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bx + 2 + seg, by + 1, seg - 2, barH - 2), new Color(200, 140, 100));

        if (_menuFont != null)
        {
            Vector2 titleM = _menuFont.MeasureString(MenuRuTitle);
            _spriteBatch.DrawString(
                _menuFont,
                MenuRuTitle,
                new Vector2((screenW - titleM.X) * 0.5f, 80f),
                new Color(235, 235, 245));

            DrawMenuBandText(_menuEasyRect, MenuRuEasyName, MenuRuEasyDesc, Color.White, new Color(200, 220, 205));
            DrawMenuBandText(_menuHardRect, MenuRuHardName, MenuRuHardDesc, new Color(255, 235, 225), new Color(220, 195, 188));
        }
    }

    private void DrawRevealMarkerDot(int tileSizePixels)
    {
        int dotSize = 6;
        int margin = 4;
        int px = _revealMarkerTileX * tileSizePixels + tileSizePixels - margin - dotSize;
        int py = _revealMarkerTileY * tileSizePixels + tileSizePixels - margin - dotSize;
        var r = new Rectangle(px, py, dotSize, dotSize);
        _spriteBatch.Draw(_pixelTexture, r, RevealMarkerColor);
    }

    private static void MapWorldPixelsToScreen(float worldPx, float worldPy, Rectangle src, Rectangle dest, out float sx, out float sy)
    {
        float rx = (worldPx - src.Left) / src.Width;
        float ry = (worldPy - src.Top) / src.Height;
        sx = dest.X + rx * dest.Width;
        sy = dest.Y + ry * dest.Height;
    }

    private static void ComputeScaledDest(int srcW, int srcH, int screenW, int screenH, out Rectangle dest)
    {
        int ix = screenW / srcW;
        int iy = screenH / srcH;
        int intScale = Math.Min(ix, iy);
        if (intScale >= 1)
        {
            int dw = srcW * intScale;
            int dh = srcH * intScale;
            dest = new Rectangle((screenW - dw) / 2, (screenH - dh) / 2, dw, dh);
            return;
        }

        float f = Math.Min((float)screenW / srcW, (float)screenH / srcH);
        int dwf = Math.Max(1, (int)Math.Floor(srcW * f));
        int dhf = Math.Max(1, (int)Math.Floor(srcH * f));
        dest = new Rectangle((screenW - dwf) / 2, (screenH - dhf) / 2, dwf, dhf);
    }

    /// <summary>
    /// Ekranda yalnızca oyuncunun bulunduğu tek oda (minimapteki bir kare); oda değişince kaynak dikdörtgen o odaya sıçrar.
    /// </summary>
    private void ComputeFlashlightSourceRect(int mapW, int mapH, int tileSize, out Rectangle src)
    {
        int step = _tileMap.RoomStepTiles;
        int tw = _tileMap.WidthInTiles;
        int th = _tileMap.HeightInTiles;

        _tileMap.GetRoomIndexForTile(_player.GridX, _player.GridY, out int rx, out int ry);

        int tileLeft = rx * step;
        int tileTop = ry * step;
        int wTiles = rx < 2 ? step : tw - tileLeft;
        int hTiles = ry < 2 ? step : th - tileTop;

        int left = tileLeft * tileSize;
        int top = tileTop * tileSize;
        int srcW = Math.Min(wTiles * tileSize, mapW - left);
        int srcH = Math.Min(hTiles * tileSize, mapH - top);
        if (srcW < 1) srcW = 1;
        if (srcH < 1) srcH = 1;
        src = new Rectangle(left, top, srcW, srcH);
    }

    /// <summary>
    /// SDL fare konumu bazen ClientBounds ölçüsünde; HUD ve çizim BackBuffer pikselinde. macOS Retina’da eşleşmezse düğme tıklanmıyor.
    /// </summary>
    private void MapMouseToBackBuffer(MouseState mouse, out int bx, out int by)
    {
        int bbw = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int bbh = GraphicsDevice.PresentationParameters.BackBufferHeight;
        var bounds = Window.ClientBounds;
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            bx = (int)Math.Round(mouse.X * (double)bbw / bounds.Width);
            by = (int)Math.Round(mouse.Y * (double)bbh / bounds.Height);
        }
        else
        {
            bx = mouse.X;
            by = mouse.Y;
        }

        bx = Math.Clamp(bx, 0, Math.Max(0, bbw - 1));
        by = Math.Clamp(by, 0, Math.Max(0, bbh - 1));
    }

    private static Rectangle GetBackButtonRect(int screenW, int screenH)
    {
        const int pad = 14;
        const int w = 176;
        const int h = 44;
        return new Rectangle(pad, screenH - pad - h, w, h);
    }

    private void DrawBackToMapButton(int screenW, int screenH)
    {
        var r = GetBackButtonRect(screenW, screenH);
        _spriteBatch.Draw(_pixelTexture, r, new Color(42, 40, 52, 245));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), new Color(55, 52, 68, 200));
        if (_menuFont == null)
            return;

        const string label = "Назад";
        Vector2 m = _menuFont.MeasureString(label);
        _spriteBatch.DrawString(
            _menuFont,
            label,
            new Vector2(r.X + (r.Width - m.X) * 0.5f, r.Y + (r.Height - m.Y) * 0.5f),
            new Color(230, 228, 242));
    }

    /// <summary>Stok görseldeki gibi yumuşak kenarlı radyal geçiş (0–1).</summary>
    private static float Smoothstep01(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Çarpımsal karıştırma için: merkez RGB≈1 (harita korunur), kenar 0 (siyah). Üst üste çizilir: çıktı = harita × maske.
    /// </summary>
    private static Texture2D CreateFlashlightMultiplyRadialTexture(GraphicsDevice device, int size)
    {
        var tex = new Texture2D(device, size, size, false, SurfaceFormat.Color);
        var data = new Color[size * size];
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float maxDist = size * 0.5f * 0.998f;
        const float fadeStartNorm = 0.1f;
        const float fadeEndNorm = 0.98f;
        float invFade = fadeEndNorm > fadeStartNorm ? 1f / (fadeEndNorm - fadeStartNorm) : 0f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                float r = maxDist > 0f ? d / maxDist : 0f;
                if (r > 1f) r = 1f;

                float t = (r - fadeStartNorm) * invFade;
                if (t < 0f) t = 0f;
                else if (t > 1f) t = 1f;
                t = Smoothstep01(Smoothstep01(t));
                float factor = 1f - t;
                byte b = (byte)Math.Clamp((int)Math.Round(factor * 255f), 0, 255);
                data[y * size + x] = new Color(b, b, b, (byte)255);
            }
        }

        tex.SetData(data);
        return tex;
    }

    private void DrawMiniMapHint()
    {
        int cell = 18;
        int pad = 8;
        int bx = pad;
        int by = pad;
        int n = _tileMap.RoomsPerSide;

        var bg = new Rectangle(bx - 3, by - 3, cell * n + 6, cell * n + 6);
        _spriteBatch.Draw(_pixelTexture, bg, MiniMapBg);

        int ex = _tileMap.ExitRoomIndexX;
        int ey = _tileMap.ExitRoomIndexY;

        for (int ry = 0; ry < n; ry++)
        {
            for (int rx = 0; rx < n; rx++)
            {
                var cellRect = new Rectangle(bx + rx * cell, by + ry * cell, cell - 2, cell - 2);
                Color c = MiniMapCell;
                if (rx == ex && ry == ey)
                    c = MiniMapExit;
                _spriteBatch.Draw(_pixelTexture, cellRect, c);
            }
        }

        // Çıkış odası: ince çerçeve — yeşil kare = gidilecek çıkış bölgesi.
        {
            var exitOuter = new Rectangle(bx + ex * cell - 1, by + ey * cell - 1, cell, cell);
            _spriteBatch.Draw(_pixelTexture, exitOuter, MiniMapExitBorder);
            var exitInner = new Rectangle(exitOuter.X + 1, exitOuter.Y + 1, exitOuter.Width - 2, exitOuter.Height - 2);
            Color fill = MiniMapExit;
            _spriteBatch.Draw(_pixelTexture, exitInner, fill);
        }

        _tileMap.GetRoomIndexForTile(_player.GridX, _player.GridY, out int px, out int py);
        var playerMark = new Rectangle(bx + px * cell + 5, by + py * cell + 5, 8, 8);
        _spriteBatch.Draw(_pixelTexture, playerMark, MiniMapPlayer);
    }
}
