// Позиция игрока и управление WASD. 0x72 DungeonTileset II — спрайт knight_m.
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Outlast2D;

public class Player
{
    public int GridX { get; private set; }
    public int GridY { get; private set; }

    private float _secondsSinceLastStep;
    private const float SecondsBetweenSteps = 0.12f;

    // tile_list_v1.7 — knight_m_idle / knight_m_run (16x28)
    private static readonly Rectangle[] KnightMIdleFrames =
    {
        new Rectangle(128, 100, 16, 28),
        new Rectangle(144, 100, 16, 28),
        new Rectangle(160, 100, 16, 28),
        new Rectangle(176, 100, 16, 28),
    };

    private static readonly Rectangle[] KnightMRunFrames =
    {
        new Rectangle(192, 100, 16, 28),
        new Rectangle(208, 100, 16, 28),
        new Rectangle(224, 100, 16, 28),
        new Rectangle(240, 100, 16, 28),
    };

    private int _idleFrame;
    private int _runFrame;
    private float _idleAnimTimer;
    private float _runAnimTimer;
    private bool _wantsMove;
    /// <summary>Горизонтальное направление: true = смотрит влево (спрайт отражён).</summary>
    private bool _faceLeft;

    /// <summary>Son başarılı grid adımı (Space mermi yönü için).</summary>
    private int _lastMoveDx = 1;
    private int _lastMoveDy;

    public Player(int startGridX, int startGridY)
    {
        GridX = startGridX;
        GridY = startGridY;
    }

    /// <summary>Harita modülleri yer değiştirdikten sonra ızgara konumu güncellenir.</summary>
    public void SetGridPosition(int gridX, int gridY)
    {
        GridX = gridX;
        GridY = gridY;
    }

    /// <param name="moveSpeedMultiplier">1 = varsayılan; 2 = iki kat hızlı grid adımı.</param>
    public void Update(KeyboardState kb, TileMap map, float deltaSeconds, int keysHeld, float moveSpeedMultiplier = 1f)
    {
        _secondsSinceLastStep += deltaSeconds;
        float interval = SecondsBetweenSteps / Math.Max(moveSpeedMultiplier, 0.01f);

        _wantsMove =
            kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)
            || kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)
            || kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)
            || kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right);

        // Горизонталь (атлас в одну сторону; зеркалирование влево)
        if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))
            _faceLeft = true;
        else if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right))
            _faceLeft = false;

        if (_wantsMove)
        {
            _runAnimTimer += deltaSeconds;
            if (_runAnimTimer >= 0.07f)
            {
                _runAnimTimer = 0f;
                _runFrame = (_runFrame + 1) % KnightMRunFrames.Length;
            }
        }
        else
        {
            _idleAnimTimer += deltaSeconds;
            if (_idleAnimTimer >= 0.18f)
            {
                _idleAnimTimer = 0f;
                _idleFrame = (_idleFrame + 1) % KnightMIdleFrames.Length;
            }
        }

        if (_secondsSinceLastStep >= interval)
        {
            int prevX = GridX;
            int prevY = GridY;
            int newX = GridX;
            int newY = GridY;

            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up))
                newY -= 1;
            else if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))
                newY += 1;
            else if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))
                newX -= 1;
            else if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right))
                newX += 1;

            if (newX != GridX || newY != GridY)
            {
                if (newX >= 0 && newX < map.WidthInTiles && newY >= 0 && newY < map.HeightInTiles)
                {
                    if (!map.BlocksPlayer(newX, newY, keysHeld))
                    {
                        GridX = newX;
                        GridY = newY;
                        _lastMoveDx = GridX - prevX;
                        _lastMoveDy = GridY - prevY;
                    }
                }

                _secondsSinceLastStep = 0f;
            }
        }
    }

    /// <summary>Yön önce WASD; basılı değilse son yürüme yönü.</summary>
    public void GetShootDirection(KeyboardState kb, out int dx, out int dy)
    {
        if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up))
        {
            dx = 0;
            dy = -1;
            return;
        }

        if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))
        {
            dx = 0;
            dy = 1;
            return;
        }

        if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))
        {
            dx = -1;
            dy = 0;
            return;
        }

        if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right))
        {
            dx = 1;
            dy = 0;
            return;
        }

        dx = _lastMoveDx;
        dy = _lastMoveDy;
        if (dx == 0 && dy == 0)
        {
            dx = 1;
            dy = 0;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D characterAtlas, int tileSizePixels)
    {
        Rectangle src = _wantsMove ? KnightMRunFrames[_runFrame] : KnightMIdleFrames[_idleFrame];

        float scale = tileSizePixels / 16f;
        int destW = (int)(src.Width * scale);
        int destH = (int)(src.Height * scale);

        int px = GridX * tileSizePixels + (tileSizePixels - destW) / 2;
        int py = GridY * tileSizePixels + tileSizePixels - destH;

        var dest = new Rectangle(px, py, destW, destH);
        var flip = _faceLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.Draw(characterAtlas, dest, src, Color.White, 0f, Vector2.Zero, flip, 0f);
    }
}
