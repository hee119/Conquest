using System;

// 원리: 알파벳별 개수를 센 뒤 A~Z, a~z 순서대로 출력하는 계수 정렬.
// 예제 ABcdCDef -> ABCDcdef에 맞춰 각 그룹 안에서도 알파벳순으로 정렬한다.
internal static class LetterProgram
{
    private static void Main()
    {
        Console.Write("영문 대소문자로만 이루어진 문자열을 입력하세요: ");
        string input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("문자열이 비어 있습니다.");
            return;
        }

        int[] counts = new int[52]; // 0~25: A~Z, 26~51: a~z

        foreach (char letter in input)
        {
            if (letter >= 'A' && letter <= 'Z')
                counts[letter - 'A']++;
            else if (letter >= 'a' && letter <= 'z')
                counts[26 + letter - 'a']++;
            else
            {
                Console.WriteLine("공백, 숫자, 한글 없이 영문자만 입력하세요.");
                return;
            }
        }

        // 문자마다 Console을 호출하지 않고, 정확히 필요한 길이만 할당한다.
        char[] output = new char[input.Length];
        int position = 0;
        for (int index = 0; index < counts.Length; index++)
        {
            char letter = index < 26 ? (char)('A' + index) : (char)('a' + index - 26);
            for (int count = 0; count < counts[index]; count++)
                output[position++] = letter;
        }
        Console.WriteLine(output);
    }
}
