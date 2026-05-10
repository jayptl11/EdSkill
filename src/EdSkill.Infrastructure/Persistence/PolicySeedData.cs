using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;

namespace EdSkill.Infrastructure.Persistence;

internal static class PolicySeedData
{
    private static readonly DateTime SeedTimestamp = new(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
    internal const string InitialVersion = "2026-05-10.v1";

    public static IReadOnlyCollection<PolicyDocument> Documents { get; } =
    [
        new PolicyDocument
        {
            PolicyDocumentId = Guid.Parse("70000000-0000-0000-0000-000000000001"),
            Slug = "terms",
            Category = "legal",
            Audience = "all",
            PolicyType = PolicyType.Terms,
            Version = InitialVersion,
            Title = "Điều khoản sử dụng nền tảng EdSkill",
            Summary = "Quy định quyền, nghĩa vụ và giới hạn trách nhiệm giữa EdSkill, Learner và Companion.",
            ContentMarkdown = """
# Điều khoản sử dụng EdSkill

EdSkill là nền tảng kết nối Learner và Companion để trao đổi kỹ năng theo hình thức 1-1 trong phạm vi Phase 1 MVP.

## Quy định chung

- Người dùng phải cung cấp thông tin đúng sự thật khi đăng ký và cập nhật hồ sơ.
- Learner không được tự đặt phiên với chính mình.
- Mọi hoạt động trên nền tảng phải tuân thủ quy định pháp luật, quy tắc cộng đồng và các chính sách hiện hành của EdSkill.
- EdSkill có quyền tạm khóa hoặc chấm dứt quyền truy cập khi phát hiện gian lận, lạm dụng hệ thống, quấy rối hoặc vi phạm chính sách.

## Trách nhiệm của người dùng

- Learner chịu trách nhiệm kiểm tra lịch học, số Points và thông tin phiên trước khi đặt lịch.
- Companion chịu trách nhiệm mô tả kỹ năng, nội dung hỗ trợ và xác nhận hoặc từ chối phiên đúng luồng hệ thống.
- Cả hai bên phải tham gia phiên với thái độ chuyên nghiệp, tôn trọng và không đăng tải nội dung trái phép.

## Giới hạn trách nhiệm

- EdSkill cung cấp nền tảng công nghệ và không cam kết kết quả học tập cụ thể.
- Tranh chấp phát sinh từ hành vi người dùng có thể bị xử lý theo cơ chế hỗ trợ, hoàn điểm hoặc khóa tài khoản tùy mức độ.
""",
            RequiresAcceptance = true,
            IsActive = true,
            EffectiveAt = SeedTimestamp,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        },
        new PolicyDocument
        {
            PolicyDocumentId = Guid.Parse("70000000-0000-0000-0000-000000000002"),
            Slug = "privacy",
            Category = "privacy",
            Audience = "all",
            PolicyType = PolicyType.Privacy,
            Version = InitialVersion,
            Title = "Chính sách riêng tư dữ liệu cá nhân",
            Summary = "Mô tả cách EdSkill thu thập, lưu trữ và xử lý dữ liệu tài khoản, phiên học và giao dịch.",
            ContentMarkdown = """
# Chính sách riêng tư

EdSkill thu thập dữ liệu cần thiết để vận hành đăng ký, hồ sơ người dùng, phiên học, lịch sử Points/Tokens và thông báo hệ thống.

## Dữ liệu được xử lý

- Thông tin tài khoản: email, username, họ tên, vai trò.
- Thông tin hồ sơ: bio, kỹ năng, trường, khoa, ảnh đại diện và dữ liệu người dùng tự cập nhật.
- Dữ liệu vận hành: lịch sử phiên, trạng thái tham gia, giao dịch Points/Tokens, nhật ký đồng ý chính sách.

## Mục đích sử dụng

- Xác thực tài khoản và bảo vệ an toàn hệ thống.
- Vận hành tính năng đặt lịch, thông báo, hỗ trợ tranh chấp và báo cáo quản trị.
- Cải thiện chất lượng dịch vụ trong phạm vi cho phép của chính sách này.

## Cam kết

- EdSkill không bán dữ liệu cá nhân của người dùng.
- Chỉ nhân sự hoặc hệ thống được ủy quyền mới được truy cập dữ liệu phục vụ vận hành, hỗ trợ hoặc tuân thủ pháp lý.
- Dữ liệu được lưu trong thời hạn phù hợp với nhu cầu vận hành, bảo mật và xử lý khiếu nại.
""",
            RequiresAcceptance = true,
            IsActive = true,
            EffectiveAt = SeedTimestamp,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        },
        new PolicyDocument
        {
            PolicyDocumentId = Guid.Parse("70000000-0000-0000-0000-000000000003"),
            Slug = "points-tokens",
            Category = "wallet",
            Audience = "all",
            PolicyType = PolicyType.PointsTokens,
            Version = InitialVersion,
            Title = "Chính sách Points và Tokens",
            Summary = "Làm rõ Points là đơn vị nội bộ để thanh toán phiên, Tokens là loyalty reward; không phải crypto và không quy đổi tiền mặt.",
            ContentMarkdown = """
# Chính sách Points và Tokens

## Định nghĩa

- Points là đơn vị thanh toán nội bộ của EdSkill để Learner đặt phiên với Companion.
- Tokens là điểm thưởng loyalty để ghi nhận hoạt động tích cực, không dùng thanh toán trực tiếp cho phiên học.

## Cam kết bắt buộc

- Points và Tokens không phải tiền điện tử, tài sản ảo hoặc sản phẩm đầu tư.
- Points và Tokens không được quy đổi thành tiền mặt, không được rút ra ngân hàng và không được giao dịch ngoài nền tảng.
- Mọi thay đổi số dư Points hoặc Tokens đều phải đi qua giao dịch hệ thống và được ghi log.

## Quy tắc sử dụng

- Learner chỉ được đặt phiên khi có đủ Points theo chi phí phiên.
- Companion nhận Points theo cơ chế giải ngân hợp lệ của hệ thống.
- EdSkill có quyền điều chỉnh, hoàn hoặc khóa giao dịch khi phát hiện lỗi hệ thống, gian lận hoặc tranh chấp hợp lệ.
""",
            RequiresAcceptance = true,
            IsActive = true,
            EffectiveAt = SeedTimestamp,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        },
        new PolicyDocument
        {
            PolicyDocumentId = Guid.Parse("70000000-0000-0000-0000-000000000004"),
            Slug = "cancellation-refund",
            Category = "sessions",
            Audience = "all",
            PolicyType = null,
            Version = InitialVersion,
            Title = "Chính sách hủy phiên, hoàn điểm và no-show",
            Summary = "Quy định hủy sớm, hủy muộn, hoàn Points, no-show và hướng xử lý dispute trong MVP.",
            ContentMarkdown = """
# Chính sách hủy phiên, hoàn điểm và no-show

## Hủy phiên

- Learner được hủy không mất Points khi phiên đang ở trạng thái hợp lệ và thời điểm hủy sớm hơn ít nhất 2 giờ trước giờ bắt đầu.
- Nếu Learner hủy muộn sau mốc 2 giờ, hệ thống mặc định không hoàn Points cho Learner.

## Phân bổ khi hủy muộn

- Tỷ lệ mặc định của nền tảng là Companion nhận 70% và EdSkill ghi nhận 30% khi hủy muộn thuộc trường hợp áp dụng.
- Cách xử lý cuối cùng vẫn phụ thuộc trạng thái phiên, log tham gia và quy trình dispute của hệ thống.

## No-show và tranh chấp

- No-show hoặc lỗi phát sinh trong phiên có thể được admin xem xét theo hồ sơ tham gia và log hệ thống.
- Kết quả xử lý có thể là hoàn Points cho Learner, thanh toán cho Companion, chia một phần hoặc không hành động tùy case.
""",
            RequiresAcceptance = false,
            IsActive = true,
            EffectiveAt = SeedTimestamp,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        },
        new PolicyDocument
        {
            PolicyDocumentId = Guid.Parse("70000000-0000-0000-0000-000000000005"),
            Slug = "community-guidelines-learner",
            Category = "community",
            Audience = "learner",
            PolicyType = PolicyType.CommunityGuidelines,
            Version = InitialVersion,
            Title = "Community Guidelines cho Learner",
            Summary = "Quy tắc ứng xử ngắn gọn dành cho Learner trong profile, chat và phiên học.",
            ContentMarkdown = """
# Community Guidelines cho Learner

- Tôn trọng thời gian, kỹ năng và công sức của Companion.
- Mô tả nhu cầu học rõ ràng, không spam, không quấy rối và không yêu cầu nội dung trái pháp luật.
- Tham gia phiên đúng giờ, xác nhận hoàn thành trung thực và gửi review có trách nhiệm.
- Không chia sẻ tài khoản, không lạm dụng chính sách hoàn điểm và không dùng Points/Tokens sai mục đích.
""",
            RequiresAcceptance = false,
            IsActive = true,
            EffectiveAt = SeedTimestamp,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        },
        new PolicyDocument
        {
            PolicyDocumentId = Guid.Parse("70000000-0000-0000-0000-000000000006"),
            Slug = "community-guidelines-companion",
            Category = "community",
            Audience = "companion",
            PolicyType = PolicyType.CommunityGuidelines,
            Version = InitialVersion,
            Title = "Community Guidelines cho Companion",
            Summary = "Quy tắc ứng xử ngắn gọn dành cho Companion khi tạo hồ sơ, nhận phiên và hỗ trợ Learner.",
            ContentMarkdown = """
# Community Guidelines cho Companion

- Cung cấp mô tả kỹ năng, phạm vi hỗ trợ và lịch rảnh trung thực.
- Xác nhận hoặc từ chối phiên đúng thời gian, không dụ giao dịch ngoài nền tảng.
- Tôn trọng Learner, không phân biệt đối xử, không lạm dụng quyền từ chối hoặc xác nhận hoàn thành sai sự thật.
- Bảo mật dữ liệu phiên học và không sử dụng thông tin của Learner ngoài mục đích hỗ trợ trên EdSkill.
""",
            RequiresAcceptance = false,
            IsActive = true,
            EffectiveAt = SeedTimestamp,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        }
    ];
}
