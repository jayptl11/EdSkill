-- ============================================================
-- EdSkill demo seed: Companion Search Class Cards
-- Target: 10 public Companion profiles, 10 available online offers each.
-- Notes:
-- - All timestamps are stored in UTC.
-- - Vietnamese strings must stay Unicode: keep this file UTF-8 and use N'...'.
-- - This script only deletes/recreates data under @DemoEmailDomain.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @DemoEmailDomain nvarchar(100) = N'@demo.edskill.local';
    DECLARE @SeedCreatedAtUtc datetime2(7) = '2026-05-30T02:00:00';
    DECLARE @FirstClassDateLocal date = '2026-06-02';
    DECLARE @OfferCreatedStepMinutes int = 12;
    DECLARE @DemoPasswordHash nvarchar(255) = N'$2b$12$.GWoOeLcyLsrJUJQk8K4LuNsBj.pOLo7Gg8fzsNEOMfOhTNV7RNSm'; -- password: 123

    DECLARE @DemoUsers TABLE
    (
        Ordinal int NOT NULL PRIMARY KEY,
        UserId uniqueidentifier NOT NULL,
        ProfileId uniqueidentifier NOT NULL,
        WalletId uniqueidentifier NOT NULL,
        Username nvarchar(100) NOT NULL,
        Email nvarchar(256) NOT NULL,
        FirstName nvarchar(100) NOT NULL,
        LastName nvarchar(100) NOT NULL,
        DisplayName nvarchar(50) NOT NULL,
        Bio nvarchar(500) NOT NULL,
        AvatarUrl nvarchar(2048) NOT NULL,
        Phone nvarchar(50) NOT NULL,
        Gender nvarchar(32) NOT NULL,
        DateOfBirth date NOT NULL,
        Address nvarchar(500) NOT NULL,
        SkillsToTeach nvarchar(max) NOT NULL,
        CredentialUrls nvarchar(max) NOT NULL,
        ReputationScore float NOT NULL,
        TotalSessions int NOT NULL,
        LastActiveAt datetime2(7) NOT NULL
    );

    INSERT INTO @DemoUsers
        (Ordinal, UserId, ProfileId, WalletId, Username, Email, FirstName, LastName, DisplayName, Bio, AvatarUrl, Phone, Gender, DateOfBirth, Address, SkillsToTeach, CredentialUrls, ReputationScore, TotalSessions, LastActiveAt)
    VALUES
        (1,  'd1000000-0000-0000-0000-000000000001', 'd2000000-0000-0000-0000-000000000001', 'd3000000-0000-0000-0000-000000000001', N'companion_minh_anh',  N'minh.anh@demo.edskill.local',  N'Minh Anh', N'Nguyễn', N'Nguyễn Minh Anh', N'Companion chuyên tiếng Anh giao tiếp và IELTS Speaking cho sinh viên năm nhất. Mình tập trung sửa phát âm, tăng phản xạ và giúp bạn nói tự nhiên hơn trong tình huống học tập, phỏng vấn.', N'https://cdn.edskill.test/avatars/demo/minh-anh.png',  N'0901000001', N'Female', '1999-04-12', N'Quận Cầu Giấy, Hà Nội',       N'["Tiếng Anh giao tiếp","IELTS Speaking","CV & Phỏng vấn","Thuyết trình","PowerPoint"]', N'["https://cdn.edskill.test/credentials/minh-anh-ielts.pdf","https://cdn.edskill.test/credentials/minh-anh-speaking.pdf"]', 4.86, 128, '2026-05-29T14:20:00'),
        (2,  'd1000000-0000-0000-0000-000000000002', 'd2000000-0000-0000-0000-000000000002', 'd3000000-0000-0000-0000-000000000002', N'companion_hoang_nam', N'hoang.nam@demo.edskill.local', N'Hoàng Nam', N'Trần',  N'Trần Hoàng Nam',  N'Frontend developer tại một startup giáo dục, thường hướng dẫn React, JavaScript và tư duy chia component. Mình ưu tiên bài tập nhỏ, review code trực tiếp và giải thích bằng ví dụ dễ hiểu.', N'https://cdn.edskill.test/avatars/demo/hoang-nam.png', N'0901000002', N'Male',   '1998-09-03', N'Thành phố Thủ Đức, TP. Hồ Chí Minh', N'["React","JavaScript","Python","SQL","PowerPoint"]', N'["https://cdn.edskill.test/credentials/hoang-nam-frontend.pdf","https://cdn.edskill.test/credentials/hoang-nam-react.pdf"]', 4.79, 96,  '2026-05-29T13:55:00'),
        (3,  'd1000000-0000-0000-0000-000000000003', 'd2000000-0000-0000-0000-000000000003', 'd3000000-0000-0000-0000-000000000003', N'companion_thao_vy',   N'thao.vy@demo.edskill.local',   N'Thảo Vy', N'Lê',     N'Lê Thảo Vy',     N'Mình hỗ trợ Canva, thiết kế slide và thuyết trình học thuật. Các buổi học thường đi từ bố cục, màu sắc, typography đến cách kể câu chuyện rõ ràng trên từng slide.', N'https://cdn.edskill.test/avatars/demo/thao-vy.png',   N'0901000003', N'Female', '2000-01-27', N'Quận Hải Châu, Đà Nẵng',       N'["Canva","PowerPoint","Thuyết trình","CV & Phỏng vấn","Tiếng Anh giao tiếp"]', N'["https://cdn.edskill.test/credentials/thao-vy-design.pdf","https://cdn.edskill.test/credentials/thao-vy-slide.pdf"]', 4.92, 142, '2026-05-29T15:10:00'),
        (4,  'd1000000-0000-0000-0000-000000000004', 'd2000000-0000-0000-0000-000000000004', 'd3000000-0000-0000-0000-000000000004', N'companion_gia_huy',   N'gia.huy@demo.edskill.local',   N'Gia Huy', N'Phạm',   N'Phạm Gia Huy',   N'Sinh viên năm cuối khoa khoa học dữ liệu, có kinh nghiệm kèm Python, SQL và phân tích dữ liệu cơ bản. Mình thích dạy theo dự án nhỏ để bạn hiểu cách áp dụng vào bài tập thật.', N'https://cdn.edskill.test/avatars/demo/gia-huy.png',   N'0901000004', N'Male',   '1999-12-18', N'Quận Bình Thạnh, TP. Hồ Chí Minh', N'["Python","SQL","Excel","React","JavaScript"]', N'["https://cdn.edskill.test/credentials/gia-huy-data.pdf","https://cdn.edskill.test/credentials/gia-huy-python.pdf"]', 4.74, 87,  '2026-05-29T12:30:00'),
        (5,  'd1000000-0000-0000-0000-000000000005', 'd2000000-0000-0000-0000-000000000005', 'd3000000-0000-0000-0000-000000000005', N'companion_khanh_linh',N'khanh.linh@demo.edskill.local',N'Khánh Linh', N'Võ', N'Võ Khánh Linh', N'Companion về Excel, báo cáo học tập và kỹ năng văn phòng. Mình giúp bạn xử lý bảng tính gọn hơn, biết dùng công thức đúng lúc và trình bày số liệu dễ hiểu.', N'https://cdn.edskill.test/avatars/demo/khanh-linh.png',N'0901000005', N'Female', '1997-06-09', N'Quận Ninh Kiều, Cần Thơ',      N'["Excel","SQL","PowerPoint","Canva","CV & Phỏng vấn"]', N'["https://cdn.edskill.test/credentials/khanh-linh-excel.pdf","https://cdn.edskill.test/credentials/khanh-linh-office.pdf"]', 4.81, 111, '2026-05-29T14:45:00'),
        (6,  'd1000000-0000-0000-0000-000000000006', 'd2000000-0000-0000-0000-000000000006', 'd3000000-0000-0000-0000-000000000006', N'companion_quoc_bao',  N'quoc.bao@demo.edskill.local',  N'Quốc Bảo', N'Đặng',  N'Đặng Quốc Bảo',  N'Mình đồng hành với các bạn chuẩn bị phỏng vấn thực tập, viết CV và luyện trình bày dự án cá nhân. Buổi học đi thẳng vào hồ sơ thật, câu hỏi thật và phản hồi cụ thể.', N'https://cdn.edskill.test/avatars/demo/quoc-bao.png',  N'0901000006', N'Male',   '1998-03-21', N'Quận Thanh Xuân, Hà Nội',      N'["CV & Phỏng vấn","Thuyết trình","Tiếng Anh giao tiếp","IELTS Speaking","PowerPoint"]', N'["https://cdn.edskill.test/credentials/quoc-bao-career.pdf","https://cdn.edskill.test/credentials/quoc-bao-interview.pdf"]', 4.88, 119, '2026-05-29T16:05:00'),
        (7,  'd1000000-0000-0000-0000-000000000007', 'd2000000-0000-0000-0000-000000000007', 'd3000000-0000-0000-0000-000000000007', N'companion_ngoc_han',  N'ngoc.han@demo.edskill.local',  N'Ngọc Hân', N'Bùi',   N'Bùi Ngọc Hân',  N'Companion về JavaScript, React và tư duy làm sản phẩm web. Mình thường dùng ví dụ từ giao diện thật để bạn hiểu state, props, API và cách debug lỗi phổ biến.', N'https://cdn.edskill.test/avatars/demo/ngoc-han.png',  N'0901000007', N'Female', '2001-02-14', N'Quận Sơn Trà, Đà Nẵng',        N'["JavaScript","React","SQL","Python","Canva"]', N'["https://cdn.edskill.test/credentials/ngoc-han-web.pdf","https://cdn.edskill.test/credentials/ngoc-han-javascript.pdf"]', 4.76, 78,  '2026-05-29T11:40:00'),
        (8,  'd1000000-0000-0000-0000-000000000008', 'd2000000-0000-0000-0000-000000000008', 'd3000000-0000-0000-0000-000000000008', N'companion_tuan_kiet', N'tuan.kiet@demo.edskill.local', N'Tuấn Kiệt', N'Hồ',   N'Hồ Tuấn Kiệt',  N'Mình hỗ trợ SQL, Excel và tư duy phân tích dữ liệu cho người mới bắt đầu. Mỗi buổi học có dữ liệu mẫu, câu hỏi thực tế và phần tổng kết để bạn tự luyện thêm.', N'https://cdn.edskill.test/avatars/demo/tuan-kiet.png', N'0901000008', N'Male',   '1997-11-30', N'Thành phố Huế',                 N'["SQL","Excel","Python","PowerPoint","Thuyết trình"]', N'["https://cdn.edskill.test/credentials/tuan-kiet-sql.pdf","https://cdn.edskill.test/credentials/tuan-kiet-analytics.pdf"]', 4.69, 83,  '2026-05-29T10:25:00'),
        (9,  'd1000000-0000-0000-0000-000000000009', 'd2000000-0000-0000-0000-000000000009', 'd3000000-0000-0000-0000-000000000009', N'companion_phuong_mai',N'phuong.mai@demo.edskill.local',N'Phương Mai', N'Ngô', N'Ngô Phương Mai', N'Companion luyện IELTS Speaking, tiếng Anh giao tiếp và thuyết trình song ngữ. Mình giúp bạn xây câu trả lời rõ ý, giảm ngập ngừng và tăng độ tự nhiên khi nói.', N'https://cdn.edskill.test/avatars/demo/phuong-mai.png',N'0901000009', N'Female', '1999-08-08', N'Quận 3, TP. Hồ Chí Minh',       N'["IELTS Speaking","Tiếng Anh giao tiếp","Thuyết trình","CV & Phỏng vấn","Canva"]', N'["https://cdn.edskill.test/credentials/phuong-mai-ielts.pdf","https://cdn.edskill.test/credentials/phuong-mai-speaking.pdf"]', 4.95, 156, '2026-05-29T15:35:00'),
        (10, 'd1000000-0000-0000-0000-000000000010', 'd2000000-0000-0000-0000-000000000010', 'd3000000-0000-0000-0000-000000000010', N'companion_anh_tu',    N'anh.tu@demo.edskill.local',    N'Anh Tú', N'Vũ',      N'Vũ Anh Tú',      N'Mình kèm Python, React và xây portfolio cá nhân cho sinh viên IT. Cách học là vừa làm vừa giải thích, tập trung vào lỗi thường gặp và cách tự tìm hướng xử lý.', N'https://cdn.edskill.test/avatars/demo/anh-tu.png',    N'0901000010', N'Male',   '1998-05-05', N'Quận Đống Đa, Hà Nội',        N'["Python","React","JavaScript","CV & Phỏng vấn","SQL"]', N'["https://cdn.edskill.test/credentials/anh-tu-fullstack.pdf","https://cdn.edskill.test/credentials/anh-tu-portfolio.pdf"]', 4.72, 91,  '2026-05-29T13:15:00');

    DECLARE @SkillSeed TABLE
    (
        SkillId uniqueidentifier NOT NULL,
        Name nvarchar(50) NOT NULL,
        Slug nvarchar(100) NOT NULL,
        Category nvarchar(100) NOT NULL,
        IconKey nvarchar(50) NOT NULL,
        BasePointCost int NOT NULL,
        Aliases nvarchar(max) NOT NULL
    );

    INSERT INTO @SkillSeed (SkillId, Name, Slug, Category, IconKey, BasePointCost, Aliases)
    VALUES
        ('d5000000-0000-0000-0000-000000000001', N'Tiếng Anh giao tiếp', N'tieng-anh-giao-tiep', N'Ngoại ngữ', N'language', 120, N'["English Communication","Giao tiếp tiếng Anh","Speaking cơ bản"]'),
        ('d5000000-0000-0000-0000-000000000002', N'IELTS Speaking',      N'ielts-speaking',      N'Ngoại ngữ', N'mic',      160, N'["IELTS","Speaking test","Luyện nói IELTS"]'),
        ('d5000000-0000-0000-0000-000000000003', N'React',              N'react',               N'Lập trình', N'react',    150, N'["ReactJS","Frontend","Component"]'),
        ('d5000000-0000-0000-0000-000000000004', N'JavaScript',         N'javascript',          N'Lập trình', N'code',     130, N'["JS","ECMAScript","Web căn bản"]'),
        ('d5000000-0000-0000-0000-000000000005', N'Python',             N'python',              N'Lập trình', N'python',   140, N'["Python cơ bản","Data Python","Automation"]'),
        ('d5000000-0000-0000-0000-000000000006', N'SQL',                N'sql',                 N'Dữ liệu',   N'database', 125, N'["Cơ sở dữ liệu","SQL Server","Truy vấn dữ liệu"]'),
        ('d5000000-0000-0000-0000-000000000007', N'Excel',              N'excel',               N'Công việc', N'sheet',    110, N'["Tin học văn phòng","Bảng tính","Pivot table"]'),
        ('d5000000-0000-0000-0000-000000000008', N'PowerPoint',         N'powerpoint',          N'Công việc', N'slides',   105, N'["Làm slide","Thuyết trình bằng slide","Presentation deck"]'),
        ('d5000000-0000-0000-0000-000000000009', N'Canva',              N'canva',               N'Thiết kế',  N'palette',  100, N'["Thiết kế nhanh","Poster","Social design"]'),
        ('d5000000-0000-0000-0000-000000000010', N'CV & Phỏng vấn',     N'cv-phong-van',        N'Nghề nghiệp', N'briefcase', 115, N'["CV","Interview","Phỏng vấn thực tập"]'),
        ('d5000000-0000-0000-0000-000000000011', N'Thuyết trình',       N'thuyet-trinh',        N'Kỹ năng mềm', N'presentation', 115, N'["Presentation","Public Speaking","Nói trước đám đông"]');

    DECLARE @DemoUserIds TABLE (UserId uniqueidentifier NOT NULL PRIMARY KEY);
    INSERT INTO @DemoUserIds (UserId)
    SELECT UserId
    FROM [Users]
    WHERE Email LIKE N'%' + @DemoEmailDomain;

    DECLARE @DemoSessionIds TABLE (SessionId uniqueidentifier NOT NULL PRIMARY KEY);
    INSERT INTO @DemoSessionIds (SessionId)
    SELECT session.SessionId
    FROM [Sessions] session
    WHERE EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = session.CompanionId)
       OR EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = session.LearnerId);

    DELETE review
    FROM [Reviews] review
    WHERE EXISTS (SELECT 1 FROM @DemoSessionIds session WHERE session.SessionId = review.SessionId)
       OR EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = review.ReviewerId)
       OR EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = review.RevieweeId);

    DELETE segment
    FROM [SessionPresenceSegments] segment
    WHERE EXISTS (SELECT 1 FROM @DemoSessionIds session WHERE session.SessionId = segment.SessionId)
       OR EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = segment.UserId);

    DELETE pointTransaction
    FROM [PointTransactions] pointTransaction
    WHERE EXISTS (SELECT 1 FROM @DemoSessionIds session WHERE session.SessionId = pointTransaction.SessionId)
       OR EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = pointTransaction.UserId);

    DELETE tokenTransaction
    FROM [TokenTransactions] tokenTransaction
    WHERE EXISTS (SELECT 1 FROM @DemoSessionIds session WHERE session.SessionId = tokenTransaction.SessionId)
       OR EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = tokenTransaction.UserId);

    DELETE subscription
    FROM [UserSubscriptions] subscription
    WHERE EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = subscription.UserId);

    DELETE payment
    FROM [PaymentTransactions] payment
    WHERE EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = payment.UserId);

    DELETE achievement
    FROM [UserAchievements] achievement
    WHERE EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = achievement.UserId);

    DELETE consent
    FROM [PolicyConsents] consent
    WHERE EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = consent.UserId);

    DELETE refreshToken
    FROM [RefreshTokens] refreshToken
    WHERE EXISTS (SELECT 1 FROM @DemoUserIds demo WHERE demo.UserId = refreshToken.UserId);

    DELETE r
    FROM [Reviews] r
    INNER JOIN [Sessions] s ON s.SessionId = r.SessionId
    INNER JOIN @DemoUserIds u ON u.UserId = s.CompanionId;

    DELETE s
    FROM [Sessions] s
    INNER JOIN @DemoUserIds u ON u.UserId = s.CompanionId;

    DELETE us
    FROM [UserSkills] us
    INNER JOIN @DemoUserIds u ON u.UserId = us.UserId;

    DELETE pw
    FROM [PointWallets] pw
    INNER JOIN @DemoUserIds u ON u.UserId = pw.UserId;

    DELETE up
    FROM [UserProfiles] up
    INNER JOIN @DemoUserIds u ON u.UserId = up.UserId;

    DELETE u
    FROM [Users] u
    INNER JOIN @DemoUserIds du ON du.UserId = u.UserId;

    DECLARE @ResolvedSkillSeed TABLE
    (
        SeedSlug nvarchar(100) NOT NULL PRIMARY KEY,
        ExistingSkillId uniqueidentifier NULL,
        MatchedBySlug bit NOT NULL DEFAULT 0
    );

    INSERT INTO @ResolvedSkillSeed (SeedSlug, ExistingSkillId, MatchedBySlug)
    SELECT seed.Slug, resolved.SkillId, COALESCE(resolved.MatchedBySlug, 0)
    FROM @SkillSeed seed
    OUTER APPLY
    (
        SELECT TOP (1)
            skill.SkillId,
            CAST(CASE WHEN LOWER(skill.Slug) = LOWER(seed.Slug) THEN 1 ELSE 0 END AS bit) AS MatchedBySlug
        FROM [Skills] skill
        WHERE LOWER(skill.Slug) = LOWER(seed.Slug)
           OR LOWER(skill.Name) = LOWER(seed.Name)
        ORDER BY
            CASE WHEN LOWER(skill.Slug) = LOWER(seed.Slug) THEN 0 ELSE 1 END,
            CASE WHEN skill.IsDeleted = 0 THEN 0 ELSE 1 END,
            skill.CreatedAt
    ) resolved;

    INSERT INTO [Skills] ([SkillId], [Name], [Slug], [Category], [IconKey], [BasePointCost], [Aliases], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
    SELECT
        seed.SkillId,
        seed.Name,
        seed.Slug,
        seed.Category,
        seed.IconKey,
        seed.BasePointCost,
        seed.Aliases,
        1,
        0,
        @SeedCreatedAtUtc,
        @SeedCreatedAtUtc
    FROM @SkillSeed seed
    INNER JOIN @ResolvedSkillSeed resolved ON resolved.SeedSlug = seed.Slug
    WHERE resolved.ExistingSkillId IS NULL;

    UPDATE skill
    SET
        Name = CASE WHEN resolved.MatchedBySlug = 1 THEN seed.Name ELSE skill.Name END,
        Category = CASE WHEN resolved.MatchedBySlug = 1 THEN seed.Category ELSE COALESCE(skill.Category, seed.Category) END,
        BasePointCost = seed.BasePointCost,
        IconKey = COALESCE(skill.IconKey, seed.IconKey),
        Aliases = CASE WHEN resolved.MatchedBySlug = 1 THEN seed.Aliases ELSE skill.Aliases END,
        IsActive = 1,
        IsDeleted = 0,
        UpdatedAt = @SeedCreatedAtUtc
    FROM [Skills] skill
    INNER JOIN @ResolvedSkillSeed resolved ON resolved.ExistingSkillId = skill.SkillId
    INNER JOIN @SkillSeed seed ON seed.Slug = resolved.SeedSlug
    WHERE skill.BasePointCost <= 0
       OR skill.IsActive = 0
       OR skill.IsDeleted = 1
       OR skill.IconKey IS NULL
       OR resolved.MatchedBySlug = 1;

    DECLARE @SkillMap TABLE
    (
        Slug nvarchar(100) NOT NULL PRIMARY KEY,
        SkillId uniqueidentifier NOT NULL,
        Name nvarchar(50) NOT NULL
    );

    INSERT INTO @SkillMap (Slug, SkillId, Name)
    SELECT seed.Slug, skill.SkillId, skill.Name
    FROM @SkillSeed seed
    INNER JOIN @ResolvedSkillSeed resolved ON resolved.SeedSlug = seed.Slug
    INNER JOIN [Skills] skill ON skill.SkillId = COALESCE(resolved.ExistingSkillId, seed.SkillId);

    INSERT INTO [Users] ([UserId], [Username], [Email], [PasswordHash], [FirstName], [LastName], [CreatedAt], [LastLogin], [Status], [Roles], [TokenBalance])
    SELECT
        UserId,
        Username,
        Email,
        @DemoPasswordHash,
        FirstName,
        LastName,
        DATEADD(day, -30 - Ordinal, @SeedCreatedAtUtc),
        LastActiveAt,
        N'active',
        N'["learner","companion"]',
        0
    FROM @DemoUsers;

    INSERT INTO [UserProfiles]
        ([ProfileId], [UserId], [DisplayName], [Bio], [AvatarUrl], [SkillsToTeach], [SkillsToLearn], [IsPublic], [ReputationScore], [TotalSessions], [LastActiveAt], [CreatedAt], [UpdatedAt], [DateOfBirth], [Phone], [Gender], [SocialLinkUrl], [DegreeUrl], [CredentialUrls], [Address])
    SELECT
        ProfileId,
        UserId,
        DisplayName,
        Bio,
        AvatarUrl,
        SkillsToTeach,
        N'["Quản lý thời gian","Giao tiếp nhóm"]',
        1,
        ReputationScore,
        TotalSessions,
        LastActiveAt,
        DATEADD(day, -30 - Ordinal, @SeedCreatedAtUtc),
        LastActiveAt,
        DateOfBirth,
        Phone,
        Gender,
        CONCAT(N'https://edskill.test/@', Username),
        CONCAT(N'https://cdn.edskill.test/degrees/demo/', Username, N'.pdf'),
        CredentialUrls,
        Address
    FROM @DemoUsers;

    INSERT INTO [PointWallets] ([PointWalletId], [UserId], [Balance], [HeldBalance], [TotalEarned], [TotalSpent], [CreatedAt], [UpdatedAt])
    SELECT
        WalletId,
        UserId,
        1200 + (Ordinal * 50),
        0,
        3000 + (TotalSessions * 12),
        800 + (Ordinal * 30),
        DATEADD(day, -30 - Ordinal, @SeedCreatedAtUtc),
        LastActiveAt
    FROM @DemoUsers;

    DECLARE @CompanionSkillPlan TABLE
    (
        CompanionOrdinal int NOT NULL,
        SkillOrder int NOT NULL,
        SkillSlug nvarchar(100) NOT NULL,
        PRIMARY KEY (CompanionOrdinal, SkillOrder)
    );

    INSERT INTO @CompanionSkillPlan (CompanionOrdinal, SkillOrder, SkillSlug)
    VALUES
        (1, 1, N'tieng-anh-giao-tiep'), (1, 2, N'ielts-speaking'),      (1, 3, N'cv-phong-van'),        (1, 4, N'thuyet-trinh'), (1, 5, N'powerpoint'),
        (2, 1, N'react'),               (2, 2, N'javascript'),          (2, 3, N'python'),              (2, 4, N'sql'),          (2, 5, N'powerpoint'),
        (3, 1, N'canva'),               (3, 2, N'powerpoint'),          (3, 3, N'thuyet-trinh'),        (3, 4, N'cv-phong-van'), (3, 5, N'tieng-anh-giao-tiep'),
        (4, 1, N'python'),              (4, 2, N'sql'),                 (4, 3, N'excel'),               (4, 4, N'react'),        (4, 5, N'javascript'),
        (5, 1, N'excel'),               (5, 2, N'sql'),                 (5, 3, N'powerpoint'),          (5, 4, N'canva'),        (5, 5, N'cv-phong-van'),
        (6, 1, N'cv-phong-van'),        (6, 2, N'thuyet-trinh'),        (6, 3, N'tieng-anh-giao-tiep'), (6, 4, N'ielts-speaking'), (6, 5, N'powerpoint'),
        (7, 1, N'javascript'),          (7, 2, N'react'),               (7, 3, N'sql'),                 (7, 4, N'python'),       (7, 5, N'canva'),
        (8, 1, N'sql'),                 (8, 2, N'excel'),               (8, 3, N'python'),              (8, 4, N'powerpoint'),   (8, 5, N'thuyet-trinh'),
        (9, 1, N'ielts-speaking'),      (9, 2, N'tieng-anh-giao-tiep'), (9, 3, N'thuyet-trinh'),        (9, 4, N'cv-phong-van'), (9, 5, N'canva'),
        (10, 1, N'python'),             (10, 2, N'react'),              (10, 3, N'javascript'),         (10, 4, N'cv-phong-van'), (10, 5, N'sql');

    INSERT INTO [UserSkills] ([UserSkillId], [UserId], [SkillId], [Type], [CreatedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT(
            'd7',
            RIGHT(CONCAT('000000', CAST(((skillPlan.CompanionOrdinal - 1) * 5 + skillPlan.SkillOrder) AS varchar(6))), 6),
            '-0000-0000-0000-',
            RIGHT(CONCAT('000000000000', CAST(((skillPlan.CompanionOrdinal - 1) * 5 + skillPlan.SkillOrder) AS varchar(12))), 12)
        )),
        demo.UserId,
        skill.SkillId,
        N'Teach',
        DATEADD(minute, skillPlan.SkillOrder, DATEADD(day, -30 - demo.Ordinal, @SeedCreatedAtUtc))
    FROM @CompanionSkillPlan skillPlan
    INNER JOIN @DemoUsers demo ON demo.Ordinal = skillPlan.CompanionOrdinal
    INNER JOIN @SkillMap skill ON skill.Slug = skillPlan.SkillSlug;

    DECLARE @ClassNumbers TABLE (ClassNo int NOT NULL PRIMARY KEY);
    INSERT INTO @ClassNumbers (ClassNo)
    VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10);

    DECLARE @LocalTimes TABLE
    (
        CompanionOrdinal int NOT NULL PRIMARY KEY,
        StartHour int NOT NULL,
        StartMinute int NOT NULL
    );

    INSERT INTO @LocalTimes (CompanionOrdinal, StartHour, StartMinute)
    VALUES
        (1, 8, 0), (2, 9, 30), (3, 13, 30), (4, 19, 0), (5, 10, 0),
        (6, 15, 0), (7, 20, 0), (8, 7, 30), (9, 18, 30), (10, 11, 0);

    INSERT INTO [Sessions]
        ([SessionId], [CompanionId], [LearnerId], [SkillId], [Skill], [Description], [DeliveryMode], [Location], [DurationMinutes], [PointCost], [PricingModel], [DurationOptions], [SelectedDurationMinutes], [CompanionPayoutPoints], [LearnerChargePoints], [PlatformFeePoints], [SkillBasePointsSnapshot], [CredentialBonusPointsSnapshot], [DurationMultiplierPercentSnapshot], [ScheduledAt], [Status], [JitsiRoomId], [ActualStartAt], [ActualEndAt], [ActualDuration], [LearnerConfirmed], [CompanionConfirmed], [CancelledBy], [CancelReason], [CancelledAt], [DisbursedAt], [CreatedAt], [UpdatedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT(
            'd4',
            RIGHT(CONCAT('000000', CAST(((class.ClassNo - 1) * 10 + demo.Ordinal) AS varchar(6))), 6),
            '-0000-0000-0000-',
            RIGHT(CONCAT('000000000000', CAST(((class.ClassNo - 1) * 10 + demo.Ordinal) AS varchar(12))), 12)
        )) AS SessionId,
        demo.UserId AS CompanionId,
        NULL AS LearnerId,
        skill.SkillId,
        skill.Name AS Skill,
        CASE class.ClassNo
            WHEN 1 THEN CONCAT(N'Khởi động với ', skill.Name, N': kiểm tra nền tảng hiện tại, thống nhất mục tiêu học và làm bài thực hành đầu tiên ngay trong buổi.')
            WHEN 2 THEN CONCAT(N'Lớp ', skill.Name, N' cho người mới bắt đầu, đi chậm từng bước và có checklist tự luyện sau buổi học.')
            WHEN 3 THEN CONCAT(N'Thực hành ', skill.Name, N' qua tình huống thật của sinh viên: bài tập, dự án cá nhân hoặc hồ sơ ứng tuyển.')
            WHEN 4 THEN CONCAT(N'Sửa lỗi thường gặp khi học ', skill.Name, N', kèm ví dụ cụ thể để bạn biết cách tự kiểm tra sau này.')
            WHEN 5 THEN CONCAT(N'Tăng tốc ', skill.Name, N' trong 90 phút: ôn nhanh nền tảng, làm bài mẫu và nhận phản hồi trực tiếp.')
            WHEN 6 THEN CONCAT(N'Ứng dụng ', skill.Name, N' vào sản phẩm hoặc bài nộp thật, phù hợp với bạn đã biết cơ bản nhưng còn thiếu hệ thống.')
            WHEN 7 THEN CONCAT(N'Buổi hỏi đáp chuyên sâu về ', skill.Name, N', ưu tiên giải quyết vướng mắc cá nhân và hướng dẫn tài liệu học tiếp.')
            WHEN 8 THEN CONCAT(N'Luyện tập ', skill.Name, N' theo lộ trình ngắn: mục tiêu rõ, bài tập vừa sức và phần tổng kết sau buổi.')
            WHEN 9 THEN CONCAT(N'Xây dựng portfolio nhỏ bằng ', skill.Name, N', giúp bạn có sản phẩm hoặc ví dụ cụ thể để đưa vào hồ sơ.')
            ELSE CONCAT(N'Tổng ôn ', skill.Name, N': hệ thống lại kiến thức, sửa bài thực hành và chốt kế hoạch tự học trong hai tuần tiếp theo.')
        END AS Description,
        N'Online' AS DeliveryMode,
        NULL AS Location,
        120 AS DurationMinutes,
        0 AS PointCost,
        N'FormulaV1' AS PricingModel,
        N'[60,90,120]' AS DurationOptions,
        NULL AS SelectedDurationMinutes,
        NULL AS CompanionPayoutPoints,
        NULL AS LearnerChargePoints,
        NULL AS PlatformFeePoints,
        NULL AS SkillBasePointsSnapshot,
        NULL AS CredentialBonusPointsSnapshot,
        NULL AS DurationMultiplierPercentSnapshot,
        DATEADD(hour, -7,
            DATEADD(minute, localTime.StartHour * 60 + localTime.StartMinute,
                DATEADD(day, ((class.ClassNo - 1) * 2) + ((demo.Ordinal - 1) % 2), CONVERT(datetime2(7), @FirstClassDateLocal))
            )
        ) AS ScheduledAt,
        N'Available' AS Status,
        NULL AS JitsiRoomId,
        NULL AS ActualStartAt,
        NULL AS ActualEndAt,
        NULL AS ActualDuration,
        0 AS LearnerConfirmed,
        0 AS CompanionConfirmed,
        NULL AS CancelledBy,
        NULL AS CancelReason,
        NULL AS CancelledAt,
        NULL AS DisbursedAt,
        DATEADD(minute, ((class.ClassNo - 1) * 10 + demo.Ordinal - 1) * @OfferCreatedStepMinutes, @SeedCreatedAtUtc) AS CreatedAt,
        DATEADD(minute, ((class.ClassNo - 1) * 10 + demo.Ordinal - 1) * @OfferCreatedStepMinutes, @SeedCreatedAtUtc) AS UpdatedAt
    FROM @DemoUsers demo
    CROSS JOIN @ClassNumbers class
    INNER JOIN @CompanionSkillPlan skillPlan
        ON skillPlan.CompanionOrdinal = demo.Ordinal
        AND skillPlan.SkillOrder = ((class.ClassNo - 1) % 5) + 1
    INNER JOIN @SkillMap skill ON skill.Slug = skillPlan.SkillSlug
    INNER JOIN @LocalTimes localTime ON localTime.CompanionOrdinal = demo.Ordinal;

    -- ============================================================
    -- Verification output
    -- ============================================================
    SELECT
        COUNT(*) AS DemoCompanionCount
    FROM [Users]
    WHERE Email LIKE N'%' + @DemoEmailDomain
      AND Roles LIKE N'%companion%';

    SELECT
        COUNT(*) AS DemoAvailableOnlineOfferCount
    FROM [Sessions] session
    INNER JOIN [Users] companion ON companion.UserId = session.CompanionId
    WHERE companion.Email LIKE N'%' + @DemoEmailDomain
      AND session.Status = N'Available'
      AND session.DeliveryMode = N'Online'
      AND session.LearnerId IS NULL;

    SELECT
        COUNT(*) AS OffersBeforeJuneSecondVietnamTime
    FROM [Sessions] session
    INNER JOIN [Users] companion ON companion.UserId = session.CompanionId
    WHERE companion.Email LIKE N'%' + @DemoEmailDomain
      AND DATEADD(hour, 7, session.ScheduledAt) < '2026-06-02';

    SELECT TOP (30)
        session.CreatedAt,
        profile.DisplayName,
        session.Skill,
        DATEADD(hour, 7, session.ScheduledAt) AS ScheduledAtVietnamTime
    FROM [Sessions] session
    INNER JOIN [Users] companion ON companion.UserId = session.CompanionId
    INNER JOIN [UserProfiles] profile ON profile.UserId = companion.UserId
    WHERE companion.Email LIKE N'%' + @DemoEmailDomain
    ORDER BY session.CreatedAt;

    SELECT
        seed.Slug AS SeedSlug,
        seed.Name AS SeedName,
        COUNT(skill.SkillId) AS MatchingExistingSkillCount,
        STRING_AGG(CONVERT(nvarchar(36), skill.SkillId), N', ') AS MatchingSkillIds
    FROM @SkillSeed seed
    INNER JOIN [Skills] skill
        ON LOWER(skill.Slug) = LOWER(seed.Slug)
        OR LOWER(skill.Name) = LOWER(seed.Name)
    GROUP BY seed.Slug, seed.Name
    HAVING COUNT(skill.SkillId) > 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
