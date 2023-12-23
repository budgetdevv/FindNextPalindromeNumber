// define PRINT_DEBUG

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NextPalindrome // Note: actual namespace depends on the project name.
{
    internal static class Program
    {
        // Everything is inlined so that they are compiled to tier 1 immediately.
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        static void Main(string[] args)
        {
            // NextPalindrome(1001);

            // return;

            const bool TEST = false;

            if (TEST)
            {
                for (uint I = 0; I < 1_000_000; I++)
                {
                    DEBUG(() => Console.WriteLine($"Current Term: {I}"));
                
                    if (NextPalindrome(I) != NextPalindromeNaive(I))
                    {
                        throw new Exception("You're stupid");
                    }
                }

                Console.WriteLine("You're not so stupid after all");
            }

            else
            {
                Console.WriteLine("Churning!");
                
                var startTime = DateTime.UtcNow;
                
                for (uint I = 0; I < int.MaxValue; I++)
                {
                    NextPalindrome(I);
                }

                var endTime = DateTime.UtcNow;

                var totalTime = endTime - startTime;

                Console.WriteLine($"Done! Took {totalTime.Minutes} Min(s) {totalTime.Seconds} Second(s)!");

                Console.ReadKey();
            }
            
            return;
            
            // Generated with AI
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsPalindrome(uint n)
            {
                if (n < 0 || (n % 10 == 0 && n != 0)) 
                {
                    return false;
                }

                uint reversedNumber = 0;
                while (n > reversedNumber)
                {
                    reversedNumber = reversedNumber * 10 + n % 10;
                    n /= 10;
                }

                return n == reversedNumber || n == reversedNumber / 10;
            }

            // Generated with AI
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static uint NextPalindromeNaive(uint n)
            {
                while (true)
                {
                    if (IsPalindrome(n))
                    {
                        return n;
                    }
                    
                    n++;
                }
            }
            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetDigits(int num)
        {
            var digits = 1;
            
            while (num > 9)
            {
                num /= 10;
                digits++;
            }

            return digits;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int GetDigitsFast(uint num, uint* multiplesOf10Table)
        {
            var indexOfHighestSetBit = 31 - BitOperations.LeadingZeroCount(num);

            // Int mul followed by int div ( Truncates ).
            // Do not convert it to indexOfHighestSetBit * ( 77 / 256 ), as it will become int * float
            var estDigits = (indexOfHighestSetBit * 77) / 256;

            var offset = (num >= multiplesOf10Table[estDigits]) ? 2 : 1;

            return estDigits + offset;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReverseDigits(uint num)
        {
            uint newNum = 0;

            while (true)
            {
                var cont = num > 9;
                
                var (quotient, rem) = Math.DivRem(num, 10);

                num = quotient;
                
                newNum += rem;

                if (cont)
                {
                    newNum *= 10;
                    continue;
                }

                break;
            }

            return newNum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DEBUG(Action action)
        {
            #if PRINT_DEBUG
            action();
            #endif
        }

        private static ReadOnlySpan<uint> MultiplesOf10 => new uint[] { 1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint NextPalindrome(uint num)
        {
            var multiplesTable = (uint*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(MultiplesOf10));

            var multipleTableGetDigitsFast = multiplesTable + 1;
            
            // var digits = GetDigits((int) num);

            var digits = GetDigitsFast(num, multipleTableGetDigitsFast);
            
            DEBUG(() => Console.WriteLine($"Digits: {digits}"));

            switch (digits)
            {
                case 1:
                    goto Ret;
                case 2:
                    goto TwoDigits;
            }
            
            // Division truncates. E.x. 9 / 2 = 4. We don't care about middle digit.
            var digitsPerHalf = digits / 2;
            
            DEBUG(() => Console.WriteLine($"Digits per half: {digitsPerHalf}"));
            
            // var divisor = (int) Math.Pow(10, digitsPerHalf);
            var divisor = multiplesTable[digitsPerHalf]; 
            
            DEBUG(() => Console.WriteLine($"Divisor: {divisor}"));

            var (leftMiddleTermInclusive, right) = Math.DivRem(num, divisor);

            // var leftMiddleTermInclusive = num / divisor;
            //
            // var right = num - (leftMiddleTermInclusive * divisor);
            
            DEBUG(() => Console.WriteLine($"Left + M | R -> {leftMiddleTermInclusive} | {right}"));
            
            // A term like 1001 would fail, as 01 is treated as 1, so $"{leftMiddleTermInclusive}{right}" will become 101
            // Debug.Assert($"{leftMiddleTermInclusive}{right}" == num.ToString());

            var left = leftMiddleTermInclusive;

            DEBUG(() => Console.WriteLine($"{left}{right}"));
            
            var leftReversed = ReverseDigits(left);

            DEBUG(() => Console.WriteLine($"R: {right} | L-R: {leftReversed}"));
            
            if (leftReversed >= right)
            {
                goto AddLeftReversed;
            }
            
            leftMiddleTermInclusive++;
            
            // Unfortunately, this will require more work. E.x. 619: 9 > 6, so we need to add until 626.
            // Here's the cool part: If there's a middle term, and while it is < 9, we can "reset" the right term for "free"

            var hasMiddleTerm = digits % 2 != 0;
            
            if (hasMiddleTerm)
            {
                (left, var middleTerm) = Math.DivRem(left, 10);
                DEBUG(() => Console.WriteLine($"Middle Term: {middleTerm}"));

                if (middleTerm < 9)
                {
                    // The middle term is actually the leftMiddleTermInclusive's last digit!
                    goto AddLeftReversed;
                }
            }
            
            // Wow this is really unfortunate...Since left will change, we will have to reverse left to right again...
            
            DEBUG(() => Console.WriteLine($"Unfortunate: {leftMiddleTermInclusive}(L+M)"));
            
            // L  M T
            // 50 9 70 ( L-R: 05 )-> 51 0 15
            // Increment last digit of left term, reverse it, and now it is the right term.
            left++;
            
            // Well, the number of digits will remain the same, unless...
            // 99 9 70 
            // But that is not possible, since L-R of 99 is well...99. You can't have L-R of > 99.

            DEBUG(() => Console.WriteLine($"Unfortunate: {leftMiddleTermInclusive}(L+M)"));

            leftReversed = ReverseDigits(left);
            
            DEBUG(() => Console.WriteLine($"Unfortunate: L-R ( Post ): {leftReversed}"));
            
            AddLeftReversed:
            var numWithoutRightHalf = leftMiddleTermInclusive * divisor;
            
            DEBUG(() => Console.WriteLine(numWithoutRightHalf));
            
            num = numWithoutRightHalf + leftReversed;
            
            DEBUG(() => Console.WriteLine(num));

            Ret:
            return num;
            
            TwoDigits:
            // 10 to 99. Any two-digit divisible by 11 is a palindrome. E.x. 11, 22, 33 ... up to 99.
            
            // However, a number less than 11 mod 11 would just return the number itself...
            if (num > 11)
            {
                var rem = num % 11;
                
                if (rem != 0)
                {
                    num += (11 - rem);
                }
            }

            else
            {
                num = 11;
            }
            
            goto Ret;
        }
    }
}