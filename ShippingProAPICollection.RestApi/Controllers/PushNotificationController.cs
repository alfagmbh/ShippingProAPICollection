using System.Net;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Provider.DHL.Entities;
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

        [HttpPost]
        [Route("{type}")]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProcessPushNotification([FromRoute] ShippingProviderType type, [FromBody] object notificationData, CancellationToken ct) =>
            await HandleProcess(t => shippingProAPICollectionService.ProcessPushNotification(type, notificationData, Request.Headers.Authorization.ToString(), t), ct);


        #endregion push notification endpoits

        #region subscribe

        /// <summary>
        /// Sets up push notifications for the provider (auth and subscribe as needed) using the provider settings.
        /// DHL subscriptions still have to be activated via dhl/activate.
        /// </summary>
        [HttpPost]
        [Route("{type}/ini")]
        public async Task<IActionResult> Ini([FromRoute] ShippingProviderType type, [FromBody, BindRequired] PushInitRequest request, CancellationToken ct) =>
            await HandleProcess(t => shippingProAPICollectionService.InitializePushNotification(type, request.HookUri, t), ct);

        [HttpPost]
        [Route("dhl/activate")]
        public async Task<IActionResult> ActivateDHLSubscriber([FromBody, BindRequired] DHLSubscriberActivatorRequest request, CancellationToken ct) =>
            await HandleProcess(t => shippingProAPICollectionService.ActivateDHLSubscriber(request, t), ct);

        #endregion subscribe

        #region private handle method

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

    public record PushInitRequest(string HookUri);
}