using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class AnalyticsManager : MonoBehaviour
{
    async void Start()
    {
        Debug.Log($"[{nameof(AnalyticsManager)}]: Initializing Unity services ...");

        await UnityServices.InitializeAsync();

        Debug.Log($"[{nameof(AnalyticsManager)}]: Unity services initialized");

        AskForConsent();
    }

    void AskForConsent()
    {
        // ... show the player a UI element that asks for consent.
        ConsentGiven();
    }

    void ConsentGiven()
    {
        Debug.Log($"[{nameof(AnalyticsManager)}]: Start Analytics data collection");
        
        AnalyticsService.Instance.StartDataCollection();
    }
}
