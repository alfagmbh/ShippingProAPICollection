using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using ShippingProAPICollection._Provider.DHL;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Models.Utils;
using ShippingProAPICollection.Provider.DHL.Entities;
using ShippingProAPICollection.Provider.DHL.Entities.VASService;

namespace ShippingProAPICollection.Provider.DHL
{
    public class DHLShipmentService : IShippingProviderService
    {
        private const string DHLPushFormat = "Default";
        private const string DHLPushScopePush = "subscription.push";
        private const string DHLPushScopeValidate = "subscription.validate";
        private const string DHLHookSecretHeader = "DHL-API-Hook-Secret";
        private const int MaxShipmentIdsPerSubscription = 10;

        private DHLSettings providerSettings = null!;
        private ShippingProAPIShipFromAddress defaultShipFromAddress = null!;

        public DHLShipmentService(ShippingProAPIShipFromAddress defaultShipFromAddress, DHLSettings providerSettings)
        {
            this.defaultShipFromAddress = defaultShipFromAddress;
            this.providerSettings = providerSettings;
        }

        public async Task<List<RequestShippingLabelResponse>> RequestLabel(RequestShipmentBase request, CancellationToken cancelToken = default)
        {
            var DHLRequest = request as DHLShipmentRequestModel;
            if (DHLRequest == null) throw new Exception("Cannot convert request to DHLShipmentRequestModel");

            var urlBuilder_ = new System.Text.StringBuilder();
            var baseUrl = string.Format("https://api-{0}.dhl.com/parcel/de/shipping/v2/", providerSettings.ApiDomain);

            urlBuilder_.Append(baseUrl != null ? baseUrl.TrimEnd('/') : "").Append("/orders?");
            urlBuilder_.Append(Uri.EscapeDataString("docFormat") + "=").Append(DHLLabelDocFormat.PDF.ToString()).Append("&");
            urlBuilder_.Append(Uri.EscapeDataString("printFormat") + "=").Append(providerSettings.LabelPrintFormat).Append("&");
            urlBuilder_.Length--;

            var clientOptions = new RestClientOptions(new Uri(urlBuilder_.ToString(), UriKind.RelativeOrAbsolute))
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                Authenticator = new HttpBasicAuthenticator(providerSettings.Username, providerSettings.Password)
            };

            using (RestClient client = new RestClient(clientOptions))
            {
                var clientRequest = new RestRequest()
                {
                    Method = Method.Post
                };

                clientRequest.AddHeader("dhl-api-key", providerSettings.APIKey);
                clientRequest.AddHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(providerSettings.APILanguage))
                {
                    clientRequest.AddHeader("Accept-Language", providerSettings.APILanguage);
                }

                var requestBody = CreateRequestModel(DHLRequest);

                string requestJson = JsonConvert.SerializeObject(requestBody, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                });

                clientRequest.AddBody(requestJson);

                RestResponse response = await client.ExecuteAsync(clientRequest, cancelToken).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.MultiStatus)
                {
                    var objectResponse_ = JsonConvert.DeserializeObject<LabelDataResponse>(response.Content ?? "");
                    if (objectResponse_ == null)
                    {
                        throw new DHLException(ShippingErrorCode.CANNOT_CONVERT_RESPONSE, "Cannot convert reponse to object", new { payload = requestBody, respone = response.Content });
                    }

                    List<RequestShippingLabelResponse> labels = new List<RequestShippingLabelResponse>();

                    for (int i = 0; i < objectResponse_.Items.Count; i++)
                    {
                        var c = objectResponse_.Items.ElementAt(i);

                        labels.Add(new RequestShippingLabelResponse()
                        {
                            Labels = [Convert.FromBase64String(c.Label.B64)],
                            ParcelNumber = c.ShipmentNo,
                            CancelId = c.ShipmentNo,
                            LabelType = request.IsExpress() ? ShippingLabelType.EXPRESS : ShippingLabelType.NORMAL,
                            Weight = request.Items[i].Weight,
                        });
                    }

                    await SubscribeToPushNotification(labels.Select(l => l.ParcelNumber), ct: cancelToken);

                    return labels;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var objectResponse_ = JsonConvert.DeserializeObject<LabelDataResponse>(response.Content ?? "");
                    if (objectResponse_ == null)
                    {
                        throw new DHLException(ShippingErrorCode.CANNOT_CONVERT_RESPONSE, "Cannot convert reponse to object", new { payload = requestBody, respone = response.Content });
                    }

                    throw new DHLException(ShippingErrorCode.BAD_REQUEST_ERROR, objectResponse_.Items?.FirstOrDefault()?.ValidationMessages.FirstOrDefault()?.ValidationMessage ?? "Unknow message", new { payload = requestBody, respone = response.Content });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new DHLException(ShippingErrorCode.UNAUTHORIZED, $"Request reponsed with HTTP status code {(int)response.StatusCode}", new { payload = requestBody, respone = response.Content });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new DHLException(ShippingErrorCode.TO_MANY_REQUESTS, $"Request reponsed with HTTP status code {(int)response.StatusCode}", new { payload = requestBody, respone = response.Content });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw new DHLException(ShippingErrorCode.INTERNAL_SERVER_ERROR, $"Request reponsed with HTTP status code {(int)response.StatusCode}", new { payload = requestBody, respone = response.Content });
                }
                else
                {
                    throw new DHLException(ShippingErrorCode.UNKNOW, "The HTTP status code of the response was not expected (" + (int)response.StatusCode + ").", new { payload = requestBody, respone = response.Content });
                }
            }
        }

        public async Task<ShippingCancelResult> CancelLabel(string cancelId, CancellationToken cancelToken = default)
        {
            var urlBuilder_ = new System.Text.StringBuilder();
            var baseUrl = string.Format("https://api-{0}.dhl.com/parcel/de/shipping/v2/", providerSettings.ApiDomain);

            urlBuilder_.Append(baseUrl != null ? baseUrl.TrimEnd('/') : "").Append("/orders?");
            urlBuilder_.Append(Uri.EscapeDataString("profile") + "=").Append(providerSettings.DHLShipmentProfile).Append("&");
            urlBuilder_.Append(Uri.EscapeDataString("shipment") + "=").Append(cancelId).Append("&");
            urlBuilder_.Length--;

            var clientOptions = new RestClientOptions(new Uri(urlBuilder_.ToString(), UriKind.RelativeOrAbsolute))
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                Authenticator = new HttpBasicAuthenticator(providerSettings.Username, providerSettings.Password)
            };

            using (RestClient client = new RestClient(clientOptions))
            {
                var clientRequest = new RestRequest()
                {
                    Method = Method.Delete
                };

                clientRequest.AddHeader("dhl-api-key", providerSettings.APIKey);
                clientRequest.AddHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(providerSettings.APILanguage))
                {
                    clientRequest.AddHeader("Accept-Language", providerSettings.APILanguage);
                }

                RestResponse response = await client.ExecuteAsync(clientRequest, cancelToken).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.MultiStatus)
                {
                    var objectResponse_ = JsonConvert.DeserializeObject<LabelDataResponse>(response.Content ?? "");
                    if (objectResponse_ == null)
                    {
                        throw new DHLException(ShippingErrorCode.CANNOT_CONVERT_RESPONSE, "Cannot convert reponse to object", new { payload = cancelId, respone = response.Content });
                    }

                    return ShippingCancelResult.CANCELED;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var objectResponse_ = JsonConvert.DeserializeObject<LabelDataResponse>(response.Content ?? "");
                    if (objectResponse_ == null)
                    {
                        throw new DHLException(ShippingErrorCode.CANNOT_CONVERT_RESPONSE, "Cannot convert reponse to object", new { payload = cancelId, respone = response.Content });
                    }

                    throw new DHLException(ShippingErrorCode.BAD_REQUEST_ERROR, objectResponse_.Items?.FirstOrDefault()?.ValidationMessages.FirstOrDefault()?.ValidationMessage ?? "Unknow message", new { payload = cancelId, respone = response.Content });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new DHLException(ShippingErrorCode.UNAUTHORIZED, $"Request reponsed with HTTP status code {(int)response.StatusCode}", new { payload = cancelId, respone = response.Content });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new DHLException(ShippingErrorCode.TO_MANY_REQUESTS, $"Request reponsed with HTTP status code {(int)response.StatusCode}", new { payload = cancelId, respone = response.Content });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw new DHLException(ShippingErrorCode.INTERNAL_SERVER_ERROR, $"Request reponsed with HTTP status code {(int)response.StatusCode}", new { payload = cancelId, respone = response.Content });
                }
                else
                {
                    throw new DHLException(ShippingErrorCode.UNKNOW, "The HTTP status code of the response was not expected (" + (int)response.StatusCode + ").", new { payload = cancelId, respone = response.Content });
                }
            }
        }

        public async Task<ValidationReponse> ValidateLabel(RequestShipmentBase request, CancellationToken cancelToken)
        {
            throw new DHLException(ShippingErrorCode.NOT_AVAILABLE, "Feature not available for DHL");
        }

        public async Task<uint> GetEstimatedDeliveryDays(RequestShipmentBase request, CancellationToken cancelToken)
        {
            throw new DHLException(ShippingErrorCode.NOT_AVAILABLE, "Feature not available for DHL");
        }

        public async Task ConfirmShipment(string parcelId, CancellationToken cancelToken)
        {
            throw new DHLException(ShippingErrorCode.NOT_AVAILABLE, "Feature not available for DHL");
        }

        public float GetMaxPackageWeight()
        {
            return providerSettings.MaxPackageWeight;
        }

        /// <summary>
        /// Create the DHL request body informations
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private ShipmentOrderRequest CreateRequestModel(DHLShipmentRequestModel request)
        {
            var from = request.ShipFromAddress ?? defaultShipFromAddress;

            // Resolve dependent Services
            foreach (var dS in request.ServiceProduct.GetDependentServices()
                     .Where(dS => request.VASServices.All(x => x.GetType() != dS.GetType())))
            {
                request.VASServices.Add(dS);
            }

            // Build shipper infos
            var shipper = new Shipper()
            {
                Name1 = from.Name,
                Name2 = string.IsNullOrWhiteSpace(from.Name2) ? null : from.Name2,
                Name3 = string.IsNullOrWhiteSpace(from.Name3) ? null : from.Name3,
                AddressStreet = from.Street,
                PostalCode = from.PostCode,
                City = from.City,
                Country = EnumUtils.ToEnum(ThreeLetterCountryCodeHelper.GetThreeLetterCountryCode(from.CountryIsoA2Code), Country.UNKNOWN),
                ContactName = from.ContactName,
                Email = from.Email,
                AddressHouse = null,
            };

            Consignee consignee = GetConsignee(request);

            ShipmentOrderRequest shipmentOrderRequest = new ShipmentOrderRequest();
            shipmentOrderRequest.Profile = providerSettings.DHLShipmentProfile;

            // Build shipment for each requested label
            for (int i = 0; i < request.Items.Count; i++)
            {
                _Provider.DHL.Shipment shipment = new _Provider.DHL.Shipment();

                shipment.Product = request.ServiceProduct.ToServiceString();
                shipment.BillingNumber = GetBillingNumber(request);
                shipment.ShipDate = DateTimeOffset.Now;
                shipment.Shipper = shipper;
                shipment.Consignee = consignee;
                shipment.RefNo = request.GetRefString();

                shipment.Details = new _Provider.DHL.ShipmentDetails()
                {
                    Weight = new _Provider.DHL.Weight()
                    {
                        Uom = WeightUom.Kg,
                        Value = request.Items[i].Weight
                    }
                };

                shipment.Services = ResolveVASService(request);

                shipmentOrderRequest.Shipments.Add(shipment);
            }

            return shipmentOrderRequest;
        }

        private VAS ResolveVASService(DHLShipmentRequestModel request)
        {
            VAS vas = new VAS();

            foreach (var service in request.VASServices)
            {
                switch (service)
                {
                    case VASPlaceOfDepositService placeOfDepositService:
                        vas.PreferredLocation = request.ServiceType == DHLServiceType.DEPOSIT ? placeOfDepositService.PlaceOfDeposit : null;
                        break;

                    case VASPreferredNeighbourService preferredNeighbourService:
                        vas.PreferredNeighbour = preferredNeighbourService.PreferredNeighbour;
                        break;

                    case VASPreferredLocationService preferredLocationService:
                        vas.PreferredLocation = preferredLocationService.PreferredLocation;
                        break;

                    case VASIdentCheckService identCheckService:
                        vas.IdentCheck = new VASIdentCheck()
                        {
                            DateOfBirth = identCheckService.DateOfBirth,
                            FirstName = identCheckService.FirstName,
                            LastName = identCheckService.LastName,
                            MinimumAge = identCheckService.MinimumAge == MinimumAge.A16 ? VASIdentCheckMinimumAge.A16 : VASIdentCheckMinimumAge.A18,
                        };
                        break;

                    case VASTransportInsuranceService transportInsuranceService:
                        vas.AdditionalInsurance = new Value
                        {
                            Value1 = transportInsuranceService.Value,
                            Currency = EnumUtils.ToEnum(transportInsuranceService.Currency, ValueCurrency.UNKNOWN),
                        };
                        break;

                    case VASPremiumService:
                        vas.Premium = true;
                        break;
                }
            }

            return vas;
        }

        /// <summary>
        /// Build consignee informations
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private Consignee GetConsignee(DHLShipmentRequestModel request)
        {
            Consignee consignee = new Consignee();
            consignee.AdditionalProperties.Add("city", request.City);
            consignee.AdditionalProperties.Add("postalCode", request.PostCode);
            consignee.AdditionalProperties.Add("country", ThreeLetterCountryCodeHelper.GetThreeLetterCountryCode(request.Country));

            if (request.WithEmailNotification && !string.IsNullOrEmpty(request.EMail))
            {
                consignee.AdditionalProperties.Add("email", request.EMail);
            }

            if (!string.IsNullOrWhiteSpace(request.ContactName))
            {
                consignee.AdditionalProperties.Add("contactName", request.ContactName);
            }

            switch (request.ServiceType)
            {
                case DHLServiceType.NONE:
                case DHLServiceType.DEPOSIT:
                    consignee.AdditionalProperties.Add("name1", request.Addressline1);
                    consignee.AdditionalProperties.Add("addressStreet", request.Street);

                    if (!String.IsNullOrEmpty(request.Addressline2)) consignee.AdditionalProperties.Add("name2", request.Addressline2);
                    if (!String.IsNullOrEmpty(request.Addressline3)) consignee.AdditionalProperties.Add("name3", request.Addressline3);
                    if (!String.IsNullOrEmpty(request.Note1)) consignee.AdditionalProperties.Add("dispatchingInformation", request.Note1);
                    if (!String.IsNullOrEmpty(request.StreetNumber)) consignee.AdditionalProperties.Add("addressHouse", request.StreetNumber);
                    if (!String.IsNullOrEmpty(request.Phone)) consignee.AdditionalProperties.Add("phone", request.Phone);
                    break;

                case DHLServiceType.LOCKER:
                    if (request.Locker == null) throw new ShipmentRequestNotNullException("Locker");
                    consignee.AdditionalProperties.Add("name", request.Addressline1);
                    consignee.AdditionalProperties.Add("lockerID", request.Locker.PackstationNumber);
                    consignee.AdditionalProperties.Add("postNumber", request.Locker.PostNumber);
                    break;

                case DHLServiceType.POSTOFFICE:
                    if (request.PostOffice == null) throw new ShipmentRequestNotNullException("PostOffice");
                    consignee.AdditionalProperties.Add("name", request.Addressline1);
                    consignee.AdditionalProperties.Add("retailID", request.PostOffice.PostfilialeNumber);
                    consignee.AdditionalProperties.Add("postNumber", request.PostOffice.PostNumber);
                    break;
            }

            return consignee;
        }

        /// <summary>
        /// Get DHL billing number by product type
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private string GetBillingNumber(DHLShipmentRequestModel request)
        {
            switch (request.ServiceProduct)
            {
                case DHLProductType.V01PAK:
                case DHLProductType.V01PRIO:
                    return providerSettings.NationalAccountNumber;

                case DHLProductType.V53WPAK:
                    return providerSettings.InternationalAccountNumber;

                case DHLProductType.V62KP:
                    if (string.IsNullOrWhiteSpace(providerSettings.WarenpostNationalAccountNumber))
                        throw new Exception("Warenpost national Accountnumber not defined");
                    return providerSettings.WarenpostNationalAccountNumber;

                case DHLProductType.V66WPI:
                case DHLProductType.V66WPI_V66PREM:
                    if (string.IsNullOrWhiteSpace(providerSettings.WarenpostInternationalAccountNumber))
                        throw new Exception("Warenpost international Accountnumber not defined");
                    return providerSettings.WarenpostInternationalAccountNumber;

                default:
                    throw new Exception("Accountnumber not defined");
            }
        }

        /// <summary>
        /// Subscribes the parcels to DHL push notifications (subscription type "Shipment", at most <see cref="MaxShipmentIdsPerSubscription"/> parcels per request)
        /// for every active subscription entry. Stops when the daily limit of the contract is reached or DHL answers 429.
        /// Returns one entry per request with what DHL answered (a rejected subscription has <see cref="PushSubscriptionInfo.Error"/> set).
        /// DHL calls <paramref name="hookUri"/>, or the HookUri of the push settings when it is not given.
        /// The subscriptions are not active yet: the caller activates each one with DHL's validation push, see <see cref="ActivateSubscriber"/>.
        /// Parcels that were not subscribed because of the daily limit are returned as an entry with an error.
        /// </summary>
        public async Task<List<PushSubscriptionInfo>> SubscribeToPushNotification(IEnumerable<string?> trackingNumbers, string? hookUri = null, CancellationToken ct = default)
        {
            var results = new List<PushSubscriptionInfo>();
            var settings = providerSettings.PushNotificationSettings;
            var numbers = trackingNumbers.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).Distinct().ToList();
            if (settings is null || numbers.Count == 0)
                return results;

            hookUri ??= settings.HookUri;
            ValidatePushSettings(settings, hookUri);

            using var client = new RestClient(new RestClientOptions(string.Format("https://api-{0}.dhl.com/tracking/push/v1", settings.ApiDomain))
            {
                Timeout = TimeSpan.FromSeconds(30),
            });

            var limitReached = false;
            foreach (var subscription in settings.Subscriptions.Where(s => s.IsActive))
            {
                var allowed = DHLPushSubscriptionQuota.Reserve(providerSettings.ContractID, numbers.Count, providerSettings.PushSubscriptionDailyLimit, DateTime.UtcNow);
                var handled = 0;

                for (int i = 0; i < allowed && !limitReached; i += MaxShipmentIdsPerSubscription)
                {
                    var batch = numbers.Skip(i).Take(Math.Min(MaxShipmentIdsPerSubscription, allowed - i)).ToList();
                    var request = new RestRequest("/subscription", Method.Post);
                    request.AddHeader("DHL-API-Key", settings.APIKey);
                    request.AddStringBody(BuildSubscriptionRequestJson(settings, subscription, batch, hookUri), DataFormat.Json);

                    var response = await client.ExecuteAsync(request, ct);
                    var info = ParseSubscriptionResponse(subscription.Service, (int)response.StatusCode, response.IsSuccessful, response.Content);
                    info.TrackingNumbers = [.. batch];
                    results.Add(info);
                    handled += batch.Count;

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        DHLPushSubscriptionQuota.Exhaust(providerSettings.ContractID, DateTime.UtcNow);
                        limitReached = true;
                    }
                }

                if (handled < numbers.Count)
                    results.Add(new PushSubscriptionInfo
                    {
                        Service = subscription.Service.ToString(),
                        TrackingNumbers = [.. numbers.Skip(handled)],
                        Error = $"Not subscribed: the daily limit of {providerSettings.PushSubscriptionDailyLimit} subscriptions is reached, try again tomorrow."
                    });

                if (limitReached)
                    break;
            }

            return results;
        }

        /// <summary>
        /// Subscribing is best effort: the labels are already created at DHL, so a failed or skipped subscription must not fail the label request.
        /// </summary>
        private async Task TrySubscribeShipments(IEnumerable<string?> trackingNumbers, CancellationToken ct)
        {
            try
            {
                await SubscribeToPushNotification(trackingNumbers, ct: ct).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Parcels are subscribed when their label is created (<see cref="RequestLabel"/>); there is nothing to set up in advance.
        /// </summary>
        public Task InitializePushNotification(string hookUri, CancellationToken ct = default) => Task.CompletedTask;

        /// <summary>
        /// Turns DHL's answer to a subscribe request into a <see cref="PushSubscriptionInfo"/>.
        /// The error only carries the status code and DHL's problem detail, never the request (it contains the push token).
        /// </summary>
        public static PushSubscriptionInfo ParseSubscriptionResponse(DHLServiceDivisionTypes service, int statusCode, bool isSuccessful, string? content)
        {
            var info = new PushSubscriptionInfo { Service = service.ToString() };

            try
            {
                if (isSuccessful)
                {
                    var state = JsonConvert.DeserializeObject<DHLSubscriptionResponse>(content ?? "");
                    info.Scope = state?.Scope;
                    info.SubscriptionId = SubscriptionIdFromSelf(state?.Self);
                    info.Expires = state?.Expires;
                    info.Note = state?.Note;
                }
                else
                {
                    var detail = JsonConvert.DeserializeObject<DHLProblemDetail>(content ?? "")?.Detail;
                    info.Error = string.IsNullOrWhiteSpace(detail) ? $"HTTP {statusCode}" : $"HTTP {statusCode}: {detail}";
                }
            }
            catch (JsonException)
            {
                if (!isSuccessful) info.Error = $"HTTP {statusCode}";
            }

            return info;
        }

        /// <summary>
        /// The subscription id is the last path segment of DHL's "self" link.
        /// </summary>
        private static string? SubscriptionIdFromSelf(string? self) =>
            Uri.TryCreate(self, UriKind.Absolute, out var uri)
                ? uri.Segments.Select(s => s.Trim('/')).LastOrDefault(s => s.Length > 0)
                : null;

        /// <summary>
        /// Serializes the subscription request with Newtonsoft; RestSharp's default (System.Text.Json) ignores the Newtonsoft enum/property attributes
        /// and would send the enums as numbers.
        /// </summary>
        public static string BuildSubscriptionRequestJson(DHLPushNotificationSettings settings, DHLPushSubscription subscription, IEnumerable<string> shipmentIds, string hookUri) =>
            JsonConvert.SerializeObject(new DHLWebhookSubscriptionRequest
            {
                Format = settings.Format,
                Service = subscription.Service,
                Hook = new DHLWebhookHook
                {
                    Uri = hookUri,
                    Authentication = new DHLWebhookAuthentication
                    {
                        Type = "ApiKeyAuthentication",
                        Header = settings.AuthenticationHeader,
                        Token = settings.AuthenticationToken
                    }
                },
                Events = [.. subscription.Events],
                Type = "Shipment",
                ShipmentIDs = [.. shipmentIds]
            }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        private static void ValidatePushSettings(DHLPushNotificationSettings settings, string hookUri)
        {
            if (!settings.Subscriptions.Any(s => s.IsActive))
                throw new InvalidOperationException("At least one active DHL push subscription must be configured. Entries with an unknown Service (parcel-de, express, ecommerce) or Events value (pre-transit, transit, delivered, failure, unknown) are dropped when the settings are read.");

            if (!Uri.TryCreate(hookUri, UriKind.Absolute, out var hook) || hook.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("DHL push HookUri must be an absolute https URL.");

            // The notification handler only understands the UTAPI "Default" format.
            if (settings.Format != DHLPushFormat)
                throw new InvalidOperationException($"DHL push Format must be '{DHLPushFormat}'.");

            // Spec: ApiKeyAuthentication.header must match ^[a-zA-Z0-9\-]+$
            if (!System.Text.RegularExpressions.Regex.IsMatch(settings.AuthenticationHeader ?? "", @"^[a-zA-Z0-9\-]+$"))
                throw new InvalidOperationException("DHL push AuthenticationHeader may only contain letters, digits and '-'.");

            if (string.IsNullOrEmpty(settings.AuthenticationToken))
                throw new InvalidOperationException("DHL push AuthenticationToken must be configured.");
        }

        /// <summary>
        /// Activates a subscription at DHL with the secret of its validation push (<c>subscription.validate</c>).
        /// </summary>
        private async Task ActivateSubscriber(string subscriptionId, string secret, CancellationToken ct)
        {
            var settings = providerSettings.PushNotificationSettings
                ?? throw new InvalidOperationException($"Push notification settings for contract {providerSettings.ContractID} are missing in the secrets file.");
            var activatorOptions = new RestClientOptions(string.Format("https://api-{0}.dhl.com/tracking/push/v1", settings.ApiDomain))
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            using var activatorClient = new RestClient(activatorOptions);
            var activatorRequest = new RestRequest("/subscription/{subscriberId}", Method.Post);
            activatorRequest.AddUrlSegment("subscriberId", subscriptionId);
            activatorRequest.AddHeader(DHLHookSecretHeader, secret);
            activatorRequest.AddHeader("DHL-API-Key", settings.APIKey);
            RestResponse activatorResponse = await activatorClient.ExecuteAsync(activatorRequest, ct);

            if (!activatorResponse.IsSuccessful)
                throw new DHLException(ShippingErrorCode.UNKNOW, $"DHL rejected the subscriber activation: {(int)activatorResponse.StatusCode} {activatorResponse.Content}");
        }

        /// <summary>
        /// DHL sends the secret of the validation push in the body and in the DHL-API-Hook-Secret header; both have to be there and match.
        /// </summary>
        private static bool HasMatchingHookSecret(IReadOnlyDictionary<string, string>? headers, string? secret) =>
            !string.IsNullOrEmpty(secret) && headers is not null && headers.TryGetValue(DHLHookSecretHeader, out var headerSecret) &&
            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(headerSecret), System.Text.Encoding.UTF8.GetBytes(secret));

        /// <summary>
        /// DHL calls the hook with the header/token registered at subscription time; nothing is processed without them.
        /// Fails closed when no token is configured.
        /// </summary>
        private bool HasValidPushCredentials(IReadOnlyDictionary<string, string>? headers)
        {
            var settings = providerSettings.PushNotificationSettings;
            if (settings is null || headers is null || string.IsNullOrEmpty(settings.AuthenticationHeader) || string.IsNullOrEmpty(settings.AuthenticationToken))
                return false;

            bool Matches(string name, string expected) =>
                headers.TryGetValue(name, out var value) &&
                System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(value), System.Text.Encoding.UTF8.GetBytes(expected));

            return Matches(settings.AuthenticationHeader, settings.AuthenticationToken);
        }

        public async Task<ParcelTrackingEventResult> ProcessParcelStatusChangeNotification(ParcelStatusChangeNotification? parcelStatusChangeNotification, CancellationToken cancelToken)
        {
            if (parcelStatusChangeNotification is not DHLParcelStatusChangeNotification dhlParcelStatusChangeNotification)
                throw new UnauthorizedAccessException("Invalid DHL push notification credentials.");

            // Subscription handshake messages (validate/ready/...) carry no shipments and DHL does not send the push token with them.
            // They are not processed as tracking data: ready is only acknowledged, validate is answered with the activation of the subscription.
            var isHandshake = dhlParcelStatusChangeNotification.Scope is not null && dhlParcelStatusChangeNotification.Scope != DHLPushScopePush;

            if (!isHandshake && !HasValidPushCredentials(dhlParcelStatusChangeNotification.RequestHeaders))
                throw new UnauthorizedAccessException("Invalid DHL push notification credentials.");

            return isHandshake ? await ProzessHandshake(dhlParcelStatusChangeNotification, cancelToken)
                : ProzessParcelStatus(dhlParcelStatusChangeNotification);
        }

        private static ParcelTrackingEventResult ProzessParcelStatus(DHLParcelStatusChangeNotification dhlParcelStatusChangeNotification)
        {
            if (dhlParcelStatusChangeNotification.Shipments is null ||
                dhlParcelStatusChangeNotification.Shipments.Count == 0 ||
                    dhlParcelStatusChangeNotification.Shipments.Any(s => s is null || string.IsNullOrWhiteSpace(s.Id) || s.Status is null))
                return new ParcelTrackingEventResult()
                {
                    Status = NotificationResultStatus.Failed,
                    Message = "Invalid DHL parcel status change notification payload",
                };

            string apiValue = JsonConvert.SerializeObject(dhlParcelStatusChangeNotification);
            List<ParcelTrackingEvent> trackingEvents = new List<ParcelTrackingEvent>();
            // A single push can contain several shipments; publish one tracking event per shipment.
            foreach (DHLPushShipment shipment in dhlParcelStatusChangeNotification.Shipments)
            {
                var statusType = DHLTrackingStatusTypes.GetTrackingStatusType(shipment.Status.StatusCode ?? "");
                ParcelTrackingEvent trackParcelResponse = new ParcelTrackingEvent()
                {
                    Status = statusType.ToString(),
                    StatusMessage = statusType.TranslateTrackingStatusMessage(),
                    APIValue = apiValue,
                    EstimatedDeliveryDate = shipment.EstimatedTimeOfDelivery,
                    IsReturned = shipment.ReturnFlag.HasValue ? (shipment.ReturnFlag.Value ? ReturnStatus.Returned : ReturnStatus.NotReturned) : ReturnStatus.Unknown,
                    ParcelNumber = shipment.Id,
                };

                trackingEvents.Add(trackParcelResponse);
            }

            return new ParcelTrackingEventResult()
            {
                Status = NotificationResultStatus.Success,
                TrackingEvent = [.. trackingEvents]
            };
        }

        private async Task<ParcelTrackingEventResult> ProzessHandshake(DHLParcelStatusChangeNotification dhlParcelStatusChangeNotification, CancellationToken cancelToken)
        {
            var subscriptionId = SubscriptionIdFromSelf(dhlParcelStatusChangeNotification.Self);

            if (dhlParcelStatusChangeNotification.Scope == DHLPushScopeValidate)
            {
                if (!HasMatchingHookSecret(dhlParcelStatusChangeNotification.RequestHeaders, dhlParcelStatusChangeNotification.Secret))
                    throw new UnauthorizedAccessException($"The {DHLHookSecretHeader} header is missing or does not match the secret of the DHL push.");

                if (string.IsNullOrEmpty(subscriptionId))
                    throw new ArgumentException("The DHL validation push has no valid 'self' link.");

                await ActivateSubscriber(subscriptionId, dhlParcelStatusChangeNotification.Secret!, cancelToken).ConfigureAwait(false);
            }

            return new ParcelTrackingEventResult()
            {
                Status = NotificationResultStatus.Success,
                Message = $"DHL subscription message '{dhlParcelStatusChangeNotification.Scope}' acknowledged",
                Subscription = new PushSubscriptionInfo
                {
                    Scope = dhlParcelStatusChangeNotification.Scope,
                    SubscriptionId = subscriptionId,
                    Expires = dhlParcelStatusChangeNotification.Expires,
                    Note = dhlParcelStatusChangeNotification.Note,
                },
            };
        }
    }
}