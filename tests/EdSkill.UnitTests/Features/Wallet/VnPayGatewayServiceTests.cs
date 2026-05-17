using EdSkill.Application.Common.Models;
using EdSkill.Infrastructure.Services;
using EdSkill.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace EdSkill.UnitTests.Features.Wallet;

public class VnPayGatewayServiceTests
{
    [Fact]
    public void CreatePaymentUrl_WhenPointPurchase_UsesPointReturnUrlAndSanitizesOrderInfo()
    {
        var settings = CreateSettings();
        var service = new VnPayGatewayService(Options.Create(settings), NullLogger<VnPayGatewayService>.Instance);

        var result = service.CreatePaymentUrl(new VnPayCreatePaymentRequest(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.NewGuid(),
            59000,
            "Nap diem Gói 1!!!",
            new DateTime(2026, 5, 18, 2, 0, 0, DateTimeKind.Utc),
            VnPayPaymentPurpose.PointPurchase,
            "203.0.113.10"));

        result.IsSuccess.Should().BeTrue();
        var query = ParseQuery(result.Value!.PaymentUrl);
        query["vnp_ReturnUrl"].Should().Be(settings.PointPurchaseReturnUrl);
        query.Should().NotContainKey("vnp_IpnUrl");
        query["vnp_IpAddr"].Should().Be("203.0.113.10");
        query["vnp_OrderInfo"].Should().Be("Nap diem Goi 1");
        query["vnp_CreateDate"].Should().Be("20260518090000");
        query["vnp_SecureHash"].Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreatePaymentUrl_WhenSubscriptionPurchase_UsesSubscriptionReturnUrl()
    {
        var settings = CreateSettings();
        var service = new VnPayGatewayService(Options.Create(settings), NullLogger<VnPayGatewayService>.Instance);

        var result = service.CreatePaymentUrl(new VnPayCreatePaymentRequest(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.NewGuid(),
            79000,
            "Mua goi Companion Pro",
            new DateTime(2026, 5, 18, 2, 0, 0, DateTimeKind.Utc),
            VnPayPaymentPurpose.SubscriptionPurchase,
            "198.51.100.25"));

        result.IsSuccess.Should().BeTrue();
        var query = ParseQuery(result.Value!.PaymentUrl);
        query["vnp_ReturnUrl"].Should().Be(settings.SubscriptionPurchaseReturnUrl);
        query.Should().NotContainKey("vnp_IpnUrl");
        query["vnp_IpAddr"].Should().Be("198.51.100.25");
        query["vnp_OrderInfo"].Should().Be("Mua goi Companion Pro");
    }

    private static VnPaySettings CreateSettings()
    {
        return new VnPaySettings
        {
            TerminalCode = "GNRCGAVJ",
            HashSecret = "D1XBVC43RTEDQKZ63AJUFLC5D45I0QY3",
            BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
            PointPurchaseReturnUrl = "https://edskill.vercel.app/dashboard/wallet/points/return",
            PointPurchaseIpnUrl = "https://edskill-production.up.railway.app/api/wallet/points/purchase/vnpay-ipn",
            SubscriptionPurchaseReturnUrl = "https://edskill.vercel.app/dashboard/wallet/subscriptions/return",
            SubscriptionPurchaseIpnUrl = "https://edskill-production.up.railway.app/api/wallet/subscriptions/purchase/vnpay-ipn",
            ExpireMinutes = 15
        };
    }

    private static Dictionary<string, string> ParseQuery(string paymentUrl)
    {
        var uri = new Uri(paymentUrl);
        return uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                part => WebUtility.UrlDecode(part[0]),
                part => part.Length > 1 ? WebUtility.UrlDecode(part[1]) : string.Empty,
                StringComparer.Ordinal);
    }
}
