using System.Net;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.RestApi.Entities.Error;

namespace ShippingProAPICollection.RestApi.Controllers
{
    [Route("api/push/notification")]
    [ApiController]
    [EnableCors("External")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(InternalServerErrorReponse), (int)HttpStatusCode.InternalServerError)]
    [ProducesResponseType(typeof(BadRequestReponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(UnprocessableEntityResponse), (int)HttpStatusCode.UnprocessableEntity)]
    public class PushNotificationController(
        ShippingProAPICollectionService shippingProAPICollectionService,
        ILogger<PushNotificationController> logger
                                           ) : ControllerBase
    {
        private const int ClientClosedRequest = 499;

        #region push notification endpoits

        /// <summary>
        /// Receives a provider push. The caller forwards the request exactly as it received it (method, headers, query and body), nothing is bound or built by the caller:
        /// the request is handled here by the type in the route (DHL, GLS or DPD). 200 when processed, 203 when the push was received but could not be processed (result status Failed).
        /// DHL: tracking pushes need the push token header; the handshake pushes are acknowledged with the subscription id, and subscription.validate activates
        /// the subscription (its DHL-API-Hook-Secret header has to be forwarded as received).
        /// </summary>
        [AcceptVerbs("GET", "POST")]
        [Route("{type}")]
        [ProducesResponseType(typeof(ParcelTrackingEventResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ParcelTrackingEventResult), (int)HttpStatusCode.NonAuthoritativeInformation)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProcessPushNotification([FromRoute] ShippingProviderType type, CancellationToken ct) =>
            await HandleProcessWithResult(
                t => shippingProAPICollectionService.ProcessPushNotification(type, Request, t),
                ct,
                result => result.Status == NotificationResultStatus.Failed ? StatusCode(StatusCodes.Status203NonAuthoritative, result) : Ok(result));


        #endregion push notification endpoits

        #region subscribe

        /// <summary>
        /// Sets up push notifications for the provider (auth and subscribe as needed) using the provider settings.
        /// DHL parcels are subscribed when their label is created; TrackingNumber subscribes parcels manually (subject to the same daily limit),
        /// with HookUri overriding the one in the secrets file. Each entry lists its TrackingNumbers; an entry that was not subscribed carries an Error.
        /// The subscriptions are activated when DHL's validation push reaches the push endpoint.
        /// Returns what the provider answered per subscription: 200 when all were accepted, 207 when any was rejected (see the Error of each entry).
        /// ValuesToSave holds provider-issued values (e.g. the GLS WebhookUserId) that have to be stored in the secrets file.
        /// </summary>
        [HttpPost]
        [Route("{type}/ini")]
        [ProducesResponseType(typeof(IReadOnlyList<PushSubscriptionInfo>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(IReadOnlyList<PushSubscriptionInfo>), StatusCodes.Status207MultiStatus)]
        public async Task<IActionResult> Ini([FromRoute] ShippingProviderType type, [FromBody, BindRequired] PushInitRequest request, CancellationToken ct) =>
            await HandleProcessWithResult(
                t => shippingProAPICollectionService.InitializePushNotification(type, request.HookUri, request.TrackingNumber, t),
                ct,
                SubscriptionsResult);

        #endregion subscribe

        #region private handle method

        private IActionResult SubscriptionsResult(IReadOnlyList<PushSubscriptionInfo> subscriptions) =>
            subscriptions.Any(s => s.Error is not null) ? StatusCode(StatusCodes.Status207MultiStatus, subscriptions) : Ok(subscriptions);

        private async Task<IActionResult> HandleProcessWithResult<T>(Func<CancellationToken, Task<T>> process, CancellationToken ct, Func<T, IActionResult> onSuccess)
        {
            T result = default!;
            return await HandleProcess(async t => result = await process(t), ct, () => onSuccess(result));
        }

        private async Task<IActionResult> HandleProcess(Func<CancellationToken, Task> process, CancellationToken ct, Func<IActionResult>? onSuccess = null)
        {
            try
            {
                await process(ct);
                return onSuccess?.Invoke() ?? Ok();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return StatusCode(ClientClosedRequest);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unauthorized push notification request.");
                Response.Headers.WWWAuthenticate = "Basic";
                return Unauthorized();
            }
            catch (ShippingProviderErrorCodeException ex)
            {
                // ex.Payload may contain provider responses/credentials, so it is logged by type only and never returned.
                logger.LogWarning(ex, "Provider error while processing push notification request ({ErrorCode}).", ex.ErrorCode);
                return BadRequest(new BadRequestReponse(new ShippingProviderErrorCodeException(ex.ErrorCode, "Provider request failed.", null)));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation while processing push notification request.");
                return BadRequest(new BadRequestReponse(new ShippingProviderErrorCodeException(ShippingErrorCode.UNKNOW, "Invalid operation.", null)));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid argument while processing push notification request.");
                return UnprocessableEntity(new UnprocessableEntityResponse(new List<string> { "Invalid argument provided." }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing push notification request.");
                return StatusCode((int)HttpStatusCode.InternalServerError, new InternalServerErrorReponse("An error occurred while processing the request."));
            }
        }

        #endregion private handle method
    }

    public record PushInitRequest(string HookUri, string[]? TrackingNumber = null);
}