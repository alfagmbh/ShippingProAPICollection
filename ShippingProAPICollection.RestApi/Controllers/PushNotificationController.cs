using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DHL.Entities;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.Provider.GLS.Entities;
using ShippingProAPICollection.RestApi.Entities.Error;
using System.Net;

namespace ShippingProAPICollection.RestApi.Controllers
{
    [Route("api/push/notification")]
    [ApiController]
    [EnableCors("External")]
    [ProducesResponseType(typeof(InternalServerErrorReponse), (int)HttpStatusCode.InternalServerError)]
    [ProducesResponseType(typeof(BadRequestReponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(UnprocessableEntityResponse), (int)HttpStatusCode.UnprocessableEntity)]
    public class PushNotificationController(
        ShippingProAPICollectionService shippingProAPICollectionService
                                           ) : ControllerBase
    {
        #region push notification endpoits

        [HttpPost]
        [Route("gls")]
        public async Task<IActionResult> GetPushNotification([FromBody, BindRequired] GLSParcelStatusChangeNotification notificationData) =>
            await HandleProcess(() => shippingProAPICollectionService.ProcessPushNotification(notificationData));

        [HttpPost]
        [Route("dhl")]
        public async Task<IActionResult> GetPushNotification([FromBody, BindRequired] DHLParcelStatusChangeNotification notificationData) =>
            await HandleProcess(() => shippingProAPICollectionService.ProcessPushNotification(notificationData));

        [HttpGet]
        [Route("dpd")]
        public async Task<IActionResult> GetPushNotification([FromQuery, BindRequired] DPDParcelStatusChangeNotification notificationData) =>
            await HandleProcess(() => shippingProAPICollectionService.ProcessPushNotification(notificationData));

        #endregion push notification endpoits

        #region subscribe

        [HttpPost]
        [Route("dhl/subscribe")]
        public async Task<IActionResult> Subscribe([FromBody, BindRequired] DHLWebhookSubscriberRequest notificationData) =>
            await HandleProcess(() => { return shippingProAPICollectionService.SubscribeToPushNotification(notificationData); });

        [HttpPost]
        [Route("dhl/activate")]
        public async Task<IActionResult> ActivateDHLSubscriber([FromBody, BindRequired] DHLSubscriberActivatorRequest request) =>
            await HandleProcess(() => { return shippingProAPICollectionService.ActivateDHLSubscriber(request); });

        [HttpPost]
        [Route("gls/subscribe")]
        public async Task<IActionResult> Subscribe([FromBody, BindRequired] GLSWebhookSubscriberRequest notificationData) =>
            await HandleProcess(() => { return shippingProAPICollectionService.SubscribeToPushNotification(notificationData); });

        #endregion subscribe

        #region private handle method

        private async Task<IActionResult> HandleProcess(Func<Task> process)
        {
            try
            {
                await process();
                return Ok();
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new BadRequestReponse(new ShippingProviderErrorCodeException(ShippingErrorCode.UNKNOW, "Invalid operation.", null)));
            }
            catch (ArgumentException)
            {
                return UnprocessableEntity(new UnprocessableEntityResponse(new List<string> { "Invalid argument provided." }));
            }
            catch (Exception)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new InternalServerErrorReponse("An error occurred while processing the request."));
            }
        }

        #endregion private handle method
    }
}