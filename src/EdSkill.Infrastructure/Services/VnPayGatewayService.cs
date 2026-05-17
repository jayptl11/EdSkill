using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Domain.Enums;
using EdSkill.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace EdSkill.Infrastructure.Services;

public class VnPayGatewayService : IVnPayGatewayService
{
    private const string Version = "2.1.0";
    private const string Command = "pay";
    private readonly VnPaySettings _settings;

    public VnPayGatewayService(IOptions<VnPaySettings> settings)
    {
        _settings = settings.Value;
    }

    public Result<VnPayCreatePaymentResult> CreatePaymentUrl(VnPayCreatePaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(_settings.TerminalCode)
            || string.IsNullOrWhiteSpace(_settings.HashSecret)
            || string.IsNullOrWhiteSpace(_settings.BaseUrl)
            || string.IsNullOrWhiteSpace(_settings.ReturnUrl)
            || string.IsNullOrWhiteSpace(_settings.IpnUrl))
        {
            return Result<VnPayCreatePaymentResult>.Failure("PAYMENT_PROVIDER_NOT_CONFIGURED", "VNPay settings are missing.");
        }

        var createTime = ConvertToVietnamTime(request.CreatedAtUtc);
        var expiresAt = createTime.AddMinutes(_settings.ExpireMinutes <= 0 ? 15 : _settings.ExpireMinutes);
        var transactionRef = request.PaymentTransactionId.ToString("N");

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = Version,
            ["vnp_Command"] = Command,
            ["vnp_TmnCode"] = _settings.TerminalCode,
            ["vnp_Amount"] = (request.AmountVnd * 100L).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = createTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = "127.0.0.1",
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = request.OrderDescription,
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_TxnRef"] = transactionRef,
            ["vnp_ExpireDate"] = expiresAt.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(_settings.IpnUrl))
        {
            parameters["vnp_IpnUrl"] = _settings.IpnUrl;
        }

        var query = BuildQueryString(parameters);
        var hash = ComputeHash(BuildHashData(parameters));
        var paymentUrl = $"{_settings.BaseUrl}?{query}&vnp_SecureHash={hash}";

        return Result<VnPayCreatePaymentResult>.Success(
            new VnPayCreatePaymentResult(paymentUrl, ConvertToUtc(expiresAt), transactionRef));
    }

    public Result<VnPayCallbackParseResult> ParseCallback(IReadOnlyDictionary<string, string> payload)
    {
        if (payload.Count == 0)
        {
            return Result<VnPayCallbackParseResult>.Failure("PAYMENT_CALLBACK_INVALID", "VNPay callback payload is empty.");
        }

        if (!payload.TryGetValue("vnp_SecureHash", out var secureHash) || string.IsNullOrWhiteSpace(secureHash))
        {
            return Result<VnPayCallbackParseResult>.Failure("PAYMENT_CALLBACK_INVALID", "VNPay secure hash is missing.");
        }

        var normalized = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in payload)
        {
            if (string.IsNullOrWhiteSpace(pair.Key)
                || pair.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            normalized[pair.Key] = pair.Value;
        }

        var computedHash = ComputeHash(BuildHashData(normalized));
        if (!string.Equals(computedHash, secureHash, StringComparison.OrdinalIgnoreCase))
        {
            return Result<VnPayCallbackParseResult>.Failure("PAYMENT_PROVIDER_INVALID_SIGNATURE", "VNPay callback signature is invalid.");
        }

        if (!normalized.TryGetValue("vnp_TxnRef", out var transactionRef)
            || !TryParseTransactionRef(transactionRef, out var paymentTransactionId))
        {
            return Result<VnPayCallbackParseResult>.Failure("PAYMENT_CALLBACK_INVALID", "VNPay transaction reference is invalid.");
        }

        if (!normalized.TryGetValue("vnp_Amount", out var amountRaw)
            || !long.TryParse(amountRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amountInMinorUnits)
            || amountInMinorUnits < 0)
        {
            return Result<VnPayCallbackParseResult>.Failure("PAYMENT_CALLBACK_INVALID", "VNPay amount is invalid.");
        }

        var paymentStatus = MapStatus(
            normalized.GetValueOrDefault("vnp_ResponseCode"),
            normalized.GetValueOrDefault("vnp_TransactionStatus"));

        var paidAt = TryParsePaymentDate(normalized.GetValueOrDefault("vnp_PayDate"));

        return Result<VnPayCallbackParseResult>.Success(
            new VnPayCallbackParseResult(
                paymentTransactionId,
                paymentStatus,
                normalized.GetValueOrDefault("vnp_TransactionNo"),
                (int)(amountInMinorUnits / 100L),
                paidAt,
                new Dictionary<string, string>(payload, StringComparer.Ordinal)));
    }

    private static PaymentStatus MapStatus(string? responseCode, string? transactionStatus)
    {
        if (string.Equals(responseCode, "00", StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(transactionStatus)
                || string.Equals(transactionStatus, "00", StringComparison.OrdinalIgnoreCase)))
        {
            return PaymentStatus.Success;
        }

        if (string.Equals(responseCode, "24", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentStatus.Cancelled;
        }

        return PaymentStatus.Failed;
    }

    private static DateTime? TryParsePaymentDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParseExact(
                value,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var paymentDate))
        {
            return null;
        }

        return ConvertToUtc(paymentDate);
    }

    private static bool TryParseTransactionRef(string transactionRef, out Guid paymentTransactionId)
    {
        return Guid.TryParseExact(transactionRef, "N", out paymentTransactionId)
            || Guid.TryParse(transactionRef, out paymentTransactionId);
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join("&", parameters.Select(pair => $"{pair.Key}={Encode(pair.Value)}"));
    }

    private static string BuildHashData(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join("&", parameters.Select(pair => $"{pair.Key}={Encode(pair.Value)}"));
    }

    private string ComputeHash(string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_settings.HashSecret);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Encode(string value)
    {
        return Uri.EscapeDataString(value ?? string.Empty);
    }

    private static DateTime ConvertToVietnamTime(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            return utc.AddHours(7);
        }
    }

    private static DateTime ConvertToUtc(DateTime localDateTime)
    {
        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.SpecifyKind(localDateTime.AddHours(-7), DateTimeKind.Utc);
        }
    }
}
