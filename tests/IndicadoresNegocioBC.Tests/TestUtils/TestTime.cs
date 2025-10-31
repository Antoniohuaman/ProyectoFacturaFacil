using System;

namespace IndicadoresNegocioBC.Tests.TestUtils
{
    public static class TestTime
    {
        public static readonly TimeZoneInfo LimaTz = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

        // Fecha base fija: mitad de mes, 10:00 local Lima
        public static DateTime BaseLocal() => new DateTime(2025, 11, 15, 10, 0, 0, DateTimeKind.Unspecified);
        public static DateTime BaseUtc() => TimeZoneInfo.ConvertTimeToUtc(BaseLocal(), LimaTz);

        public static DateTime AyerUtc() => BaseUtc().AddDays(-1);
        public static DateTime HoyUtc() => BaseUtc();
        public static DateTime MananaUtc() => BaseUtc().AddDays(1);

        public static DateOnly InicioMesLocal() => new DateOnly(2025, 11, 1);
        public static DateOnly FinMesLocal() => new DateOnly(2025, 11, 30);
    }
}
