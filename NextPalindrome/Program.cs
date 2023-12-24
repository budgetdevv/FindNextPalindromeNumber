﻿// #define PRINT_DEBUG

using System.Diagnostics;
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
            ReverseDigitsFast(100_000, ReversedTwoDigitsTable, 6);
            
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

        private static (uint quotient, uint remainder) DivRemFast(uint num, uint divisor)
        {
            return Math.DivRem(num, divisor);
        }
        
        private static uint GetHighestPalindrome()
        {
            #if RELEASE
            return 4_294_884_924;
            #endif
            
            var num = uint.MaxValue;

            while (true)
            {
                num--;

                if (IsPalindrome(num))
                {
                    return num;
                }
            }
        }

        private static bool IsPalindrome(uint num)
        {
            // Most of the code are copied from GetCurrentOrNextPalindrome(), so jump to that method if you are looking
            // for comments.
            
            var multiplesTable = MultiplesOf10TablePtr;
            
            // This should be constant-folded
            var multipleTableGetDigitsFast = multiplesTable + 1;

            var digits = GetDigitsFast(num, multipleTableGetDigitsFast);
            
            // Forward jump to favor slower paths.
            switch (digits)
            {
                case 1:
                    goto OneDigit;
                case 2:
                    goto TwoDigits;
            }
            
            var digitsPerHalf = digits / 2;
            
            var divisor = multiplesTable[digitsPerHalf]; 
            
            var (leftMiddleTermInclusive, right) = Math.DivRem(num, divisor);
            
            var hasMiddleTerm = digits % 2 != 0;

            uint left;

            if (hasMiddleTerm)
            {
                left = leftMiddleTermInclusive / 10;
            }

            else
            {
                left = leftMiddleTermInclusive;
            }
            
            var leftReversed = ReverseDigits(left);

            return leftReversed == right;
            
            TwoDigits:
            return TwoDigitsImpl(num);
                
            OneDigit:
            return true;
            
            // Don't pollute hot path...it only happens for a minority subset of numbers [ 10 to 99 ]
            [MethodImpl(MethodImplOptions.NoInlining)]
            bool TwoDigitsImpl(uint num)
            {
                // The only two-digit value < 11 is 10.
                return num != 10 && num % 11 == 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Loop()
        {
            for (uint I = 0; I < int.MaxValue; I++)
            {
                GetCurrentOrNextPalindrome(I);
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
                const int ITERATIONS = 1_000_000;
                
                var multipleTableGetDigitsFast = MultiplesOf10TablePtr + 1;

                var highestPalindrome = GetHighestPalindrome();
                
                for (uint i = highestPalindrome; i >= highestPalindrome - ITERATIONS; i++)
                {
                    if (GetDigitsFast(i, multipleTableGetDigitsFast) != GetDigits(i))
                    {
                        throw new Exception($"{nameof(GetDigitsFast)} failed for: {i}");
                    }
                }
            }

            if (true)
            {
                var multiplesOf10Table = MultiplesOf10TablePtr;
                
                for (uint i = 0; i < 99_999; i++)
                {
                    if (ReverseDigitsFast(i, ReversedTwoDigitsTable, GetDigits(i)) != ReverseDigits(i))
                    {
                        throw new Exception($"{nameof(ReverseDigitsFast)} failed: {i}");
                    }
                }
            }
            
            
            for (uint i= 0; i < 1_000_000; i++)
            {
                #if PRINT_DEBUG
                Console.WriteLine($"Current Term: {i}");
                #endif
                
                if (GetCurrentOrNextPalindrome(i) != NextPalindromeNaive(i))
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

            #if PRINT_DEBUG
            if (GetDigits(num) != result)
            {
                throw new Exception($"{nameof(GetDigitsFast)} failed for: {num}");
            }
            #endif
            
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

                return newNum;
            }
        }

        // private static uint[] GenerateReverseTwoDigitsTable()
        // {
        //     // Yes, we don't have to handle numbers < 10, but we still allocate extra anyway.
        //     // This will save us from decrementing by 11 every loop.
        //     // We also generate reverse variant for 0 - 9 ( Which will return the same number anyway ) to simplify
        //     // loop in ReverseDigitsFast(), allowing to do newNum += reversedTwoDigitsTable[rem] even when rem <= 9.
        //     const uint START = 0, END = 99;
        //     
        //     // + 1, as array indexes are 0-based.
        //     // E.x. An index of 99 would mean accessing the 100 th element in the array.
        //     const int LENGTH = (int) (END + 1);
        //
        //     var arr = GC.AllocateUninitializedArray<uint>(LENGTH, true);
        //     
        //     var num = START;
        //     
        //     for (; num <= END; num++)
        //     {
        //         arr[num] = ReverseDigits(num);
        //     }
        //     
        //     return arr;
        // }
        //
        // private static readonly uint[] ReversedTwoDigitsTable = GenerateReverseTwoDigitsTable();
        
        // Yes, we don't have to handle numbers < 10, but we still allocate extra anyway.
        // This will save us from decrementing by 11 every loop.
        // We also generate reverse variant for 0 - 9 ( Which will return the same number anyway ) to simplify
        // loop in ReverseDigitsFast(), allowing to do newNum += reversedTwoDigitsTable[rem] even when rem <= 9.
        private const uint REVERSE_TWO_DIGITS_TABLE_START = 0, 
                           REVERSE_TWO_DIGITS_TABLE_END = 99;
        
        // + 1, as array indexes are 0-based.
        // E.x. An index of 99 would mean accessing the 100 th element in the array.
        private const int REVERSE_TWO_DIGITS_TABLE_LENGTH = (int) (REVERSE_TWO_DIGITS_TABLE_END + 1);
        
        private static sbyte* GenerateReverseTwoDigitsTable()
        {
            var table = (sbyte*) NativeMemory.AlignedAlloc(REVERSE_TWO_DIGITS_TABLE_LENGTH * sizeof(sbyte), 64);
            
            var num = REVERSE_TWO_DIGITS_TABLE_START;
            
            // Could be more efficient, but who cares
            for (; num <= REVERSE_TWO_DIGITS_TABLE_END; num++)
            {
                var reversed = (num < 10) ? (int) (num * 10) : (int) ReverseDigits(num);
             
                // var endsWithZero = num % 10 == 0;
                //
                // if (endsWithZero)
                // {
                //     reversed = -reversed;
                // }
                
                table[num] = (sbyte) reversed;
            }
            
            return table;
        }
        
        private static readonly sbyte* ReversedTwoDigitsTable = GenerateReverseTwoDigitsTable();
        
        // This is mainly for debugging, don't actually use this.
        public static ReadOnlySpan<sbyte> ReversedTwoDigitsTableSpan => new ReadOnlySpan<sbyte>(ReversedTwoDigitsTable, REVERSE_TWO_DIGITS_TABLE_LENGTH);
        
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // private static uint ReverseDigitsFast(uint num, sbyte* reversedTwoDigitsTable)
        // {
        //     uint newNum = 0;
        //
        //     while (true)
        //     {
        //         var cont = num > 99;
        //         
        //         (num, var rem) = DivRemFast(num, 100);
        //         
        //         // Reverse remainder and insert. Do so even if num <= 9 ( See comment in GenerateReverseTwoDigitsTable() )
        //         
        //         // 120
        //         // 20 ( First pair )
        //         // endsWithZero: True, cont: True
        //         // 20 -> 2 ( Reverse )
        //         // 2 + sum ( 0 ) = 2 ( New sum )
        //         // sum * 10 = 20 ( New sum ) [ Happens because cont == true ]
        //         // 1 ( Second pair )
        //         // endsWithZero: True, cont: False
        //         // 1 -> 1 ( Reverse )
        //         // 1 + sum ( 20 ) = 21 ( New sum )
        //         // Multiplication is skipped, since cont == false
        //         
        //         var tableValue = reversedTwoDigitsTable[rem];
        //
        //         // Ironic, I know. But we do not consider 0 to endWithZero, as 0 multiplied by anything is well...itself
        //         var endsWithZero = tableValue < 0;
        //
        //         var addedValue = endsWithZero ? (uint) -tableValue : (uint) tableValue;
        //         
        //         newNum += addedValue;
        //
        //         if (cont)
        //         {
        //             var multiplier = (endsWithZero || num < 10) ? (uint) 10 : 100;
        //             newNum *= multiplier;
        //             continue;
        //         }
        //
        //         return newNum;
        //     }
        // }
        
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReverseDigitsFast(uint num, sbyte* reversedTwoDigitsTable, int digitCount)
        {
            //uint newNum = 0;

            var isOdd = true;
            
            Unsafe.SkipInit(out uint block1);
            Unsafe.SkipInit(out uint block2);
            Unsafe.SkipInit(out uint block3);
            Unsafe.SkipInit(out uint block2Multiplier);
            
            switch (digitCount)
            {
                case 5:
                    goto DivTwice;
                case 4:
                    isOdd = false;
                    goto DivTwice;
                case 3:
                    goto DivOnce;
                case 2:
                    return (uint) reversedTwoDigitsTable[num];
                default: // case 1:
                    goto One;
            }
            
            DivTwice:
            (num, block1) = DivRemFast(num, 100);
            block1 = (uint) reversedTwoDigitsTable[block1];
            DivOnce:
            (num, block2) = DivRemFast(num, 100);
            // var block2GreaterEqual10 = block2 >= 10;
            block2 = (uint) reversedTwoDigitsTable[block2];
            
            if (!isOdd)
            {
                goto Ret;
            }

            // block2Multiplier = block2GreaterEqual10 ? (uint) 0_00_10 : 0_01_00;
            
            ProcessRemainder:
            block3 = num; // (uint) reversedTwoDigitsTable[num];
            
            Ret:
            switch (digitCount)
            {
                case 5:
                    return (block1 * 0_10_00) + (block2 * 0_00_10) + block3;
                case 4:
                    return (block1 * 0_01_00) + block2;
                case 3:
                    return (block2 * 0_00_10) + block3;
                // case 2:
                //     return block3;
            }
            
            One:
            return num;
        }

        private static ReadOnlySpan<uint> MultiplesOf10Table => new uint[] { 1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000 };

        private static readonly uint* MultiplesOf10TablePtr = (uint*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(MultiplesOf10Table));
        
        // [MethodImpl(MethodImplOptions.NoInlining)] // For checking codegen
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetCurrentOrNextPalindrome(uint num)
        {
            var multiplesTable = MultiplesOf10TablePtr;
            
            // This should be constant-folded
            var multipleTableGetDigitsFast = multiplesTable + 1;

            var digits = GetDigitsFast(num, multipleTableGetDigitsFast);

            #if PRINT_DEBUG
            Console.WriteLine($"Digits: {digits}");
            #endif

            // Forward jump to favor slower paths.
            switch (digits)
            {
                case 1:
                    goto Ret;
                case 2:
                    goto TwoDigits;
            }
            
            // Division truncates. E.x. 9 / 2 = 4. We don't care about middle digit.
            var digitsPerHalf = digits / 2;
            
            #if PRINT_DEBUG
            Console.WriteLine($"Digits per half: {digitsPerHalf}");
            #endif
            
            var divisor = multiplesTable[digitsPerHalf]; 
            
            #if PRINT_DEBUG
            Console.WriteLine($"Divisor: {divisor}");
            #endif

            var (leftMiddleTermInclusive, right) = Math.DivRem(num, divisor);
            
            #if PRINT_DEBUG
            Console.WriteLine($"Left + M | R -> {leftMiddleTermInclusive} | {right}");
            #endif
            
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

            #if PRINT_DEBUG
            Console.WriteLine($"{left}{right}");
            #endif

            var reversedTwoDigitsTable = ReversedTwoDigitsTable;
            
            // var leftReversed = ReverseDigits(left);   
            var leftReversed = ReverseDigitsFast(left, reversedTwoDigitsTable, digitsPerHalf);

            #if PRINT_DEBUG
            Console.WriteLine($"R: {right} | L-R: {leftReversed}");
            #endif
            
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
            
            #if PRINT_DEBUG
            Console.WriteLine($"Unfortunate: {leftMiddleTermInclusive}(L+M)");
            #endif
            
            // L  M T
            // 50 9 70 ( L-R: 05 )-> 51 0 15
            // Increment last digit of left term, reverse it, and now it is the right term.
            left++;
            
            // Well, the number of digits will remain the same, unless...
            // 99 9 70 
            // But that is not possible, since L-R of 99 is well...99. You can't have L-R of > 99.

            #if PRINT_DEBUG
            Console.WriteLine($"Unfortunate: {leftMiddleTermInclusive}(L+M)");
            #endif
            
            // leftReversed = ReverseDigits(left);
            leftReversed = ReverseDigitsFast(left, reversedTwoDigitsTable, digitsPerHalf);
            #if PRINT_DEBUG
            Console.WriteLine($"Unfortunate: L-R ( Post ): {leftReversed}");
            #endif
            
            AddLeftReversed:
            var numWithoutRightHalf = leftMiddleTermInclusive * divisor;
            
            #if PRINT_DEBUG
            Console.WriteLine(numWithoutRightHalf);
            #endif
            
            num = numWithoutRightHalf + leftReversed;
            
            #if PRINT_DEBUG
            Console.WriteLine(num);
            #endif
            
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