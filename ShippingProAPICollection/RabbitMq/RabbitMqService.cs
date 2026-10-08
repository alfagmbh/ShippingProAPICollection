using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using RabbitMQ.Client;
using ShippingProAPICollection._Provider.DHL;
using ShippingProAPICollection.Models;

namespace ShippingProAPICollection.RabbitMq
{
    internal class RabbitMqService
    {
        #region Fields

        private const string TargetQueue = "ALFATOOLS";
        private const string JobQueue = "SHIPPING_PRO_API";

        private string RabbitMqUser = "";
        private string RabbitMqPassword = "";
        private string RabbitMqHostname = "";
        private uint RabbitMqPort = 5672;
        private string RabbitMqVirtualHost = "";

        #endregion Fields

        public async Task AddTaskToQueueAsync(TrackingEvent eventData, CancellationToken cancellationToken)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = { new StringEnumConverter() }
            };

            string jsonMessage = JsonConvert.SerializeObject(eventData, settings);

            LocalRabbitMqEvent ev = new LocalRabbitMqEvent()
            {
                Body = jsonMessage,
            };

            await PublishLocalEventsAsync(ev, cancellationToken);
        }

        public async Task PublishLocalEventsAsync(LocalRabbitMqEvent localEvent, CancellationToken cancellationToken)
        {
            using var token = CreateLinkedTokenWithTimeout(TimeSpan.FromMinutes(1), cancellationToken);

            using (IConnection _connection = await BuildConnectionAsync(token.Token))
            {
                var option = new CreateChannelOptions(publisherConfirmationsEnabled: true,
                                     publisherConfirmationTrackingEnabled: true);

                using (IChannel _channel = await _connection.CreateChannelAsync(option, token.Token))
                {
                    PublishTracker tracker = new PublishTracker();

                    _channel.BasicNacksAsync += async (sender, e) =>
                    {
                        tracker.OnNack();
                        if (tracker._localRabbitEvent.TriesToPublish > 3)
                        {
                            throw new Exception("Unable to publish event after 3 attempts.");
                        }
                        await PublishEventAsync(_channel, tracker._localRabbitEvent, tracker, new RabbitPayloadCipher(), token.Token);
                    };

                    _channel.BasicAcksAsync += (sender, e) =>
                    {
                        tracker.OnAck();
                        return Task.CompletedTask;
                    };

                    RabbitPayloadCipher payloadCipher = new RabbitPayloadCipher();

                    await PublishEventAsync(_channel, localEvent, tracker, payloadCipher, token.Token);
                }
            }
        }

        private static string BuildQueueName(string q)
        {
#if RELEASE
            return q;
#else
            return "DEV_" + q;
#endif
        }

        public static LinkedTokenWithTimeout CreateLinkedTokenWithTimeout(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            if (timeSpan <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSpan), "Timeout must be greater than zero.");
            }

            return new LinkedTokenWithTimeout(timeSpan, cancellationToken);
        }

        private async Task PublishEventAsync(IChannel channel, LocalRabbitMqEvent rabbitEvent, PublishTracker tracker, RabbitPayloadCipher payloadCipher, CancellationToken cancellationToken)
        {
            var (bodyEncrypt, correlationId) = payloadCipher.Encrypt(rabbitEvent.Body);

            // Konvertiere die JSON-Nachricht in ein Byte-Array
            byte[] body = Encoding.UTF8.GetBytes(bodyEncrypt);

            var properties = new BasicProperties();

            properties.MessageId = rabbitEvent.Id;
            properties.CorrelationId = correlationId;
            tracker.Track(rabbitEvent);

            // Veröffentliche die Nachricht in die Queue

            await channel.BasicPublishAsync(exchange: "", routingKey: BuildQueueName(JobQueue), true, basicProperties: properties, body: body, cancellationToken);
        }

        private async Task<IConnection> BuildConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (
                    String.IsNullOrEmpty(RabbitMqUser) ||
                    String.IsNullOrEmpty(RabbitMqPassword) ||
                    String.IsNullOrEmpty(RabbitMqHostname) ||
                    RabbitMqPort == 0 ||
                    String.IsNullOrEmpty(RabbitMqVirtualHost)
                   )
                {
                    throw new("RabbitMQ Einstellungen nicht vollständig vorhanden. Connection kann nicht aufgebaut werden.");
                }

                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var connection = await new ConnectionFactory()
                {
                    Uri = new Uri($"amqps://{RabbitMqUser}:{RabbitMqPassword}@{RabbitMqHostname}:{RabbitMqPort}/{RabbitMqVirtualHost}"),
                    Ssl = new SslOption
                    {
                        Enabled = true,
                        Version = SslProtocols.Tls12,
                        ServerName = RabbitMqHostname,
                        AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors |
                                                 SslPolicyErrors.RemoteCertificateNameMismatch |
                                                 SslPolicyErrors.RemoteCertificateNotAvailable,
                    }
                }.CreateConnectionAsync(cancellationToken);
                return connection;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    #region Private Helper Types

    public class TrackingEvent
    {
        [JsonProperty("event_code")]
        public string EventType = "PARCEL_TRACKING_INFO";

        [JsonProperty("event_version")]
        public string EventVersion { get; set; } = "1.0";

        [JsonProperty("event")]
        public ParcelTrackingEvent Event { get; set; }

        public DateTime Created { get; set; }

        public async Task ExecuteTaskAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            string _version = "1.0";

            if (!EventVersion.Equals(_version)) throw new Exception("Model-Version wird aktuell nicht unterstützt");
        }
    }

    internal sealed class PublishTracker
    {
        public LocalRabbitMqEvent _localRabbitEvent { get; set; }

        public void Track(LocalRabbitMqEvent rabbitEvent)
        {
            _localRabbitEvent = rabbitEvent;
        }

        public void OnNack()
        {
            _localRabbitEvent.TriesToPublish++;
        }

        public void OnAck()
        {
            _localRabbitEvent.IsPublished = true;
        }
    }

    internal sealed class RabbitPayloadCipher
    {
        private readonly RabbitSecurity _security = new();

        public (string EncryptedBody, string CorrelationId) Encrypt(string plainBody)
        {
            byte[] privateKey = _security.GetRandomIV();
            string bodyEncrypt = _security.EncryptString(plainBody, privateKey);
            return (bodyEncrypt, Convert.ToBase64String(privateKey));
        }

        public string Decrypt(string encryptedBody, string correlationId)
        {
            byte[] publicKey = Convert.FromBase64String(correlationId);
            return _security.DecryptString(encryptedBody, publicKey);
        }
    }

    internal class LocalRabbitMqEvent
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Body { get; set; }
        public int TriesToPublish { get; set; } = 0;
        public bool IsPublished { get; set; } = false;
    }

    #endregion Private Helper Types

    #region RabbitSecurity

    internal class RabbitSecurity
    {
        #region Fields

        private const string privatePassword = "*8UG^x*F~J:ragYaYJJzr.E";
        private byte[] privateKey;

        #endregion Fields

        #region Constructor

        public RabbitSecurity()
        {
            SHA256 mySHA256 = SHA256.Create();
            privateKey = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(privatePassword));
        }

        #endregion Constructor

        #region Public API

        public string EncryptString(string plainText, byte[] publicKey)
        {
            // Instantiate a new Aes object to perform string symmetric encryption
            Aes encryptor = Aes.Create();

            encryptor.Mode = CipherMode.CBC;
            //encryptor.KeySize = 256;
            //encryptor.BlockSize = 128;
            //encryptor.Padding = PaddingMode.Zeros;

            // Set key and IV
            encryptor.Key = privateKey;
            encryptor.IV = publicKey;

            // Instantiate a new MemoryStream object to contain the encrypted bytes
            MemoryStream memoryStream = new MemoryStream();

            // Instantiate a new encryptor from our Aes object
            ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

            // Instantiate a new CryptoStream object to process the data and write it to the
            // memory stream
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

            // Convert the plainText string into a byte array
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Encrypt the input plaintext string
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);

            // Complete the encryption process
            cryptoStream.FlushFinalBlock();

            // Convert the encrypted data from a MemoryStream to a byte array
            byte[] cipherBytes = memoryStream.ToArray();

            // Close both the MemoryStream and the CryptoStream
            memoryStream.Close();
            cryptoStream.Close();

            // Convert the encrypted byte array to a base64 encoded string
            string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

            // Return the encrypted data as a string
            return cipherText;
        }

        public string DecryptString(string cipherText, byte[] publicKey)
        {
            // Instantiate a new Aes object to perform string symmetric encryption
            Aes encryptor = Aes.Create();

            encryptor.Mode = CipherMode.CBC;
            //encryptor.KeySize = 256;
            //encryptor.BlockSize = 128;
            //encryptor.Padding = PaddingMode.Zeros;

            // Set key and IV
            encryptor.Key = privateKey;
            encryptor.IV = publicKey;

            // Instantiate a new MemoryStream object to contain the encrypted bytes
            MemoryStream memoryStream = new MemoryStream();

            // Instantiate a new encryptor from our Aes object
            ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();

            // Instantiate a new CryptoStream object to process the data and write it to the
            // memory stream
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

            // Will contain decrypted plaintext
            string plainText = String.Empty;

            try
            {
                // Convert the ciphertext string into a byte array
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                // Decrypt the input ciphertext string
                cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

                // Complete the decryption process
                cryptoStream.FlushFinalBlock();

                // Convert the decrypted data from a MemoryStream to a byte array
                byte[] plainBytes = memoryStream.ToArray();

                // Convert the decrypted byte array to string
                plainText = Encoding.UTF8.GetString(plainBytes, 0, plainBytes.Length);
            }
            finally
            {
                // Close both the MemoryStream and the CryptoStream
                memoryStream.Close();
                cryptoStream.Close();
            }

            // Return the decrypted data as a string
            return plainText;
        }

        public byte[] GetRandomIV()
        {
            byte[] randomBytes = new byte[16];
            Random random = new Random();

            random.NextBytes(randomBytes);
            return randomBytes;
        }

        #endregion Public API
    }

    #endregion RabbitSecurity

    public sealed class LinkedTokenWithTimeout : IDisposable
    {
        #region Fields

        private readonly CancellationTokenSource _timeoutCts;
        private readonly CancellationTokenSource _linkedCts;
        private bool _disposed;

        #endregion Fields

        #region Constructor

        public LinkedTokenWithTimeout(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            _timeoutCts = new CancellationTokenSource(timeSpan);
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _timeoutCts.Token);
            Token = _linkedCts.Token;
        }

        #endregion Constructor

        #region Public API

        public CancellationToken Token { get; }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _linkedCts.Dispose();
            _timeoutCts.Dispose();
        }

        #endregion Public API
    }
}