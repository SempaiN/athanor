using System;
using System.Globalization;

namespace Athanor.Infra
{
    public static class NumberFormat
    {
        static readonly string[] suffixes =
            { "", " K", " M", " B", " T", " Qa", " Qi", " Sx", " Sp", " Oc", " No", " Dc" };

        /// 1234 → "1.23 K", 987 → "987", 12345678 → "12.3 M"
        public static string Fmt(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "∞";
            if (v < 0) return "-" + Fmt(-v);
            if (v < 1000) return Math.Floor(v).ToString(CultureInfo.InvariantCulture);

            int mag = (int)Math.Floor(Math.Log10(v) / 3);
            if (mag >= suffixes.Length) mag = suffixes.Length - 1;
            double scaled = v / Math.Pow(1000, mag);

            string digits = scaled >= 100 ? "0" : scaled >= 10 ? "0.#" : "0.##";
            return scaled.ToString(digits, CultureInfo.InvariantCulture) + suffixes[mag];
        }
    }
}
