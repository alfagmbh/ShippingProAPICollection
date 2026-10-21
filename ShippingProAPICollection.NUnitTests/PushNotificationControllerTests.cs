using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DHL.Entities;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.Provider.GLS.Entities;
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
        // fails with an InvalidOperationException.
        private const string GlsPushUser = "gls-push";
        private const string GlsPushPassword = "secret";

        private static string BasicHeader(string user, string password) =>
            "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{password}"));

        private PushNotificationController CreateController(bool withDpdProvider = false, bool withGlsProvider = false, string? authorization = null, bool withDhlProvider = false)
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
                    Password = "xMmshh1",
                    PushNotificationSettings = new DPDPushNotificationSettings
                    {
                        IsActive = false,
                        
                    }
                });

            }

            if (withDhlProvider)
            {
                settings.AddSettings("DHL", new DHLSettings
                {
                    ApiDomain = "sandbox",
                    NationalAccountNumber = "33333333330102",
                    InternationalAccountNumber = "33333333335301",
                    Username = "user",
                    Password = "password",
                    LabelPrintFormat = "910-300-410",
                    DHLShipmentProfile = "STANDARD_GRUPPENPROFIL",
                    APIKey = "key",
                    APILanguage = "de-DE",
                    PushNotificationSettings = new DHLPushNotificationSettings
                    {
                        ApiDomain = "sandbox",
                        APIKey = "key",
                        APILanguage = "de-DE",
                        Format = "JSON",
                        AuthenticationHeader = "Authorization",
                        AuthenticationToken = "token",
                        WebhookHeaderName = "X-Header",
                        WebhookHeaderValue = "value",
                        AccountID = "account",
                        Subscriptions = []
                    }
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
                   PushNotificationSettings = new GLSPushNotificationSettings
                   {
                       ApiDomain = "https://example.com",
                       ContactID = "contact",
                       WebhookName = "webhook",
                       WebhookUserId = "webhook-user",
                       IsActive = false,
                       WebhookUsername = GlsPushUser,
                       WebhookPassword = GlsPushPassword
                   }
                });
            }

            
            var service = new ShippingProAPICollectionService(_cache, settings);

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

        private static Dictionary<string, string?> ValidDpdValues() => new()
        {
            ["pushid"] = "push-1",
            ["pnr"] = "01905123456789",
            ["status"] = "delivery_customer"
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
        public async Task GLSPush_WithoutGlsProvider_ReturnsBadRequest()
        {
            var result = await CreateController(authorization: BasicHeader(GlsPushUser, GlsPushPassword))
                .ProcessPushNotification(ShippingProviderType.GLS, new GLSParcelStatusChangeNotification(), CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Bearer token")]
        [TestCase("Basic !!!not-base64!!!")]
        public async Task GLSPush_WithMissingOrMalformedAuthorization_ReturnsUnauthorized(string? authorization)
        {
            var controller = CreateController(withGlsProvider: true, authorization: authorization);

            var result = await controller.ProcessPushNotification(ShippingProviderType.GLS, new GLSParcelStatusChangeNotification(), CancellationToken.None);

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
            Assert.That(controller.Response.Headers.WWWAuthenticate.ToString(), Is.EqualTo("Basic"));
        }

        [Test]
        public async Task GLSPush_WithWrongCredentials_ReturnsUnauthorized()
        {
            var result = await CreateController(withGlsProvider: true, authorization: BasicHeader(GlsPushUser, "wrong"))
                .ProcessPushNotification(ShippingProviderType.GLS, new GLSParcelStatusChangeNotification(), CancellationToken.None);

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task GLSPush_WithValidCredentials_ReturnsOk()
        {
            var notification = new GLSParcelStatusChangeNotification { ParcelId = "1", ParcelStatus = "DELIVERED" };

            var result = await CreateController(withGlsProvider: true, authorization: BasicHeader(GlsPushUser, GlsPushPassword))
                .ProcessPushNotification(ShippingProviderType.GLS, notification, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task GLSPush_AuthorizationHeaderInPayload_IsIgnored()
        {
            // The header is a per-request value; a payload must not be able to bring its own.
            var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<GLSParcelStatusChangeNotification>(
                $"{{\"parcelId\":\"1\",\"parcelStatus\":\"DELIVERED\",\"AuthorizationHeader\":\"{BasicHeader(GlsPushUser, GlsPushPassword)}\"}}")!;

            var result = await CreateController(withGlsProvider: true)
                .ProcessPushNotification(ShippingProviderType.GLS, payload, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task DHLPush_WithValidShipment_ReturnsOk()
        {
            var notification = new DHLParcelStatusChangeNotification
            {
                Shipments = [new DHLPushShipment { Id = "1234567890", Status = new DHLPushStatus { StatusCode = "delivered" } }]
            };

            var result = await CreateController(withDhlProvider: true).ProcessPushNotification(ShippingProviderType.DHL, notification, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task DHLPush_WithoutDhlProvider_ReturnsBadRequest()
        {
            var notification = new DHLParcelStatusChangeNotification { Shipments = new List<DHLPushShipment>() };

            var result = await CreateController().ProcessPushNotification(ShippingProviderType.DHL, notification, CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [Test]
        public async Task DPDPush_WithKeyValueDictionary_ReturnsOk()
        {
            var result = await CreateController(withDpdProvider: true)
                .ProcessPushNotification(ShippingProviderType.DPD, ValidDpdValues(), CancellationToken.None);

            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task DPDPush_WithoutDpdProvider_ReturnsBadRequest()
        {
            var result = await CreateController()
                .ProcessPushNotification(ShippingProviderType.DPD, ValidDpdValues(), CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [Test]
        public async Task DPDPush_WithNonDictionaryPayload_ReturnsBadRequest()
        {
            var result = await CreateController(withDpdProvider: true)
                .ProcessPushNotification(ShippingProviderType.DPD, new object(), CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [TestCase("pnr")]
        [TestCase("status")]
        public async Task DPDPush_WithMissingKeys_IsStillAccepted(string missingKey)
        {
            // An incomplete payload is only a failed result, not an exception (4xx only on exceptions).
            var values = ValidDpdValues();
            values.Remove(missingKey);

            var result = await CreateController(withDpdProvider: true)
                .ProcessPushNotification(ShippingProviderType.DPD, values, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task PushNotification_WithUnsupportedProvider_ReturnsBadRequest()
        {
            var result = await CreateController().ProcessPushNotification(ShippingProviderType.TRANSOFLEX, new object(), CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        #endregion push notification endpoints

        #region subscribe endpoints

        [TestCase(ShippingProviderType.DHL, "http://example.com/hook")]
        [TestCase(ShippingProviderType.DHL, "not-a-url")]
        [TestCase(ShippingProviderType.DHL, "")]
        [TestCase(ShippingProviderType.GLS, "http://example.com/hook")]
        [TestCase(ShippingProviderType.GLS, "not-a-url")]
        [TestCase(ShippingProviderType.GLS, "")]
        [TestCase(ShippingProviderType.DPD, "http://example.com/hook")]
        public async Task Ini_WithNonHttpsHookUri_ReturnsUnprocessableEntity(ShippingProviderType type, string hookUri)
        {
            var result = await CreateController().Ini(type, new PushInitRequest(hookUri), CancellationToken.None);

            AssertUnprocessable(result);
        }

        [TestCase(ShippingProviderType.DHL)]
        [TestCase(ShippingProviderType.GLS)]
        [TestCase(ShippingProviderType.TRANSOFLEX)]
        [TestCase(ShippingProviderType.CUSTOM)]
        public async Task Ini_WithoutProvider_ReturnsBadRequest(ShippingProviderType type)
        {
            var result = await CreateController().Ini(type, new PushInitRequest("https://example.com/hook"), CancellationToken.None);

            AssertBadRequest(result, ShippingErrorCode.UNKNOW, "Invalid operation.");
        }

        [Test]
        public async Task Ini_ForDpd_ReturnsEmptyOk()
        {
            var result = await CreateController().Ini(ShippingProviderType.DPD, new PushInitRequest("https://example.com/hook"), CancellationToken.None);

            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task Ini_ForGlsWithoutPortalSettings_ReturnsBadRequest()
        {
            var result = await CreateController(withGlsProvider: true)
                .Ini(ShippingProviderType.GLS, new PushInitRequest("https://example.com/hook"), CancellationToken.None);

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
        public void DHLSubscriptionRequest_Serialization_UsesDhlWireValues()
        {
            var json = Newtonsoft.Json.Linq.JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(new DHLWebhookSubscriptionRequest
            {
                Service = DHLServiceDivisionTypes.ParcelDe,
                Events = [DHLNotificationEventTypes.PreTransit, DHLNotificationEventTypes.Delivered]
            }));

            Assert.Multiple(() =>
            {
                Assert.That(json["service"]?.ToString(), Is.EqualTo("parcel-de"));
                Assert.That(json["events"]!.Select(e => e.ToString()), Is.EqualTo(new[] { "Pre-Transit", "Delivered" }));
            });
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

        [Test]
        public async Task HandleProcess_OnUnauthorizedAccess_ReturnsUnauthorizedWithChallenge()
        {
            var controller = CreateController();

            var result = await InvokeHandleProcess(controller, _ => throw new UnauthorizedAccessException("internal detail"));

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
            Assert.That(controller.Response.Headers.WWWAuthenticate.ToString(), Is.EqualTo("Basic"));
        }

        #endregion HandleProcess error mapping

        #region GLS push authorization

        private static GLSPushNotificationSettings GlsPushSettings(string username = GlsPushUser, string password = GlsPushPassword) => new()
        {
            ApiDomain = "https://example.com",
            ContactID = "contact",
            WebhookName = "webhook",
            WebhookUserId = "webhook-user",
            WebhookUsername = username,
            WebhookPassword = password
        };

        [Test]
        public void GLSPushSettings_WithMatchingBasicHeader_IsValid()
        {
            Assert.That(GlsPushSettings().IsValidAuthorizationHeader(BasicHeader(GlsPushUser, GlsPushPassword)), Is.True);
        }

        [Test]
        public void GLSPushSettings_SchemeIsCaseInsensitive()
        {
            var header = "basic " + BasicHeader(GlsPushUser, GlsPushPassword).Substring("Basic ".Length);

            Assert.That(GlsPushSettings().IsValidAuthorizationHeader(header), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Bearer token")]
        [TestCase("Basic !!!not-base64!!!")]
        public void GLSPushSettings_WithMissingOrMalformedHeader_IsInvalid(string? header)
        {
            Assert.That(GlsPushSettings().IsValidAuthorizationHeader(header), Is.False);
        }

        [Test]
        public void GLSPushSettings_WithWrongPassword_IsInvalid()
        {
            Assert.That(GlsPushSettings().IsValidAuthorizationHeader(BasicHeader(GlsPushUser, "wrong")), Is.False);
        }

        [Test]
        public void GLSPushSettings_WithoutConfiguredCredentials_FailsClosed()
        {
            // Empty credentials must never match an empty "Basic :" header.
            Assert.That(GlsPushSettings(username: "", password: "").IsValidAuthorizationHeader(BasicHeader("", "")), Is.False);
        }

        #endregion GLS push authorization

        #region DPD key/value mapping

        [Test]
        public void DPDNotification_FromKeyValues_MapsAllQueryKeys()
        {
            var values = new Dictionary<string, string?>
            {
                ["pushid"] = "push-1",
                ["ref"] = "reference",
                ["pnr"] = "01905123456789",
                ["depot"] = "0191",
                ["status"] = "delivery_customer",
                ["statusdate"] = "21102026143000",
                ["weight"] = "1.5",
                ["receiver"] = "Homer",
                ["pod"] = "pod-link",
                ["services"] = "service",
                ["scaninfo"] = "scan"
            };

            var notification = DPDParcelStatusChangeNotification.FromKeyValues(values);

            Assert.Multiple(() =>
            {
                Assert.That(notification.PushId, Is.EqualTo("push-1"));
                Assert.That(notification.Reference, Is.EqualTo("reference"));
                Assert.That(notification.ParcelNumber, Is.EqualTo("01905123456789"));
                Assert.That(notification.Depot, Is.EqualTo("0191"));
                Assert.That(notification.Status, Is.EqualTo("delivery_customer"));
                Assert.That(notification.StatusDate, Is.EqualTo(new DateTime(2026, 10, 21, 14, 30, 0)));
                Assert.That(notification.Weight, Is.EqualTo("1.5"));
                Assert.That(notification.Receiver, Is.EqualTo("Homer"));
                Assert.That(notification.ProofOfDelivery, Is.EqualTo("pod-link"));
                Assert.That(notification.Services, Is.EqualTo("service"));
                Assert.That(notification.ScanInfo, Is.EqualTo("scan"));
            });
        }

        [Test]
        public void DPDNotification_FromKeyValues_IsCaseInsensitiveAndLeavesMissingKeysNull()
        {
            var notification = DPDParcelStatusChangeNotification.FromKeyValues(new Dictionary<string, string?> { ["PushID"] = "push-1" });

            Assert.Multiple(() =>
            {
                Assert.That(notification.PushId, Is.EqualTo("push-1"));
                Assert.That(notification.ParcelNumber, Is.Null);
                Assert.That(notification.Status, Is.Null);
                Assert.That(notification.StatusDate, Is.Null);
            });
        }

        #endregion DPD key/value mapping
    }
}
