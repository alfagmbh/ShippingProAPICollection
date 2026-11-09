# Push notification setup: requirements for the receiving system

DHL, GLS and DPD send their tracking pushes to the webhook of the receiving system, not to the API. The receiving system only forwards each received push, unchanged, to the API. The API reads the request, checks it, processes it and answers with uniform tracking events.

Pushes are not parsed, rebuilt, escaped or filtered by the receiving system. Method, headers, query string and body are forwarded exactly as received (see [3. Forwarding requirements](#3-forwarding-requirements)).

## 1. What to call

| Method | URL | Purpose |
| --- | --- | --- |
| `GET` or `POST` | `/api/push/notification/{type}` | Forward a received push |

`{type}` is the carrier that sent the push:

| `{type}` | Carrier |
| --- | --- |
| `DHL` | DHL |
| `GLS` | GLS |
| `DPD` | DPD |

The method of the original push is used.

The endpoint is called once for every push, including the DHL handshake pushes (`subscription.validate`, `subscription.ready`). Without the forwarded validate push, the DHL subscription is never activated.

## 2. How to call

The push is forwarded as it arrived. What the API reads from each carrier:

| Carrier | What is read | What must be kept |
| --- | --- | --- |
| **DHL** | JSON body and headers | The `Authorization` header as received. It carries the push token that is checked. For `subscription.validate`, also the `DHL-API-Hook-Secret` header. |
| **GLS** | JSON body and `Authorization` header | The `Authorization` header (Basic) as received. |
| **DPD** | Query string (a JSON object body is accepted too) | The full query string: `pushid`, `ref`, `pnr`, `depot`, `status`, `statusdate`, `weight`, `receiver`, `pod`, `services`, `scaninfo`. |

Example (DHL tracking push):

    POST /api/push/notification/DHL
    Authorization: <as received from DHL>
    Content-Type: application/json

    <body as received from DHL>

Example (DPD push):

    GET /api/push/notification/DPD?pushid=...&pnr=...&status=...&statusdate=ddMMyyyyHHmmss

Rules:

- The body must be valid JSON for DHL and GLS, otherwise the answer is `400`.
- The `Authorization` and `DHL-API-Hook-Secret` headers must not be added, dropped or changed. Pushes with wrong or missing credentials are rejected.
- The carrier is answered only after the API has answered, unless the push is acknowledged immediately and forwarded in the background (see the implementation notes in section 3). Failures are passed on so that the carrier can retry.

## 3. Forwarding requirements

Calling the endpoint with an HTTP client creates a new request. Only what is copied explicitly is carried along.

| Topic | Requirement |
| --- | --- |
| Method | The method of the received push is used. A received GET is forwarded as GET. |
| Body | Forwarded as raw bytes. Deserializing and re-serializing can change the formatting or the field order, or drop unknown fields. |
| Headers | `Authorization`, `DHL-API-Hook-Secret` and `Content-Type` are copied as received, because the API checks them. `Host`, `Content-Length` and hop-by-hop headers (`Connection`, `Transfer-Encoding`) are not copied. |
| Query string | Forwarded exactly as received (for example `req.QueryString.Value`). It is already encoded and is not encoded again. Required for DPD, whose data is in the query string. Rebuilding it manually can merge repeated keys. |
| Caller identity | The API sees the address of the receiving system. The address of the original sender can be passed in `X-Forwarded-For`. |

Implementation notes:

- The request body can normally be read only once. `EnableBuffering()` is needed if the body is read before it is forwarded.
- A reverse proxy library such as YARP handles these details when forwarding is the main purpose of the service.
- If a carrier expects a fast response, the push can be acknowledged immediately and forwarded in the background. The `401` and `203` answers of the API then cannot be passed on to the carrier, so failures have to be logged and retried by the receiving system.

## 4. What to expect

### Status codes

| Code | Meaning | Action |
| --- | --- | --- |
| `200` | Push processed. | None. |
| `203` | Push received, but it could not be processed (`status` is `Failed`, see `message`). | Log it. Retrying the same push does not help. |
| `400` | Body is not valid JSON, or the provider request failed. | Verify that the body is forwarded unchanged. |
| `401` | Credentials missing or wrong. For DHL `subscription.validate`, the `DHL-API-Hook-Secret` header does not match the secret in the body. | Verify that the headers are forwarded as received. |
| `422` | Invalid argument, for example a DHL validation push without a valid `self` link. | Log it and contact the API maintainers. |
| `500` | Unexpected error in the API. | Retry later. |

### Response body (`200` and `203`)

    {
      "status": "Success",
      "message": null,
      "trackingEvent": [
        {
          "parcelNumber": "...",
          "status": "...",
          "statusMessage": "...",
          "estimatedDeliveryDate": "...",
          "isReturn": "IsNotReturn",
          "apiValue": "<raw carrier data as JSON text>",
          "createdAt": "2026-11-09T08:30:00Z"
        }
      ],
      "subscription": null
    }

- `status` is `Success`, `Failed` or `Unknown`.
- `trackingEvent` has one entry per parcel. A DHL push with several shipments gives several entries. GLS and DPD give one.
- `status` and `statusMessage` of an event are uniform values, the same for every carrier.
- `isReturn` is `IsReturn`, `IsNotReturn` or `Unknown`. GLS and DPD always give `Unknown`, because they do not report returns in the push.
- `apiValue` holds the raw carrier data for further detail.

### DHL handshake pushes

When a parcel is subscribed, DHL sends two pushes that carry no tracking data. Both are forwarded:

| Push | Processing | Response |
| --- | --- | --- |
| `subscription.validate` | The `DHL-API-Hook-Secret` header is compared with the `secret` in the body, then the subscription is activated at DHL. | `200`, `trackingEvent` empty, `subscription` with `scope`, `subscriptionId`, `expires`. |
| `subscription.ready` | Acknowledged only. | `200`, same shape as above. |

The secret of the validate push is only valid until its `expires` date. If the push is not forwarded in time, the subscription stays inactive and the parcel has to be subscribed again.

## 5. Checklist

- [ ] The webhook forwards GET and POST for `DHL`, `GLS` and `DPD` to `/api/push/notification/{type}`.
- [ ] Method, headers, query string and body are forwarded unchanged, as described in section 3.
- [ ] The webhook URL is registered as the DHL `HookUri` and the GLS hook URI (it must be an absolute `https` URL).
- [ ] DPD pushes are set up on the DPD side and point to the webhook.
- [ ] The status codes `401`, `203` and `5xx` are handled as described above.
