using System;

// 원리: 같은 행을 한 번만 만든 뒤 반복 출력하여 Console 호출을 줄인다.
// C# 콘솔에서는 C의 printf/scanf 대신 Console.Write와 Console.ReadLine을 쓴다.
internal static class SquareProgram
{
    private static void Main()
    {
        Console.Write("정사각형 한 변의 길이(2 이상)를 입력하세요: ");
        int side;
        if (!int.TryParse(Console.ReadLine(), out side) || side < 2)
        {
            Console.WriteLine("2 이상의 정수가 필요합니다.");
            return;
        }

        if (side > int.MaxValue / 2)
        {
            Console.WriteLine("한 행을 표현할 수 있는 길이를 초과했습니다.");
            return;
        }

        char[] characters = new char[side * 2];
        for (int column = 0; column < side; column++)
        {
            characters[column * 2] = '*';
            characters[column * 2 + 1] = ' ';
        }
        string line = new string(characters);
        for (int row = 0; row < side; row++)
            Console.WriteLine(line);
    }
}
