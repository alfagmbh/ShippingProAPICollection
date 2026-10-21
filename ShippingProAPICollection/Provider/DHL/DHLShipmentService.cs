using System.Globalization;
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
        /// Subscribes to DHL push notifications for every configured service division / event combination.
        /// The subscriptions must afterwards be activated via <see cref="ActivateSubscriber"/>.
        /// </summary>
        public async Task InitializePushNotification(string hookUri, CancellationToken ct = default)
        {
            var settings = providerSettings.PushNotificationSettings;

            using var client = new RestClient(new RestClientOptions(string.Format("https://api-{0}.dhl.com/tracking/push/v1", settings.ApiDomain))
            {
                Timeout = TimeSpan.FromSeconds(30),
            });

            var failures = new List<string>();
            foreach (var subscription in settings.Subscriptions)
            {
                var request = new RestRequest("/subscription", Method.Post);
                request.AddHeader("DHL-API-Key", settings.APIKey);
                request.AddJsonBody(new DHLWebhookSubscriptionRequest
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
                        },
                        Headers = new List<DHLWebhookHeader>
                        {
                            new() { Name = settings.WebhookHeaderName, Value = settings.WebhookHeaderValue }
                        }
                    },
                    Events = [.. subscription.Events],
                    Type = "Account",
                    AccountID = settings.AccountID
                });

                var response = await client.ExecuteAsync(request, ct);
                if (!response.IsSuccessful)
                    failures.Add($"{subscription.Service}: {(int)response.StatusCode} {response.Content}");
            }

            if (failures.Count > 0)
                throw new DHLException(ShippingErrorCode.UNKNOW, "DHL rejected push subscription(s): " + string.Join("; ", failures));
        }

        public async Task ActivateSubscriber(DHLSubscriberActivatorRequest activator, CancellationToken ct = default)
        {
            var activatorOptions = new RestClientOptions(string.Format("https://api-{0}.dhl.com/tracking/push/v1", providerSettings.ApiDomain))
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            using var activatorClient = new RestClient(activatorOptions);
            var activatorRequest = new RestRequest("/subscription/{subscriberId}", Method.Post);
            activatorRequest.AddUrlSegment("subscriberId", activator.SubscriberId);
            activatorRequest.AddHeader("DHL-API-Hook-Secret", activator.DhlApiHookSecret);
            activatorRequest.AddHeader("DHL-API-Key", activator.DhlApiKey);
            activatorRequest.AddHeader("Content-Length", "0");
            RestResponse activatorResponse = await activatorClient.ExecuteAsync(activatorRequest, ct);

            if (!activatorResponse.IsSuccessful)
                throw new DHLException(ShippingErrorCode.UNKNOW, $"DHL rejected the subscriber activation: {(int)activatorResponse.StatusCode} {activatorResponse.Content}");
        }

        public ParcelTrackingEventResult ProcessParcelStatusChangeNotification(ParcelStatusChangeNotification? parcelStatusChangeNotification, CancellationToken cancelToken)
        {
            if (parcelStatusChangeNotification is not DHLParcelStatusChangeNotification dhlParcelStatusChangeNotification ||
                dhlParcelStatusChangeNotification.Shipments is null ||
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
            foreach (var shipment in dhlParcelStatusChangeNotification.Shipments)
            {
                ParcelTrackingEvent trackParcelResponse = new ParcelTrackingEvent()
                {
                    Status = DHLTrackingStatusTypes.GetTrackingStatusType(shipment.Status.StatusCode ?? "").ToString(),
                    APIValue = apiValue,
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
    }
}