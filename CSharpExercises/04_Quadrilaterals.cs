using System;
using System.Collections.Generic;
using System.Globalization;

// 원리: 네 내각의 합과 각도 관계를 이용해 만들 수 있는 사각형을 판별한다.
internal static class QuadrilateralsProgram
{
    private static void Main()
    {
        try
        {
            decimal[] angles = new decimal[4];
            for (int i = 0; i < angles.Length; i++)
            {
                Console.Write($"{i + 1}번째 내각(도): ");
                angles[i] = ReadAngle(Console.ReadLine());
            }

            List<string> types = GetPossibleTypes(angles);

            Console.WriteLine("생성 가능한 사각형 종류:");
            foreach (string type in types)
                Console.WriteLine($"- {type}");
        }
        catch (FormatException)
        {
            Console.WriteLine("입력 오류: 각도는 숫자로 입력해야 합니다.");
        }
        catch (OverflowException)
        {
            Console.WriteLine("입력 오류: 숫자가 너무 커서 처리할 수 없습니다.");
        }
        catch (ArgumentException exception)
        {
            Console.WriteLine($"사각형 생성 불가: {exception.Message}");
        }
    }

    private static decimal ReadAngle(string input)
    {
        if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal angle))
            throw new FormatException();
        if (angle <= 0 || angle >= 180)
            throw new ArgumentException("볼록 사각형의 각도는 각각 0도보다 크고 180도보다 작아야 합니다.");
        return angle;
    }

    private static List<string> GetPossibleTypes(decimal[] angles)
    {
        if (angles[0] + angles[1] + angles[2] + angles[3] != 360)
            throw new ArgumentException("네 내각의 합은 360도여야 합니다.");

        decimal a = angles[0];
        decimal b = angles[1];
        decimal c = angles[2];
        decimal d = angles[3];
        bool parallelogram = a == c && b == d;
        bool rectangle = a == 90 && b == 90 && c == 90 && d == 90;
        bool kite = a == c || b == d;
        bool trapezoid = !parallelogram && (a + d == 180 || a + b == 180);
        bool isoscelesTrapezoid = trapezoid &&
            ((a == b && c == d) || (b == c && d == a));

        var types = new List<string> { "사각형" };

        if (parallelogram)
            types.Add("평행사변형");
        if (parallelogram)
            types.Add("마름모");
        if (rectangle)
        {
            types.Add("직사각형");
            types.Add("정사각형");
        }
        if (kite)
            types.Add("연꼴");
        if (trapezoid)
            types.Add("사다리꼴");
        if (isoscelesTrapezoid)
            types.Add("등변사다리꼴");

        return types;
    }
}
