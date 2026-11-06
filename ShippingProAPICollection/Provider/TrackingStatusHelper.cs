using ShippingProAPICollection.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShippingProAPICollection.Provider
{
    public static class TrackingStatusHelper
    {
        public static string TranslateTrackingStatusMessage(this TrackingStatusType trackingStatusType)
        {
            return trackingStatusType switch
            {
                TrackingStatusType.DELIVERED => "Das Paket wurde erfolgreich an den Empfänger zugestellt.",
                TrackingStatusType.FAILED => "Das Paket konnte nicht zugestellt werden; der Zustellversuch ist fehlgeschlagen.",
                TrackingStatusType.TRANSIT => "Das Paket befindet sich im Transport und ist auf dem Weg zum Zielort.",
                TrackingStatusType.UNKNOWN => "Der Status des Pakets ist unbekannt; es liegen keine Tracking-Informationen vor.",
                _ => throw new ArgumentOutOfRangeException(nameof(trackingStatusType), trackingStatusType, null)
            };
        }
    }
}
