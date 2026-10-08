using ShippingProAPICollection.Models;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Provider.GLS;

namespace ShippingProAPICollection.Provider
{
    public interface IShippingProviderService
    {
        /// <summary>
        /// Erstellen ein Versandlabels |
        /// Create shipping label
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task<List<RequestShippingLabelResponse>> RequestLabel(RequestShipmentBase request, CancellationToken cancelToken);

        /// <summary>
        /// Stornieren eines Versandlabels mit der Stornierungsnummer |
        /// Cancel shipping label
        /// </summary>
        /// <param name="cancelId"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task<ShippingCancelResult> CancelLabel(string cancelId, CancellationToken cancelToken);

        /// <summary>
        /// Prüfe einen Versandlabel request |
        /// Validate a shippint label request
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public Task<ValidationReponse> ValidateLabel(RequestShipmentBase request, CancellationToken cancelToken);

        /// <summary>
        /// Prüfe wieviel Tage bis zur Auflieferung benötigt werden
        /// This operation estimates the days needed to deliver a shipment unit from the given origin address to the given destination address.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task<uint> GetEstimatedDeliveryDays(RequestShipmentBase request, CancellationToken cancelToken);

        /// <summary>
        /// Confirm shipment
        /// </summary>
        /// <param name="confirmId"></param>
        /// <returns></returns>
        public Task ConfirmShipment(string confirmId, CancellationToken cancelToken);

        public float GetMaxPackageWeight();

        /// <summary>
        /// Subscribe to a webhook for tracking updates
        /// </summary>
        /// <param name="webhookUrl">The URL of the webhook to subscribe to</param>
        /// <param name="cancelToken">A token to cancel the operation</param>
        /// <returns></returns>
        public Task<WebHookSubsribeAttemptResponse> SubscribeToWebhook(CancellationToken cancelToken);

        /// <summary>
        /// Process incoming webhook data
        /// </summary>
        /// <param name="webhookData"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task ProcessParcelStatusChangeNotification(IParcelStatusChangeNotification? notificationData, CancellationToken cancelToken);
    }
}