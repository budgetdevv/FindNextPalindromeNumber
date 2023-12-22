// define PRINT_DEBUG

using System.Diagnostics;
using System.Runtime.CompilerServices;

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
                for (int I = 0; I < 1_000_000; I++)
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
                for (int I = 0; I < int.MaxValue; I++)
                {
                    NextPalindrome(I);
                }
            }
            
            return;
            
            // Generated with AI
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsPalindrome(int n)
            {
                if (n < 0 || (n % 10 == 0 && n != 0)) 
                {
                    return false;
                }

                int reversedNumber = 0;
                while (n > reversedNumber)
                {
                    reversedNumber = reversedNumber * 10 + n % 10;
                    n /= 10;
                }

                return n == reversedNumber || n == reversedNumber / 10;
            }

            // Generated with AI
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int NextPalindromeNaive(int n)
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
        private static int ReverseDigits(int num)
        {
            var newNum = 0;

            while (true)
            {
                var cont = num > 9;
                
                num = Math.DivRem(num, 10, out var rem);
                
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NextPalindrome(int num)
        {
            var digits = GetDigits(num);

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
            
            var divisor = (int) Math.Pow(10, digitsPerHalf);
            
            DEBUG(() => Console.WriteLine($"Divisor: {divisor}"));

            var (leftMiddleTermInclusive, right) = Math.DivRem(num, divisor);

            // var leftMiddleTermInclusive = num / divisor;
            //
            // var right = num - (leftMiddleTermInclusive * divisor);
            
            DEBUG(() => Console.WriteLine($"Left + M | R -> {leftMiddleTermInclusive} | {right}"));

            
            // A term like 1001 would fail, as 01 is treated as 1, so $"{leftMiddleTermInclusive}{right}" will become 101
            // Debug.Assert($"{leftMiddleTermInclusive}{right}" == num.ToString());

            var left = leftMiddleTermInclusive;

            var hasMiddleTerm = digits % 2 != 0;

            Unsafe.SkipInit(out int middleTerm);
            
            if (hasMiddleTerm)
            {
                left = Math.DivRem(left, 10, out middleTerm);
                DEBUG(() => Console.WriteLine($"Middle Term: {middleTerm}"));
            }

            DEBUG(() => Console.WriteLine($"{left}{right}"));
            
            var leftReversed = ReverseDigits(left);

            DEBUG(() => Console.WriteLine($"R: {right} | L-R: {leftReversed}"));
            
            if (leftReversed >= right)
            {
                goto GreaterOrEquals;
            }
            
            // Unfortunately, this will require more work. E.x. 619: 9 > 6, so we need to add until 626.
            // Here's the cool part: If there's a middle term, and while it is < 9, we can "reset" the right term for "free"

            int numWithoutRightHalf;
            
            if (hasMiddleTerm && middleTerm < 9)
            {
                // The middle term is actually the leftMiddleTermInclusive's last digit!
                leftMiddleTermInclusive += 1;
                numWithoutRightHalf = leftMiddleTermInclusive * divisor;
                goto AddLeftReversed;
            }
            
            // Wow this is really unfortunate...

            if (hasMiddleTerm)
            {
                leftMiddleTermInclusive = leftMiddleTermInclusive - middleTerm + 10;
            }

            else
            {
                leftMiddleTermInclusive++;
            }
            
            DEBUG(() => Console.WriteLine($"Unfortunate: {leftMiddleTermInclusive}(L+M)"));
            
            // Middle term is now zero. E.x. 699 ( L-R: 6 ) -> 707 ( L-R: 7 ) [ digitsPerHalf: 1 ], 60999 ( L-R: 06 ) -> 61016 ( L-R: 16 ) [ digitsPerHalf: 2 ]
            middleTerm = 0;

            // leftReversed is treated as right
            
            // right += (digitsPerHalf - 1) * 10;
            
            // right = leftReversed + ((digitsPerHalf - 1) * 10);

            
            // Unreliable approach, due to overflow. E.x. 91 + 1 = 101
            // // 192 ( L-R: 1 ) 
            // // R = 1 ( Which is L-R of 1 ) + 10 ^ ( 1 ( Which is digitsPerHalf ) ) = 1 + 1 = 2
            // // The end result is ( left++ ( Which is 1 + 1 ) ) middleTerm ( Set to 0 ) ( R, which is 2 ) ->
            // // ( 1 + 1 ) ( 0 ) ( 2 ) ->
            // // 2 0 2
            //
            // // 12992 ( L-R: 21 ) 
            // // R = 21 ( Which is L-R of 12 ) + 10 ^ ( 2 ( Which is digitsPerHalf ) ) = 21 + 10 = 31
            // // The end result is ( left++ ( Which is ( 12 + 1 ) ) middleTerm ( Set to 0 ) ( R, which is 31 ) ->
            // // ( 12 + 1 ) ( 0 ) ( 31 ) ->
            // // 13 0 31
            //
            // DEBUG(() => Console.WriteLine($"Unfortunate: L-R ( Pre ): {leftReversed}"));
            //
            // leftReversed += (int) Math.Pow(10, (digitsPerHalf - 1));
            
            // L  M T
            // 50 9 70 ( L-R: 05 )-> 51 0 15
            // Increment last digit of left term, reverse it, and now it is the right term.
            left++;
            
            // Well, the number of digits will remain the same, unless...
            // 99 9 70 
            // But that is not possible, since L-R of 99 is well...99. You can't have L-R of > 99.

            DEBUG(() => Console.WriteLine($"Unfortunate: {leftMiddleTermInclusive}(L+M) {left}(L) {middleTerm}(M)"));

            leftReversed = ReverseDigits(left);
            
            DEBUG(() => Console.WriteLine($"Unfortunate: L-R ( Post ): {leftReversed}"));
            
            DEBUG(() => Console.WriteLine($"Unfortunate: {left}(L){middleTerm}(M){right}(R) | {leftMiddleTermInclusive}(L+M){right}(R) | LR: {leftReversed} | D-Half: {digitsPerHalf}"));
            
            goto GreaterOrEquals;
            
            Ret:
            return num;
            
            GreaterOrEquals:
            numWithoutRightHalf = leftMiddleTermInclusive * divisor;
            
            AddLeftReversed:
            DEBUG(() => Console.WriteLine(numWithoutRightHalf));
            
            num = numWithoutRightHalf + leftReversed;
            
            DEBUG(() => Console.WriteLine(num));

            goto Ret;
            
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