using System;

// 캐릭터 하나의 상태를 구조체로 묶는다. Unity나 외부 라이브러리는 사용하지 않는다.
internal struct Character
{
    public string Name;
    public decimal Health;
    public decimal DefensePercent;
    public decimal Attack;
    public decimal AttackSpeed; // 초당 공격 횟수. 2라면 0.5초마다 한 번 공격한다.

    public Character(string name, decimal health, decimal defensePercent,
        decimal attack, decimal attackSpeed)
    {
        Name = name;
        Health = health;
        DefensePercent = defensePercent;
        Attack = attack;
        AttackSpeed = attackSpeed;
    }
}

internal static class BattleProgram
{
    // 이미 O(1)인 방식: 프레임/공격 반복 없이 처치 시점을 직접 비교한다.
    // 공격 횟수가 늘어나도 계산 횟수는 일정하므로 이 원리를 유지한다.
    private static void Main()
    {
        // 이 값들을 수정해서 다른 전투를 실행할 수 있다.
        Character a = new Character("A", 100m, 20m, 25m, 2m);
        Character b = new Character("B", 120m, 10m, 30m, 1m);

        Simulate(a, b);
    }

    private static void Simulate(Character a, Character b)
    {
        if (!IsValid(a) || !IsValid(b))
        {
            Console.WriteLine("체력은 0 초과, 방어율은 0~100%, 공격력/속도는 0 이상이어야 합니다.");
            Console.WriteLine("체력/공격력 상한은 1,000,000, 공격 속도 상한은 1,000입니다. 소수 둘째 자리까지만 사용하세요.");
            return;
        }

        decimal damageToB = a.Attack * (1m - b.DefensePercent / 100m);
        decimal damageToA = b.Attack * (1m - a.DefensePercent / 100m);
        bool aCanDamage = a.AttackSpeed > 0m && damageToB > 0m;
        bool bCanDamage = b.AttackSpeed > 0m && damageToA > 0m;

        // 서로 피해를 줄 수 없다면 시간을 계속 진행해도 결과가 바뀌지 않는다.
        if (!aCanDamage && !bCanDamage)
        {
            Console.WriteLine("무승부: 서로 피해를 줄 수 없습니다.");
            return;
        }

        // 필요한 타격 수 = 올림(상대 체력 / 1회 피해).
        // 첫 공격은 0초가 아니라 1 / 공격 속도 초에 발생한다.
        // 따라서 상대를 쓰러뜨리는 시각 = 필요한 타격 수 / 공격 속도.
        // 모든 공격을 반복하지 않고도, 공격 시점을 정확히 반영한 결과를 구한다.
        if (!aCanDamage)
        {
            PrintOneSidedWinner(b, a.Health, damageToA);
            return;
        }
        if (!bCanDamage)
        {
            PrintOneSidedWinner(a, b.Health, damageToB);
            return;
        }

        decimal aHits = CeilingPositive(b.Health / damageToB);
        decimal bHits = CeilingPositive(a.Health / damageToA);

        // aHits / a.Speed와 bHits / b.Speed를 교차 곱으로 비교한다.
        // 나눗셈 반올림 때문에 동시 공격을 다르게 판정하는 문제를 줄인다.
        decimal aOrder = aHits * b.AttackSpeed;
        decimal bOrder = bHits * a.AttackSpeed;

        if (aOrder == bOrder)
        {
            decimal endTime = aHits / a.AttackSpeed;
            Console.WriteLine("{0:0.###}초: 동시 사망, 무승부", endTime);
        }
        else if (aOrder < bOrder)
        {
            // 종료 시각까지 B의 공격 횟수. 같은 시각의 공격도 적용한다.
            decimal bAttacks = FloorPositive(aHits * b.AttackSpeed / a.AttackSpeed);
            a.Health = SubtractWithoutGoingBelowZero(a.Health, bAttacks * damageToA);
            b.Health = 0m;
            PrintResult(a, b, aHits / a.AttackSpeed);
        }
        else
        {
            decimal aAttacks = FloorPositive(bHits * a.AttackSpeed / b.AttackSpeed);
            b.Health = SubtractWithoutGoingBelowZero(b.Health, aAttacks * damageToB);
            a.Health = 0m;
            PrintResult(b, a, bHits / b.AttackSpeed);
        }
    }

    private static bool IsValid(Character character)
    {
        return character.Health > 0m && character.Health <= 1000000m
            && character.DefensePercent >= 0m && character.DefensePercent <= 100m
            && character.Attack >= 0m && character.Attack <= 1000000m
            && character.AttackSpeed >= 0m && character.AttackSpeed <= 1000m
            && HasTwoDecimalPlaces(character.Health)
            && HasTwoDecimalPlaces(character.DefensePercent)
            && HasTwoDecimalPlaces(character.Attack)
            && HasTwoDecimalPlaces(character.AttackSpeed);
    }

    private static bool HasTwoDecimalPlaces(decimal value)
    {
        return value * 100m % 1m == 0m;
    }

    // 호출 전에 0 이상이고 long 범위 안인지 보장한다.
    // decimal을 정수로 변환하면 소수 부분이 버려지므로 직접 올림·내림을 만들 수 있다.
    private static decimal CeilingPositive(decimal value)
    {
        long wholePart = (long)value;
        return value > wholePart ? wholePart + 1m : wholePart;
    }

    private static decimal FloorPositive(decimal value)
    {
        return (long)value;
    }

    private static decimal SubtractWithoutGoingBelowZero(decimal value, decimal amount)
    {
        decimal result = value - amount;
        return result > 0m ? result : 0m;
    }

    private static void PrintOneSidedWinner(Character winner, decimal targetHealth, decimal damage)
    {
        decimal hits = CeilingPositive(targetHealth / damage);
        Console.WriteLine("{0:0.###}초: {1} 승리, 남은 체력 {2:0.###}",
            hits / winner.AttackSpeed, winner.Name, winner.Health);
    }

    private static void PrintResult(Character winner, Character loser, decimal time)
    {
        Console.WriteLine("{0:0.###}초: {1} 승리", time, winner.Name);
        Console.WriteLine("{0} 체력: {1:0.###}, {2} 체력: {3:0.###}",
            winner.Name, winner.Health, loser.Name, loser.Health);
    }
}
