// #define PRINT_DEBUG

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NextPalindrome
{
    internal static unsafe class Program
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void Main(string[] args)
        {
            // var multipleTableGetDigitsFast = MultiplesTable + 1;
            //
            // GetDigitsFast(1073741824, multipleTableGetDigitsFast);
            //
            // return;
            
            // How to check codegen:
            // Mac:
            // export DOTNET_JitDisasm="Loop"
            // dotnet run -c Release

            Validate();

            Console.WriteLine("Churning!");
            
            var startTime = DateTime.UtcNow;
                
            Loop();
            
            var endTime = DateTime.UtcNow;

            var totalTime = endTime - startTime;

            Console.WriteLine($"Done! Took {totalTime.Minutes} Min(s) {totalTime.Seconds} Second(s)!");

            Console.ReadKey();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Loop()
        {
            for (uint I = 0; I < int.MaxValue; I++)
            {
                NextPalindrome(I);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetDigits(uint num)
        {
            var digits = 1;
            
            while (num > 9)
            {
                num /= 10;
                digits++;
            }

            return digits;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Validate(bool skipGetDigitsFastValidate = true)
        {
            if (!skipGetDigitsFastValidate)
            {
                var multipleTableGetDigitsFast = MultiplesTable + 1;
            
                for (uint i = 0; i < uint.MaxValue; i++)
                {
                    if (GetDigitsFast(i, multipleTableGetDigitsFast) != GetDigits(i))
                    {
                        throw new Exception($"{nameof(GetDigitsFast)} failed for: {i}");
                    }
                }
            }
            
            for (uint i= 0; i < 1_000_000; i++)
            {
                DEBUG(() => Console.WriteLine($"Current Term: {i}"));
                
                if (NextPalindrome(i) != NextPalindromeNaive(i))
                {
                    throw new Exception($"You're stupid [ {i} ]");
                }
            }

            Console.WriteLine("You're not so stupid after all!");

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
        private static int GetDigitsFast(uint num, uint* multiplesOf10Table)
        {
            // uint.MaxValue is 4_294_967_295, which is 10 digits. Furthermore, we won't be able to fit a 9th element
            // in multiplesOf10Table, because the value is too huge for uint.
            if (num >= 1_000_000_000)
            {
                goto Ret10;
            }
            
            var indexOfHighestSetBit = 31 - BitOperations.LeadingZeroCount(num);
            
            // Int mul followed by int div ( Truncates ).
            // Do not convert it to indexOfHighestSetBit * ( 77 / 256 ), as it will become int * float
            var estDigits = (indexOfHighestSetBit * 77) / 256;

            var tableVal = multiplesOf10Table[estDigits];
            
            var offset = (num >= tableVal) ? 2 : 1;

            var result = estDigits + offset;

            DEBUG(() =>
            {
                if (GetDigits(num) != result)
                {
                    throw new Exception($"{nameof(GetDigitsFast)} failed for: {num}");
                }
            });
            
            return result;
            
            Ret10:
            return 10;
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

        private static readonly uint* MultiplesTable = (uint*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(MultiplesOf10));
        
        // [MethodImpl(MethodImplOptions.NoInlining)] // For checking codegen
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint NextPalindrome(uint num)
        {
            var multiplesTable = MultiplesTable;
            
            // This should be constant-folded
            var multipleTableGetDigitsFast = multiplesTable + 1;

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
            
            DEBUG(() => Console.WriteLine($"Left + M | R -> {leftMiddleTermInclusive} | {right}"));
            
            // A term like 1001 would fail, as 01 is treated as 1, so $"{leftMiddleTermInclusive}{right}" will become 101
            // Debug.Assert($"{leftMiddleTermInclusive}{right}" == num.ToString());

            var hasMiddleTerm = digits % 2 != 0;

            uint left, middleTerm;

            if (hasMiddleTerm)
            {
                (left, middleTerm) = Math.DivRem(leftMiddleTermInclusive, 10);
            }

            else
            {
                left = leftMiddleTermInclusive;
                middleTerm = uint.MaxValue; // Any middleTerm value > 9 denotes absence of middleTerm value
            }

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
            
            // Any middleTerm value > 9 denotes absence of middleTerm value
            // Instead of if (hasMiddleTerm && middleTerm < 9 ), since we already perform hasMiddleTerm check above.
            if (middleTerm < 9)
            {
                // The middle term is actually the leftMiddleTermInclusive's last digit!
                goto AddLeftReversed;
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
            return TwoDigitsImpl(num);
                
            // Don't pollute hot path...it only happens for a minority subset of numbers [ 10 to 99 ]
            [MethodImpl(MethodImplOptions.NoInlining)]
            uint TwoDigitsImpl(uint num)
            {
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

                return num;
            }
        }
    }
}