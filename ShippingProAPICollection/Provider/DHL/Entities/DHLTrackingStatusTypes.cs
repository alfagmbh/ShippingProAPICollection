using ShippingProAPICollection.Models.Entities;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    /// <summary>
    /// see  https://developer.dhl.com/sites/default/files/2024-10/status_2.csv
    /// </summary>
    public static class DHLTrackingStatusTypes
    {
        public static readonly IReadOnlyDictionary<string, TrackingStatusType> _trackingStatusTypes = new Dictionary<string, TrackingStatusType>
        {
            #region general

            ["transit"] = TrackingStatusType.TRANSIT,
            ["delivered"] = TrackingStatusType.DELIVERED,
            ["failed"] = TrackingStatusType.FAILED,
            ["indelivery"] = TrackingStatusType.INDELIVERY,
            ["cancelled"] = TrackingStatusType.CANCELLED,

            #endregion general

            #region parcel-de

            ["AA"] = TrackingStatusType.TRANSIT, // Departed from … (initial scan at inbound hub or departure from intransit-hub)
            ["AE"] = TrackingStatusType.TRANSIT, // Pickup successful at customer site  [Pickup at sender done]
            ["AN"] = TrackingStatusType.FAILED, // Pickup not successful at customer site  [Pickup attempt failed]
            ["BV"] = TrackingStatusType.TRANSIT, // Exception occured, e.g. damaged parcel or delay in delivery  [Exception (damage/delay) - parcel still moving; review]
            ["DD"] = TrackingStatusType.TRANSIT, // Data service, e.g. a delivery day was booked by receiver  [Data service only]
            ["EE"] = TrackingStatusType.TRANSIT, // Arrived at … (arrival scan at delivery hub or intransit-hub)
            ["ES"] = TrackingStatusType.TRANSIT, // First processed by DHL, e.g. handover by customer to a DHL facility
            ["GT"] = TrackingStatusType.TRANSIT, // Money transfer  [Money transfer (COD) - no real parcel movement; review]
            ["LA"] = TrackingStatusType.INDELIVERY, // In storage, e.g. waiting for pickup by receiver  [Stored, waiting for receiver pickup]
            ["NB"] = TrackingStatusType.TRANSIT, // Normal processing during transit
            ["PO"] = TrackingStatusType.INDELIVERY, // In delivery process  [Out for delivery]
            ["VA"] = TrackingStatusType.TRANSIT, // Electronic pre advise of a shipment  [Pre-advice; enum has no PRE_TRANSIT]
            ["ZF"] = TrackingStatusType.DELIVERED, // Delivery to agency / postal office / Packstation  [Handed to agency/post/Packstation (same as ecommerce-europe 43-46)]
            ["ZN"] = TrackingStatusType.FAILED, // Delivery attempt not successful
            ["ZO"] = TrackingStatusType.TRANSIT, // Customs clearance  [Customs]
            ["ZU"] = TrackingStatusType.DELIVERED, // Delivery successful

            #endregion parcel-de

            #region ecommerce europe

            ["1"] = TrackingStatusType.TRANSIT, // Electronical shipment data has been provided to DHL
            ["2"] = TrackingStatusType.TRANSIT, // The shipment has been collected
            ["3"] = TrackingStatusType.TRANSIT, // The shipment has been dropped off at Parcelshop by sender
            ["4"] = TrackingStatusType.TRANSIT, // The shipment has been dropped off at Parcelstation by sender
            ["5"] = TrackingStatusType.TRANSIT, // The shipment has been dropped off at Postoffice by sender
            ["6"] = TrackingStatusType.TRANSIT, // The shipment has been dropped off at depot by sender
            ["7"] = TrackingStatusType.TRANSIT, // The shipment has been dropped off at sorting center by sender
            ["8"] = TrackingStatusType.TRANSIT, // The shipment has been dropped off at gateway by sender
            ["9"] = TrackingStatusType.TRANSIT, // The shipment has been picked up at sender
            ["14"] = TrackingStatusType.TRANSIT, // The shipment has been picked up at Parcelshop by courier
            ["15"] = TrackingStatusType.TRANSIT, // The shipment has been picked up at Parcelstation by courier
            ["16"] = TrackingStatusType.TRANSIT, // The shipment has been picked up at Access Point by courier
            ["17"] = TrackingStatusType.TRANSIT, // The shipment has been picked up at Postoffice by courier
            ["19"] = TrackingStatusType.TRANSIT, // The shipment has been processed at terminal
            ["20"] = TrackingStatusType.TRANSIT, // The shipment has departed from terminal
            ["22"] = TrackingStatusType.TRANSIT, // The shipment has been processed at depot
            ["23"] = TrackingStatusType.TRANSIT, // The shipment has left the depot
            ["25"] = TrackingStatusType.TRANSIT, // The shipment has been processed at sorting center
            ["26"] = TrackingStatusType.TRANSIT, // The shipment has been processed and left the sorting center
            ["27"] = TrackingStatusType.TRANSIT, // The shipment has arrived at gateway in origin country
            ["28"] = TrackingStatusType.TRANSIT, // The shipment has been processed at gateway in origin country
            ["29"] = TrackingStatusType.TRANSIT, // The shipment has left origin country
            ["30"] = TrackingStatusType.TRANSIT, // The shipment has arrived in transit country
            ["31"] = TrackingStatusType.TRANSIT, // The shipment has been processed in transit country
            ["32"] = TrackingStatusType.TRANSIT, // The shipment has left transit country
            ["33"] = TrackingStatusType.TRANSIT, // The shipment has arrived in destination country
            ["37"] = TrackingStatusType.TRANSIT, // The shipment has arrived at sorting center
            ["38"] = TrackingStatusType.TRANSIT, // The shipment has been processed at sorting center
            ["40"] = TrackingStatusType.TRANSIT, // The shipment has arrived at depot
            ["41"] = TrackingStatusType.TRANSIT, // The shipment has been processed at depot
            ["42"] = TrackingStatusType.INDELIVERY, // The shipment has been loaded to the van  [Loaded to van]
            ["43"] = TrackingStatusType.DELIVERED, // The shipment has been handed over to the Access Point
            ["44"] = TrackingStatusType.DELIVERED, // The shipment has been handed over to the Parcelshop
            ["45"] = TrackingStatusType.DELIVERED, // The shipment has been handed over to the Parcelstation
            ["46"] = TrackingStatusType.DELIVERED, // The shipment has been handed over to the Postoffice
            ["47"] = TrackingStatusType.TRANSIT, // The shipment has been handed over to the depot
            ["52"] = TrackingStatusType.TRANSIT, // The recipient has been notified (e.g. via email) about delivery details and available services
            ["53"] = TrackingStatusType.TRANSIT, // The recipient has requested changes to the delivery process
            ["56"] = TrackingStatusType.TRANSIT, // The recipient has requested changes to the delivery process
            ["57"] = TrackingStatusType.TRANSIT, // The recipient has requested changes to the delivery process
            ["58"] = TrackingStatusType.TRANSIT, // The recipient has requested changes to the delivery process
            ["59"] = TrackingStatusType.TRANSIT, // The recipient has requested changes to the delivery process
            ["60"] = TrackingStatusType.TRANSIT, // The recipient has requested changes to the delivery process
            ["61"] = TrackingStatusType.TRANSIT, // The recipient has requested changes to the delivery process
            ["62"] = TrackingStatusType.TRANSIT, // The recipient has requested changes to the delivery process
            ["63"] = TrackingStatusType.DELIVERED, // The shipment has been successfully delivered
            ["64"] = TrackingStatusType.DELIVERED, // The shipment has been successfully delivered
            ["65"] = TrackingStatusType.DELIVERED, // The shipment has been picked up by the recipient from depot
            ["66"] = TrackingStatusType.DELIVERED, // The shipment has been picked up by the recipient from Parcelshop
            ["67"] = TrackingStatusType.DELIVERED, // The shipment has been picked up by the recipient from Parcelstation
            ["68"] = TrackingStatusType.DELIVERED, // The shipment has been picked up by the recipient from Postoffice
            ["69"] = TrackingStatusType.DELIVERED, // The shipment has been successfully delivered
            ["70"] = TrackingStatusType.DELIVERED, // The shipment has been successfully delivered
            ["71"] = TrackingStatusType.DELIVERED, // The shipment has been successfully delivered
            ["72"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient
            ["73"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient was not available
            ["74"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient was not available
            ["75"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient was not available
            ["76"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient has refused the shipment
            ["77"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, due to unsuccessful COD payment
            ["78"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, due to unsuccessful COD payment
            ["79"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because of an incorrect delivery address
            ["80"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because of an incorrect delivery address
            ["81"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient has moved away
            ["82"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient could not show the mandatory ID for personal identification
            ["83"] = TrackingStatusType.FAILED, // During the delivery, the shipment was damaged
            ["85"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient was not available
            ["86"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the payment of charges is missing
            ["87"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Service Point because the Service point was closed
            ["88"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Service Point because of capacity restraints in the Service Point
            ["89"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Service Point because it exceeds the maximum weight or dimensions allowed in the Service Point
            ["90"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Service Point because the Service point was closed
            ["91"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Service Point because of capacity restraints in the Service Point
            ["92"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Service Point because it exceeds the maximum weight or dimensions allowed in the Service Point
            ["93"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Parcel Locker because of capacity restraints in the Parcel Locker
            ["94"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Parcel Locker because the Parcel Locker is out of service
            ["95"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Parcel Locker because it violates the GT&C of the Parcel Locker
            ["96"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Post Office because the Post Office was closed
            ["97"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Post Office because of capacity restraints in the Post Office
            ["98"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Post Office because it exceeds the maximum weight or dimensions allowed in the Post Office
            ["99"] = TrackingStatusType.FAILED, // The shipment has no been picked up by the recipient within the defined time and will be sent back to the sender
            ["100"] = TrackingStatusType.FAILED, // The shipment has no been picked up by the recipient within the defined time and will be sent back to the sender
            ["101"] = TrackingStatusType.FAILED, // The shipment has no been picked up by the recipient within the defined time and will be sent back to the sender
            ["102"] = TrackingStatusType.FAILED, // The shipment has no been picked up by the recipient within the defined time and will be sent back to the sender
            ["130"] = TrackingStatusType.TRANSIT, // The shipment has been released from export Customs
            ["133"] = TrackingStatusType.TRANSIT, // The shipment has been cleared at import Customs in destination country / region
            ["141"] = TrackingStatusType.TRANSIT, // The shipment has been held until next working day because recipient is not available
            ["142"] = TrackingStatusType.TRANSIT, // The shipment has been held because recipient requested later delivery
            ["149"] = TrackingStatusType.FAILED, // The shipment has been held and will be returned to sender
            ["151"] = TrackingStatusType.FAILED, // The shipment has been stopped without delivery attempt because recipient has refused the shipment.The shipment will be returned to sender
            ["152"] = TrackingStatusType.FAILED, // The import of the shipment has been terminated. The shipment will be returned to sender
            ["154"] = TrackingStatusType.FAILED, // The delivery of the shipment failed  [Source says Transit but text is "delivery failed"; review]
            ["155"] = TrackingStatusType.FAILED, // The shipment has not been picked up within the defined time and will be sent back to the sender
            ["156"] = TrackingStatusType.FAILED, // The shipment could not be delivered because of an exceptional event
            ["157"] = TrackingStatusType.FAILED, // The shipment could not be delivered because recipient moved. Shipment will be returned to sender
            ["159"] = TrackingStatusType.TRANSIT, // The shipment could not be delivered because recipient requested to forward/redirect parcel
            ["160"] = TrackingStatusType.FAILED, // The shipment could not be delivered and will be returned to the sender
            ["161"] = TrackingStatusType.FAILED, // The shipment could not be delivered  [Source says Transit but text is "could not be delivered"; review]
            ["172"] = TrackingStatusType.TRANSIT, // The shipment has been held because recipient requested later delivery
            ["194"] = TrackingStatusType.TRANSIT, // Shipment has been held according to recipient's request
            ["200"] = TrackingStatusType.FAILED, // The shipment could not be handed over to the Service Point because it exceeds the maximum weight or dimensions allowed in the Service Point
            ["206"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because of incorrect address  [Source says Transit but text is "could not be delivered"; review]
            ["207"] = TrackingStatusType.FAILED, // Recipient has refused the shipment
            ["208"] = TrackingStatusType.FAILED, // Recipient has refused the shipment during collection
            ["209"] = TrackingStatusType.FAILED, // Recipient has refused the shipment during collection
            ["210"] = TrackingStatusType.FAILED, // The shipment could not be delivered, because the recipient has refused it
            ["211"] = TrackingStatusType.FAILED, // The shipment could not be delivered, because the recipient has refused it
            ["214"] = TrackingStatusType.FAILED, // The shipment could not be handed over because payment of charges is missing
            ["215"] = TrackingStatusType.FAILED, // The shipment could not be handed over because payment of charges is missing
            ["217"] = TrackingStatusType.TRANSIT, // The shipment could not be delivered and is scheduled for further processing on next working day
            ["218"] = TrackingStatusType.TRANSIT, // The shipment could not be delivered because recipient requested later delivery
            ["219"] = TrackingStatusType.TRANSIT, // The shipment could not be delivered and is being held because recipient requested later delivery
            ["222"] = TrackingStatusType.FAILED, // The shipment  could not be delivered due recipient's request
            ["223"] = TrackingStatusType.FAILED, // The shipment  could not be delivered due recipient's request
            ["226"] = TrackingStatusType.TRANSIT, // The shipment has been collected
            ["228"] = TrackingStatusType.TRANSIT, // The shipment has been picked up at access point by courier
            ["230"] = TrackingStatusType.TRANSIT, // The shipment has been processed at terminal in origin country
            ["231"] = TrackingStatusType.TRANSIT, // The shipment has departed from terminal
            ["232"] = TrackingStatusType.TRANSIT, // The shipment has been processed in transit country
            ["233"] = TrackingStatusType.TRANSIT, // The shipment has been processed at terminal in destination country
            ["234"] = TrackingStatusType.TRANSIT, // The shipment has arrived at gateway in origin country
            ["235"] = TrackingStatusType.TRANSIT, // The shipment has left origin country
            ["236"] = TrackingStatusType.TRANSIT, // The shipment has arrived in transit country
            ["237"] = TrackingStatusType.TRANSIT, // The shipment has left transit country
            ["238"] = TrackingStatusType.TRANSIT, // The shipment has arrived in destination country
            ["240"] = TrackingStatusType.TRANSIT, // The shipment has arrived at sorting center
            ["242"] = TrackingStatusType.TRANSIT, // The shipment has arrived at depot
            ["248"] = TrackingStatusType.FAILED, // The shipment is undeliverable and will be returned to destination under a new ID
            ["249"] = TrackingStatusType.FAILED, // The shipment is undeliverable and will be returned to the agreed return address in the destination country
            ["253"] = TrackingStatusType.FAILED, // The shipment couldn´t be delivered because of an exceptional event. Further attempt on next working day
            ["254"] = TrackingStatusType.TRANSIT, // The shipment has left the customer facility
            ["255"] = TrackingStatusType.TRANSIT, // The shipment was pre-sorted by 3rd party provider
            ["258"] = TrackingStatusType.TRANSIT, // The shipment was presented to import  customs by 3rd party provider  [Source says Failure; identical text to 370 which is Pre-transit; likely source typo; review]
            ["259"] = TrackingStatusType.FAILED, // The shipment has been held because of missing or inapproriate documentation and will be returned to the sender
            ["264"] = TrackingStatusType.FAILED, // The shipment could not be delivered because of bad weather conditions. Further processing expected on next working day
            ["265"] = TrackingStatusType.FAILED, // The shipment could not be delivered because of capacity issues. Further processing expected on next working day
            ["266"] = TrackingStatusType.FAILED, // The shipment has been held for bad weather conditions. Further processing expected next working day
            ["267"] = TrackingStatusType.FAILED, // The shipment has been held for capacity issues. Further processing expected next working day
            ["268"] = TrackingStatusType.FAILED, // The shipment has been held and is awaiting payment of charges
            ["269"] = TrackingStatusType.FAILED, // The shipment has been held. Further processing expected next working day
            ["302"] = TrackingStatusType.INDELIVERY, // The shipment has been handed over for final delivery  [Source says Delivered but text is "handed over for final delivery"; review]
            ["306"] = TrackingStatusType.DELIVERED, // Packaging of parcel was torn. Parcel has been successfully delivered
            ["330"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient could not show the mandatory ID for personal identification
            ["354"] = TrackingStatusType.TRANSIT, // The shipment has been processed at terminal
            ["355"] = TrackingStatusType.TRANSIT, // The shipment has been processed in transit country
            ["356"] = TrackingStatusType.TRANSIT, // The shipment has been processed at terminal in destination country
            ["358"] = TrackingStatusType.TRANSIT, // The shipment has been identified as oversized
            ["363"] = TrackingStatusType.TRANSIT, // The shipment is handled by a return consolidator
            ["364"] = TrackingStatusType.TRANSIT, // The shipment is assigned to a transport container
            ["365"] = TrackingStatusType.TRANSIT, // The shipment was assigned to a transport container and is ready for departure from return consolidator with new shipment ID
            ["366"] = TrackingStatusType.DELIVERED, // The shipment has been delivered back to the business sender
            ["367"] = TrackingStatusType.TRANSIT, // The shipment was pre-sorted by 3rd party provider
            ["368"] = TrackingStatusType.TRANSIT, // The shipment was presented to export customs by 3rd party provider
            ["369"] = TrackingStatusType.TRANSIT, // The shipment was handed over to transport provider
            ["370"] = TrackingStatusType.TRANSIT, // The shipment was presented to import  customs by 3rd party provider
            ["372"] = TrackingStatusType.FAILED, // The shipment could not be delivered to the recipient, because the recipient was not available
            ["377"] = TrackingStatusType.TRANSIT, // The shipment has been processed in transit country
            ["378"] = TrackingStatusType.TRANSIT, // The shipment has been processed in transit country
            ["379"] = TrackingStatusType.TRANSIT, // The shipment has been processed in transit country
            ["380"] = TrackingStatusType.TRANSIT, // The shipment has been processed according to the business customer's instructions
            ["392"] = TrackingStatusType.TRANSIT, // The shipment could not be picked up due to a malfuntion of the Locker
            ["395"] = TrackingStatusType.TRANSIT, // The shipment couldn´t be handed over to the Service Point as it was closed

            #endregion ecommerce europe
        };

        public static TrackingStatusType GetTrackingStatusType(string dhlTrackingStatusType)
            => _trackingStatusTypes.TryGetValue(dhlTrackingStatusType, out var trackingStatusType)
            ? trackingStatusType
            : throw new ArgumentException($"Invalid DHL tracking status type: {dhlTrackingStatusType}");
    }
}