// Ana oyun: yükleme, güncelleme, çizim. Oda büyütme, harita açma, minimap burada.
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
    private RenderTarget2D _fullMapTarget;
    /// <summary>Oyuncu merkezli fener — kenarlara siyah gradient (yumuşak görüş sınırı).</summary>
    private Texture2D _flashlightVignette;

    /// <summary>Görünen alan ≈ toplam harita alanının bu kesri. 1f/8f = dar fener, 2f/8f = daha geniş.</summary>
    private const float FlashlightVisibleAreaFraction = 2f / 8f;
    private const float FlashlightFlickerSpeed = 5f;
    private const float FlashlightFlickerAmplitude = 0.07f;
    private const float FlashlightMinAreaFraction = 1f / 16f;
    private const float FlashlightMaxAreaFraction = 4f / 8f;

    private const int RevealMarkerTileX = 36;
    private const int RevealMarkerTileY = 36;

    private float _mapRevealTimer;
    private const float MapRevealDurationSeconds = 1f;
    private bool _wasOnRevealMarkerLastFrame;

    private static readonly Color RevealMarkerColor = new Color(255, 230, 80);

    private static readonly Color MiniMapBg = new Color(30, 30, 35);
    private static readonly Color MiniMapCell = new Color(55, 55, 60);
    private static readonly Color MiniMapExit = new Color(60, 200, 90);
    private static readonly Color MiniMapPlayer = new Color(220, 60, 60);

    private int _windowedW;
    private int _windowedH;
    private bool _f11WasDown;
    private bool _altEnterWasDown;

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

        const int tileSizePixels = 24;

        var built = MapData.CreateDungeonMap(tileSizePixels);
        _tileMap = built.TileMap;

        _fullMapTarget = new RenderTarget2D(
            GraphicsDevice,
            _tileMap.WidthPixels,
            _tileMap.HeightPixels,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);

        _flashlightVignette = CreateFlashlightVignetteTexture(GraphicsDevice, 512);

        _chestOpened = new bool[_tileMap.WidthInTiles, _tileMap.HeightInTiles];

        _player = new Player(8, 8);
        _keysCollected = 0;
        _gameWon = false;

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

        Console.WriteLine($"[Game1] Tam ekran: {display.Width}x{display.Height}. F11 veya Alt+Enter = pencere modu. Oyun çalışıyor!");
        Console.Out.Flush();
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
        _flashlightVignette?.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState kb = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || kb.IsKeyDown(Keys.Escape))
            Exit();

        float dt = Math.Max((float)gameTime.ElapsedGameTime.TotalSeconds, 1e-6f);
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

        bool onRevealMarker = _player.GridX == RevealMarkerTileX && _player.GridY == RevealMarkerTileY;
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

        base.Update(gameTime);
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
        GraphicsDevice.Clear(new Color(22, 16, 28));

        int sw = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int sh = GraphicsDevice.PresentationParameters.BackBufferHeight;

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (_mapRevealTimer > 0f)
        {
            var src = new Rectangle(0, 0, mapW, mapH);
            ComputeScaledDest(src.Width, src.Height, sw, sh, out Rectangle dest);
            _spriteBatch.Draw(_fullMapTarget, dest, src, Color.White);
        }
        else
        {
            float flicker = 1f
                + FlashlightFlickerAmplitude
                    * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * FlashlightFlickerSpeed);
            float areaFrac = Math.Clamp(
                FlashlightVisibleAreaFraction * flicker,
                FlashlightMinAreaFraction,
                FlashlightMaxAreaFraction);
            ComputeFlashlightSourceRect(mapW, mapH, t, areaFrac, out Rectangle flashlightSrc);
            ComputeScaledDest(flashlightSrc.Width, flashlightSrc.Height, sw, sh, out Rectangle dest);
            _spriteBatch.Draw(_fullMapTarget, dest, flashlightSrc, Color.White);

            DrawFlashlightVignetteOverlay(sw, sh);
        }

        DrawMiniMapHint();
        DrawKeysHud(sw, sh);
        if (_gameWon)
            DrawWinOverlay(sw, sh);
        _spriteBatch.End();

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

    private void DrawRevealMarkerDot(int tileSizePixels)
    {
        int dotSize = 6;
        int margin = 4;
        int px = RevealMarkerTileX * tileSizePixels + tileSizePixels - margin - dotSize;
        int py = RevealMarkerTileY * tileSizePixels + tileSizePixels - margin - dotSize;
        var r = new Rectangle(px, py, dotSize, dotSize);
        _spriteBatch.Draw(_pixelTexture, r, RevealMarkerColor);
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

    /// <summary>Oyuncu piksel merkezine göre harita uzayında görünür dikdörtgen. Alan kesri: görünür alan / tüm harita alanı.</summary>
    private void ComputeFlashlightSourceRect(int mapW, int mapH, int tileSize, float areaFraction, out Rectangle src)
    {
        float linear = MathF.Sqrt(Math.Clamp(areaFraction, 0.02f, 0.95f));
        int srcW = Math.Max(tileSize * 2, (int)(mapW * linear));
        int srcH = Math.Max(tileSize * 2, (int)(mapH * linear));
        srcW = Math.Min(srcW, mapW);
        srcH = Math.Min(srcH, mapH);

        int cx = (int)((_player.GridX + 0.5f) * tileSize);
        int cy = (int)((_player.GridY + 0.5f) * tileSize);
        int left = cx - srcW / 2;
        int top = cy - srcH / 2;
        if (left < 0) left = 0;
        if (top < 0) top = 0;
        if (left + srcW > mapW) left = mapW - srcW;
        if (top + srcH > mapH) top = mapH - srcH;
        src = new Rectangle(left, top, srcW, srcH);
    }

    private void DrawFlashlightVignetteOverlay(int sw, int sh)
    {
        if (_flashlightVignette == null) return;
        float texW = _flashlightVignette.Width;
        float texH = _flashlightVignette.Height;
        var origin = new Vector2(texW * 0.5f, texH * 0.5f);
        var pos = new Vector2(sw * 0.5f, sh * 0.5f);
        float scale = Math.Max(sw / texW, sh / texH) * 1.08f;
        _spriteBatch.Draw(_flashlightVignette, pos, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static Texture2D CreateFlashlightVignetteTexture(GraphicsDevice device, int size)
    {
        var tex = new Texture2D(device, size, size, false, SurfaceFormat.Color);
        var data = new Color[size * size];
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float maxDist = size * 0.5f * 0.99f;
        float inner = maxDist * 0.28f;
        float outer = maxDist * 0.94f;
        float invRange = outer > inner ? 1f / (outer - inner) : 0f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                float t = (d - inner) * invRange;
                if (t < 0f) t = 0f;
                else if (t > 1f) t = 1f;
                t = t * t * (3f - 2f * t);
                byte a = (byte)Math.Clamp((int)Math.Round(t * 255f), 0, 255);
                data[y * size + x] = new Color((byte)0, (byte)0, (byte)0, a);
            }
        }

        tex.SetData(data);
        return tex;
    }

    private static void GetRoomIndexForPlayer(int gridX, int gridY, out int roomX, out int roomY)
    {
        roomX = gridX / MapData.RoomStepTiles;
        roomY = gridY / MapData.RoomStepTiles;
        if (roomX > 2)
            roomX = 2;
        if (roomY > 2)
            roomY = 2;
    }

    private void DrawMiniMapHint()
    {
        int cell = 18;
        int pad = 8;
        int bx = pad;
        int by = pad;

        var bg = new Rectangle(bx - 3, by - 3, cell * 3 + 6, cell * 3 + 6);
        _spriteBatch.Draw(_pixelTexture, bg, MiniMapBg);

        for (int ry = 0; ry < 3; ry++)
        {
            for (int rx = 0; rx < 3; rx++)
            {
                var cellRect = new Rectangle(bx + rx * cell, by + ry * cell, cell - 2, cell - 2);
                Color c = MiniMapCell;
                if (rx == _tileMap.ExitRoomIndexX && ry == _tileMap.ExitRoomIndexY)
                    c = MiniMapExit;
                _spriteBatch.Draw(_pixelTexture, cellRect, c);
            }
        }

        GetRoomIndexForPlayer(_player.GridX, _player.GridY, out int px, out int py);
        var playerMark = new Rectangle(bx + px * cell + 5, by + py * cell + 5, 8, 8);
        _spriteBatch.Draw(_pixelTexture, playerMark, MiniMapPlayer);
    }
}
