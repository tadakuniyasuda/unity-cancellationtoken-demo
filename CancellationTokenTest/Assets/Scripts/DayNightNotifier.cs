using System;

public enum DayNight
{
    Day,
    Night
}

public static class DayNightNotifier
{
    public static DayNight current = DayNight.Day;

    public static event Action<DayNight> OnDayNightChanged;

    public static void Broadcast(DayNight state)
    {
        current = state;
        OnDayNightChanged?.Invoke(state);
    }
}