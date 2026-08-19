// ==========================================
// ФАЙЛ 1: ЧИСТАЯ МАТЕМАТИКА ГЕНЕРАЦИИ В С#
// ==========================================
using Godot;
using System;
using System.Collections.Generic;

public static class DungeonMath
{
    public const int ChunkTiles = 24;
    private const int WorldSeed = 654321;

    public struct RoomData
    {
        public int X, Y, W, H;
        public RoomData(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }
    }

    public struct DoorData
    {
        public int X, Y;
        public string Type;
        public bool Closed;
        public DoorData(int x, int y, string type, bool closed) { X = x; Y = y; Type = type; Closed = closed; }
    }

    public class ChunkData
    {
        public int[,] Grid = new int[ChunkTiles, ChunkTiles];
        public List<RoomData> Rooms = new List<RoomData>();
        public List<DoorData> Doors = new List<DoorData>();
        public Vector2I Teleport = Vector2I.Zero;
        public bool HasTeleport = false;
        public Node3D Chunk3DNode = null;
        public int Cx;
        public int Cy;
    }

    public static float SeededRandom(int x, int y, int extraSeed = 5)
    {
        double input = x * 12.9898 + y * 78.233 + extraSeed + WorldSeed;
        double sx = Math.Sin(input) * 43758.5453;
        return (float)(sx - Math.Floor(sx));
    }

    public static bool CheckRawConnection(int cx1, int cy1, int cx2, int cy2)
    {
        int minX = Math.Min(cx1, cx2);
        int maxX = Math.Max(cx1, cx2);
        int minY = Math.Min(cy1, cy2);
        int maxY = Math.Max(cy1, cy2);
        return SeededRandom(minX + maxX, minY + maxY, 555) > 0.46f;
    }

    public static bool HasConnection(int cx, int cy, string dir)
    {
        int nX = cx, nY = cy;
        if (dir == "north") nY--; if (dir == "south") nY++; if (dir == "west") nX--; if (dir == "east") nX++;

        if (CheckRawConnection(cx, cy, nX, nY)) return true;

        bool cN = CheckRawConnection(cx, cy, cx, cy - 1);
        bool cS = CheckRawConnection(cx, cy, cx, cy + 1);
        bool cW = CheckRawConnection(cx, cy, cx - 1, cy);
        bool cE = CheckRawConnection(cx, cy, cx + 1, cy);

        if (!cN && !cS && !cW && !cE)
        {
            string[] dirs = { "north", "south", "west", "east" };
            int idx = Mathf.FloorToInt(SeededRandom(cx, cy, 999) * 4);
            if (dir == dirs[idx]) return true;
        }

        bool nN = CheckRawConnection(nX, nY, nX, nY - 1);
        bool nS = CheckRawConnection(nX, nY, nX, nY + 1);
        bool nW = CheckRawConnection(nX, nY, nX - 1, nY);
        bool nE = CheckRawConnection(nX, nY, nX + 1, nY);

        if (!nN && !nS && !nW && !nE)
        {
            string[] dirs = { "north", "south", "west", "east" };
            int idx = Mathf.FloorToInt(SeededRandom(nX, nY, 999) * 4);
            string neighborForcedDir = dirs[idx];
            if (dir == "north" && neighborForcedDir == "south") return true;
            if (dir == "south" && neighborForcedDir == "north") return true;
            if (dir == "west" && neighborForcedDir == "east") return true;
            if (dir == "east" && neighborForcedDir == "west") return true;
        }
        return false;
    }

    public static ChunkData PreGenerateChunkGeometry(int cx, int cy)
    {
        ChunkData chunk = new ChunkData();
        int mid = ChunkTiles / 2;
        float cSeed = SeededRandom(cx, cy, 777);

        float Rand() {
            cSeed = (float)((Math.Sin(cSeed * 8321.3) * 43758.5) % 1);
            return Math.Abs(cSeed);
        }

        bool IsAreaFree(int rx, int ry, int rw, int rh) {
            if (rx < 1 || ry < 1 || rx + rw > ChunkTiles - 1 || ry + rh > ChunkTiles - 1) return false;
            foreach (var r in chunk.Rooms) {
                if (rx < r.X + r.W && rx + rw > r.X && ry < r.Y + r.H && ry + rh > r.Y) return false;
            }
            return true;
        }

        int w1 = Mathf.FloorToInt(Rand() * 4) + 5;
        int h1 = Mathf.FloorToInt(Rand() * 4) + 5;
        RoomData startRoom = new RoomData(mid - w1 / 2, mid - h1 / 2, w1, h1);
        chunk.Rooms.Add(startRoom);

        int totalRooms = Mathf.FloorToInt(Rand() * 8) + 15;
        int attempts = 300;

        while (attempts-- > 0 && chunk.Rooms.Count < totalRooms)
        {
            var p = chunk.Rooms[Mathf.FloorToInt(Rand() * chunk.Rooms.Count)];
            int rw = Mathf.FloorToInt(Rand() * 4) + 4;
            int rh = Mathf.FloorToInt(Rand() * 4) + 4;
            int rx = 0, ry = 0;
            int side = Mathf.FloorToInt(Rand() * 4);

            if (side == 0) { rx = p.X + Mathf.FloorToInt(Rand() * (p.W - 2)); ry = p.Y - rh; }
            else if (side == 1) { rx = p.X + Mathf.FloorToInt(Rand() * (p.W - 2)); ry = p.Y + p.H; }
            else if (side == 2) { rx = p.X - rw; ry = p.Y + Mathf.FloorToInt(Rand() * (p.H - 2)); }
            else { rx = p.X + p.W; ry = p.Y + Mathf.FloorToInt(Rand() * (p.H - 2)); }

            if (IsAreaFree(rx, ry, rw, rh)) {
                chunk.Rooms.Add(new RoomData(rx, ry, rw, rh));
            }
        }

        for (int i = 0; i < chunk.Rooms.Count; i++)
        {
            var r = chunk.Rooms[i];
            for (int x = r.X; x < r.X + r.W; x++)
            {
                for (int y = r.Y; y < r.Y + r.H; y++) chunk.Grid[x, y] = i + 1;
            }
        }
        return chunk;
    }

    public static ChunkData GenerateChunk(int cx, int cy, Dictionary<string, ChunkData> cache)
    {
        string key = $"{cx},{cy}";
        if (cache.ContainsKey(key)) return cache[key];

        ChunkData chunk = PreGenerateChunkGeometry(cx, cy);
        float cSeed = SeededRandom(cx, cy, 12345);
        float Rand() { cSeed = (float)((Math.Sin(cSeed * 4321.3) * 43758.5) % 1); return Math.Abs(cSeed); }

        int mid = ChunkTiles / 2;
        int w1 = Mathf.FloorToInt(SeededRandom(cx, cy, 777) * 4) + 5;

        int activeConnections = 0;
        string[] directions = { "north", "south", "west", "east" };
        foreach (var d in directions) { if (HasConnection(cx, cy, d)) activeConnections++; }

        if ((activeConnections == 1 || Rand() < 0.10f) && !(cx == 0 && cy == 0) && chunk.Rooms.Count > 0)
        {
            var mainRoom = chunk.Rooms[0];
            chunk.Teleport = new Vector2I(mainRoom.X + mainRoom.W / 2, mainRoom.Y + mainRoom.H / 2);
            chunk.HasTeleport = true;
        }

        // Начиная со второй комнаты (i = 1), ищем стену соприкосновения с предыдущими комнатами
for (int i = 1; i < chunk.Rooms.Count; i++)
{
    var r = chunk.Rooms[i];
    bool doorPlaced = false;

    for (int j = 0; j < i; j++)
    {
        var other = chunk.Rooms[j];

        // 1. Текущая комната (r) находится СНИЗУ от предыдущей (other)
        if (r.Y == other.Y + other.H)
        {
            int minX = Math.Max(r.X, other.X);
            int maxX = Math.Min(r.X + r.W, other.X + other.W);
            if (minX < maxX) // Есть общее ребро
            {
                int dx = (minX + maxX) / 2;
                int dy = r.Y;
                float dSeed = SeededRandom(cx + r.X, cy + r.Y, 54321);
                bool closed = (dSeed - Math.Floor(dSeed)) > 0.5f;
                chunk.Doors.Add(new DoorData(dx, dy, "H", closed));
                doorPlaced = true;
                break;
            }
        }
        // 2. Текущая комната (r) находится СВЕРХУ от предыдущей (other)
        else if (r.Y + r.H == other.Y)
        {
            int minX = Math.Max(r.X, other.X);
            int maxX = Math.Min(r.X + r.W, other.X + other.W);
            if (minX < maxX)
            {
                int dx = (minX + maxX) / 2;
                int dy = other.Y;
                float dSeed = SeededRandom(cx + r.X, cy + r.Y, 54321);
                bool closed = (dSeed - Math.Floor(dSeed)) > 0.5f;
                chunk.Doors.Add(new DoorData(dx, dy, "H", closed));
                doorPlaced = true;
                break;
            }
        }
        // 3. Текущая комната (r) находится СПРАВА от предыдущей (other)
        else if (r.X == other.X + other.W)
        {
            int minY = Math.Max(r.Y, other.Y);
            int maxY = Math.Min(r.Y + r.H, other.Y + other.H);
            if (minY < maxY)
            {
                int dx = r.X;
                int dy = (minY + maxY) / 2;
                float dSeed = SeededRandom(cx + r.X, cy + r.Y, 54321);
                bool closed = (dSeed - Math.Floor(dSeed)) > 0.5f;
                chunk.Doors.Add(new DoorData(dx, dy, "V", closed)); // "V" для вертикального прохода
                doorPlaced = true;
                break;
            }
        }
        // 4. Текущая комната (r) находится СЛЕВА от предыдущей (other)
        else if (r.X + r.W == other.X)
        {
            int minY = Math.Max(r.Y, other.Y);
            int maxY = Math.Min(r.Y + r.H, other.Y + other.H);
            if (minY < maxY)
            {
                int dx = other.X;
                int dy = (minY + maxY) / 2;
                float dSeed = SeededRandom(cx + r.X, cy + r.Y, 54321);
                bool closed = (dSeed - Math.Floor(dSeed)) > 0.5f;
                chunk.Doors.Add(new DoorData(dx, dy, "V", closed)); // "V" для вертикального прохода
                doorPlaced = true;
                break;
            }
        }
    }
}


        var neighbors = new[] {
            new { dir = "north", dx = 0, dy = -1, bEdge = 0, axis = 'x' },
            new { dir = "south", dx = 0, dy = 1, bEdge = ChunkTiles - 1, axis = 'x' },
            new { dir = "west", dx = -1, dy = 0, bEdge = 0, axis = 'y' },
            new { dir = "east", dx = 1, dy = 0, bEdge = ChunkTiles - 1, axis = 'y' }
        };

        foreach (var n in neighbors)
        {
            if (HasConnection(cx, cy, n.dir) || HasConnection(cx + n.dx, cy + n.dy, n.dir == "north" ? "south" : n.dir == "south" ? "north" : n.dir == "west" ? "east" : "west"))
            {
                ChunkData nChunk = PreGenerateChunkGeometry(cx + n.dx, cy + n.dy);
                int bestCoord = -1, minDist = 999;

                for (int i = 1; i < ChunkTiles - 1; i++)
                {
                    int bHit = -1, nHit = -1;
                    if (n.axis == 'x') {
                        for (int s = n.bEdge; n.dir == "north" ? s < ChunkTiles : s >= 0; s += n.dir == "north" ? 1 : -1) if (chunk.Grid[i, s] > 0) { bHit = s; break; }
                        for (int s = n.bEdge == 0 ? ChunkTiles - 1 : 0; n.dir == "north" ? s >= 0 : s < ChunkTiles; s += n.dir == "north" ? -1 : 1) if (nChunk.Grid[i, s] > 0) { nHit = s; break; }
                    } else {
                        for (int s = n.bEdge; n.dir == "west" ? s < ChunkTiles : s >= 0; s += n.dir == "west" ? 1 : -1) if (chunk.Grid[s, i] > 0) { bHit = s; break; }
                        for (int s = n.bEdge == 0 ? ChunkTiles - 1 : 0; n.dir == "west" ? s >= 0 : s < ChunkTiles; s += n.dir == "west" ? -1 : 1) if (nChunk.Grid[s, i] > 0) { nHit = s; break; }
                    }
                    if (bHit != -1 && nHit != -1) {
                        int d = Math.Abs(bHit - n.bEdge) + Math.Abs(nHit - (n.bEdge == 0 ? ChunkTiles - 1 : 0));
                        if (d < minDist) { minDist = d; bestCoord = i; }
                    }
                }

                if (bestCoord != -1) {
                    if (n.axis == 'x') {
                        int sY = n.bEdge;
                        for (int s = n.bEdge; n.dir == "north" ? s < ChunkTiles : s >= 0; s += n.dir == "north" ? 1 : -1) if (chunk.Grid[bestCoord, s] > 0) { sY = s; break; }
                        int minY = Math.Min(n.bEdge, sY), maxY = Math.Max(n.bEdge, sY);
                        for (int y = minY; y <= maxY; y++) chunk.Grid[bestCoord, y] = 2;
                    } else {
                        int sX = n.bEdge;
                        for (int s = n.bEdge; n.dir == "west" ? s < ChunkTiles : s >= 0; s += n.dir == "west" ? 1 : -1) if (chunk.Grid[s, bestCoord] > 0) { sX = s; break; }
                        int minX = Math.Min(n.bEdge, sX), maxX = Math.Max(n.bEdge, sX);
                        for (int x = minX; x <= maxX; x++) chunk.Grid[x, bestCoord] = 2;
                    }
                }
            }
        }

        chunk.Cx = cx;
        chunk.Cy = cy;
        cache[key] = chunk;
        return chunk;
    }
}
