using UnityEngine;

public class ToggleDayNight : MonoBehaviour
{
    public void Toggle()
    {
        var newState = DayNightNotifier.current == DayNight.Day ? DayNight.Night : DayNight.Day;
        DayNightNotifier.Broadcast(newState);
    }
}
