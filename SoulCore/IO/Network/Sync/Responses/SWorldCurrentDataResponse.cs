﻿using System;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record SWorldCurrentDataResponse
    {
        public long UnixTimeSeconds { get; }
        public ushort Year { get; }
        public ushort Month { get; }
        public ushort Day { get; }
        public ushort Hour { get; }
        public ushort Minute { get; }
        public ushort Second { get; }
        public ushort IsDaylightSavingTime { get; }

        public SWorldCurrentDataResponse()
        {
            DateTimeOffset dateTime = DateTimeOffset.Now;

            UnixTimeSeconds = dateTime.ToUnixTimeSeconds();
            Year = (ushort)dateTime.Year;
            Month = (ushort)dateTime.Month;
            Day = (ushort)dateTime.Day;
            Hour = (ushort)dateTime.Hour;
            Minute = (ushort)dateTime.Minute;
            Second = (ushort)dateTime.Second;
            IsDaylightSavingTime = Convert.ToUInt16(TimeZoneInfo.Local.IsDaylightSavingTime(dateTime));
        }
    }
}
