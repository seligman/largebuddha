using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Utils
{
    public class TilePos
    {
        /*
            Javascript Version:
        
        // NOTE: The "13" down below must match the 13 in the function, as much the number
        // of characters for 'ret'
         
        function tilePos(type, level, x, y, maxLevel){
            var val=(((1<<(level*2))-1)&0x55555555)+x+(y*(1<<level))+(type*(((1<<((maxLevel+1)*2))-1)&0x55555555));
            var ret=['a','a','/','a','a','/','a','a'];
            var pos=7;
            while (val>0){
                ret[pos--]=String.fromCharCode(val%13+97);
                val=(val-(val%13))/13;
                if (ret[pos]!='a'){pos--;}
            }
            return ret.join('');
        }

        */

        //   Types: 0 .. 1
        //  Levels: 0 .. maxLevel
        public static string GetTilePos(int type, int level, int x, int y, int maxLevel)
        {
            int val =
                // This calculates the total of all previous levels
                (((1 << (level * 2)) - 1) & 0x55555555) +
                // Add one for each x over
                x +
                // And add the max of x for the level for each y
                (y * (1 << level));

            // If this is a second or third type, then add an offset to the end
            while (type > 0)
            {
                val += (((1 << ((maxLevel + 1) * 2)) - 1) & 0x55555555);
                type--;
            }

            // At this point, val tops out at different values depending on maxLevel
            //  0 -           2
            //  1 -          14
            //  2 -          62
            //  3 -         254
            //  4 -        1022
            //  5 -        4094
            //  6 -       16382
            //  7 -       65534
            //  8 -      262142
            //  9 -     1048574
            // 10 -     4194302
            // 11 -    16777214
            // 12 -    67108862
            // 13 -   268435454
            // 14 - (Not supported)

            // This formula shows how many characters are needed for a given number 
            // of alphaChars:
            //
            //   Math.Ceiling(Math.Log(Math.Pow(4,maxLevel))/Math.Log(alphaChars))
            //
            // This formula shows how many alphaChars are needed for a given number 
            // of digits:
            //
            //   Math.Ceiling(Math.Pow(Math.Pow(4,maxLevel),1.0/numDigits))
            //
            // Right now for maxLevel of 11 and alphaChars of 13, we need 6 digits

            char[] ret = new char[] { 'a', 'a', '\\', 'a', 'a', '\\', 'a', 'a' };
            string letters = "abcdefghijklmnopqrstuvwxyz";
            int pos = ret.Length - 1;

            while (val > 0)
            {
                if (pos < 0)
                {
                    throw new Exception("Not enough characters to fill the index");
                }
                ret[pos] = letters[val % 13]; // alphaChars
                val /= 13; // alphaChars
                pos--;
                if (pos > 0)
                {
                    if (ret[pos] != 'a')
                    {
                        pos--;
                    }
                }
            }

            return new string(ret);
        }
    }
}
