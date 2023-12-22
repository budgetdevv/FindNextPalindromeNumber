#define PRINT_DEBUG

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NextPalindrome // Note: actual namespace depends on the project name.
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            NextPalindrome(899);
        }

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

        public static void DEBUG(Action action)
        {
            #if PRINT_DEBUG
            action();
            #endif
        }

        private static int NextPalindrome(int num)
        {
            var digits = GetDigits(num);

            DEBUG(() => Console.WriteLine(digits));
            
            // Division truncates. E.x. 9 / 2 = 4. We don't care about middle digit.
            var digitsPerHalf = digits / 2;

            var divisor = digitsPerHalf * 10;
            
            if (digits == 1)
            {
                goto Ret;
            }

            var (leftMiddleTermInclusive, right) = Math.DivRem(num, divisor);

            DEBUG(() => Console.WriteLine($"{leftMiddleTermInclusive}{right}"));
            
            Debug.Assert($"{leftMiddleTermInclusive}{right}" == num.ToString());

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
            
            // L  M T
            // 50 9 70 ( L-R: 05 )-> 51 0 15
            // Increment last digit of left term, reverse it, and now it is the right term.
            left++;
            
            // Well, the number of digits will remain the same, unless...
            // 99 9 70 
            // But that is not possible, since L-R of 99 is well...99. You can't have L-R of > 99.

            // TODO: Will we be using this moving forward?
            leftMiddleTermInclusive = leftMiddleTermInclusive - middleTerm + 10;
            
            // Middle term is now zero. E.x. 699 ( L-R: 6 ) -> 707 ( L-R: 7 ) [ digitsPerHalf: 1 ], 60999 ( L-R: 06 ) -> 61016 ( L-R: 16 ) [ digitsPerHalf: 2 ]
            middleTerm = 0;

            // leftReversed is treated as right
            
            // right += (digitsPerHalf - 1) * 10;
            
            // right = leftReversed + ((digitsPerHalf - 1) * 10);

            // 192 ( L-R: 1 ) 
            // R = 1 ( Which is L-R of 1 ) + 10 ^ ( 1 ( Which is digitsPerHalf ) ) = 1 + 1 = 2
            // The end result is ( left++ ( Which is 1 + 1 ) ) middleTerm ( Set to 0 ) ( R, which is 2 ) ->
            // ( 1 + 1 ) ( 0 ) ( 2 ) ->
            // 2 0 2
            
            // 12992 ( L-R: 21 ) 
            // R = 21 ( Which is L-R of 12 ) + 10 ^ ( 2 ( Which is digitsPerHalf ) ) = 21 + 10 = 31
            // The end result is ( left++ ( Which is ( 12 + 1 ) ) middleTerm ( Set to 0 ) ( R, which is 31 ) ->
            // ( 12 + 1 ) ( 0 ) ( 31 ) ->
            // 13 0 31
            leftReversed = leftReversed + (int) Math.Pow(10, (digitsPerHalf - 1));

            Console.WriteLine($"Unfortunate: {left}{middleTerm}{right} | {leftMiddleTermInclusive}{right} | LR: {leftReversed} | D-Half: {digitsPerHalf}");
            
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
        }
    }
}