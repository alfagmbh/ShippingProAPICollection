namespace ShippingProAPICollection.Provider.GLS.Entities
{
    /// <summary>
    /// The current status of the parcel
    /// </summary>
    public enum GLSParcelStatus
    {
        PLANNEDPICKUP,
        INPICKUP,
        NOTPICKEDUP,
        PREADVICE,
        INTRANSIT,
        INDELIVERY,
        DELIVEREDPS,
        DELIVERED,
        INWAREHOUSE,
        NOTDELIVERED,
        CANCELED,
        FINAL
    }
}
