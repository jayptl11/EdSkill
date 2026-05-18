# EdSkill Skill Taxonomy FE Integration

Tài liệu này dành cho FE tích hợp phần `Skill Taxonomy cơ bản` đã được implement ở BE.

## 1. Mục tiêu tích hợp

BE hiện hỗ trợ 3 luồng chính:

1. Search skill public để dùng cho autocomplete / picker.
2. Gắn skill vào profile theo 2 loại:
   - `skillsToTeach`
   - `skillsToLearn`
3. Admin quản lý skill catalog:
   - xem danh sách
   - thêm skill
   - sửa skill
   - ẩn/hiện skill bằng `isActive`

Quan trọng:

- FE vẫn gửi profile skills dưới dạng `string[]`.
- BE sẽ tự resolve string đó sang `Skill` canonical qua `name`, `slug`, `aliases`.
- FE không cần gửi `skillId` ở profile flow hiện tại.

---

## 2. Base URL và auth

Base path đang dùng trong code:

- public skill APIs: `/api/skills`
- profile APIs: `/api/profile`
- admin skill APIs: `/api/admin/skills`

Auth:

- `GET /api/skills` là public.
- `GET /api/profile/me`, `PATCH /api/profile/me` cần `Bearer token`.
- toàn bộ `/api/admin/skills` cần `Bearer token` và user phải có role `admin`.

Header mẫu:

```http
Authorization: Bearer <access_token>
Content-Type: application/json
```

---

## 3. Public Skill Search

### 3.1. Endpoint

```http
GET /api/skills?q=&category=&limit=
```

### 3.2. Query params

- `q`: optional, search theo `name`, `slug`, `aliases`, `category`
- `category`: optional, filter chính xác theo category sau normalize
- `limit`: optional, mặc định `20`, min `1`, max `100`

### 3.3. Response 200

```json
[
  {
    "id": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0001",
    "name": "Speaking",
    "slug": "speaking",
    "category": "Communication"
  },
  {
    "id": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0008",
    "name": "Presentation",
    "slug": "presentation",
    "category": "Communication"
  }
]
```

### 3.4. FE behavior khuyến nghị

- Dùng endpoint này cho autocomplete khi user chọn skill.
- Nên debounce 250-400ms.
- Chỉ cho user chọn từ list trả về thay vì nhập free text nếu muốn UX ổn định hơn.
- Có thể cache theo `q` ngắn hoặc preload skill phổ biến bằng `GET /api/skills?limit=20`.

### 3.5. Lưu ý normalize

BE match không phân biệt:

- hoa thường
- khoảng trắng dư
- dấu tiếng Việt

Ví dụ:

- `tieng anh` có thể match alias `Tiếng Anh`
- `speaking` có thể match `Speaking`

---

## 4. Profile Skill Integration

## 4.1. Lấy profile hiện tại

```http
GET /api/profile/me
```

### Response 200

```json
{
  "userId": "d8c0d0a8-3d8c-4d0f-8d13-2b6d8f519001",
  "displayName": "Nguyen Van A",
  "avatarUrl": "https://cdn.example.com/avatar.jpg",
  "bio": "Companion for speaking and CV review",
  "university": "FPT University",
  "faculty": "Software Engineering",
  "yearOfStudy": 4,
  "skillsToTeach": ["Speaking", "CV"],
  "skillsToLearn": ["AI Tools"],
  "isPublic": true,
  "roles": ["learner", "companion"],
  "totalSessions": 0,
  "lastActiveAt": "2026-05-10T08:30:00Z"
}
```

Lưu ý:

- `skillsToTeach` và `skillsToLearn` FE nhận vẫn là `string[]`.
- Đây là canonical names từ catalog, không nhất thiết đúng raw text user từng nhập trước đây.

## 4.2. Update profile skills

```http
PATCH /api/profile/me
```

### Request body tối thiểu để update skills

```json
{
  "skillsToTeach": ["Speaking", "CV"],
  "skillsToLearn": ["AI Tools", "Presentation"]
}
```

### Request body partial update

BE đang support patch theo field nào có mặt trong JSON thì mới update field đó.

Ví dụ chỉ update `skillsToTeach`:

```json
{
  "skillsToTeach": ["Speaking", "Interview"]
}
```

Ví dụ clear list:

```json
{
  "skillsToLearn": []
}
```

### Response 200

Shape giống `GET /api/profile/me`.

---

## 5. Rule FE cần hiểu khi update profile

### 5.1. Skill input là string, nhưng phải map được vào catalog

BE sẽ resolve từng item theo:

- `Skill.Name`
- `Skill.Slug`
- `Skill.Aliases`

Nếu không resolve được:

- trả `404` với `SKILL_NOT_FOUND`

### 5.2. Không nên gửi duplicate canonical skill trong cùng một list

Ví dụ các input sau có thể cùng map về 1 skill:

```json
["Speaking", "speaking", "Tiếng Anh"]
```

Khi đó BE trả:

- `400` với `DUPLICATE_SKILL_SELECTION`

Khuyến nghị FE:

- chỉ cho chọn từ search result
- dedupe client-side theo `name` trước khi submit

### 5.3. Skill inactive không được gắn mới vào profile

Nếu user somehow submit skill đã bị hidden:

- trả `400` với `SKILL_INACTIVE`

### 5.4. Một skill có thể đồng thời nằm ở cả teach và learn

Điều này được phép.

Ví dụ:

```json
{
  "skillsToTeach": ["Speaking"],
  "skillsToLearn": ["Speaking", "AI Tools"]
}
```

---

## 6. Error Handling cho Profile Flow

### 6.1. Business error

Ví dụ:

```json
{
  "errorCode": "SKILL_NOT_FOUND",
  "errorMessage": "Skill was not found."
}
```

Các mã FE cần handle:

- `PROFILE_NOT_FOUND`
- `PROFILE_PRIVATE`
- `SKILL_NOT_FOUND`
- `SKILL_INACTIVE`
- `DUPLICATE_SKILL_SELECTION`

### 6.2. Validation error 422

Khi FluentValidation fail, response có shape:

```json
{
  "statusCode": 422,
  "errorCode": "VALIDATION_ERROR",
  "message": "Validation failed",
  "errors": [
    {
      "property": "SkillsToTeach",
      "message": "Skills to teach are invalid",
      "errorCode": "INVALID_SKILLS_TO_TEACH"
    }
  ]
}
```

Khuyến nghị FE:

- ưu tiên render theo `errors[].property`
- fallback theo `errorCode`

---

## 7. Admin Skill Catalog APIs

## 7.1. Get admin skill list

```http
GET /api/admin/skills?q=&includeInactive=
```

### Response 200

```json
[
  {
    "id": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0001",
    "name": "Speaking",
    "slug": "speaking",
    "category": "Communication",
    "aliases": ["English", "Tiếng Anh", "English Speaking"],
    "isActive": true
  }
]
```

### FE use cases

- admin table
- search/filter skill
- toggle hidden/active state

## 7.2. Create skill

```http
POST /api/admin/skills
```

### Request

```json
{
  "name": "Mock Interview",
  "slug": "mock-interview",
  "category": "Career",
  "aliases": ["Interview Mock", "Phỏng vấn thử"]
}
```

### Response 201

```json
{
  "id": "f50a0bb8-0f76-4c88-a06f-8de03d4f2c6d",
  "name": "Mock Interview",
  "slug": "mock-interview",
  "category": "Career",
  "aliases": ["Interview Mock", "Phỏng vấn thử"],
  "isActive": true
}
```

## 7.3. Update skill

```http
PATCH /api/admin/skills/{skillId}
```

### Request ví dụ rename + hide

```json
{
  "name": "Presentation Skills",
  "aliases": ["Presentation", "Thuyết trình"],
  "isActive": false
}
```

### Response 200

```json
{
  "id": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0008",
  "name": "Presentation Skills",
  "slug": "presentation",
  "category": "Communication",
  "aliases": ["Presentation", "Thuyết trình"],
  "isActive": false
}
```

## 7.4. Delete skill (Soft Delete)

```http
DELETE /api/admin/skills/{skillId}
```

### Request

- Cần truyền `skillId` trên URL path.
- Endpoint thực hiện "soft delete" (chuyển `isDeleted = true` và `isActive = false`), không xóa cứng khỏi database.

### Response 200 OK

```json
// Không có body (trả về status 200 OK)
```

### Response 404 Not Found

```json
{
  "errorCode": "SKILL_NOT_FOUND",
  "errorMessage": "The specified skill does not exist."
}
```

---

## 8. Rule FE cần hiểu ở Admin Flow

- `slug` nếu để trống ở create thì BE tự generate từ `name`
- `PATCH` là partial update, chỉ field có mặt trong JSON mới được đổi
- `isActive = false` là hide mềm, không xóa skill
- skill hidden:
  - không hiện trong public search
  - không gắn mới vào profile được
  - vẫn có thể còn xuất hiện trên profile cũ do historical mapping

---

## 9. Admin Error Handling

### 9.1. Conflict 409

Các mã FE cần handle:

- `SKILL_NAME_EXISTS`
- `SKILL_SLUG_EXISTS`
- `SKILL_ALIAS_CONFLICT`

Ví dụ:

```json
{
  "errorCode": "SKILL_SLUG_EXISTS",
  "errorMessage": "Skill conflicts with an existing skill."
}
```

### 9.2. Not found 404

```json
{
  "errorCode": "SKILL_NOT_FOUND",
  "errorMessage": "Skill was not found."
}
```

### 9.3. Validation 422

Ví dụ:

```json
{
  "statusCode": 422,
  "errorCode": "VALIDATION_ERROR",
  "message": "Validation failed",
  "errors": [
    {
      "property": "Name",
      "message": "Skill name is required",
      "errorCode": "INVALID_SKILL_NAME"
    }
  ]
}
```

Các validation codes có thể gặp:

- `INVALID_SKILL_NAME`
- `INVALID_SKILL_SLUG`
- `INVALID_SKILL_CATEGORY`
- `INVALID_SKILL_ALIASES`
- `INVALID_LIMIT`

---

## 10. FE UX khuyến nghị

## 10.1. Profile screen

- dùng multi-select autocomplete cho `skillsToTeach` và `skillsToLearn`
- search qua `GET /api/skills`
- submit canonical `name` từ option đã chọn
- không nên cho free-text submit ở phase này

### Option shape gợi ý ở FE

```ts
type SkillOption = {
  id: string;
  name: string;
  slug: string;
  category: string | null;
};
```

### Profile payload gợi ý

```ts
type UpdateProfilePayload = {
  skillsToTeach?: string[];
  skillsToLearn?: string[];
};
```

## 10.2. Admin screen

- bảng skill catalog
- filter `includeInactive=true` khi ở admin management page
- form create/edit gồm:
  - `name`
  - `slug`
  - `category`
  - `aliases`
  - `isActive`

---

## 11. Seed data hiện có

BE đã seed 8 skill ban đầu:

- `Speaking` → `Communication`
- `CV` → `Career`
- `Interview` → `Career`
- `Excel` → `Productivity`
- `PowerPoint` → `Productivity`
- `Canva` → `Design`
- `AI Tools` → `AI`
- `Presentation` → `Communication`

FE có thể dựa vào đó để build UI mặc định hoặc smoke test.

---

## 12. Tóm tắt tích hợp nhanh

### Public picker

```http
GET /api/skills?q=spea
```

### Save profile

```http
PATCH /api/profile/me
{
  "skillsToTeach": ["Speaking", "CV"],
  "skillsToLearn": ["AI Tools"]
}
```

### Admin create

```http
POST /api/admin/skills
{
  "name": "Mock Interview",
  "category": "Career",
  "aliases": ["Phỏng vấn thử"]
}
```

### Admin hide

```http
PATCH /api/admin/skills/{skillId}
{
  "isActive": false
}
```
