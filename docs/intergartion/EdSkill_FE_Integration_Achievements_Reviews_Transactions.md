# EdSkill FE Integration - Achievements, Reviews, Transactions

Tai lieu nay cover:

- tab `Bang thanh tich`
- tab `Danh gia`
- tab `Lich su giao dich`

Muc tieu la de FE co the dung dung contract backend hien tai, khong tu ghep tu nhieu endpoint mot cach mo ho.

---

## 1. Scope backend da co

### 1.1. Achievements

- `GET /api/achievements/me`
- `GET /api/profile/me` van co field `achievements`

### 1.2. Reviews

- `GET /api/reviews/me/dashboard`
- `POST /api/reviews`

### 1.3. Transactions

Khong co API moi. FE reuse:

- `GET /api/wallet/points`
- `GET /api/wallet/points/transactions`
- `GET /api/wallet/payments`

---

## 2. Auth rules

Can Bearer token:

- `GET /api/achievements/me`
- `GET /api/reviews/me/dashboard`
- `POST /api/reviews`
- tat ca route wallet o tren

---

## 3. Achievements API

### 3.1. Endpoint

```http
GET /api/achievements/me
```

### 3.2. DTOs

```ts
export interface MyAchievementEarnedDto {
  achievementId: string;
  name: string;
  description: string;
  iconUrl: string | null;
  track: "learner" | "companion";
  metric: "completed_sessions" | "completed_hours" | "distinct_completed_learners";
  threshold: number;
  awardedAt: string;
}

export interface MyUpcomingAchievementDto {
  achievementId: string;
  name: string;
  description: string;
  iconUrl: string | null;
  track: "learner" | "companion";
  metric: "completed_sessions" | "completed_hours" | "distinct_completed_learners";
  currentValue: number;
  threshold: number;
  remainingValue: number;
  progressPercent: number;
}

export interface MyAchievementsDto {
  earned: MyAchievementEarnedDto[];
  upcoming: MyUpcomingAchievementDto[];
}
```

### 3.3. Response sample

```json
{
  "earned": [
    {
      "achievementId": "11111111-1111-1111-1111-111111111111",
      "name": "Buoi hoc dau tien",
      "description": "Hoan thanh buoi hoc dau tien",
      "iconUrl": "https://cdn.edskill.test/achievement/1.png",
      "track": "learner",
      "metric": "completed_sessions",
      "threshold": 1,
      "awardedAt": "2026-05-18T10:00:00Z"
    }
  ],
  "upcoming": [
    {
      "achievementId": "22222222-2222-2222-2222-222222222222",
      "name": "3 buoi hoc",
      "description": "Hoan thanh 3 buoi hoc",
      "iconUrl": "https://cdn.edskill.test/achievement/2.png",
      "track": "learner",
      "metric": "completed_sessions",
      "currentValue": 2,
      "threshold": 3,
      "remainingValue": 1,
      "progressPercent": 66.67
    }
  ]
}
```

### 3.4. FE rules

- `earned` dung cho section `Cac thanh tich noi bat`
- `upcoming` dung cho section `Cac thanh tich sap dat duoc`
- FE khong tu tinh progress
- FE khong tu suy ra metric tu session history
- `progressPercent` da duoc backend tinh san

---

## 4. Reviews dashboard API

### 4.1. Endpoint

```http
GET /api/reviews/me/dashboard
```

### 4.2. DTOs

```ts
export interface ReviewDto {
  reviewId: string;
  sessionId: string;
  reviewerId: string;
  revieweeId: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface ReviewRatingBreakdownDto {
  rating: number;
  count: number;
}

export interface ReceivedReviewDto {
  reviewId: string;
  sessionId: string;
  rating: number;
  comment: string | null;
  reviewerDisplayName: string;
  reviewerAvatarUrl: string | null;
  createdAt: string;
}

export interface ReviewReceivedSummaryDto {
  avgRating: number;
  totalReviews: number;
  ratingBreakdown: ReviewRatingBreakdownDto[];
  recentReviews: ReceivedReviewDto[];
}

export type ReviewStatus = "can_review" | "already_reviewed" | "window_closed";

export interface ReviewTaskDto {
  sessionId: string;
  revieweeId: string;
  revieweeDisplayName: string;
  revieweeAvatarUrl: string | null;
  skillName: string;
  pricePoints: number;
  description: string | null;
  reviewStatus: ReviewStatus;
  existingReview: ReviewDto | null;
  completedAt: string;
  reviewWindowClosesAt: string;
}

export interface ReviewDashboardDto {
  receivedSummary: ReviewReceivedSummaryDto;
  reviewTasks: ReviewTaskDto[];
}
```

### 4.3. Response meaning

`receivedSummary`:

- mac dinh tong hop review current user nhan duoc khi current user la `companion`
- `recentReviews` da co `reviewerDisplayName` va `reviewerAvatarUrl`

`reviewTasks`:

- la danh sach session `Completed` ma current user la participant
- backend da gan san `reviewStatus`
- `existingReview != null` neu current user da review session do

### 4.4. Review status rules

- `can_review`: chua review va van trong 48h
- `already_reviewed`: current user da review roi
- `window_closed`: qua 48h

FE dung thang `reviewStatus`, khong tu tinh lai.

---

## 5. Create review API

### 5.1. Endpoint

```http
POST /api/reviews
```

### 5.2. Request

```ts
export interface CreateReviewRequest {
  sessionId: string;
  rating: 1 | 2 | 3 | 4 | 5;
  comment?: string | null;
}
```

Example:

```json
{
  "sessionId": "33333333-3333-3333-3333-333333333333",
  "rating": 5,
  "comment": "Rat de hieu"
}
```

### 5.3. Response

Tra ve `ReviewDto`.

### 5.4. Error codes FE nen support

- `SESSION_NOT_FOUND`
- `NOT_SESSION_PARTICIPANT`
- `SESSION_INVALID_STATUS`
- `REVIEW_ALREADY_EXISTS`
- `REVIEW_WINDOW_CLOSED`

### 5.5. FE behavior

- neu `reviewStatus = can_review`: show CTA `Danh gia ngay`
- neu `reviewStatus = already_reviewed`: show state `Da danh gia`
- neu `reviewStatus = window_closed`: disable CTA hoac an form
- sau khi submit review thanh cong:
  - refresh `GET /api/reviews/me/dashboard`
  - hoac optimistic update item do sang `already_reviewed`

Luu y:

- backend hien khong co `+2 Tokens` contract rieng cho action review
- FE khong tu hard-code reward token tren API response

---

## 6. Transactions tab

Backend khong co unified endpoint moi cho tab nay.

FE can ghep 3 API co san:

### 6.1. Wallet summary

```http
GET /api/wallet/points
```

DTO:

```ts
export interface PointWalletSummaryDto {
  balance: number;
  heldBalance: number;
  totalEarned: number;
  totalSpent: number;
}
```

### 6.2. Point transaction history

```http
GET /api/wallet/points/transactions?type=&page=1&limit=20
```

DTO:

```ts
export interface PointTransactionDto {
  pointTransactionId: string;
  type: string;
  amount: number;
  balanceBefore: number;
  balanceAfter: number;
  heldBalanceBefore: number;
  heldBalanceAfter: number;
  sessionId: string | null;
  note: string | null;
  createdAt: string;
}

export interface PointTransactionHistoryDto {
  data: PointTransactionDto[];
  total: number;
  page: number;
  limit: number;
}
```

### 6.3. Payment history

```http
GET /api/wallet/payments?status=&page=1&limit=20
```

DTO:

```ts
export interface PaymentTransactionDto {
  paymentTransactionId: string;
  packageId: string | null;
  packageName: string | null;
  subscriptionPlanId: string | null;
  subscriptionPlanName: string | null;
  provider: string;
  amountVnd: number;
  currency: string;
  status: string;
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
```

### 6.4. FE rendering notes

Tab `Lich su giao dich` nen chia:

1. wallet summary
2. point transaction list
3. payment history list

FE khong assume:

- moi transaction deu la payment
- moi payment deu la point top-up
- co 1 endpoint hop nhat cho tat ca giao dich

---

## 7. Suggested FE service layer

```ts
export const achievementsApi = {
  getMyAchievements: () => api.get<MyAchievementsDto>("/achievements/me"),
};

export const reviewsApi = {
  getMyDashboard: () => api.get<ReviewDashboardDto>("/reviews/me/dashboard"),
  createReview: (payload: CreateReviewRequest) =>
    api.post<ReviewDto>("/reviews", payload),
};

export const transactionApi = {
  getWalletSummary: () => api.get<PointWalletSummaryDto>("/wallet/points"),
  getPointTransactions: (params?: { type?: string; page?: number; limit?: number }) =>
    api.get<PointTransactionHistoryDto>("/wallet/points/transactions", { params }),
  getPayments: (params?: { status?: string; page?: number; limit?: number }) =>
    api.get<PaymentTransactionHistoryDto>("/wallet/payments", { params }),
};
```

---

## 8. UI mapping goi y

### 8.1. Bang thanh tich

- `earned.length === 0`: show empty state
- `upcoming.length > 0`: render progress bar / progress chip tu `progressPercent`

### 8.2. Danh gia

- overview card:
  - `avgRating`
  - `totalReviews`
  - histogram tu `ratingBreakdown`
- task list:
  - render `revieweeDisplayName`, `skillName`, `pricePoints`, `description`
  - CTA dua vao `reviewStatus`

### 8.3. Lich su giao dich

- wallet summary card:
  - `balance`
  - `heldBalance`
  - `totalEarned`
  - `totalSpent`
- list 1:
  - point ledger
- list 2:
  - payment/subscription history

---

## 9. Source of truth trong repo

- `src/EdSkill.API/Controllers/MyAchievementsController.cs`
- `src/EdSkill.API/Controllers/ReviewsController.cs`
- `src/EdSkill.API/Controllers/WalletController.cs`
- `src/EdSkill.API/Controllers/WalletPaymentsController.cs`
- `src/EdSkill.Application/Features/Achievements/DTOs/AchievementDtos.cs`
- `src/EdSkill.Application/Features/Reviews/DTOs/ReviewDtos.cs`
- `src/EdSkill.Application/Features/Wallet/DTOs/WalletDtos.cs`
