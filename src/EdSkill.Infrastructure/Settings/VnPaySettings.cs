namespace EdSkill.Infrastructure.Settings;

public class VnPaySettings
{
    public const string SectionName = "VnPaySettings";

    public string TerminalCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string PointPurchaseReturnUrl { get; set; } = string.Empty;
    public string PointPurchaseIpnUrl { get; set; } = string.Empty;
    public string SubscriptionPurchaseReturnUrl { get; set; } = string.Empty;
    public string SubscriptionPurchaseIpnUrl { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 15;
}
