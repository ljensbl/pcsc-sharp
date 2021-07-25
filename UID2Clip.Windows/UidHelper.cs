namespace UidHelper
{
    using System;

    public class UidHelper
    {
        public static string FormatUid(ulong id, UidFormat format)
        {
            switch (format)
            {
                case UidFormat.SPCWiegand:
                case UidFormat.SPCAR618X:
                case UidFormat.ACTMifareRevSerial:
                case UidFormat.OMNIS:
                    {
                        string asHex = $"{id:X}";
                        string[] asHexParts = SplitHexString(asHex, '\0');
                        Array.Reverse(asHexParts);
                        return $"{Convert.ToUInt64(string.Join("", asHexParts), 16)}";
                    }
                case UidFormat.ACTMifareSerial:
                    return $"{id:D10}";
                case UidFormat.SPCPace:
                    {
                        string asHex = $"{id:X}";
                        string[] asHexParts = SplitHexString(asHex, '\0');
                        return (Convert.ToUInt64(string.Join("", asHexParts), 16) * 0x100).ToString();
                    }
                case UidFormat.HEX:
                    {
                        return $"{id:X}";
                    }
                case UidFormat.HEXRev:
                    {
                        string asHex = $"{id:X}";
                        string[] asHexParts = SplitHexString(asHex, '\0');
                        Array.Reverse(asHexParts);
                        return string.Join("", asHexParts);
                    }
                default:
                    return null;
            }
        }

        public static ulong ParseUid(UidFormat uidFormat, string input, char hexInputSeparator = '-')
        {
            ulong result = 0;
            string[] hexParts;
            string hexString;

            switch (uidFormat)
            {
                case UidFormat.HEX:

                    if (hexInputSeparator != '\0' && input.Contains(hexInputSeparator))
                    {
                        hexParts = input.Split(hexInputSeparator);
                        hexString = string.Join("", hexParts);
                    }
                    else
                    {
                        hexString = input;
                    }
                    result = Convert.ToUInt64(hexString, 16);
                    break;
                case UidFormat.HEXRev:
                    hexParts = SplitHexString(input, hexInputSeparator);

                    Array.Reverse(hexParts);
                    hexString = string.Join("", hexParts);
                    result = Convert.ToUInt64(hexString, 16);

                    break;
                case UidFormat.SPCPace:
                    result = ulong.Parse(input) / 0x100;
                    var numberAsHexParts = SplitHexString($"{result:x}", '\0');
                    result = Convert.ToUInt64(string.Join("", numberAsHexParts), 16);
                    break;
                case UidFormat.SPCWiegand:
                case UidFormat.SPCAR618X:
                case UidFormat.ACTMifareRevSerial:
                case UidFormat.OMNIS:
                    result = ulong.Parse(input);
                    numberAsHexParts = SplitHexString($"{result:x}", '\0');
                    Array.Reverse(numberAsHexParts);
                    result = Convert.ToUInt64(string.Join("", numberAsHexParts), 16);
                    break;
                case UidFormat.ACTMifareSerial:
                    result = ulong.Parse(input);
                    numberAsHexParts = SplitHexString($"{result:x}", '\0');
                    result = Convert.ToUInt64(string.Join("", numberAsHexParts), 16);
                    break;
                default:
                    break;
            }

            return result;
        }

        private static string[] SplitHexString(string input, char hexInputSeparator)
        {
            string tempHex;
            string[] hexParts;


            if (hexInputSeparator != '\0' && input.Contains(hexInputSeparator))
            {
                hexParts = input.Split(hexInputSeparator);
            }
            else
            {
                if (input.Length > 8)
                {
                    hexParts = new string[7];
                }
                else
                {
                    hexParts = new string[4];
                }

                tempHex = ((input.Length % 2 == 0) ? "" : "0") + input;

                for (int i = 0; i < tempHex.Length / 2; i++)
                {
                    hexParts[i] = $"{tempHex[i * 2]}{tempHex[i * 2 + 1]}";
                }
            }

            return hexParts;
        }

    }
}
