using System;
using System.Collections.Generic;

// 원리: 삼각 격자에서 가능한 배치를 완전 탐색(백트래킹)한다.
// 같은 길이의 성냥을 끝점끼리 연결하며, 교차하거나 포개지 않는다.
// 변 공유와 큰 삼각형을 허용하고, 성냥을 일부 남겨도 된다.
// 모든 크기의 완성된 삼각형을 센다. 단순한 n / 3 공식이 아니다.
// 탐색량이 급격히 증가하므로 먼저 0~9개 정도의 작은 입력으로 실행한다.
internal static class MatchstickProgram
{
    private static int budget;
    private static int bestCount;
    private static readonly HashSet<string> visited = new HashSet<string>();

    private static Template[] templates;

    // 삼각 격자 좌표: X축과 Y축이 60도를 이룬다.
    // 화면의 직각 좌표가 아니므로 (0,0), (1,0), (0,1)은 정삼각형이다.
    private struct Point : IEquatable<Point>, IComparable<Point>
    {
        public int X;
        public int Y;
        public Point(int x, int y) { X = x; Y = y; }
        public bool Equals(Point other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object value) { return value is Point && Equals((Point)value); }
        public override int GetHashCode() { unchecked { return X * 397 ^ Y; } }
        public int CompareTo(Point other)
        {
            int result = X.CompareTo(other.X);
            return result != 0 ? result : Y.CompareTo(other.Y);
        }
    }

    // 성냥 하나를 두 끝점으로 표현한다. 끝점 순서가 달라도 같은 성냥이다.
    private struct Edge : IEquatable<Edge>, IComparable<Edge>
    {
        public Point A;
        public Point B;
        public Edge(Point a, Point b)
        {
            if (a.CompareTo(b) <= 0) { A = a; B = b; }
            else { A = b; B = a; }
        }
        public bool Equals(Edge other) { return A.Equals(other.A) && B.Equals(other.B); }
        public override bool Equals(object value) { return value is Edge && Equals((Edge)value); }
        public override int GetHashCode() { unchecked { return A.GetHashCode() * 397 ^ B.GetHashCode(); } }
        public int CompareTo(Edge other)
        {
            int result = A.CompareTo(other.A);
            return result != 0 ? result : B.CompareTo(other.B);
        }
    }

    // 원점의 삼각형 성냥/끝점은 시작할 때 한 번만 계산한다.
    private sealed class Template
    {
        public Edge[] Edges;
        public Point[] Perimeter;
    }

    // 후보를 문자열 대신 값 형식으로 비교한다. 같은 위치/크기/방향이면 같다.
    private struct Candidate : IEquatable<Candidate>
    {
        public Point Origin;
        public int TemplateIndex;
        public Candidate(Point origin, int index) { Origin = origin; TemplateIndex = index; }
        public bool Equals(Candidate other)
        {
            return Origin.Equals(other.Origin) && TemplateIndex == other.TemplateIndex;
        }
        public override bool Equals(object value) { return value is Candidate && Equals((Candidate)value); }
        public override int GetHashCode()
        {
            unchecked { return Origin.GetHashCode() * 397 ^ TemplateIndex; }
        }
    }

    private static void Main()
    {
        Console.Write("성냥개비 개수(0 이상의 정수)를 입력하세요: ");
        if (!int.TryParse(Console.ReadLine(), out budget) || budget < 0)
        {
            Console.WriteLine("0 이상의 정수가 필요합니다.");
            return;
        }
        if (budget > 9)
            Console.WriteLine("완전 탐색이므로 큰 입력은 오래 걸릴 수 있습니다. 중단: Ctrl+C");

        bestCount = 0;
        visited.Clear();
        BuildTemplates();
        // 처음 놓는 삼각형의 크기도 모두 고려한다. 방향은 대칭이므로 하나면 된다.
        for (int index = 0; index < templates.Length; index += 2)
            Search(new HashSet<Edge>(templates[index].Edges));

        Console.WriteLine("최대 정삼각형 개수: {0}", bestCount);
    }

    private static void BuildTemplates()
    {
        templates = new Template[2 * (budget / 3)];
        for (int side = 1; side <= budget / 3; side++)
        {
            for (int direction = 0; direction < 2; direction++)
            {
                List<Edge> edges = MakeTriangle(0, 0, side, direction == 1);
                templates[(side - 1) * 2 + direction] = new Template
                {
                    Edges = edges.ToArray(),
                    Perimeter = GetVertices(new HashSet<Edge>(edges)).ToArray()
                };
            }
        }
    }

    private static void Search(HashSet<Edge> layout)
    {
        if (!visited.Add(GetCanonicalKey(layout))) return;

        List<Point> vertices = GetVertices(layout);
        int count = CountTriangles(layout, vertices);
        if (count > bestCount) bestCount = count;

        int remaining = budget - layout.Count;
        if (remaining == 0) return;

        HashSet<Candidate> candidates = new HashSet<Candidate>();
        // 후보마다 List를 만들지 않고, 이 탐색 단계에서 배열 하나를 재사용한다.
        Edge[] addedEdges = new Edge[remaining];
        foreach (Point anchor in vertices)
        {
            for (int index = 0; index < templates.Length; index++)
            {
                Template template = templates[index];
                foreach (Point touching in template.Perimeter)
                {
                    Point origin = new Point(anchor.X - touching.X, anchor.Y - touching.Y);
                    if (!candidates.Add(new Candidate(origin, index))) continue;

                    int added = 0;
                    bool fits = true;
                    foreach (Edge local in template.Edges)
                    {
                        Edge edge = Move(local, origin.X, origin.Y);
                        if (layout.Contains(edge)) continue;
                        // 예산 초과가 확인되는 즉시 나머지 변 검사를 생략한다.
                        if (added == remaining) { fits = false; break; }
                        addedEdges[added++] = edge;
                    }
                    if (!fits || added == 0) continue;

                    for (int i = 0; i < added; i++) layout.Add(addedEdges[i]);
                    Search(layout);
                    // 자식 호출은 자신의 배열을 쓰므로 이 배열 내용은 보존된다.
                    for (int i = 0; i < added; i++) layout.Remove(addedEdges[i]);
                }
            }
        }
    }

    private static int CountTriangles(HashSet<Edge> layout, List<Point> vertices)
    {
        int count = 0;
        int maxSide = layout.Count / 3;
        foreach (Point start in vertices)
        {
            // 수평 밑변이 이어지는 길이까지만 검사한다.
            // 삼각형 목록을 생성하지 않고 해시 조회만으로 세 변을 확인한다.
            for (int side = 1; side <= maxSide; side++)
            {
                Point end = new Point(start.X + side, start.Y);
                if (!layout.Contains(new Edge(new Point(end.X - 1, end.Y), end))) break;

                if (HasLine(layout, start, 0, 1, side) && HasLine(layout, end, -1, 1, side))
                    count++;
                if (HasLine(layout, start, 1, -1, side) && HasLine(layout, end, 0, -1, side))
                    count++;
            }
        }
        return count;
    }

    private static bool HasLine(HashSet<Edge> layout, Point start, int dx, int dy, int length)
    {
        for (int step = 0; step < length; step++)
        {
            Point end = new Point(start.X + dx, start.Y + dy);
            if (!layout.Contains(new Edge(start, end))) return false;
            start = end;
        }
        return true;
    }

    private static List<Edge> MakeTriangle(int x, int y, int side, bool downward)
    {
        Point a = new Point(x, y);
        Point b = new Point(x + side, y);
        Point c = downward ? new Point(x + side, y - side) : new Point(x, y + side);
        List<Edge> edges = new List<Edge>(3 * side);
        AddSide(edges, a, b, side);
        AddSide(edges, b, c, side);
        AddSide(edges, c, a, side);
        return edges;
    }

    private static void AddSide(List<Edge> edges, Point start, Point end, int length)
    {
        int dx = (end.X - start.X) / length;
        int dy = (end.Y - start.Y) / length;
        for (int step = 0; step < length; step++)
        {
            Point next = new Point(start.X + dx, start.Y + dy);
            edges.Add(new Edge(start, next));
            start = next;
        }
    }

    private static List<Point> GetVertices(HashSet<Edge> edges)
    {
        HashSet<Point> points = new HashSet<Point>();
        foreach (Edge edge in edges) { points.Add(edge.A); points.Add(edge.B); }
        return new List<Point>(points);
    }

    private static Edge Move(Edge edge, int dx, int dy)
    {
        return new Edge(new Point(edge.A.X + dx, edge.A.Y + dy),
            new Point(edge.B.X + dx, edge.B.Y + dy));
    }

    private static Point Transform(Point point, int rotation, bool reflect)
    {
        int x = reflect ? point.Y : point.X;
        int y = reflect ? point.X : point.Y;
        // 같은 점을 반복 회전하지 않고 목적 좌표를 바로 계산한다.
        switch (rotation)
        {
            case 1: return new Point(-y, x + y);
            case 2: return new Point(-x - y, x);
            case 3: return new Point(-x, -y);
            case 4: return new Point(y, -x - y);
            case 5: return new Point(x + y, -x);
            default: return new Point(x, y);
        }
    }

    private static string GetCanonicalKey(HashSet<Edge> layout)
    {
        // 12개 방향마다 문자열을 만들지 않고, 배열 두 개만 재사용한다.
        Edge[] working = new Edge[layout.Count];
        Edge[] best = new Edge[layout.Count];
        bool hasBest = false;
        for (int reflection = 0; reflection < 2; reflection++)
        {
            for (int rotation = 0; rotation < 6; rotation++)
            {
                int index = 0;
                int minX = int.MaxValue;
                int minY = int.MaxValue;
                foreach (Edge edge in layout)
                {
                    Point a = Transform(edge.A, rotation, reflection == 1);
                    Point b = Transform(edge.B, rotation, reflection == 1);
                    working[index++] = new Edge(a, b);
                    int edgeMinX = a.X < b.X ? a.X : b.X;
                    int edgeMinY = a.Y < b.Y ? a.Y : b.Y;
                    if (edgeMinX < minX) minX = edgeMinX;
                    if (edgeMinY < minY) minY = edgeMinY;
                }
                for (int i = 0; i < working.Length; i++) working[i] = Move(working[i], -minX, -minY);
                SortEdges(working);
                if (!hasBest || CompareLayouts(working, best) < 0)
                {
                    for (int edgeIndex = 0; edgeIndex < working.Length; edgeIndex++)
                        best[edgeIndex] = working[edgeIndex];
                    hasBest = true;
                }
            }
        }
        return Encode(best);
    }

    // 라이브러리 정렬 메서드 대신 추가 배열을 만들지 않는 힙 정렬을 사용한다.
    // CompareTo 기준 오름차순으로 정렬하며 시간은 O(n log n), 추가 공간은 O(1)이다.
    private static void SortEdges(Edge[] edges)
    {
        int count = edges.Length;
        for (int root = count / 2 - 1; root >= 0; root--)
            SiftDown(edges, root, count);

        for (int end = count - 1; end > 0; end--)
        {
            Edge first = edges[0];
            edges[0] = edges[end];
            edges[end] = first;
            SiftDown(edges, 0, end);
        }
    }

    private static void SiftDown(Edge[] edges, int root, int count)
    {
        while (root * 2 + 1 < count)
        {
            int child = root * 2 + 1;
            if (child + 1 < count && edges[child].CompareTo(edges[child + 1]) < 0)
                child++;

            if (edges[root].CompareTo(edges[child]) >= 0)
                return;

            Edge parent = edges[root];
            edges[root] = edges[child];
            edges[child] = parent;
            root = child;
        }
    }

    private static int CompareLayouts(Edge[] a, Edge[] b)
    {
        for (int i = 0; i < a.Length; i++)
        {
            int comparison = a[i].CompareTo(b[i]);
            if (comparison != 0) return comparison;
        }
        return 0;
    }

    private static string Encode(Edge[] edges)
    {
        // 좌표의 32비트를 char 두 개에 나눠 담는다. 숫자 문자열 변환을 피한다.
        // 이 문자열은 내부 저장용이다. 화면에 출력하지 않는다.
        // 정확한 좌표를 모두 저장하므로 해시 충돌이 배치를 같게 만들지 않는다.
        char[] key = new char[edges.Length * 8];
        int offset = 0;
        foreach (Edge edge in edges)
        {
            WriteCoordinate(key, ref offset, edge.A.X);
            WriteCoordinate(key, ref offset, edge.A.Y);
            WriteCoordinate(key, ref offset, edge.B.X);
            WriteCoordinate(key, ref offset, edge.B.Y);
        }
        return new string(key);
    }

    private static void WriteCoordinate(char[] key, ref int offset, int value)
    {
        key[offset++] = (char)((uint)value >> 16);
        key[offset++] = (char)(value & 65535);
    }
}
