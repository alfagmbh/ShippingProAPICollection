using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DHL.Entities;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.Provider.GLS.Entities;
using ShippingProAPICollection.RabbitMq;
using ShippingProAPICollection.RestApi.Controllers;
using ShippingProAPICollection.RestApi.Entities.Error;
using System.Reflection;

namespace ShippingProAPICollection.NUnitTests
{
    [TestFixture]
    public class PushNotificationControllerTests
    {
        private MemoryCache _cache = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _cache = new MemoryCache(new MemoryCacheOptions());

        [OneTimeTearDown]
        public void OneTimeTearDown() => _cache.Dispose();

        #region helpers

        // The service is a concrete class, so it is used for real: with no providers configured every provider lookup
        // fails with an InvalidOperationException, and an unconfigured RabbitMqConnector fails on publish.
        private const string GlsPushUser = "gls-push";
        private const string GlsPushPassword = "secret";

        private static string BasicHeader(string user, string password) =>
            "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{password}"));

        private PushNotificationController CreateController(bool withDpdProvider = false, bool withGlsProvider = false, string? authorization = null)
        {
            var settings = new ShippingProAPICollectionSettings(new ShippingProAPIShipFromAddress
            {
                Name = "Homer Simpson",
                Street = "Simpsonstreet 1",
                ContactName = "Maggie Simpson",
                CountryIsoA2Code = "DE",
                City = "Springfield",
                Email = "homer@duffbeer.de",
                PostCode = "73479",
                Phone = "123456789"
            });

            if (withDpdProvider)
            {
                settings.AddSettings("DPD", new DPDSettings
                {
                    ApiDomain = "ws-stage",
                    APILanguage = "de_DE",
                    DepotNumber = "0191",
                    Username = "sandboxdpd",
                    Password = "xMmshh1"
                });
            }

            if (withGlsProvider)
            {
                settings.AddSettings("GLS", new GLSSettings
                {
                    ApiDomain = "https://example.com",
                    Username = "user",
                    Password = "password",
                    ApplicationID = "app",
                    ApplicationSecret = "app-secret",
                    ContactID = "contact",
                    PushNotificationUsername = GlsPushUser,
                    PushNotificationPassword = GlsPushPassword
                });
            }

            var rabbitMq = new RabbitMqConnector(Options.Create(new RabbitMqSecrets()));
            var service = new ShippingProAPICollectionService(_cache, settings, rabbitMq);

            var httpContext = new DefaultHttpContext();
            if (authorization != null)
                httpContext.Request.Headers.Authorization = authorization;

            return new PushNotificationController(service, NullLogger<PushNotificationController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        private static Task<IActionResult> InvokeHandleProcess(
            PushNotificationController controller,
            Func<CancellationToken, Task> process,
            CancellationToken ct = default,
            Func<IActionResult>? onSuccess = null)
        {
            var method = typeof(PushNotificationController).GetMethod("HandleProcess", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (Task<IActionResult>)method.Invoke(controller, new object?[] { process, ct, onSuccess })!;
        }

        private static Task<IActionResult> InvokeHandleSubscription(
            PushNotificationController controller,
            Func<CancellationToken, Task<WebHookSubsribeAttemptResponse>> subscribe,
            CancellationToken ct = default)
        {
            var method = typeof(PushNotificationController).GetMethod("HandleSubscription", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (Task<IActionResult>)method.Invoke(controller, new object?[] { subscribe, ct })!;
        }

        private static DPDParcelStatusChangeNotification ValidDpdNotification() => new()
        {
            PushId = "push-1",
            ParcelNumber = "01905123456789",
            Status = "delivery_customer"
        };

        private static DHLWebhookSubscriberRequest DhlSubscriberRequest(string hookUri) => new()
        {
            DhlApiKey = "key",
            Format = "JSON",
            Service = "shipment-status",
            AuthenticationHeader = "Authorization",
            AuthenticationToken = "token",
            WebhookHeaderName = "X-Header",
            WebhookHeaderValue = "value",
            Events = new[] { "shipment" },
            AccountID = "account",
            HookUri = hookUri
        };

        private static GLSWebhookSubscriberRequest GlsSubscriberRequest(string hookUri) => new()
        {
            WebhookUserId = "webhook-user",
            CustomerId = "customer",
            ContactId = "contact",
            PortalUsername = "user",
            PortalPassword = "password",
            HookUri = hookUri
        };

        private static void AssertBadRequest(IActionResult result, ShippingErrorCode expectedCode, string expectedMessage)
        {
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var body = (BadRequestReponse)((BadRequestObjectResult)result).Value!;
            Assert.That(body.ProviderException.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(body.ProviderException.Message, Is.EqualTo(expectedMessage));
            Assert.That(body.ProviderException.Payload, Is.Null);
        }

        private static void AssertUnprocessable(IActionResult result)
        {
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
            var body = (UnprocessableEntityResponse)((UnprocessableEntityObjectResult)result).Value!;
            Assert.That(body.ErrorMessages, Is.EqualTo(new[] { "Invalid argument provided." }));
        }

        private static void AssertInternalServerError(IActionResult result)
        {
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            var body = (InternalServerErrorReponse)objectResult.Value!;
            Assert.That(body.ErrorMessages, Is.EqualTo("An error occurred while processing the request."));
        }

        #endregion helpers

        #region push notification endpoints

        [Test]
        public async Task GLSPush_WithoutGlsProvider_ReturnsUnauthorized()
        {
            var result = await CreateController(authorization: BasicHeader(GlsPushUser, GlsPushPassword))
                .GetPushNotification(new GLSParcelStatusChangeNotification(), CancellationToken.None);

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Bearer token")]
        [TestCase("Basic !!!not-base64!!!")]
        public async Task GLSPush_WithMissingOrMalformedAuthorization_ReturnsUnauthorized(string? authorization)
        {
            var controller = CreateController(withGlsProvider: true, authorization: authorization);

            var result = await controller.GetPushNotification(new GLSParcelStatusChangeNotification(), CancellationToken.None);

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
            Assert.That(controller.Response.Headers.WWWAuthenticate.ToString(), Is.EqualTo("Basic"));
        }

        [Test]
        public async Task GLSPush_WithWrongCredentials_ReturnsUnauthorized()
        {
            var result = await CreateController(withGlsProvider: true, authorization: BasicHeader(GlsPushUser, "wrong"))
                .GetPushNotification(new GLSParcelStatusChangeNotification(), CancellationToken.None);

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task GLSPush_WithValidCredentials_IsProcessed()
        {
            // Authentication passes; the unconfigured RabbitMQ then fails on publish, which proves the notification was processed.
            var notification = new GLSParcelStatusChangeNotification { ParcelId = "1", ParcelStatus = "DELIVERED" };

            var result = await CreateController(withGlsProvider: true, authorization: BasicHeader(GlsPushUser, GlsPushPassword))
                .GetPushNotification(notification, CancellationToken.None);

            AssertInternalServerError(result);
        }

        [Test]
        public async Task DHLPush_WithoutDhlProvider_ReturnsBadRequest()
        {
            var notification = new DHLParcelStatusChangeNotification { Shipments = new List<DHLPushShipment>() };

            var result = await CreateController().GetPushNotification(notification, CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task DPDPush_WithEmptyPushId_ReturnsBadRequestWithoutBody(string? pushId)
        {
            var notification = ValidDpdNotification();
            notification.PushId = pushId;

            var result = await CreateController(withDpdProvider: true).GetPushNotification(notification, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task DPDPush_WithoutDpdProvider_ReturnsBadRequest()
        {
            var result = await CreateController().GetPushNotification(ValidDpdNotification(), CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [TestCase(null, "delivery_customer")]
        [TestCase("01905123456789", null)]
        public async Task DPDPush_WithMissingParcelNumberOrStatus_ReturnsUnprocessableEntity(string? parcelNumber, string? status)
        {
            var notification = ValidDpdNotification();
            notification.ParcelNumber = parcelNumber;
            notification.Status = status;

            var result = await CreateController(withDpdProvider: true).GetPushNotification(notification, CancellationToken.None);

            AssertUnprocessable(result);
        }

        [Test]
        public async Task DPDPush_WhenQueuePublishFails_ReturnsInternalServerErrorWithoutAcknowledgement()
        {
            var result = await CreateController(withDpdProvider: true).GetPushNotification(ValidDpdNotification(), CancellationToken.None);

            AssertInternalServerError(result);
        }

        #endregion push notification endpoints

        #region subscribe endpoints

        [TestCase("http://example.com/hook")]
        [TestCase("not-a-url")]
        [TestCase("")]
        public async Task DHLSubscribe_WithNonHttpsHookUri_ReturnsUnprocessableEntity(string hookUri)
        {
            var result = await CreateController().Subscribe(DhlSubscriberRequest(hookUri), CancellationToken.None);

            AssertUnprocessable(result);
        }

        [TestCase("http://example.com/hook")]
        [TestCase("not-a-url")]
        [TestCase("")]
        public async Task GLSSubscribe_WithNonHttpsHookUri_ReturnsUnprocessableEntity(string hookUri)
        {
            var result = await CreateController().Subscribe(GlsSubscriberRequest(hookUri), CancellationToken.None);

            AssertUnprocessable(result);
        }

        [TestCase("http://example.com/hook")]
        [TestCase("not-a-url")]
        public async Task GLSWebhookUser_WithNonHttpsUrl_ReturnsUnprocessableEntity(string url)
        {
            var result = await CreateController(withGlsProvider: true)
                .CreateGLSWebhookUser(new GLSWebhookUserRequest { Name = "Customer - Solution", WebhookUrl = url }, CancellationToken.None);

            AssertUnprocessable(result);
        }

        [TestCase("http://example.com/hook")]
        [TestCase("not-a-url")]
        public async Task GLSWebhookAuth_WithNonHttpsUrl_ReturnsUnprocessableEntity(string url)
        {
            var result = await CreateController(withGlsProvider: true)
                .ConfigureGLSWebhookAuth(new GLSWebhookAuthRequest { WebhookUserId = "user", WebhookUrl = url }, CancellationToken.None);

            AssertUnprocessable(result);
        }

        [Test]
        public async Task GLSWebhookAuth_WithoutConfiguredCredentials_ReturnsBadRequest()
        {
            var settings = new ShippingProAPICollectionSettings(new ShippingProAPIShipFromAddress
            {
                Name = "n", Street = "s", ContactName = "c", CountryIsoA2Code = "DE", City = "c", Email = "e@e.de", PostCode = "1", Phone = "1"
            });
            settings.AddSettings("GLS", new GLSSettings
            {
                ApiDomain = "https://example.com", Username = "u", Password = "p", ApplicationID = "a", ApplicationSecret = "s", ContactID = "c"
            });
            var service = new ShippingProAPICollectionService(_cache, settings, new RabbitMqConnector(Options.Create(new RabbitMqSecrets())));

            var result = await new PushNotificationController(service, NullLogger<PushNotificationController>.Instance)
                .ConfigureGLSWebhookAuth(new GLSWebhookAuthRequest { WebhookUserId = "user", WebhookUrl = "https://example.com/hook" }, CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [Test]
        public void GLSSubscriberRequest_Serialization_MatchesCookbookFieldNames()
        {
            var json = Newtonsoft.Json.Linq.JObject.Parse(
                Newtonsoft.Json.JsonConvert.SerializeObject(GlsSubscriberRequest("https://example.com/hook")));

            Assert.Multiple(() =>
            {
                Assert.That(json["id"]?.ToString(), Is.EqualTo("webhook-user"));
                Assert.That(json["webhookUrl"]?.ToString(), Is.EqualTo("https://example.com/hook"));
                Assert.That(json["customerId"]?.ToString(), Is.EqualTo("customer"));
                Assert.That(json["contactId"]?.ToString(), Is.EqualTo("contact"));
                Assert.That(json["portalUsername"]?.ToString(), Is.EqualTo("user"));
                Assert.That(json["portalPassword"]?.ToString(), Is.EqualTo("password"));
                Assert.That(json.ContainsKey("hookUri"), Is.False);
            });
        }

        [Test]
        public async Task DHLSubscribe_WithoutDhlProvider_ReturnsBadRequest()
        {
            var result = await CreateController().Subscribe(DhlSubscriberRequest("https://example.com/hook"), CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [Test]
        public async Task GLSSubscribe_WithoutGlsProvider_ReturnsBadRequest()
        {
            var result = await CreateController().Subscribe(GlsSubscriberRequest("https://example.com/hook"), CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [Test]
        public async Task DHLActivate_WithoutDhlProvider_ReturnsBadRequest()
        {
            var request = new DHLSubscriberActivatorRequest { SubscriberId = "id", DhlApiHookSecret = "secret", DhlApiKey = "key" };

            var result = await CreateController().ActivateDHLSubscriber(request, CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        #endregion subscribe endpoints

        #region HandleProcess error mapping

        [Test]
        public async Task HandleProcess_OnSuccess_ReturnsOk()
        {
            var result = await InvokeHandleProcess(CreateController(), _ => Task.CompletedTask);

            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task HandleProcess_OnSuccessWithCallback_ReturnsCallbackResult()
        {
            var expected = new ContentResult { Content = "<ack/>", ContentType = "application/xml", StatusCode = StatusCodes.Status200OK };

            var result = await InvokeHandleProcess(CreateController(), _ => Task.CompletedTask, onSuccess: () => expected);

            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public async Task HandleProcess_PassesCancellationTokenToProcess()
        {
            using var cts = new CancellationTokenSource();
            CancellationToken received = default;

            await InvokeHandleProcess(CreateController(), t => { received = t; return Task.CompletedTask; }, cts.Token);

            Assert.That(received, Is.EqualTo(cts.Token));
        }

        [Test]
        public async Task HandleProcess_WhenClientCancelled_Returns499()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await InvokeHandleProcess(CreateController(), t => Task.FromCanceled(t), cts.Token);

            Assert.That(result, Is.InstanceOf<StatusCodeResult>());
            Assert.That(((StatusCodeResult)result).StatusCode, Is.EqualTo(499));
        }

        [Test]
        public async Task HandleProcess_WhenOperationCancelledButClientStillConnected_Returns500()
        {
            var result = await InvokeHandleProcess(CreateController(), _ => throw new OperationCanceledException("provider timeout"));

            AssertInternalServerError(result);
        }

        [Test]
        public async Task HandleProcess_OnProviderError_ReturnsBadRequestKeepingErrorCodeAndHidingDetails()
        {
            var ex = new ShippingProviderErrorCodeException(ShippingErrorCode.UNAUTHORIZED, "secret provider message", new { Password = "secret" });

            var result = await InvokeHandleProcess(CreateController(), _ => throw ex);

            AssertBadRequest(result, ShippingErrorCode.UNAUTHORIZED, "Provider request failed.");
        }

        [Test]
        public async Task HandleProcess_OnInvalidOperation_ReturnsBadRequest()
        {
            var result = await InvokeHandleProcess(CreateController(), _ => throw new InvalidOperationException("internal detail"));

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [Test]
        public async Task HandleProcess_OnArgumentException_ReturnsUnprocessableEntity()
        {
            var result = await InvokeHandleProcess(CreateController(), _ => throw new ArgumentException("internal detail"));

            AssertUnprocessable(result);
        }

        [Test]
        public async Task HandleProcess_OnUnexpectedException_ReturnsInternalServerErrorWithoutLeakingDetails()
        {
            var result = await InvokeHandleProcess(CreateController(), _ => throw new Exception("internal detail"));

            AssertInternalServerError(result);
            var body = (InternalServerErrorReponse)((ObjectResult)result).Value!;
            Assert.That(body.ErrorMessages, Does.Not.Contain("internal detail"));
        }

        #endregion HandleProcess error mapping

        #region HandleSubscription

        [Test]
        public async Task HandleSubscription_OnSuccess_ReturnsOkWithMessage()
        {
            var result = await InvokeHandleSubscription(CreateController(),
                _ => Task.FromResult(new WebHookSubsribeAttemptResponse { IsSuccess = true, Message = "subscribed" }));

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(((OkObjectResult)result).Value, Is.EqualTo(new PushSubscriptionResponse(true, "subscribed")));
        }

        [Test]
        public async Task HandleSubscription_WhenProviderRejects_ReturnsBadRequestWithoutProviderMessage()
        {
            var result = await InvokeHandleSubscription(CreateController(),
                _ => Task.FromResult(new WebHookSubsribeAttemptResponse { IsSuccess = false, Message = "secret provider message", Exception = new Exception("boom") }));

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Subscription was not accepted by the provider.");
        }

        [Test]
        public async Task HandleSubscription_WhenSubscribeThrows_UsesHandleProcessMapping()
        {
            var result = await InvokeHandleSubscription(CreateController(), _ => throw new ArgumentException("bad hook"));

            AssertUnprocessable(result);
        }

        #endregion HandleSubscription
    }
}
