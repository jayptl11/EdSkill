# Tích hợp FE - Policy, Consent & Quy tắc nền tảng

Tài liệu này hướng dẫn FE tích hợp nhóm API policy/consent vừa được bổ sung cho EdSkill, để AI bên FE có thể triển khai onboarding, policy pages và consent refresh flow mà không cần suy đoán thêm.

---

## 1. Mục tiêu tích hợp

FE cần hỗ trợ 3 việc:

1. Hiển thị nội dung policy public cho người dùng đọc.
2. Gửi log đồng ý policy bắt buộc khi đăng ký tài khoản.
3. Sau khi user đăng nhập, kiểm tra trạng thái consent hiện tại để biết có cần yêu cầu đồng ý lại policy mới hay không.

Lưu ý:

- MVP hiện chỉ **bắt buộc consent** cho:
  - `terms`
  - `privacy`
  - `points_tokens`
- `community guidelines` chỉ là **read-only**, không bắt buộc accept ở MVP.
- Backend hiện **chưa chặn toàn cục** mọi feature khác bằng `POLICY_CONSENT_REQUIRED`, nhưng FE nên chuẩn bị flow sẵn vì status API đã có.

---

## 2. Danh sách Endpoint

### 2.1. Lấy danh sách policy public

```http
GET /api/policies
```

- Auth: không cần token
- Mục đích:
  - lấy danh sách policy active để render trang policy center
  - lấy version hiện hành cho form signup

### 2.2. Lấy chi tiết một policy theo slug

```http
GET /api/policies/{slug}
```

- Auth: không cần token
- Slug hiện có trong seed:
  - `terms`
  - `privacy`
  - `points-tokens`
  - `cancellation-refund`
  - `community-guidelines-learner`
  - `community-guidelines-companion`

### 2.3. Đăng ký tài khoản kèm consent

```http
POST /api/auth/register
```

- Auth: không cần token
- Bắt buộc gửi `acceptedPolicies`

### 2.4. Lấy trạng thái consent của user hiện tại

```http
GET /api/policies/consents/me
```

- Auth: yêu cầu `Bearer Token`
- Dùng sau login / app bootstrap / trước các flow cần policy gate

### 2.5. User đồng ý policy mới nhất sau khi đã có tài khoản

```http
POST /api/policies/consents/me
```

- Auth: yêu cầu `Bearer Token`
- Dùng khi user cũ đăng nhập và cần accept lại version mới

---

## 3. Contract chi tiết

### 3.1. `GET /api/policies`

Response `200 OK`

```json
[
  {
    "slug": "terms",
    "category": "legal",
    "audience": "all",
    "policyType": "terms",
    "version": "2026-05-10.v1",
    "title": "Điều khoản sử dụng nền tảng EdSkill",
    "summary": "Quy định quyền, nghĩa vụ và giới hạn trách nhiệm giữa EdSkill, Learner và Companion.",
    "requiresAcceptance": true,
    "effectiveAt": "2026-05-10T00:00:00Z"
  },
  {
    "slug": "community-guidelines-companion",
    "category": "community",
    "audience": "companion",
    "policyType": "community_guidelines",
    "version": "2026-05-10.v1",
    "title": "Community Guidelines cho Companion",
    "summary": "Quy tắc ứng xử ngắn gọn dành cho Companion khi tạo hồ sơ, nhận phiên và hỗ trợ Learner.",
    "requiresAcceptance": false,
    "effectiveAt": "2026-05-10T00:00:00Z"
  }
]
```

Ý nghĩa field:

- `slug`: dùng để route sang trang detail
- `category`: FE có thể group theo `legal`, `privacy`, `wallet`, `sessions`, `community`
- `audience`:
  - `all`
  - `learner`
  - `companion`
- `policyType`:
  - có giá trị với policy cần mapping vào consent
  - có thể `null` với policy chỉ public/read-only, ví dụ `cancellation-refund`
- `requiresAcceptance`:
  - `true`: policy có thể dùng trong flow consent
  - `false`: chỉ hiển thị cho đọc

### 3.2. `GET /api/policies/{slug}`

Response `200 OK`

```json
{
  "slug": "privacy",
  "category": "privacy",
  "audience": "all",
  "policyType": "privacy",
  "version": "2026-05-10.v1",
  "title": "Chính sách riêng tư dữ liệu cá nhân",
  "summary": "Mô tả cách EdSkill thu thập, lưu trữ và xử lý dữ liệu tài khoản, phiên học và giao dịch.",
  "contentMarkdown": "# Chính sách riêng tư\n\nEdSkill thu thập dữ liệu...",
  "requiresAcceptance": true,
  "effectiveAt": "2026-05-10T00:00:00Z"
}
```

Response `404 Not Found`

```json
{
  "errorCode": "POLICY_DOCUMENT_NOT_FOUND",
  "errorMessage": "Policy document was not found."
}
```

FE note:

- `contentMarkdown` là raw markdown
- FE nên render bằng markdown viewer thay vì plain text

### 3.3. `POST /api/auth/register`

Request body mới:

```json
{
  "email": "user@example.com",
  "username": "minhtran",
  "firstName": "Minh",
  "lastName": "Tran",
  "password": "Password123",
  "roles": ["learner", "companion"],
  "acceptedPolicies": [
    {
      "policyType": "terms",
      "policyVersion": "2026-05-10.v1"
    },
    {
      "policyType": "privacy",
      "policyVersion": "2026-05-10.v1"
    },
    {
      "policyType": "points_tokens",
      "policyVersion": "2026-05-10.v1"
    }
  ]
}
```

Response `200 OK`

```json
{
  "message": "Operation completed successfully"
}
```

Response lỗi có thể gặp:

```json
{
  "errorCode": "POLICY_VERSION_INVALID",
  "errorMessage": "Policy version is not the active version."
}
```

```json
{
  "errorCode": "POLICY_DOCUMENT_NOT_FOUND",
  "errorMessage": "Policy document was not found."
}
```

```json
{
  "errorCode": "EMAIL_EXISTS",
  "errorMessage": "Email already registered"
}
```

Quan trọng:

- FE **không hardcode version**.
- FE phải lấy version active từ `GET /api/policies`, sau đó map sang `acceptedPolicies`.
- Khi user tick checkbox đồng ý, FE gửi đúng version đang active tại thời điểm submit.

### 3.4. `GET /api/policies/consents/me`

Response `200 OK`

```json
{
  "isUpToDate": false,
  "missingRequiredTypes": ["privacy"],
  "requiredPolicies": [
    {
      "policyType": "terms",
      "slug": "terms",
      "title": "Điều khoản sử dụng nền tảng EdSkill",
      "requiredVersion": "2026-05-10.v1",
      "acceptedVersion": "2026-05-10.v1",
      "acceptedAt": "2026-05-10T08:00:00Z",
      "isAcceptedLatest": true
    },
    {
      "policyType": "privacy",
      "slug": "privacy",
      "title": "Chính sách riêng tư dữ liệu cá nhân",
      "requiredVersion": "2026-05-10.v1",
      "acceptedVersion": "2026-05-01.v1",
      "acceptedAt": "2026-05-01T08:00:00Z",
      "isAcceptedLatest": false
    },
    {
      "policyType": "points_tokens",
      "slug": "points-tokens",
      "title": "Chính sách Points và Tokens",
      "requiredVersion": "2026-05-10.v1",
      "acceptedVersion": null,
      "acceptedAt": null,
      "isAcceptedLatest": false
    }
  ]
}
```

Ý nghĩa field:

- `isUpToDate = true`: user đã accept đủ version mới nhất
- `missingRequiredTypes`: list policy bắt buộc còn thiếu hoặc đã cũ
- `requiredPolicies`: dữ liệu chính để render modal/flow re-consent

### 3.5. `POST /api/policies/consents/me`

Request body:

```json
{
  "acceptedPolicies": [
    {
      "policyType": "terms",
      "policyVersion": "2026-05-10.v1"
    },
    {
      "policyType": "privacy",
      "policyVersion": "2026-05-10.v1"
    },
    {
      "policyType": "points_tokens",
      "policyVersion": "2026-05-10.v1"
    }
  ]
}
```

Response thành công:

```http
HTTP/1.1 200 OK
```

Không có response body.

Response lỗi:

```json
{
  "errorCode": "POLICY_VERSION_INVALID",
  "errorMessage": "Policy version is not the active version."
}
```

```json
{
  "errorCode": "POLICY_DOCUMENT_NOT_FOUND",
  "errorMessage": "Policy document was not found."
}
```

```json
{
  "errorCode": "UNSUPPORTED_POLICY_TYPE",
  "errorMessage": "Policy type is not supported."
}
```

Behavior cần biết:

- Submit lại cùng version đã accept trước đó vẫn `200 OK`
- Tức là endpoint có tính idempotent, FE không cần chặn submit lặp quá chặt

---

## 4. Flow triển khai FE đề xuất

### 4.1. Trang / modal policy public

Nên có 1 entrypoint kiểu:

- `/policies`
- `/policies/:slug`

Logic:

1. gọi `GET /api/policies`
2. render danh sách theo nhóm:
   - Legal
   - Privacy
   - Wallet
   - Sessions
   - Community
3. click item thì gọi `GET /api/policies/{slug}` hoặc prefetch trước

### 4.2. Flow signup

Flow khuyến nghị:

1. mở trang signup
2. gọi `GET /api/policies`
3. lọc ra 3 policy bắt buộc:
   - `policyType === "terms"`
   - `policyType === "privacy"`
   - `policyType === "points_tokens"`
4. render 3 link “Đọc điều khoản”, “Chính sách riêng tư”, “Chính sách Points/Tokens”
5. render 1 checkbox tổng hoặc 3 checkbox riêng
6. khi submit:
   - build `acceptedPolicies` từ version active đang có
   - gửi cùng request `POST /api/auth/register`

Khuyến nghị UI:

- Dùng checkbox tổng:
  - “Tôi đồng ý Điều khoản sử dụng, Chính sách riêng tư và Chính sách Points/Tokens của EdSkill.”
- Bên dưới có 3 link mở modal/page detail

### 4.3. Flow sau login

Sau khi login hoặc app bootstrap với token hợp lệ:

1. gọi `GET /api/policies/consents/me`
2. nếu `isUpToDate === true`:
   - không làm gì thêm
3. nếu `isUpToDate === false`:
   - mở modal chặn tương tác chính
   - hiển thị các policy thiếu / outdated
   - cho user đọc detail và accept lại
   - submit qua `POST /api/policies/consents/me`
   - thành công thì gọi lại `GET /api/policies/consents/me` hoặc update local state sang up-to-date

### 4.4. Flow re-consent modal

Modal nên có:

- title: `EdSkill cập nhật chính sách`
- danh sách các policy đang thiếu / đã có version mới
- mỗi item có:
  - title
  - badge `Mới` hoặc `Chưa đồng ý`
  - link `Xem chi tiết`
- checkbox xác nhận
- CTA chính: `Tôi đã đọc và đồng ý`

Payload submit:

- chỉ gửi các item bắt buộc đang cần update
- hoặc gửi full 3 item bắt buộc đều được

Backend đều chấp nhận miễn version hợp lệ.

---

## 5. Mapping UI theo policy slug

FE nên dùng mapping cố định sau:

| Slug | Mục đích hiển thị |
|---|---|
| `terms` | Điều khoản sử dụng |
| `privacy` | Chính sách riêng tư |
| `points-tokens` | Chính sách Points/Tokens |
| `cancellation-refund` | Chính sách hủy phiên / hoàn điểm / no-show |
| `community-guidelines-learner` | Guidelines cho Learner |
| `community-guidelines-companion` | Guidelines cho Companion |

Mapping consent type:

| `policyType` | Dùng trong request consent |
|---|---|
| `terms` | Có |
| `privacy` | Có |
| `points_tokens` | Có |
| `community_guidelines` | Không dùng trong signup/consent MVP |

Lưu ý:

- slug `points-tokens` khác với `policyType = points_tokens`
- FE không được dùng slug thay cho `policyType` trong request consent

---

## 6. Xử lý lỗi FE

### 6.1. `POLICY_VERSION_INVALID`

Tình huống:

- FE đang giữ version cũ
- user mở form lâu, backend đã đổi version active

Xử lý:

1. gọi lại `GET /api/policies`
2. cập nhật version mới nhất
3. hiển thị message:
   - `Chính sách đã được cập nhật. Vui lòng đọc và xác nhận lại trước khi tiếp tục.`

### 6.2. `POLICY_DOCUMENT_NOT_FOUND`

Tình huống:

- cấu hình backend thiếu policy bắt buộc
- hoặc slug/policy version không còn hợp lệ

Xử lý:

- show generic blocking error:
  - `Hiện không thể tải chính sách của hệ thống. Vui lòng thử lại sau.`

### 6.3. `UNSUPPORTED_POLICY_TYPE`

Tình huống:

- FE gửi sai `policyType`

Xử lý:

- coi là lỗi integration
- log ra monitoring/Sentry

### 6.4. `401 Unauthorized`

Áp dụng cho:

- `GET /api/policies/consents/me`
- `POST /api/policies/consents/me`

Xử lý:

- refresh token nếu app đang có cơ chế refresh
- nếu vẫn fail thì logout user về màn hình đăng nhập

---

## 7. Khuyến nghị state management cho FE AI

Nên có 2 lớp state:

### 7.1. Public policy state

- `policyCatalog`
- `policyBySlug`
- `loadingPolicies`

Dùng cho:

- signup page
- policy center
- public footer links

### 7.2. Authenticated consent state

- `consentStatus`
- `isPolicyUpToDate`
- `missingRequiredPolicyTypes`
- `isConsentModalOpen`

Dùng cho:

- app shell sau login
- route guard mềm
- future booking/session gate

---

## 8. Checklist implementation cho AI bên FE

1. Tạo service API:
   - `getPolicies()`
   - `getPolicyBySlug(slug)`
   - `getMyPolicyConsents()`
   - `acceptMyPolicies(payload)`
2. Cập nhật signup payload để thêm `acceptedPolicies`
3. Tạo UI đọc policy:
   - policy list page
   - policy detail modal/page
4. Tạo checkbox đồng ý policy ở signup
5. Sau login, gọi `getMyPolicyConsents()`
6. Nếu consent cũ/thiếu, mở re-consent modal
7. Submit modal bằng `acceptMyPolicies()`
8. Handle các lỗi `POLICY_VERSION_INVALID`, `POLICY_DOCUMENT_NOT_FOUND`, `401`

---

## 9. Gợi ý triển khai nhanh ở FE

Nếu FE dùng React/Next:

- fetch `GET /api/policies` ở signup page load
- build `requiredPolicies = policies.filter(x => x.requiresAcceptance && ["terms", "privacy", "points_tokens"].includes(x.policyType))`
- store `requiredPolicies` vào local state
- submit:

```ts
acceptedPolicies: requiredPolicies.map((x) => ({
  policyType: x.policyType!,
  policyVersion: x.version,
}))
```

Sau login:

```ts
const consentStatus = await getMyPolicyConsents();
if (!consentStatus.isUpToDate) {
  openPolicyConsentModal(consentStatus.requiredPolicies);
}
```

---

## 10. Tóm tắt quyết định tích hợp

- Signup phải gửi consent cho `terms`, `privacy`, `points_tokens`
- Community Guidelines chỉ đọc, chưa accept bắt buộc
- FE phải lấy version active từ backend, không hardcode
- FE nên check consent status sau login để sẵn sàng cho các phase sau
- `points-tokens` là slug, `points_tokens` là policyType request

