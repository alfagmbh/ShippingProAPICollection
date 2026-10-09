using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using ShippingProAPICollection.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace ShippingProAPICollection.RabbitMq
{
    public class RabbitMqConnector(IOptions<RabbitMqSecrets> options, CancellationToken cancellationToken) : IDisposable
    {
        private readonly LinkedTokenWithTimeout _timeoutToken = new LinkedTokenWithTimeout(TimeSpan.FromMinutes(1), cancellationToken);
        private readonly RabbitMqSecrets _secrets = options.Value;

        #region Public Methods

        public async Task AddTaskToQueueAsync(TrackingEvent eventData, CancellationToken cancellationToken)
        {
            LocalRabbitMqEvent ev = new LocalRabbitMqEvent()
            {
                Body = JsonConvert.SerializeObject(eventData, new JsonSerializerSettings
                {
                    Converters = { new StringEnumConverter() }
                })
            };

            try
            {
                await PublishLocalEventsAsync(ev, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new RabbitMqException("Error publishing event", ex);
            }
        }

        public void Dispose()
        {
            _timeoutToken?.Dispose();
        }

        #endregion Public Methods

        #region Private Methods

        private async Task PublishLocalEventsAsync(LocalRabbitMqEvent localEvent, CancellationToken cancellationToken)
        {
            const int maxAttempts = 3;

            var ct = _timeoutToken.Token;

            // Idealerweise eine langlebige, injizierte Connection statt BuildConnectionAsync pro Aufruf
            await using IConnection connection = await BuildConnectionAsync(ct);

            var options = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);

            await using IChannel channel = await connection.CreateChannelAsync(options, ct);

            // Einmalig verschlüsseln, damit CorrelationId über Retries stabil bleibt
            var (encrypted, correlationId) = new RabbitPayloadCipher(_secrets.PrivatePassword).Encrypt(localEvent.Body);
            byte[] body = Encoding.UTF8.GetBytes(encrypted);

            var properties = new BasicProperties
            {
                MessageId = localEvent.Id,
                CorrelationId = correlationId,
                Persistent = true
            };

            string routingKey = BuildQueueName(_secrets.JobQueue);

            while (true)
            {
                try
                {
                    // Wartet bei aktiviertem Tracking automatisch auf das Broker-Ack
                    await channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: routingKey,
                        mandatory: true,
                        basicProperties: properties,
                        body: body,
                        cancellationToken: ct);

                    localEvent.IsPublished = true;
                    return;
                }
                catch (PublishException ex) when (ex.IsReturn)
                {
                    // Nachricht war nicht routbar (z. B. Queue existiert nicht) – Retry bringt nichts
                    throw new InvalidOperationException(
                        $"Event {localEvent.Id} konnte nicht geroutet werden (Queue '{routingKey}').", ex);
                }
                catch (PublishException ex)
                {
                    localEvent.TriesToPublish++;
                    if (localEvent.TriesToPublish >= maxAttempts)
                    {
                        throw new InvalidOperationException(
                            $"Event {localEvent.Id} nach {maxAttempts} Versuchen nicht veröffentlicht.", ex);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(200 * localEvent.TriesToPublish), ct);
                }
            }
        }

        private string BuildQueueName(string q)
        {
#if RELEASE
            return q;
#else
            return "DEV_" + q;
#endif
        }

        public LinkedTokenWithTimeout CreateLinkedTokenWithTimeout(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            if (timeSpan <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSpan), "Timeout must be greater than zero.");
            }

            return new LinkedTokenWithTimeout(timeSpan, cancellationToken);
        }

        private async Task<IConnection> BuildConnectionAsync(CancellationToken cancellationToken)
        {
            if (
                String.IsNullOrEmpty(_secrets.RabbitMqUser) ||
                String.IsNullOrEmpty(_secrets.RabbitMqPassword) ||
                String.IsNullOrEmpty(_secrets.RabbitMqHostname) ||
                _secrets.RabbitMqPort == 0 ||
                String.IsNullOrEmpty(_secrets.RabbitMqVirtualHost)
               )
            {
                throw new("RabbitMQ Einstellungen nicht vollständig vorhanden. Connection kann nicht aufgebaut werden.");
            }

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var connection = await new ConnectionFactory()
            {
                Uri = new Uri($"amqps://{_secrets.RabbitMqUser}:{_secrets.RabbitMqPassword}@{_secrets.RabbitMqHostname}:{_secrets.RabbitMqPort}/{_secrets.RabbitMqVirtualHost}"),
                Ssl = new SslOption
                {
                    Enabled = true,
                    Version = SslProtocols.Tls12,
                    ServerName = _secrets.RabbitMqHostname,
                    AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors |
                                             SslPolicyErrors.RemoteCertificateNameMismatch |
                                             SslPolicyErrors.RemoteCertificateNotAvailable,
                }
            }.CreateConnectionAsync(cancellationToken);
            return connection;
        }

        #endregion Private Methods
    }

    #region Private Helper Types

    public sealed class TrackingEvent
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

    internal sealed class RabbitPayloadCipher(string privatePassword)
    {
        private readonly RabbitSecurity _security = new(privatePassword);

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

    internal sealed class LocalRabbitMqEvent
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Body { get; set; }
        public int TriesToPublish { get; set; } = 0;
        public bool IsPublished { get; set; } = false;
    }

    #endregion Private Helper Types

    #region RabbitSecurity

    public sealed class RabbitSecurity
    {
        #region Fields

        //private const string privatePassword = "*8UG^x*F~J:ragYaYJJzr.E";
        private byte[] privateKey;

        #endregion Fields

        #region Constructor

        public RabbitSecurity(string privatePassword)
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

    public sealed class RabbitMqSecrets
    {
        public const string SectionName = "RabbitMq";
        [Required] public string TargetQueue { get; set; } = null!;
        [Required] public string JobQueue { get; set; } = null!;
        [Required] public string RabbitMqUser { get; set; } = null!;
        [Required] public string RabbitMqPassword { get; set; } = null!;
        [Required] public string RabbitMqHostname { get; set; } = null!;
        [Range(1, 65535)] public uint RabbitMqPort { get; set; }
        [Required] public string RabbitMqVirtualHost { get; set; } = null!;
        [Required] public string PrivatePassword { get; set; } = null!;
    }

    #endregion RabbitSecurity

    #region Lifetime Management

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

    public class RabbitMqException : Exception
    {
        public RabbitMqException(string message) : base(message)
        {
        }

        public RabbitMqException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    #endregion Lifetime Management
}