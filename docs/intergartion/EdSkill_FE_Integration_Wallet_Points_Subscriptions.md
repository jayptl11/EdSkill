# EdSkill FE Integration - Wallet Point Packages + Fixed Subscriptions

Tai lieu nay danh cho FE tich hop 2 nhom monetization moi:

- mua `goi point`
- mua `subscription co dinh`

Tai lieu nay duoc viet de FE co the code truc tiep theo contract backend hien tai, khong can suy doan them.

---

## 1. Scope backend da co

Backend da implement cac capability sau:

- `GET /api/wallet/points/packages`
- `POST /api/wallet/points/purchase`
- `GET /api/wallet/points/purchase/vnpay-return`
- `GET /api/wallet/points/purchase/vnpay-ipn`
- `GET /api/wallet/subscriptions/plans`
- `POST /api/wallet/subscriptions/purchase`
- `GET /api/wallet/subscriptions/me`
- `GET /api/wallet/subscriptions/purchase/vnpay-return`
- `GET /api/wallet/subscriptions/purchase/vnpay-ipn`
- `GET /api/wallet/payments`
- `GET /api/profile/me` co them thong tin subscription dang active
- `GET /api/companions/search` co them `subscriptionBadge`, `hasPriorityVisibility`
- `GET /api/companions/{companionId}/public-profile` co them `subscriptionBadge`, `hasPriorityVisibility`

Khong co trong scope nay:

- admin CRUD cho subscription
- auto renew
- huy subscription
- refund payment

Subscription la co dinh trong DB seed, FE chi can doc va render.

---

## 2. Business meaning FE can rely on

### 2.1. Goi point

- Day la nap point 1 lan.
- Sau khi thanh toan VNPay thanh cong, backend cong point vao wallet.
- FE khong tu cong point local truoc callback thanh cong.

### 2.2. Subscription

- Day la mua goi thang co dinh `30 ngay`.
- Khong co auto renew.
- FE khong co man admin sua gia, sua benefit hay them goi moi.
- User co the mua lai khi het han.

### 2.3. Rule overlap subscription

Backend da enforce overlap theo coverage role:

- `Learner Pro` chi chiem learner coverage
- `Companion Pro` chi chiem companion coverage
- `Da nang Pro` chiem ca learner + companion coverage

FE can rely:

- `Learner Pro` + `Companion Pro` co the cung ton tai neu user co du role
- mua goi bi overlap se tra `409 SUBSCRIPTION_PLAN_CONFLICT`

### 2.4. VNPay flow

Ca point package va subscription deu dung chung pattern:

1. FE goi backend tao purchase
2. backend tra `paymentUrl`
3. FE redirect sang VNPay
4. user thanh toan xong, VNPay redirect ve FE route
5. FE goi lai backend `vnpay-return` voi toan bo query string VNPay
6. backend tra ket qua chuan hoa de FE render trang success/fail

FE khong dung endpoint `vnpay-ipn`.
Endpoint `ipn` la callback server-to-server cho VNPay.

---

## 3. Auth rules

### 3.1. Public endpoints

Khong can auth:

- `GET /api/wallet/points/packages`
- `GET /api/wallet/subscriptions/plans`
- `GET /api/wallet/points/purchase/vnpay-return`
- `GET /api/wallet/subscriptions/purchase/vnpay-return`

### 3.2. User auth endpoints

Can Bearer token:

- `POST /api/wallet/points/purchase`
- `POST /api/wallet/subscriptions/purchase`
- `GET /api/wallet/subscriptions/me`
- `GET /api/wallet/payments`
- `GET /api/profile/me`

### 3.3. Role rules

Backend chi cho user co role `learner` hoac `companion` mua:

- point package
- subscription

User chi co role `admin` se bi `403 FORBIDDEN`.

---

## 4. TypeScript contracts FE nen dung

```ts
export type PaymentStatus =
  | "Pending"
  | "Success"
  | "Failed"
  | "Refunded"
  | "Cancelled";

export type PaymentProvider = "VnPay";

export type SubscriptionTargetRole = "Learner" | "Companion" | "MultiRole";
export type SubscriptionBillingCycle = "Monthly";
export type UserSubscriptionStatus = "Active" | "Cancelled" | "Expired";

export interface PointPackageDto {
  packageId: string;
  code: string;
  name: string;
  description: string | null;
  points: number;
  bonusPoints: number;
  totalPoints: number;
  priceVnd: number;
  currency: string;
  badgeText: string | null;
  isHighlighted: boolean;
}

export interface PointPackageListDto {
  data: PointPackageDto[];
}

export interface CreatePointPurchaseRequest {
  packageId: string;
}

export interface CreatePointPurchaseResultDto {
  paymentTransactionId: string;
  paymentUrl: string;
  expiresAt: string;
}

export interface SubscriptionPlanEntitlementsDto {
  immediateBonusPoints: number;
  weeklyLearnerSessionBonusPoints: number;
  weeklyCompanionSessionBonusPoints: number;
  learnerTokenRewardRatePercent: number | null;
  companionTokenRewardRatePercent: number | null;
  companionDailySessionLimitOverride: number | null;
  companionBadgeText: string | null;
  hasPriorityVisibility: boolean;
}

export interface SubscriptionPlanDto {
  planId: string;
  code: string;
  name: string;
  targetRole: SubscriptionTargetRole;
  priceVnd: number;
  currency: string;
  billingCycle: SubscriptionBillingCycle;
  displayBenefits: string[];
  entitlements: SubscriptionPlanEntitlementsDto;
  isCurrentPlan: boolean;
  canPurchase: boolean;
  purchaseDisabledReasonCode: string | null;
  purchaseDisabledReason: string | null;
}

export interface SubscriptionPlanListDto {
  data: SubscriptionPlanDto[];
}

export interface CreateSubscriptionPurchaseRequest {
  planId: string;
}

export interface CreateSubscriptionPurchaseResultDto {
  paymentTransactionId: string;
  paymentUrl: string;
  expiresAt: string;
}

export interface ActiveSubscriptionSummaryDto {
  userSubscriptionId: string;
  planId: string;
  code: string;
  name: string;
  targetRole: SubscriptionTargetRole;
  status: UserSubscriptionStatus;
  startedAt: string;
  expiresAt: string;
}

export interface ResolvedSubscriptionEntitlementsDto {
  hasLearnerCoverage: boolean;
  hasCompanionCoverage: boolean;
  companionBadgeText: string | null;
  hasPriorityVisibility: boolean;
  companionDailySessionLimitOverride: number | null;
  learnerTokenRewardRatePercent: number | null;
  companionTokenRewardRatePercent: number | null;
  weeklyLearnerSessionBonusPoints: number;
  weeklyCompanionSessionBonusPoints: number;
}

export interface MySubscriptionsDto {
  activeSubscriptions: ActiveSubscriptionSummaryDto[];
  entitlements: ResolvedSubscriptionEntitlementsDto;
}

export interface PaymentTransactionDto {
  paymentTransactionId: string;
  packageId: string | null;
  packageName: string | null;
  subscriptionPlanId: string | null;
  subscriptionPlanName: string | null;
  provider: PaymentProvider;
  amountVnd: number;
  currency: string;
  status: PaymentStatus;
  paymentUrl: string | null;
  paidAt: string | null;
  createdAt: string;
}

export interface PaymentTransactionHistoryDto {
  data: PaymentTransactionDto[];
  total: number;
  page: number;
  limit: number;
}

export interface VnPayReturnResultDto {
  paymentTransactionId: string;
  packageId: string | null;
  packageName: string | null;
  subscriptionPlanId: string | null;
  subscriptionPlanName: string | null;
  status: PaymentStatus;
  creditedPoints: number;
  alreadyProcessed: boolean;
}

export interface SubscriptionPurchaseReturnResultDto {
  paymentTransactionId: string;
  planId: string | null;
  planName: string | null;
  status: PaymentStatus;
  creditedPoints: number;
  alreadyProcessed: boolean;
}

export interface ProfileDto {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  dateOfBirth: string | null;
  phone: string | null;
  degreeUrl: string | null;
  credentialUrls: string[];
  credentialCount: number;
  skillsToTeach: string[];
  skillsToLearn: string[];
  teachingSkills: { skillId: string; name: string; iconKey: string | null }[];
  learningSkills: { skillId: string; name: string; iconKey: string | null }[];
  achievements: {
    achievementId: string;
    name: string;
    description: string;
    iconUrl: string | null;
    awardedAt: string;
  }[];
  isPublic: boolean;
  roles: string[];
  totalSessions: number;
  lastActiveAt: string | null;
  isCompanionOnboardingComplete: boolean;
  missingCompanionProfileFields: string[];
  activeSubscriptions: ActiveSubscriptionSummaryDto[];
  subscriptionEntitlements: ResolvedSubscriptionEntitlementsDto | null;
}

export interface CompanionSearchItemDto {
  companionId: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  skillsToTeach: string[];
  credentialCount: number;
  avgRating: number;
  totalReviews: number;
  matchingSessionCount: number;
  lowestPointCost: number;
  pricingPreview: {
    minCompanionPayoutPoints: number;
    maxCompanionPayoutPoints: number;
    minLearnerChargePoints: number;
    maxLearnerChargePoints: number;
    minPlatformFeePoints: number;
    maxPlatformFeePoints: number;
  };
  nextScheduledAt: string;
  matchedOffers: unknown[];
  subscriptionBadge: string | null;
  hasPriorityVisibility: boolean;
}

export interface CompanionPublicProfileDto {
  companionId: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  roles: string[];
  activitySummary: {
    totalSessions: number;
    totalTeachingHours: number;
    avgRating: number;
    totalReviews: number;
    lastActiveAt: string | null;
  };
  achievements: unknown[];
  teachingSkills: unknown[];
  subscriptionBadge: string | null;
  hasPriorityVisibility: boolean;
}
```

---

## 5. Point package APIs

## 5.1. `GET /api/wallet/points/packages`

### Response 200

```json
{
  "data": [
    {
      "packageId": "91000000-0000-0000-0000-000000000001",
      "code": "goi_1",
      "name": "Goi 1",
      "description": "Goi nap 500 Points.",
      "points": 500,
      "bonusPoints": 0,
      "totalPoints": 500,
      "priceVnd": 59000,
      "currency": "VND",
      "badgeText": null,
      "isHighlighted": false
    },
    {
      "packageId": "91000000-0000-0000-0000-000000000002",
      "code": "goi_2",
      "name": "Goi 2",
      "description": "Goi nap 1.000 Points.",
      "points": 1000,
      "bonusPoints": 0,
      "totalPoints": 1000,
      "priceVnd": 99000,
      "currency": "VND",
      "badgeText": null,
      "isHighlighted": false
    }
  ]
}
```

### FE rendering rules

- Render theo thu tu backend tra ve.
- `totalPoints = points + bonusPoints`, FE khong can tinh lai.
- FE nen hien:
  - ten goi
  - gia VND
  - tong so point nhan duoc
  - badge neu `badgeText != null`

### Seed package FE co the expect

- `59.000 VND` -> `500 points`
- `99.000 VND` -> `1.000 points`
- `169.000 VND` -> `2.000 points`
- `379.000 VND` -> `5.000 points`

## 5.2. `POST /api/wallet/points/purchase`

```http
POST /api/wallet/points/purchase
Authorization: Bearer <token>
Content-Type: application/json
```

### Request

```json
{
  "packageId": "91000000-0000-0000-0000-000000000001"
}
```

### Response 200

```json
{
  "paymentTransactionId": "11111111-1111-1111-1111-111111111111",
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
  "expiresAt": "2026-05-17T10:15:00Z"
}
```

### FE flow

1. user click `Mua` tren package
2. FE goi endpoint tao purchase
3. neu thanh cong, redirect browser den `paymentUrl`

### Errors FE can hit

- `403 FORBIDDEN`
- `404 POINT_PACKAGE_NOT_FOUND`
- `409 POINT_PACKAGE_NOT_AVAILABLE`
- `400/422` validation fallback

---

## 6. Subscription APIs

## 6.1. `GET /api/wallet/subscriptions/plans`

### Response 200

```json
{
  "data": [
    {
      "planId": "92000000-0000-0000-0000-000000000001",
      "code": "learner_pro",
      "name": "Learner Pro",
      "targetRole": "Learner",
      "priceVnd": 119000,
      "currency": "VND",
      "billingCycle": "Monthly",
      "displayBenefits": [
        "Tặng ngay 200 Point",
        "Voucher 75% hằng tuần",
        "Không quảng cáo",
        "Ưu tiên matching",
        "Rebook nhanh"
      ],
      "entitlements": {
        "immediateBonusPoints": 200,
        "weeklyLearnerSessionBonusPoints": 0,
        "weeklyCompanionSessionBonusPoints": 0,
        "learnerTokenRewardRatePercent": null,
        "companionTokenRewardRatePercent": null,
        "companionDailySessionLimitOverride": null,
        "companionBadgeText": null,
        "hasPriorityVisibility": false
      },
      "isCurrentPlan": false,
      "canPurchase": true,
      "purchaseDisabledReasonCode": null,
      "purchaseDisabledReason": null
    }
  ]
}
```

### Fixed plans FE co the expect

- `Learner Pro`:
  - `119.000 / thang`
  - bonus ngay `200 points`
- `Companion Pro`:
  - `79.000 / thang`
  - co badge `Companion Pro`
  - `hasPriorityVisibility = true`
  - `companionDailySessionLimitOverride = 12`
  - `companionTokenRewardRatePercent = 30`
- `Da nang Pro`:
- `Đa năng Pro`:
  - `179.000 / thang`
  - co badge `Đa năng Pro`
  - `hasPriorityVisibility = true`
  - weekly learner bonus `200`
  - weekly companion bonus `200`
  - learner token rate `10`
  - companion token rate `6`

### FE rendering rules

- FE nen render `displayBenefits` de show marketing copy.
- FE co the dung `entitlements` de render badge `Noi bat`, `Tang 200 point`, `Them slot`.
- `displayBenefits` la string list da duoc backend format san.
- Khong co endpoint admin sua plan, FE khong can build CMS flow.
- Disable nut `Mua goi` khi `canPurchase = false`.
- Neu `isCurrentPlan = true`, FE nen hien label `Goi dang hoat dong`.
- Neu `purchaseDisabledReasonCode = SUBSCRIPTION_PLAN_CONFLICT`, FE nen disable nut va hien tooltip/message conflict.

## 6.2. `POST /api/wallet/subscriptions/purchase`

```http
POST /api/wallet/subscriptions/purchase
Authorization: Bearer <token>
Content-Type: application/json
```

### Request

```json
{
  "planId": "92000000-0000-0000-0000-000000000002"
}
```

### Response 200

```json
{
  "paymentTransactionId": "22222222-2222-2222-2222-222222222222",
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
  "expiresAt": "2026-05-17T10:15:00Z"
}
```

### FE flow

Giong point package:

1. user click mua plan
2. FE goi endpoint
3. redirect den `paymentUrl`

### Errors FE can hit

- `403 FORBIDDEN`
- `404 SUBSCRIPTION_PLAN_NOT_FOUND`
- `409 SUBSCRIPTION_PLAN_NOT_AVAILABLE`
- `409 SUBSCRIPTION_PLAN_CONFLICT`

### FE UX khuyen nghi

Map `SUBSCRIPTION_PLAN_CONFLICT` thanh message de hieu, vi du:

- `Ban da co goi hoc vien dang hoat dong`
- `Ban da co goi companion dang hoat dong`
- `Goi da nang khong the mua khi con goi role khac`

Backend hien tra message chung. FE co the hien message backend hoac map theo business copy rieng.

## 6.3. `GET /api/wallet/subscriptions/me`

### Response 200

```json
{
  "activeSubscriptions": [
    {
      "userSubscriptionId": "33333333-3333-3333-3333-333333333333",
      "planId": "92000000-0000-0000-0000-000000000002",
      "code": "companion_pro",
      "name": "Companion Pro",
      "targetRole": "Companion",
      "status": "Active",
      "startedAt": "2026-05-17T10:00:00Z",
      "expiresAt": "2026-06-16T10:00:00Z"
    }
  ],
  "entitlements": {
    "hasLearnerCoverage": false,
    "hasCompanionCoverage": true,
    "companionBadgeText": "Companion Pro",
    "hasPriorityVisibility": true,
    "companionDailySessionLimitOverride": 12,
    "learnerTokenRewardRatePercent": null,
    "companionTokenRewardRatePercent": 30,
    "weeklyLearnerSessionBonusPoints": 0,
    "weeklyCompanionSessionBonusPoints": 0
  }
}
```

### FE use cases

- man `Subscription cua toi`
- badge status trong dashboard
- hien `het han vao ngay ...`
- hien benefit dang active

---

## 7. VNPay return flow FE nen lam

## 7.1. Point package return

Backend endpoint:

```http
GET /api/wallet/points/purchase/vnpay-return?<full-query-from-vnpay>
```

### Response 200

```json
{
  "paymentTransactionId": "11111111-1111-1111-1111-111111111111",
  "packageId": "91000000-0000-0000-0000-000000000001",
  "packageName": "Goi 1",
  "subscriptionPlanId": null,
  "subscriptionPlanName": null,
  "status": "Success",
  "creditedPoints": 500,
  "alreadyProcessed": false
}
```

## 7.2. Subscription return

Backend endpoint:

```http
GET /api/wallet/subscriptions/purchase/vnpay-return?<full-query-from-vnpay>
```

### Response 200

```json
{
  "paymentTransactionId": "22222222-2222-2222-2222-222222222222",
  "planId": "92000000-0000-0000-0000-000000000002",
  "planName": "Companion Pro",
  "status": "Success",
  "creditedPoints": 0,
  "alreadyProcessed": false
}
```

### FE implementation rule

Sau khi VNPay redirect ve FE route:

1. giu nguyen full query string
2. FE goi backend `vnpay-return` tuong ung
3. render trang ket qua dua tren response backend

Khong tu doc `vnp_ResponseCode` hay checksum o FE de quyet dinh thanh cong.
Backend moi la noi xac thuc callback hop le.

### `alreadyProcessed`

- `false`: callback nay vua duoc xu ly
- `true`: payment nay da duoc xu ly truoc do

FE co the xem ca 2 truong hop la ket qua hop le va chi can render status cuoi cung.

### `creditedPoints`

- point package:
  - la tong point da cong
- subscription:
  - chi co gia tri > 0 neu plan co immediate bonus point
  - `Learner Pro` hien tai tra `200`

---

## 8. Wallet payment history

## 8.1. `GET /api/wallet/payments?status=pending&page=1&limit=20`

### Response 200

```json
{
  "data": [
    {
      "paymentTransactionId": "11111111-1111-1111-1111-111111111111",
      "packageId": "91000000-0000-0000-0000-000000000001",
      "packageName": "Goi 1",
      "subscriptionPlanId": null,
      "subscriptionPlanName": null,
      "provider": "VnPay",
      "amountVnd": 59000,
      "currency": "VND",
      "status": "Pending",
      "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
      "paidAt": null,
      "createdAt": "2026-05-17T09:00:00Z"
    },
    {
      "paymentTransactionId": "22222222-2222-2222-2222-222222222222",
      "packageId": null,
      "packageName": null,
      "subscriptionPlanId": "92000000-0000-0000-0000-000000000002",
      "subscriptionPlanName": "Companion Pro",
      "provider": "VnPay",
      "amountVnd": 79000,
      "currency": "VND",
      "status": "Success",
      "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
      "paidAt": "2026-05-17T10:03:00Z",
      "createdAt": "2026-05-17T10:00:00Z"
    }
  ],
  "total": 2,
  "page": 1,
  "limit": 20
}
```

### FE rendering rules

- Moi dong payment la:
  - point package neu `packageId != null`
  - subscription neu `subscriptionPlanId != null`
- FE khong assume ca 2 field cung co gia tri.
- `paymentUrl` co the duoc dung cho nut `Thu lai thanh toan` neu payment van `Pending`.

### Supported status filter

Backend parse enum khong phan biet hoa thuong:

- `pending`
- `success`
- `failed`
- `refunded`
- `cancelled`

Neu status khong hop le, backend tra `400 PAYMENT_STATUS_INVALID`.

---

## 9. `GET /api/profile/me` da co them subscription data

FE co the dung ngay `profile/me` de render badge subscription trong profile/dashboard, khong can goi rieng `subscriptions/me` neu chi can thong tin co ban.

### Field moi

- `activeSubscriptions`
- `subscriptionEntitlements`

### Response sample rut gon

```json
{
  "userId": "44444444-4444-4444-4444-444444444444",
  "displayName": "Nguyen A",
  "roles": ["learner", "companion"],
  "activeSubscriptions": [
    {
      "userSubscriptionId": "33333333-3333-3333-3333-333333333333",
      "planId": "92000000-0000-0000-0000-000000000002",
      "code": "companion_pro",
      "name": "Companion Pro",
      "targetRole": "Companion",
      "status": "Active",
      "startedAt": "2026-05-17T10:00:00Z",
      "expiresAt": "2026-06-16T10:00:00Z"
    }
  ],
  "subscriptionEntitlements": {
    "hasLearnerCoverage": false,
    "hasCompanionCoverage": true,
    "companionBadgeText": "Companion Pro",
    "hasPriorityVisibility": true,
    "companionDailySessionLimitOverride": 12,
    "learnerTokenRewardRatePercent": null,
    "companionTokenRewardRatePercent": 30,
    "weeklyLearnerSessionBonusPoints": 0,
    "weeklyCompanionSessionBonusPoints": 0
  }
}
```

### FE use cases

- profile page `Goi dang dung`
- dashboard summary
- warning `Sap het han`
- hien badge `Companion Pro` / `Da nang Pro`

---

## 10. Companion search + public profile da co field premium

## 10.1. `GET /api/companions/search`

Moi item `CompanionSearchItemDto` da co:

- `subscriptionBadge`
- `hasPriorityVisibility`

### FE rules

- Neu `subscriptionBadge != null`, FE nen render badge tren card companion.
- `hasPriorityVisibility` da duoc backend dung de sort uu tien san.
- FE khong can sort lai premium len dau.

## 10.2. `GET /api/companions/{companionId}/public-profile`

Response da co:

- `subscriptionBadge`
- `hasPriorityVisibility`

### FE rules

- Render badge premium tren public profile header neu co.
- Khong tu suy ra premium tu role; chi dung field backend tra ve.

---

## 11. FE page flow khuyen nghi

## 11.1. Man nap point

1. goi `GET /api/wallet/points/packages`
2. render card package
3. user click mua
4. goi `POST /api/wallet/points/purchase`
5. redirect `paymentUrl`
6. sau redirect tu VNPay, goi `GET /api/wallet/points/purchase/vnpay-return`
7. neu success:
   - refresh `GET /api/wallet/points`
   - refresh `GET /api/wallet/payments`

## 11.2. Man subscription

1. goi `GET /api/wallet/subscriptions/plans`
2. render 3 plan co dinh
3. user click mua
4. goi `POST /api/wallet/subscriptions/purchase`
5. redirect `paymentUrl`
6. sau redirect tu VNPay, goi `GET /api/wallet/subscriptions/purchase/vnpay-return`
7. neu success:
   - refresh `GET /api/wallet/subscriptions/me`
   - refresh `GET /api/profile/me`
   - refresh `GET /api/wallet/payments`

## 11.3. Man lich su thanh toan

1. goi `GET /api/wallet/payments?page=1&limit=20`
2. cho filter status
3. render label:
   - `Nap point` neu co `packageName`
   - `Mua goi` neu co `subscriptionPlanName`

---

## 12. Error handling FE nen support

### 12.1. Point package

- `403 FORBIDDEN`
- `404 POINT_PACKAGE_NOT_FOUND`
- `409 POINT_PACKAGE_NOT_AVAILABLE`

### 12.2. Subscription

- `403 FORBIDDEN`
- `404 SUBSCRIPTION_PLAN_NOT_FOUND`
- `409 SUBSCRIPTION_PLAN_NOT_AVAILABLE`
- `409 SUBSCRIPTION_PLAN_CONFLICT`

### 12.3. Wallet payments

- `400 PAYMENT_STATUS_INVALID`

### 12.4. VNPay return

- `400 PAYMENT_CALLBACK_INVALID`
- `400 PAYMENT_PROVIDER_INVALID_SIGNATURE`
- `404 PAYMENT_TRANSACTION_NOT_FOUND`

FE nen show thong diep loi business ro nghia, khong chi show raw status code.

---

## 13. FE should not do

- Khong hard-code quyet dinh thanh cong/that bai theo query string VNPay.
- Khong tu cong point local truoc khi backend xac nhan callback thanh cong.
- Khong cho phep FE tu sua danh sach subscription plan.
- Khong assume user chi co 1 role.
- Khong assume lich su thanh toan chi co point package.
- Khong sort premium companion o FE de thay the sort backend.

---

## 14. Recommended UI copy

Nap point:

- `Chon goi point`
- `Nhan {n} point`
- `Thanh toan voi VNPay`
- `Nap point thanh cong`

Subscription:

- `Goi cua ban`
- `Kich hoat den`
- `Mua goi`
- `Goi dang hoat dong`
- `Quyen loi`

Payment history:

- `Lich su thanh toan`
- `Nap point`
- `Subscription`
- `Dang cho thanh toan`
- `Thanh cong`
- `That bai`
