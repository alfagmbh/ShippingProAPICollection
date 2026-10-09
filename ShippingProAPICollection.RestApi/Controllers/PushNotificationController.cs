using System.Net;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.RestApi.Entities.Error;

namespace ShippingProAPICollection.RestApi.Controllers
{
    [Route("api/push/notification")]
    [ApiController]
    [EnableCors("Intern")]
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
            await Handle(() => shippingProAPICollectionService.ProcessPushNotification(notificationData));

        [HttpPost]
        [Route("dhl")]
        public async Task<IActionResult> GetPushNotification([FromBody, BindRequired] DHLParcelStatusChangeNotification notificationData) =>
        await Handle(() => shippingProAPICollectionService.ProcessPushNotification(notificationData));

        [HttpGet]
        [Route("dpd")]
        public async Task<IActionResult> GetPushNotification([FromQuery, BindRequired] DPDParcelStatusChangeNotification notificationData) =>
            await Handle(() => shippingProAPICollectionService.ProcessPushNotification(notificationData));

        #endregion push notification endpoits

        #region private handle method

        private async Task<IActionResult> Handle(Func<Task> process)
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