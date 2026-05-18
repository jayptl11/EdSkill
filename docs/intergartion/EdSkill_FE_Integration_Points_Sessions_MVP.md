# EdSkill FE Integration – Points, Wallet, Sessions MVP

Tài liệu này mô tả phần backend vừa triển khai cho:

- ví Points
- lịch sử giao dịch Points
- session offer / booking / confirm / reject / cancel / join / leave / confirm completion
- admin grant points
- admin config cho fee split, signup bonus và rule session

Mục tiêu:

- để FE tích hợp nhanh, ít phải đoán
- để AI agent đọc và hiểu đúng state machine, side effect và error handling
- để thống nhất contract hiện tại theo code đang chạy

Tài liệu này bám theo implementation hiện tại trong repo, không phải spec cũ.

## 1. Tổng quan nghiệp vụ

### 1.1. Thuật ngữ

- `Learner`: người đặt phiên, trả Points
- `Companion`: người mở session offer, nhận Points
- `Points`: đơn vị thanh toán nội bộ
- `heldBalance`: Points đã bị giữ khi Learner book, chưa giải ngân
- `platform fee`: phần Points EdSkill giữ lại trong system ledger

### 1.2. Tỷ lệ hiện tại

Runtime hiện tại seed mặc định:

- `point.signup_bonus = 50`
- `point.platform_fee_pct = 20`
- `session.late_cancel_companion_pct = 80`
- `session.late_cancel_platform_pct = 20`

Nghĩa là:

- session hoàn tất: Learner trả 100, Companion nhận 80, EdSkill thu 20
- learner hủy muộn: mặc định cũng chia 80/20

### 1.3. JSON conventions

- Tất cả enum trả về dạng string
- Tất cả time là UTC ISO-8601
- Tất cả endpoint dưới đây đều yêu cầu `Authorization: Bearer <token>` trừ khi nói khác

---

## 2. Session State Machine

### 2.1. Status values

`SessionStatus`:

- `Available`
- `Pending`
- `Confirmed`
- `InProgress`
- `PendingReview`
- `Completed`
- `Cancelled`
- `Disputed`

### 2.2. State transition thực tế

```text
Companion tạo offer
-> Available

Learner book
-> Pending
-> side effect: balance giảm, heldBalance tăng

Companion confirm
-> Confirmed
-> side effect: tạo jitsiRoomId

Companion reject
-> Cancelled
-> side effect: refund full cho Learner

Learner hoặc Companion cancel
-> Cancelled
-> side effect tùy rule:
   - Companion cancel: refund full
   - Learner cancel khi Pending: refund full
   - Learner early cancel khi Confirmed: refund full
   - Learner late cancel khi Confirmed: learner mất điểm held, Companion nhận 80%, platform nhận 20%

Join session
-> InProgress
-> side effect: set actualStartAt nếu chưa có

Leave session
-> PendingReview nếu actualDuration >= min_duration
-> Disputed nếu actualDuration < min_duration

Confirm completion từ cả hai bên
-> Completed
-> side effect:
   - learner heldBalance giảm
   - companion balance tăng
   - platform ledger tăng
   - totalSessions của Learner + Companion tăng
```

### 2.3. Lưu ý quan trọng cho FE

- `Available` là session offer chưa có Learner
- `Pending` là đã có Learner book và Points đã bị giữ
- `Completed` là đã disburse xong, không chỉ là “cả hai cùng học xong”
- `Disputed` hiện tại dùng khi `actualDuration < session.min_duration_minutes`

---

## 3. Wallet APIs

Base route: `api/wallet/points`

### 3.1. GET `/api/wallet/points`

Mục đích:

- lấy số dư available
- lấy số dư held
- lấy tổng earned / spent

Response `200`:

```json
{
  "balance": 150,
  "heldBalance": 100,
  "totalEarned": 230,
  "totalSpent": 80
}
```

Response lỗi:

- `404 POINT_WALLET_NOT_FOUND`
- `400` fallback business error

### 3.2. GET `/api/wallet/points/transactions`

Query:

- `type`: optional, enum string của `PointTransactionType`
- `page`: default `1`
- `limit`: default `20`

`PointTransactionType`:

- `SignupBonus`
- `Purchase`
- `SessionPayment`
- `SessionEarning`
- `PlatformFee`
- `Refund`
- `AdminGrant`
- `Held`
- `HoldRelease`

Response `200`:

```json
{
  "data": [
    {
      "pointTransactionId": "5f4c2f42-8a37-4ef9-89ad-09c0fb309111",
      "type": "Held",
      "amount": 0,
      "balanceBefore": 150,
      "balanceAfter": 50,
      "heldBalanceBefore": 0,
      "heldBalanceAfter": 100,
      "sessionId": "8d70ff0f-3d4c-4e42-97ea-89eb8e7b1111",
      "note": "Points held for session booking.",
      "createdAt": "2026-05-11T03:22:10.0000000Z"
    }
  ],
  "total": 1,
  "page": 1,
  "limit": 20
}
```

Lưu ý:

- `Held` có `amount = 0`, FE phải đọc thêm `balanceBefore/After` và `heldBalanceBefore/After`
- `SessionPayment` là lúc disburse hoặc late-cancel no-refund, không phải lúc book

---

## 4. Session APIs

Base route: `api/sessions`

## 4.1. Session DTO

Mọi endpoint session hiện trả cùng shape:

```json
{
  "sessionId": "8d70ff0f-3d4c-4e42-97ea-89eb8e7b1111",
  "companionId": "36ee2ab0-7be1-498f-8f76-78b5f7ce1111",
  "learnerId": "5f3aa607-a58f-4f6f-9b43-7ea095711111",
  "skill": "Excel",
  "description": "Basic formulas",
  "durationMinutes": 60,
  "pointCost": 100,
  "scheduledAt": "2026-05-12T13:00:00Z",
  "status": "Pending",
  "jitsiRoomId": "edskill-8d70ff0f3d4c4e4297ea89eb8e7b1111",
  "actualStartAt": null,
  "actualEndAt": null,
  "actualDuration": null,
  "learnerConfirmed": false,
  "companionConfirmed": false,
  "cancelReason": null,
  "cancelledAt": null,
  "disbursedAt": null,
  "createdAt": "2026-05-11T02:00:00Z",
  "updatedAt": "2026-05-11T02:05:00Z"
}
```

## 4.2. POST `/api/sessions`

Role thực tế:

- cần user có role `companion`

Request:

```json
{
  "skill": "Excel",
  "description": "Basic formulas",
  "durationMinutes": 60,
  "pointCost": 100,
  "scheduledAt": "2026-05-12T13:00:00Z"
}
```

Response:

- `201 Created`

Behavior:

- tạo session offer ở trạng thái `Available`
- chưa có Learner

Error hay gặp:

- `403 FORBIDDEN`
- `400 SESSION_LIMIT_REACHED`
- `422 VALIDATION_ERROR`

## 4.3. GET `/api/sessions`

Query:

- `status`: optional
- `role`: optional, `learner` hoặc `companion`
- `page`: default `1`
- `limit`: default `20`

Behavior:

- nếu `role=companion`: chỉ lấy session của current user ở vai trò Companion
- nếu `role=learner`: chỉ lấy session của current user ở vai trò Learner
- nếu không truyền `role`: trả union của:
  - session `Available`
  - session current user là Companion
  - session current user là Learner

Response `200`:

```json
{
  "data": [/* SessionDto[] */],
  "total": 12,
  "page": 1,
  "limit": 20
}
```

Gợi ý FE:

- trang marketplace có thể gọi không truyền `role`, rồi lọc `status=Available`
- trang “my teaching sessions” dùng `role=companion`
- trang “my learning sessions” dùng `role=learner`

## 4.4. GET `/api/sessions/{id}`

Rule truy cập:

- ai cũng xem được nếu session đang `Available`
- ngoài ra chỉ Companion hoặc Learner trong session mới xem được

Errors:

- `404 SESSION_NOT_FOUND`
- `403 FORBIDDEN`

## 4.5. POST `/api/sessions/{id}/book`

Role thực tế:

- cần user có role `learner`

Request body:

- không có body

Behavior:

- chỉ book được session `Available`
- chặn self-booking
- hold Points ngay tại thời điểm book
- set:
  - `learnerId = currentUserId`
  - `status = Pending`

Wallet side effect:

```text
balance -= pointCost
heldBalance += pointCost
PointTransaction(type = Held)
```

Errors:

- `404 SESSION_NOT_FOUND`
- `400 SESSION_NOT_AVAILABLE`
- `400 SELF_BOOKING`
- `400 INSUFFICIENT_POINTS`
- `403 FORBIDDEN`

## 4.6. POST `/api/sessions/{id}/confirm`

Role:

- Companion của session

Behavior:

- chỉ chạy khi `Pending`
- set `status = Confirmed`
- tạo `jitsiRoomId`

Errors:

- `404 SESSION_NOT_FOUND`
- `403 FORBIDDEN`
- `409 SESSION_INVALID_STATUS`

## 4.7. POST `/api/sessions/{id}/reject`

Role:

- Companion của session

Request:

```json
{
  "reason": "Not available"
}
```

Behavior:

- chỉ chạy khi `Pending`
- refund full cho Learner
- set `status = Cancelled`

Wallet side effect:

```text
heldBalance -= pointCost
balance += pointCost
PointTransaction(type = Refund)
```

## 4.8. POST `/api/sessions/{id}/cancel`

Role:

- Learner hoặc Companion của session

Request:

```json
{
  "reason": "Need to reschedule"
}
```

Behavior matrix:

### Case A: Companion cancel

- áp dụng khi current user là `Companion`
- refund full cho Learner
- status -> `Cancelled`

### Case B: Learner cancel khi `Pending`

- refund full
- status -> `Cancelled`

### Case C: Learner cancel khi `Confirmed` và còn trước deadline

- deadline lấy từ `session.cancel_deadline_hours`
- refund full
- status -> `Cancelled`

### Case D: Learner cancel khi `Confirmed` và đã quá deadline

- no refund cho Learner
- held points chuyển thành payment thực
- Companion nhận `session.late_cancel_companion_pct`
- platform ledger nhận `session.late_cancel_platform_pct`
- status -> `Cancelled`

Wallet side effect cho late cancel:

```text
Learner:
  heldBalance -= pointCost
  PointTransaction(type = SessionPayment, amount = -pointCost, note = "cancelled_no_refund")

Companion:
  balance += 80% * pointCost
  PointTransaction(type = SessionEarning)

Platform:
  ledgerBalance += 20% * pointCost
  PointTransaction(type = PlatformFee)
```

Errors:

- `404 SESSION_NOT_FOUND`
- `403 FORBIDDEN`
- `409 SESSION_INVALID_STATUS`

## 4.9. POST `/api/sessions/{id}/join`

Behavior:

- chỉ Companion hoặc Learner trong session gọi được
- chỉ hợp lệ khi `Confirmed` hoặc `InProgress`
- nếu `actualStartAt` chưa có thì set ngay
- set `status = InProgress`

FE note:

- endpoint này hiện chưa trả Jitsi JWT/url
- backend hiện chỉ dùng để log trạng thái tham gia

## 4.10. POST `/api/sessions/{id}/leave`

Request:

```json
{
  "actualDuration": 58
}
```

`actualDuration`:

- optional
- nếu FE không gửi, backend tự tính từ `actualStartAt` đến thời điểm leave

Behavior:

- chỉ hợp lệ khi `InProgress`
- set `actualEndAt`
- set `actualDuration`
- nếu `actualDuration >= session.min_duration_minutes`:
  - `status = PendingReview`
- nếu nhỏ hơn:
  - `status = Disputed`

FE note:

- sau `Disputed`, FE nên chuyển user sang màn hình support / waiting, không hiện CTA confirm completion

## 4.11. POST `/api/sessions/{id}/confirm-completion`

Behavior:

- chỉ Companion hoặc Learner trong session gọi được
- chỉ hợp lệ khi `PendingReview`
- mỗi bên gọi sẽ set cờ riêng:
  - Learner gọi -> `learnerConfirmed = true`
  - Companion gọi -> `companionConfirmed = true`
- khi cả hai cùng `true`:
  - disburse Points
  - update `totalSessions`
  - set `status = Completed`
  - set `disbursedAt`

Wallet side effect khi session hoàn tất:

```text
Learner:
  heldBalance -= pointCost
  totalSpent += pointCost
  PointTransaction(type = SessionPayment, amount = -pointCost)

Companion:
  balance += (100 - point.platform_fee_pct)% * pointCost
  totalEarned += amount
  PointTransaction(type = SessionEarning)

Platform:
  ledgerBalance += point.platform_fee_pct% * pointCost
  PointTransaction(type = PlatformFee)
```

Hiện tại với config mặc định:

- Companion nhận `80`
- EdSkill nhận `20`

Special case:

- nếu session đã `Completed`, endpoint trả success idempotent với dữ liệu hiện tại

## 4.12. GET `/api/sessions/{id}/status`

Response:

```json
{
  "status": "PendingReview",
  "learnerConfirmed": true,
  "companionConfirmed": false
}
```

Nên dùng cho:

- polling nhẹ ở màn hình waiting confirm
- badge / CTA enable-disable

---

## 5. Admin APIs

## 5.1. POST `/api/admin/points/grant`

Role:

- `admin`

Request:

```json
{
  "userIds": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222"
  ],
  "amount": 50,
  "note": "Campaign bonus"
}
```

Response:

```json
{
  "granted": 2
}
```

Side effect:

- mỗi user được cộng `balance`
- tạo `PointTransaction(type = AdminGrant)`

## 5.2. GET `/api/admin/config`

Role:

- `admin`

Response:

```json
[
  {
    "key": "point.signup_bonus",
    "value": "50",
    "description": "Điểm khởi đầu khi đăng ký.",
    "updatedAt": "2026-05-10T00:00:00Z",
    "updatedBy": null
  }
]
```

Các key đang có:

- `point.signup_bonus`
- `point.platform_fee_pct`
- `token.learner_per_session`
- `token.companion_per_session`
- `token.daily_earn_limit`
- `token.weekly_earn_limit`
- `session.min_duration_minutes`
- `session.cancel_deadline_hours`
- `session.late_cancel_companion_pct`
- `session.late_cancel_platform_pct`
- `session.max_per_day_per_companion`
- `session.buffer_minutes`

## 5.3. PATCH `/api/admin/config/{key}`

Request:

```json
{
  "value": "80"
}
```

Rule quan trọng:

- tất cả value hiện đang lưu dạng string
- backend validate range theo key
- riêng:
  - `session.late_cancel_companion_pct`
  - `session.late_cancel_platform_pct`

  luôn phải cộng lại bằng `100`

Ví dụ:

- đổi `point.signup_bonus` từ `50` -> `100`
- đổi `point.platform_fee_pct` từ `20` -> `30`

FE note:

- sau khi admin đổi config payout, FE không nên hardcode 80/20
- nếu cần hiển thị text động, lấy từ config admin hoặc một config cache ở FE

---

## 6. Error Handling cho FE

### 6.1. HTTP mapping hiện tại

- `200`: success
- `201`: create success
- `400`: business invalid input / invalid action / insufficient points
- `403`: không có quyền hoặc không thuộc session
- `404`: resource không tồn tại
- `409`: state conflict, thường là `SESSION_INVALID_STATUS`
- `422`: validation error từ FluentValidation

### 6.2. Error body

Business error:

```json
{
  "errorCode": "INSUFFICIENT_POINTS",
  "errorMessage": "Số điểm không đủ."
}
```

Validation error:

```json
{
  "statusCode": 422,
  "errorCode": "VALIDATION_ERROR",
  "message": "Validation failed",
  "errors": [
    {
      "property": "DurationMinutes",
      "message": "The specified condition was not met for 'Duration Minutes'.",
      "errorCode": "PredicateValidator"
    }
  ]
}
```

### 6.3. Error code FE nên map riêng

- `INSUFFICIENT_POINTS`
- `SELF_BOOKING`
- `SESSION_NOT_AVAILABLE`
- `SESSION_INVALID_STATUS`
- `SESSION_DURATION_INVALID`
- `POINT_WALLET_NOT_FOUND`
- `SYSTEM_CONFIG_NOT_FOUND`
- `SYSTEM_CONFIG_INVALID_VALUE`
- `FORBIDDEN`

---

## 7. FE Flow Gợi Ý

## 7.1. Marketplace / booking flow

```text
1. Companion tạo offer
2. FE learner load list Available
3. Learner chọn offer
4. FE gọi book
5. Nếu success:
   - refresh wallet
   - chuyển sang màn hình session detail
   - hiện status Pending
6. Companion confirm hoặc reject
7. FE learner poll detail/status hoặc refresh list
```

## 7.2. In-session flow

```text
1. Status Confirmed -> hiện CTA Join
2. User Join -> gọi /join
3. FE mở Jitsi room ở client
4. Khi rời phòng -> gọi /leave
5. Nếu status PendingReview -> hiện CTA confirm completion
6. Nếu status Disputed -> hiện thông báo support/manual review
```

## 7.3. Completion flow

```text
1. Learner hoặc Companion gọi /confirm-completion
2. Nếu mới chỉ 1 bên confirm:
   - status vẫn PendingReview
   - chỉ cờ confirm thay đổi
3. Khi cả hai confirm:
   - status = Completed
   - disbursedAt có giá trị
   - FE refresh wallet và transaction history
```

## 7.4. Cancel flow

```text
1. User bấm cancel
2. FE gửi reason optional
3. Nếu success:
   - refresh session detail
   - refresh wallet
   - refresh transactions
4. FE không cần tự tính refund hay split, chỉ đọc dữ liệu trả về + transaction history
```

---

## 8. AI-Friendly Flow Summary

Phần này viết cho AI agent hoặc automation đọc nhanh.

### 8.1. Canonical rules

```yaml
auth:
  all_points_and_sessions_endpoints_require_bearer: true
  admin_endpoints_require_admin_role: true

wallet:
  signup_creates_wallet: true
  signup_bonus_is_config_driven: true
  booking_holds_points_immediately: true
  disbursement_happens_only_on_completed: true

session:
  companion_creates_offer_first: true
  initial_status: Available
  learner_booking_changes_status_to: Pending
  companion_confirm_changes_status_to: Confirmed
  join_changes_status_to: InProgress
  leave_changes_status_to:
    if_duration_gte_min: PendingReview
    else: Disputed
  dual_confirm_completion_changes_status_to: Completed

cancel:
  companion_cancel: full_refund
  learner_cancel_pending: full_refund
  learner_cancel_confirmed_before_deadline: full_refund
  learner_cancel_confirmed_after_deadline: split_80_20

payout:
  completed_session:
    learner_pays: 100_percent
    companion_receives: 80_percent_default
    platform_receives: 20_percent_default
  late_cancel:
    companion_receives: 80_percent_default
    platform_receives: 20_percent_default

special_notes:
  enums_are_strings_in_json: true
  times_are_utc: true
  held_transaction_amount_is_zero: true
  completed_confirm_endpoint_is_idempotent: true
```

### 8.2. AI decision hints

- Nếu cần biết session có còn chờ confirm completion không: đọc `status == PendingReview`
- Nếu cần biết disbursement đã chạy chưa: đọc `status == Completed` hoặc `disbursedAt != null`
- Nếu cần biết refund/full/late-cancel split đã áp dụng ra sao: xem `PointTransactionHistory`
- Nếu AI viết FE state machine, không suy luận payout từ text; ưu tiên lấy config hoặc ledger data

---

## 9. Checklist tích hợp FE

- Thêm typed client cho:
  - wallet
  - sessions
  - admin points
  - admin config
- Map enum string chuẩn, không dùng enum number
- Parse UTC sang local timezone ở UI
- Sau các action `book`, `reject`, `cancel`, `confirm-completion`:
  - refresh session detail
  - refresh wallet summary
  - refresh wallet transactions
- Ở màn hình session:
  - CTA phụ thuộc `status` + current role
- Ở màn hình lịch sử points:
  - hiển thị cả available delta và held delta
- Không hardcode `80/20` trong UI nếu màn admin config có thể đổi

---

## 10. Known Gaps / Scope hiện tại

- Chưa có endpoint trả Jitsi JWT hoặc room URL, mới có `jitsiRoomId`
- Chưa có token earning flow dù config token đã tồn tại
- Chưa có auto-complete sau 24h, current flow cần người dùng bấm confirm completion
- Chưa có notification API riêng cho các state change này

Nếu FE cần, bước tiếp theo hợp lý là viết thêm:

- doc cho quyền hiển thị CTA theo role + status
- OpenAPI/JSON examples đầy đủ
- contract mock JSON cho frontend local development
