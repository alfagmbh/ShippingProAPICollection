using Newtonsoft.Json;
using ShippingProAPICollection.Provider.GLS.Entities;

namespace ShippingProAPICollection.NUnitTests
{
    /// <summary>
    /// Offline check of the GLS Track And Trace DTOs against the example payload of the official spec (v1.0.7, "200WithErrors").
    /// </summary>
    public class GLSParcelDetailsDeserializationTests
    {
        private const string SpecExample = """
            {
              "parcels": [
                { "requested": "42249018928", "errorCode": "E_404_01", "errorMessage": "Resource Not Found" },
                {
                  "requested": "36301917596",
                  "unitno": "36301917596",
                  "status": "PREADVICE",
                  "statusDateTime": "2024-10-11T15:24:57+0200",
                  "events": [
                    {
                      "code": "INTIAL.E48_DATA", "city": "", "postalCode": "", "country": "DE",
                      "description": "The parcel was provided by the sender for collection by GLS.",
                      "eventDateTime": "2024-10-11T15:24:52+0200"
                    },
                    {
                      "code": "INTIAL.PREADVICE", "city": "", "postalCode": "", "country": "DE",
                      "description": "The parcel data was entered into the GLS IT system; the parcel was not yet handed over to GLS.",
                      "eventDateTime": "2024-10-11T15:24:51+0200"
                    }
                  ]
                }
              ]
            }
            """;

        [Test]
        public void SpecExample_IsDeserialized()
        {
            var response = JsonConvert.DeserializeObject<GLSParcelsResponse>(SpecExample);

            Assert.That(response, Is.Not.Null);
            Assert.That(response!.Parcels, Has.Count.EqualTo(2));

            var failed = response.Parcels[0];
            var found = response.Parcels[1];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failed.Requested, Is.EqualTo("42249018928"));
                Assert.That(failed.ErrorCode, Is.EqualTo(GLSParcelErrorCode.E_404_01));
                Assert.That(failed.ErrorMessage, Is.EqualTo("Resource Not Found"));
                Assert.That(failed.Status, Is.Null);
                Assert.That(failed.Events, Is.Empty);

                Assert.That(found.UnitNo, Is.EqualTo("36301917596"));
                Assert.That(found.Status, Is.EqualTo(GLSParcelStatus.PREADVICE));
                Assert.That(found.StatusDateTime, Is.EqualTo(new DateTimeOffset(2024, 10, 11, 15, 24, 57, TimeSpan.FromHours(2))));
                Assert.That(found.Events, Has.Count.EqualTo(2));
                Assert.That(found.Events[0].Code, Is.EqualTo("INTIAL.E48_DATA"));
                Assert.That(found.Events[0].City, Is.Empty);
                Assert.That(found.Events[0].PostalCode, Is.Empty);
                Assert.That(found.Events[0].Country, Is.EqualTo("DE"));
                Assert.That(found.Events[0].EventDateTime, Is.EqualTo(new DateTimeOffset(2024, 10, 11, 15, 24, 52, TimeSpan.FromHours(2))));
            }
        }
    }
}
