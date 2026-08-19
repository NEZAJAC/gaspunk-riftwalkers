// ==========================================
// ФАЙЛ 3: ГЛАВНЫЙ МЕНЕДЖЕР И СТРОИТЕЛЬ 3D
// ==========================================
using Godot;
using System;
using System.Collections.Generic;

public partial class DungeonManager : Node3D
{
    private const float TileSize3D = 4.0f; 
    private const float WallHeight3D = 3.0f; 
    private const float WallThickness3D = 0.5f; 

    private bool _isMapMode = false;
    public Camera3D _gameCamera;
    private float _speed = 45.0f; 
    public Dictionary<string, DungeonMath.ChunkData> _loadedChunks = new Dictionary<string, DungeonMath.ChunkData>();
    private MapCanvas2D _mapCanvas;

    public override void _Ready()
    {
        Engine.MaxFps = 60;
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);

        _gameCamera = new Camera3D();
        AddChild(_gameCamera);
        _gameCamera.Position = new Vector3((DungeonMath.ChunkTiles * TileSize3D) / 2, 25.0f, (DungeonMath.ChunkTiles * TileSize3D) / 2);
        _gameCamera.RotationDegrees = new Vector3(-60, 0, 0);

        BuildChunk3D(0, 0);
    }

    public void BuildChunk3D(int cx, int cy)
    {
        DungeonMath.ChunkData chunk = DungeonMath.GenerateChunk(cx, cy, _loadedChunks);
        if (chunk.Chunk3DNode != null) return;

        Node3D chunkRoot = new Node3D();
        chunkRoot.Name = $"Chunk_{cx}_{cy}";
        AddChild(chunkRoot);
        chunk.Chunk3DNode = chunkRoot;

        float offset = DungeonMath.ChunkTiles * TileSize3D;
        Vector3 chunkOffset = new Vector3(cx * offset, 0, cy * offset);

        for (int x = 0; x < DungeonMath.ChunkTiles; x++)
        {
            for (int y = 0; y < DungeonMath.ChunkTiles; y++)
            {
                Vector3 tilePos = new Vector3(x * TileSize3D, 0, y * TileSize3D) + chunkOffset;

                if (chunk.Grid[x, y] > 0)
                {
                    CsgBox3D floor = new CsgBox3D();
                    floor.Size = new Vector3(TileSize3D, 0.2f, TileSize3D);
                    floor.Position = tilePos + new Vector3(0, -0.1f, 0);

                    StandardMaterial3D mat = new StandardMaterial3D();
                    mat.AlbedoColor = new Color(0.2f, 0.22f, 0.25f);
                    floor.Material = mat;
                    chunkRoot.AddChild(floor);

                    BuildSurroundingWalls(chunk, x, y, tilePos, chunkRoot);
                }
            }
        }
    }

    private void BuildSurroundingWalls(DungeonMath.ChunkData chunk, int x, int y, Vector3 tilePos, Node3D root)
    {
        int[,] dirs = { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
        int myRoomId = chunk.Grid[x, y];

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dirs[i, 0];
            int ny = y + dirs[i, 1];

            bool needWall = false;

            if (nx < 0 || nx >= DungeonMath.ChunkTiles || ny < 0 || ny >= DungeonMath.ChunkTiles) {
                needWall = true;
            }
            else {
                int neighborRoomId = chunk.Grid[nx, ny];
                if (neighborRoomId == 0) needWall = true;
                else if (myRoomId != neighborRoomId) needWall = true;
            }

            bool isDoorHere = chunk.Doors.Exists(d => 
                (i == 0 && d.X == x && d.Y == y) || 
                (i == 1 && d.X == x && d.Y == y + 1) ||
                (i == 2 && d.X == x && d.Y == y) ||
                (i == 3 && d.X == x + 1 && d.Y == y)
            );

            if (isDoorHere)
            {
                CsgBox3D door = new CsgBox3D();
                StandardMaterial3D doorMat = new StandardMaterial3D();
                doorMat.AlbedoColor = new Color(0.0f, 0.33f, 1.0f);
                door.Material = doorMat;

                if (i == 0 || i == 1) {
                    door.Size = new Vector3(TileSize3D * 0.3f, WallHeight3D * 0.8f, WallThickness3D * 1.5f);
                    door.Position = tilePos + new Vector3(0, WallHeight3D * 0.4f, i == 0 ? -TileSize3D / 2 : TileSize3D / 2);
                } else {
                    door.Size = new Vector3(WallThickness3D * 1.5f, WallHeight3D * 0.8f, TileSize3D * 0.3f);
                    door.Position = tilePos + new Vector3(i == 2 ? -TileSize3D / 2 : TileSize3D / 2, WallHeight3D * 0.4f, 0);
                }
                root.AddChild(door);
                continue;
            }

            if (needWall)
            {
                CsgBox3D wall = new CsgBox3D();
                StandardMaterial3D wallMat = new StandardMaterial3D();
                wallMat.AlbedoColor = new Color(0.4f, 0.44f, 0.5f);
                wall.Material = wallMat;

                if (i == 0) {
                    wall.Size = new Vector3(TileSize3D, WallHeight3D, WallThickness3D);
                    wall.Position = tilePos + new Vector3(0, WallHeight3D / 2, -TileSize3D / 2);
                }
                else if (i == 1) {
                    wall.Size = new Vector3(TileSize3D, WallHeight3D, WallThickness3D);
                    wall.Position = tilePos + new Vector3(0, WallHeight3D / 2, TileSize3D / 2);
                }
                else if (i == 2) {
                    wall.Size = new Vector3(WallThickness3D, WallHeight3D, TileSize3D);
                    wall.Position = tilePos + new Vector3(-TileSize3D / 2, WallHeight3D / 2, 0);
                }
                else if (i == 3) {
                    wall.Size = new Vector3(WallThickness3D, WallHeight3D, TileSize3D);
                    wall.Position = tilePos + new Vector3(TileSize3D / 2, WallHeight3D / 2, 0);
                }
                root.AddChild(wall);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.M)
            {
                _isMapMode = !_isMapMode;

                if (_isMapMode && _mapCanvas == null)
                {
                    _mapCanvas = new MapCanvas2D(this);
                    AddChild(_mapCanvas);
                }

                foreach (var chunk in _loadedChunks.Values)
                {
                    if (chunk.Chunk3DNode != null)
                        chunk.Chunk3DNode.Visible = !_isMapMode;
                }

                if (_mapCanvas != null)
                {
                    _mapCanvas.Visible = _isMapMode;
                    _mapCanvas.QueueRedraw();
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        Vector3 camMove = Vector3.Zero;
        if (Input.IsKeyPressed(Key.W)) camMove.Z -= 1;
        if (Input.IsKeyPressed(Key.S)) camMove.Z += 1;
        if (Input.IsKeyPressed(Key.A)) camMove.X -= 1;
        if (Input.IsKeyPressed(Key.D)) camMove.X += 1;
        
        _gameCamera.Translate(camMove.Normalized() * _speed * (float)delta);

        if (_isMapMode && _mapCanvas != null)
        {
            _mapCanvas.QueueRedraw();
        }
    }
}
