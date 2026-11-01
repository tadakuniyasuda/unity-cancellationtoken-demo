using System;
using UnityEngine;

public class SettingPanelIUI : MonoBehaviour
{
    void Start()
    {
        DayNightNotifier.OnDayNightChanged += HandleDayNightChanged;
    }

    void OnDisable()
    {
        DayNightNotifier.OnDayNightChanged -= HandleDayNightChanged;
    }



    void HandleDayNightChanged(DayNight state)
    {
        Refresh();
        DayNight newstate = state;
        Debug.Log($"Day Night Changed: {newstate}");
    }

    void Refresh()
    {
        // Refresh the UI based on the current day/night state
        if (DayNightNotifier.current == DayNight.Day)
        {
            // Update UI for day mode
            Debug.Log("UI updated for Day mode");
        }
        else
        {
            // Update UI for night mode
            Debug.Log("UI updated for Night mode");
        }
    }
}