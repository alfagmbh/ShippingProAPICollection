using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DHL.Entities;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.Provider.GLS.Entities;
using ShippingProAPICollection.RestApi.Entities.Error;
using System.Net;
using System.Xml.Linq;

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

        [HttpPost]
        [Route("gls")]
        public async Task<IActionResult> GetPushNotification([FromBody, BindRequired] GLSParcelStatusChangeNotification notificationData, CancellationToken ct) =>
            await HandleProcess(t => shippingProAPICollectionService.ProcessPushNotification(notificationData, t), ct);

        [HttpPost]
        [Route("dhl")]
        public async Task<IActionResult> GetPushNotification([FromBody, BindRequired] DHLParcelStatusChangeNotification notificationData, CancellationToken ct) =>
            await HandleProcess(t => shippingProAPICollectionService.ProcessPushNotification(notificationData, t), ct);

        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "application/xml")]
        [Route("dpd")]
        public async Task<IActionResult> GetPushNotification([FromQuery] DPDParcelStatusChangeNotification notificationData, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(notificationData.PushId))
                return BadRequest(); // empty body, no XML

            return await HandleProcess(
                t => shippingProAPICollectionService.ProcessPushNotification(notificationData, t),
                ct,
                () => DpdAcknowledge(notificationData.PushId));

            static ContentResult DpdAcknowledge(string pushId) => new()
            {
                Content = new XElement("push",
                    new XElement("pushid", pushId),
                        new XElement("status", "OK")
                                      ).ToString(SaveOptions.DisableFormatting),
                ContentType = "application/xml",
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion push notification endpoits

        #region subscribe

        [HttpPost]
        [Route("dhl/subscribe")]
        [ProducesResponseType(typeof(PushSubscriptionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Subscribe([FromBody, BindRequired] DHLWebhookSubscriberRequest notificationData, CancellationToken ct) =>
            await HandleSubscription(t => shippingProAPICollectionService.SubscribeToPushNotification(notificationData, t), ct);

        [HttpPost]
        [Route("dhl/activate")]
        [ProducesResponseType(typeof(PushSubscriptionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ActivateDHLSubscriber([FromBody, BindRequired] DHLSubscriberActivatorRequest request, CancellationToken ct) =>
            await HandleSubscription(t => shippingProAPICollectionService.ActivateDHLSubscriber(request, t), ct);

        [HttpPost]
        [Route("gls/subscribe")]
        [ProducesResponseType(typeof(PushSubscriptionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Subscribe([FromBody, BindRequired] GLSWebhookSubscriberRequest notificationData, CancellationToken ct) =>
            await HandleSubscription(t => shippingProAPICollectionService.SubscribeToPushNotification(notificationData, t), ct);

        #endregion subscribe

        #region private handle method

        private async Task<IActionResult> HandleSubscription(Func<CancellationToken, Task<WebHookSubsribeAttemptResponse>> subscribe, CancellationToken ct)
        {
            WebHookSubsribeAttemptResponse? result = null;

            return await HandleProcess(
                async t => result = await subscribe(t),
                ct,
                () =>
                {
                    if (result is { IsSuccess: true })
                        return Ok(new PushSubscriptionResponse(true, result.Message));

                    logger.LogWarning(result?.Exception, "Provider rejected the webhook subscription request: {Message}", result?.Message);
                    return BadRequest(new BadRequestReponse(new ShippingProviderErrorCodeException(ShippingErrorCode.UNKNOW, "Subscription was not accepted by the provider.", null)));
                });
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

    public record PushSubscriptionResponse(bool IsSuccess, string? Message);
}
