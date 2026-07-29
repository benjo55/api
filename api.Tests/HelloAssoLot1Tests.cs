using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using api.Configuration;
using api.Interfaces;
using api.Models.Enum;
using api.Services.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace api.Tests;

public sealed class HelloAssoLot1Tests
{
    [Theory]
    [InlineData(50.00, 5000)]
    [InlineData(0.01, 1)]
    [InlineData(125.50, 12550)]
    public void Euro_to_cents_conversion_is_exact(decimal amount, int expected)
    {
        Assert.Equal(expected, HelloAssoAmountConverter.EuroToCents(amount));
    }

    [Fact]
    public void Euro_to_cents_rejects_more_than_two_decimals()
    {
        Assert.Throws<ArgumentException>(() => HelloAssoAmountConverter.EuroToCents(12.345m));
    }

    [Fact]
    public void Payment_status_mapper_maps_authorized()
    {
        Assert.Equal(PaymentStatus.Authorized, HelloAssoPaymentStatusMapper.Map("Authorized"));
        Assert.Equal(PaymentStatus.Pending, HelloAssoPaymentStatusMapper.Map("pending"));
        Assert.Equal(PaymentStatus.Unknown, HelloAssoPaymentStatusMapper.Map("not-known"));
    }

    [Fact]
    public void Signature_helpers_work_with_constant_time_compare()
    {
        const string payload = "{\"id\":\"abc\"}";
        const string key = "demo-key";

        var signature = HelloAssoSecurity.ComputeHmacSha256Base64(payload, key);
        Assert.True(HelloAssoSecurity.ConstantTimeEquals(signature, signature));
        Assert.False(HelloAssoSecurity.ConstantTimeEquals(signature, signature + "x"));
    }

    [Fact]
    public void Iban_validator_accepts_valid_iban_and_rejects_invalid_values()
    {
        var validator = new IbanValidator();

        Assert.True(validator.TryNormalizeIban("FR76 3000 6000 0112 3456 7890 189", out var normalized));
        Assert.Equal("FR7630006000011234567890189", normalized);
        Assert.True(validator.IsValidBic("AGRIFRPP"));

        Assert.False(validator.TryNormalizeIban("FR76 3000 6000 0112 3456 7890 180", out _));
        Assert.False(validator.IsValidBic("BAD"));
    }

    [Fact]
    public async Task Token_provider_caches_token_until_expiration()
    {
        var handler = new FakeHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"token-1\",\"expires_in\":3600}", Encoding.UTF8, "application/json"),
            });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.helloasso-sandbox.com") };
        var factory = new SingleClientFactory(client);
        var provider = new HelloAssoTokenProvider(
            factory,
            Options.Create(new HelloAssoOptions
            {
                Enabled = true,
                BaseUrl = "https://api.helloasso-sandbox.com",
                ClientId = "id",
                ClientSecret = "secret",
                OrganizationSlug = "org",
                RetryCount = 1,
            }),
            NullLogger<HelloAssoTokenProvider>.Instance);

        var token1 = await provider.GetAccessTokenAsync(CancellationToken.None);
        var token2 = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Equal("token-1", token1);
        Assert.Equal("token-1", token2);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Reconciliation_prefers_initial_amount_over_total_amount()
    {
        var handler = new FakeHttpHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/oauth2/token")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"token-1\",\"expires_in\":3600}", Encoding.UTF8, "application/json"),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "id": 245642,
                      "initialAmount": 50000,
                      "totalAmount": 52000,
                      "currency": "EUR",
                      "metadata": {
                        "donationId": "4",
                        "paymentAttemptId": "5"
                      },
                      "order": {
                        "id": 93353,
                        "payments": [
                          {
                            "id": 112233,
                            "state": "Authorized"
                          }
                        ]
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.helloasso-sandbox.com") };
        var factory = new SingleClientFactory(client);
        var provider = new HelloAssoPaymentProvider(
            factory,
            new HelloAssoTokenProvider(
                factory,
                Options.Create(new HelloAssoOptions
                {
                    Enabled = true,
                    BaseUrl = "https://api.helloasso-sandbox.com",
                    ClientId = "id",
                    ClientSecret = "secret",
                    OrganizationSlug = "acic-tests",
                    RetryCount = 1,
                }),
                NullLogger<HelloAssoTokenProvider>.Instance),
            Options.Create(new HelloAssoOptions
            {
                Enabled = true,
                BaseUrl = "https://api.helloasso-sandbox.com",
                RetryCount = 1,
            }),
            NullLogger<HelloAssoPaymentProvider>.Instance);

        var result = await provider.ReconcilePaymentAsync(
            new PaymentReconciliationCommand("acic-tests", "245642"),
            CancellationToken.None);

        Assert.True(result.Found);
        Assert.True(result.IsAuthorized);
        Assert.Equal(50000, result.AmountInCents);
        Assert.Equal("93353", result.ExternalOrderId);
        Assert.Equal("112233", result.ExternalPaymentId);
    }

    [Fact]
    public async Task Checkout_payload_sends_payer_birth_date()
    {
        string? checkoutPayload = null;
        var handler = new FakeHttpHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/oauth2/token")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"token-1\",\"expires_in\":3600}", Encoding.UTF8, "application/json"),
                };
            }

            var stream = request.Content?.ReadAsStream(CancellationToken.None);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                checkoutPayload = reader.ReadToEnd();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"checkout-1\",\"redirectUrl\":\"https://helloasso.example/checkout-1\"}", Encoding.UTF8, "application/json"),
            };
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.helloasso-sandbox.com") };
        var factory = new SingleClientFactory(client);
        var provider = new HelloAssoPaymentProvider(
            factory,
            new HelloAssoTokenProvider(
                factory,
                Options.Create(new HelloAssoOptions
                {
                    Enabled = true,
                    BaseUrl = "https://api.helloasso-sandbox.com",
                    ClientId = "id",
                    ClientSecret = "secret",
                    OrganizationSlug = "acic-tests",
                    RetryCount = 1,
                }),
                NullLogger<HelloAssoTokenProvider>.Instance),
            Options.Create(new HelloAssoOptions
            {
                Enabled = true,
                BaseUrl = "https://api.helloasso-sandbox.com",
                RetryCount = 1,
            }),
            NullLogger<HelloAssoPaymentProvider>.Instance);

        var result = await provider.CreateCheckoutAsync(
            new CreateCheckoutCommand(
                "acic-tests",
                5000,
                "Don INT-2026-000001",
                "https://life.example/return",
                "https://life.example/back",
                "https://life.example/error",
                "Alain",
                "VERSE",
                "alain@example.com",
                "5 Rue Truffaut",
                "75017",
                "Paris",
                "FR",
                new DateTime(1964, 9, 29),
                new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(checkoutPayload);
        using var document = JsonDocument.Parse(checkoutPayload!);
        Assert.Equal("1964-09-29", document.RootElement.GetProperty("payer").GetProperty("dateOfBirth").GetString());
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public SingleClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;
        public int CallCount { get; private set; }

        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount += 1;
            return Task.FromResult(_responseFactory(request));
        }
    }
}
