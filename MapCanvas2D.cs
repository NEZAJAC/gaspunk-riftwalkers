// ==========================================
// ФАЙЛ 2: РЕНДЕРЕР 2D-КАРТЫ НА ПРИМИТИВАХ
// ==========================================
using Godot;
using System;

public partial class MapCanvas2D : Node2D
{
    private DungeonManager _manager;

    public MapCanvas2D(DungeonManager manager)
    {
        _manager = manager;
    }

    public override void _Draw()
    {
        // Черный фон карты
        DrawRect(new Rect2(Vector2.Zero, GetViewportRect().Size), new Color(0.02f, 0.02f, 0.03f));

        float mapTileSize = 12.0f; // Увеличенный масштаб карты, как ты и просил!
        float mapChunkSize = DungeonMath.ChunkTiles * mapTileSize;

        Vector2 screenCenter = GetViewportRect().Size / 2;
        Vector2 mapOffset = screenCenter - new Vector2(_manager._gameCamera.Position.X, _manager._gameCamera.Position.Z) / 4.0f * mapTileSize;

        foreach (var chunk in _manager._loadedChunks.Values)
        {
            float chX = chunk.Cx * mapChunkSize + mapOffset.X;
            float chY = chunk.Cy * mapChunkSize + mapOffset.Y;

            for (int x = 0; x < DungeonMath.ChunkTiles; x++)
            {
                for (int y = 0; y < DungeonMath.ChunkTiles; y++)
                {
                    if (chunk.Grid[x, y] > 0)
                    {
                        Rect2 tileRect = new Rect2(chX + x * mapTileSize, chY + y * mapTileSize, mapTileSize, mapTileSize);
                        DrawRect(tileRect, new Color(0.13f, 0.15f, 0.18f));
                    }
                }
            }

            if (chunk.HasTeleport)
            {
                Vector2 tpPos = new Vector2(
                    chX + chunk.Teleport.X * mapTileSize + mapTileSize / 2,
                    chY + chunk.Teleport.Y * mapTileSize + mapTileSize / 2
                );
                DrawCircle(tpPos, mapTileSize * 1.5f, new Color(1.0f, 0.45f, 0.0f));
                DrawCircle(tpPos, 1.5f, new Color(1.0f, 1.0f, 1.0f));
            }

            // Границы чанка
            DrawRect(new Rect2(chX, chY, mapChunkSize, mapChunkSize), new Color(1, 0, 0, 0.15f), false);
        }
    }
}
