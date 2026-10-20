using System;
using System.Text;

// 원리: 에라토스테네스의 체로 합성수의 배수를 한 번에 제거한다.
// 여러 수의 소수 여부를 구할 때 각 수마다 약수를 찾는 중복을 줄인다.
internal static class PrimeProgram
{
    private static void Main()
    {
        Console.Write("2~999 사이 정수를 입력하세요: ");
        int limit;
        if (!int.TryParse(Console.ReadLine(), out limit) || limit < 2 || limit > 999)
        {
            Console.WriteLine("2~999 사이의 정수가 필요합니다.");
            return;
        }

        bool[] composite = new bool[limit];
        for (int prime = 2; prime * prime < limit; prime++)
        {
            if (composite[prime]) continue;
            // prime * prime 미만의 배수는 더 작은 소수에서 이미 표시했다.
            for (int multiple = prime * prime; multiple < limit; multiple += prime)
                composite[multiple] = true;
        }

        StringBuilder output = new StringBuilder();
        for (int number = 2; number < limit; number++)
        {
            if (composite[number]) continue;
            if (output.Length > 0) output.Append(' ');
            output.Append(number);
        }
        // 결과를 모아 출력하여 Console 호출 횟수도 줄인다.
        Console.WriteLine(output.Length == 0 ? "해당하는 소수가 없습니다." : output.ToString());
    }
}
