using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace IntelogyCommonHelper
{
    public static class Extensions
    {
        public static void AppendUrlEncoded(this StringBuilder sb, string name, string value)
        {
            if (sb.Length != 0)
                sb.Append("&");
            sb.Append(HttpUtility.UrlEncode(name));
            sb.Append("=");
            sb.Append(HttpUtility.UrlEncode(value));
        }

        public static string Strip(this string value)
        {
            if (value == null)
                return string.Empty;

            int length = value.Length;
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                var x = value[i];
                if (char.IsLetterOrDigit(x))
                {
                    builder.Append(x);
                }

            }
            return builder.ToString();
        }

        public static string StripNonDigits(this string value)
        {
            if (value == null)
                return string.Empty;

            value = value.Trim();

            int length = value.Length;
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                var x = value[i];
                if (char.IsDigit(x))
                {
                    builder.Append(x);
                }

            }
            return builder.ToString();
        }

        public static string StripNonPhone(this string value, bool ifEmptyReturnNull = false)
        {
            var phone = new string(StripNonDigits(value).Reverse().ToArray());
            if (ifEmptyReturnNull && string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }
            else
            {
                return phone;
            }
        }
        public static DateTime MinDate = new DateTime(1901, 01, 01);
        public static DateTime GetDate(this IDataRecord idata, int index)
        {
            DateTime value;
            if (idata.IsDBNull(index) || (value = idata.GetDateTime(index)) <= MinDate)
            {
                return DateTime.MinValue;
            }
            else
            {
                return value;
            }
        }

        public static string GetDateString(this IDataRecord idata, int index)
        {
            DateTime value;
            if (idata.IsDBNull(index) || (value = idata.GetDateTime(index)) <= MinDate)
            {
                return string.Empty;
            }
            else
            {
                return value.ToString("yyyy-MM-dd");
            }
        }

        public static bool GetBool(this IDataRecord idata, int index)
        {
            return idata.IsDBNull(index) ? false : idata.GetInt64(index) != 0;
        }

        public static int GetInt(this IDataRecord idata, int index)
        {
            if (idata.IsDBNull(index))
            {
                return 0;
            }
            else
            {
                return idata.GetInt32(index); //.CleanString();
            }
        }

        public static double GetDbl(this IDataRecord idata, int index)
        {
            if (idata.IsDBNull(index))
            {
                return 0;
            }
            else
            {
                return idata.GetDouble(index); //.CleanString();
            }
        }

        public static long GetLong(this IDataRecord idata, int index)
        {
            if (idata.IsDBNull(index))
            {
                return 0;
            }
            else
            {
                return idata.GetInt64(index); //.CleanString();
            }
        }

        public static decimal GetCurrency(this IDataRecord idata, int index)
        {
            if (idata.IsDBNull(index))
            {
                return 0;
            }
            else
            {
                return idata.GetDecimal(index); //.CleanString();
            }
        }

        public static string GetText(this IDataRecord idata, int index)
        {
            string value;
            if (idata.IsDBNull(index) || string.IsNullOrWhiteSpace(value = idata.GetString(index)))
            {
                return null;
            }
            else
            {
                return value; //.CleanString();
            }
        }
    }
}
