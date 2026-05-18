# EdSkill Profile FE Integration Guide

Tài liệu này mô tả cách FE tích hợp với backend cho phần:

- Hồ sơ cá nhân
- Companion Profile cơ bản
- Upload avatar qua Cloudflare R2 presigned URL

Tài liệu viết để một AI agent phía FE có thể code trực tiếp mà không cần suy đoán thêm.

---

## 1. Scope đã có ở backend

Backend đã implement các capability sau:

- Lấy hồ sơ của user hiện tại
- Cập nhật hồ sơ của user hiện tại
- Tạo presigned upload URL để upload avatar trực tiếp lên R2
- Enable role `companion` cho learner cũ
- Xem hồ sơ công khai của user khác

Các field đã hỗ trợ:

- `displayName`
- `avatarUrl`
- `bio`
- `university`
- `faculty`
- `yearOfStudy`
- `skillsToTeach`
- `skillsToLearn`
- `isPublic`
- `roles`
- `totalSessions`
- `lastActiveAt`
- `isCompanionOnboardingComplete`
- `missingCompanionProfileFields`

`roles`, `totalSessions`, `lastActiveAt` là field read-only từ góc nhìn FE.

---

## 2. Base conventions cho FE

### 2.1. Base URL

Giả sử API dùng prefix:

```ts
const API_BASE_URL = "<backend-origin>/api";
```

### 2.2. Auth

Các route sau cần JWT Bearer:

- `GET /api/profile/me`
- `PATCH /api/profile/me`
- `POST /api/profile/me/avatar-upload-url`
- `POST /api/profile/me/enable-companion`

Header:

```http
Authorization: Bearer <access_token>
Content-Type: application/json
```

Route public:

- `GET /api/profile/{userId}`

### 2.3. JSON casing

ASP.NET đang trả JSON `camelCase`.

Ví dụ `DisplayName` trong backend sẽ thành `displayName` ở FE.

---

## 3. TypeScript contracts

FE nên dùng các type sau.

```ts
export type UserRole = "learner" | "companion" | "admin";

export interface ProfileDto {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  university: string | null;
  faculty: string | null;
  yearOfStudy: number | null;
  skillsToTeach: string[];
  skillsToLearn: string[];
  isPublic: boolean;
  roles: UserRole[];
  totalSessions: number;
  lastActiveAt: string | null;
  isCompanionOnboardingComplete: boolean;
  missingCompanionProfileFields: string[];
}

export interface AvatarUploadUrlDto {
  uploadUrl: string;
  publicUrl: string;
  objectKey: string;
  expiresAt: string;
}

export interface GenerateAvatarUploadUrlRequest {
  fileName: string;
  contentType: "image/jpeg" | "image/png" | "image/webp";
  fileSize: number;
}

export interface UpdateMyProfilePayload {
  displayName?: string | null;
  bio?: string | null;
  university?: string | null;
  faculty?: string | null;
  yearOfStudy?: number | null;
  skillsToTeach?: string[] | null;
  skillsToLearn?: string[] | null;
  avatarUrl?: string | null;
  isPublic?: boolean | null;
}
```

---

## 4. API contract chi tiết

### 4.1. Get current profile

```http
GET /api/profile/me
Authorization: Bearer <token>
```

Response `200 OK`:

```json
{
  "userId": "3d4c2b0d-1f4d-4ac1-a8f4-9a50b38d7b44",
  "displayName": "John Doe",
  "avatarUrl": "https://cdn.example.com/avatar/.../avatar.jpg",
  "bio": "I teach C# and SQL",
  "university": "FPT University",
  "faculty": "Software Engineering",
  "yearOfStudy": 4,
  "skillsToTeach": ["C#", "SQL"],
  "skillsToLearn": ["React"],
  "isPublic": true,
  "roles": ["learner", "companion"],
  "totalSessions": 0,
  "lastActiveAt": "2026-05-10T05:30:21.0000000Z",
  "isCompanionOnboardingComplete": true,
  "missingCompanionProfileFields": []
}
```

Possible errors:

- `401` nếu chưa auth
- `404 PROFILE_NOT_FOUND`

### 4.2. Update current profile

```http
PATCH /api/profile/me
Authorization: Bearer <token>
Content-Type: application/json
```

#### Important PATCH semantics

Backend đang dùng semantics sau:

- Omit field: giữ nguyên giá trị cũ
- Gửi `null` cho một số field: clear field
- Gửi `[]`: clear danh sách

Điểm quan trọng cho FE:

- Nếu user không sửa field nào, đừng gửi field đó
- Nếu user bấm clear `bio`, gửi `"bio": null`
- Nếu user xóa hết kỹ năng, gửi `"skillsToTeach": []` hoặc `null`
- Nếu user đổi visibility, gửi `"isPublic": true/false`
- Không gửi `"isPublic": null`
- Không gửi `"displayName": null`

#### Valid payload examples

Chỉ update text:

```json
{
  "displayName": "Phạm Long",
  "bio": "Mình dạy C# căn bản"
}
```

Update school info:

```json
{
  "university": "FPT University",
  "faculty": "Software Engineering",
  "yearOfStudy": 4
}
```

Update skills:

```json
{
  "skillsToTeach": ["C#", "ASP.NET Core"],
  "skillsToLearn": ["React", "Docker"]
}
```

Clear fields:

```json
{
  "bio": null,
  "avatarUrl": null,
  "skillsToLearn": []
}
```

Toggle public profile:

```json
{
  "isPublic": false
}
```

Response `200 OK`: trả lại `ProfileDto` mới nhất.

Possible errors:

- `401`

### 4.3. Enable companion cho learner cu

```http
POST /api/profile/me/enable-companion
Authorization: Bearer <token>
```

Response `200 OK`: tra `ProfileDto`.

Luu y cho FE:

- Endpoint nay chi append role `companion` neu user chua co.
- Endpoint khong co nghia la user da hoan tat wizard day hoc.
- FE phai doc `isCompanionOnboardingComplete` + `missingCompanionProfileFields` de ep user di tiep qua wizard.

### 4.4. Companion onboarding gate

Backend coi onboarding day hoc la chua xong neu thieu bat ky field nao trong:

- `displayName`
- `avatarUrl`
- `bio`
- `university`
- `faculty`
- `yearOfStudy`
- `skillsToTeach`
- `isPublic`

`skillsToLearn` khong nam trong gate nay.
- `404 PROFILE_NOT_FOUND`
- `422 VALIDATION_ERROR`
- `400` nếu có business error khác

### 4.3. Get public profile by userId

```http
GET /api/profile/{userId}
```

Response `200 OK`: `ProfileDto`

Possible errors:

- `404 PROFILE_NOT_FOUND`
- `403 PROFILE_PRIVATE`

FE nên xử lý `403 PROFILE_PRIVATE` như một state riêng:

- không coi là crash
- render empty-state kiểu: "Người dùng này đang để hồ sơ ở chế độ riêng tư"

### 4.4. Generate avatar upload URL

```http
POST /api/profile/me/avatar-upload-url
Authorization: Bearer <token>
Content-Type: application/json
```

Request:

```json
{
  "fileName": "my-avatar.png",
  "contentType": "image/png",
  "fileSize": 345678
}
```

Response `200 OK`:

```json
{
  "uploadUrl": "https://<presigned-url>",
  "publicUrl": "https://<public-cdn>/avatar/<userId>/<file>",
  "objectKey": "avatar/<userId>/<guid>-my-avatar.png",
  "expiresAt": "2026-05-10T05:45:00.0000000Z"
}
```

Possible errors:

- `401`
- `422 VALIDATION_ERROR`

---

## 5. Validation rules FE phải bám

FE nên validate sớm để tránh roundtrip thừa.

### 5.1. `displayName`

- required nếu field này được submit
- sau khi trim: từ `2` đến `50` ký tự
- chỉ chấp nhận chữ, số và dấu cách
- không chấp nhận ký tự đặc biệt như `@`, `#`, `%`, `_`, `-`

Recommendation:

- trim trước khi submit
- không cho nhập leading/trailing spaces

### 5.2. `bio`

- max `500` chars

### 5.3. `university`

- max `200` chars

### 5.4. `faculty`

- max `200` chars

### 5.5. `yearOfStudy`

- `null` hoặc số nguyên từ `1` đến `6`

FE UI suggestion:

- dùng select `1..6`
- thêm option `Chưa cập nhật`

### 5.6. `skillsToTeach`, `skillsToLearn`

- tối đa `20` item mỗi list
- mỗi skill sau khi trim: không rỗng
- max `50` ký tự / skill
- không được duplicate, backend check case-insensitive

FE recommendation:

- normalize skill bằng `.trim()`
- chặn duplicate theo lowercase
- render dạng chips/tag input

### 5.7. Avatar upload

Allowed MIME types:

- `image/jpeg`
- `image/png`
- `image/webp`

Max file size:

- `5 MB`

FE phải check trước khi gọi API.

---

## 6. Error response format

### 6.1. Validation error

Khi payload sai validator, backend trả:

- status `422`
- top-level `errorCode = "VALIDATION_ERROR"`

Shape:

```json
{
  "statusCode": 422,
  "errorCode": "VALIDATION_ERROR",
  "message": "Validation failed",
  "errors": [
    {
      "property": "DisplayName",
      "message": "Display name must be at least 2 characters",
      "errorCode": "INVALID_DISPLAY_NAME"
    }
  ]
}
```

FE nên:

- map `errors[]` vào form field errors
- ưu tiên dùng `errorCode` cho logic nếu cần
- fallback dùng `message`

### 6.2. Business error

Shape:

```json
{
  "errorCode": "PROFILE_PRIVATE",
  "errorMessage": "This profile is private."
}
```

Các error code hiện dùng:

- `PROFILE_NOT_FOUND`
- `PROFILE_PRIVATE`
- `INVALID_DISPLAY_NAME`
- `INVALID_BIO`
- `INVALID_UNIVERSITY`
- `INVALID_FACULTY`
- `INVALID_YEAR_OF_STUDY`
- `INVALID_SKILLS_TO_TEACH`
- `INVALID_SKILLS_TO_LEARN`
- `INVALID_AVATAR_URL`
- `INVALID_AVATAR_FILE_NAME`
- `INVALID_AVATAR_CONTENT_TYPE`
- `INVALID_AVATAR_FILE_SIZE`
- `INVALID_PROFILE_VISIBILITY`
- `VALIDATION_ERROR`

---

## 7. Avatar upload flow bắt buộc

FE không upload avatar vào backend API.

FE phải dùng flow 3 bước:

### Step 1. Xin presigned URL

Call:

```ts
const uploadMeta = await api.post<AvatarUploadUrlDto>(
  "/profile/me/avatar-upload-url",
  {
    fileName: file.name,
    contentType: file.type,
    fileSize: file.size,
  }
);
```

### Step 2. Upload trực tiếp file lên `uploadUrl`

Call `PUT` tới `uploadUrl`.

Important:

- không gửi Bearer token cho `uploadUrl`
- body là raw file
- `Content-Type` phải đúng bằng `contentType` đã xin URL

Ví dụ:

```ts
await fetch(uploadMeta.uploadUrl, {
  method: "PUT",
  headers: {
    "Content-Type": file.type,
  },
  body: file,
});
```

Nếu upload lỗi:

- không gọi `PATCH /profile/me`
- báo lỗi upload cho user

### Step 3. Save `publicUrl` vào profile

Sau khi upload thành công:

```ts
await api.patch<ProfileDto>("/profile/me", {
  avatarUrl: uploadMeta.publicUrl,
});
```

### Clear avatar

Để xóa avatar:

```ts
await api.patch<ProfileDto>("/profile/me", {
  avatarUrl: null,
});
```

Backend hiện chưa delete object cũ trên R2 khi user đổi avatar. FE không cần xử lý việc xóa object storage.

---

## 8. FE state model đề xuất

### 8.1. Profile edit form model

```ts
export interface ProfileFormValues {
  displayName: string;
  bio: string;
  university: string;
  faculty: string;
  yearOfStudy: number | null;
  skillsToTeach: string[];
  skillsToLearn: string[];
  isPublic: boolean;
  avatarUrl: string | null;
}
```

### 8.2. Transform API -> form

```ts
function toProfileFormValues(profile: ProfileDto): ProfileFormValues {
  return {
    displayName: profile.displayName ?? "",
    bio: profile.bio ?? "",
    university: profile.university ?? "",
    faculty: profile.faculty ?? "",
    yearOfStudy: profile.yearOfStudy,
    skillsToTeach: profile.skillsToTeach ?? [],
    skillsToLearn: profile.skillsToLearn ?? [],
    isPublic: profile.isPublic,
    avatarUrl: profile.avatarUrl,
  };
}
```

### 8.3. Transform form -> PATCH payload

FE nên gửi **partial payload**, không gửi full object nếu field không đổi.

```ts
function buildProfilePatch(
  current: ProfileFormValues,
  initial: ProfileFormValues
): UpdateMyProfilePayload {
  const payload: UpdateMyProfilePayload = {};

  if (current.displayName.trim() !== initial.displayName.trim()) {
    payload.displayName = current.displayName.trim();
  }

  if (current.bio.trim() !== initial.bio.trim()) {
    payload.bio = current.bio.trim() || null;
  }

  if (current.university.trim() !== initial.university.trim()) {
    payload.university = current.university.trim() || null;
  }

  if (current.faculty.trim() !== initial.faculty.trim()) {
    payload.faculty = current.faculty.trim() || null;
  }

  if (current.yearOfStudy !== initial.yearOfStudy) {
    payload.yearOfStudy = current.yearOfStudy;
  }

  if (JSON.stringify(current.skillsToTeach) !== JSON.stringify(initial.skillsToTeach)) {
    payload.skillsToTeach = current.skillsToTeach;
  }

  if (JSON.stringify(current.skillsToLearn) !== JSON.stringify(initial.skillsToLearn)) {
    payload.skillsToLearn = current.skillsToLearn;
  }

  if (current.isPublic !== initial.isPublic) {
    payload.isPublic = current.isPublic;
  }

  if (current.avatarUrl !== initial.avatarUrl) {
    payload.avatarUrl = current.avatarUrl;
  }

  return payload;
}
```

---

## 9. Rendering rules cho UI

### 9.1. Profile owner view

`GET /profile/me`

Render:

- avatar
- display name
- bio
- university / faculty / yearOfStudy
- skills to teach
- skills to learn
- role badges từ `roles`
- total sessions
- last active
- profile visibility toggle

### 9.2. Public profile view

`GET /profile/{userId}`

Render read-only:

- avatar
- display name
- bio
- school info
- `skillsToTeach`
- `skillsToLearn`
- `roles`
- `totalSessions`
- `lastActiveAt`

### 9.3. Role badge display

Backend trả `roles: string[]`.

FE mapping:

- `learner` -> `Learner`
- `companion` -> `Companion`
- `admin` -> `Admin`

Không dùng `Mentor`, `Tutor`, `Teacher`, `Coach`.

---

## 10. Suggested FE service layer

```ts
export const profileApi = {
  getMyProfile: () => api.get<ProfileDto>("/profile/me"),

  updateMyProfile: (payload: UpdateMyProfilePayload) =>
    api.patch<ProfileDto>("/profile/me", payload),

  getUserProfile: (userId: string) =>
    api.get<ProfileDto>(`/profile/${userId}`),

  createAvatarUploadUrl: (payload: GenerateAvatarUploadUrlRequest) =>
    api.post<AvatarUploadUrlDto>("/profile/me/avatar-upload-url", payload),
};
```

Recommended helper:

```ts
export async function uploadAvatar(file: File): Promise<string> {
  const meta = await profileApi.createAvatarUploadUrl({
    fileName: file.name,
    contentType: file.type as "image/jpeg" | "image/png" | "image/webp",
    fileSize: file.size,
  });

  const uploadResponse = await fetch(meta.uploadUrl, {
    method: "PUT",
    headers: {
      "Content-Type": file.type,
    },
    body: file,
  });

  if (!uploadResponse.ok) {
    throw new Error("Avatar upload failed");
  }

  return meta.publicUrl;
}
```

---

## 11. UX recommendations

### 11.1. Save profile

- disable submit button khi không có thay đổi
- optimistic update không bắt buộc
- sau `PATCH` thành công, replace local cache bằng response server

### 11.2. Avatar UX

- preview local image trước upload
- validate type/size trước khi gọi backend
- show upload progress nếu tiện
- chỉ save `avatarUrl` sau khi upload R2 thành công

### 11.3. Public/private profile

- dùng switch hoặc segmented control
- label rõ:
  - `Public profile`
  - `Private profile`

### 11.4. Last active display

`lastActiveAt` là UTC ISO string.

FE nên format theo local timezone user.

Ví dụ:

- `Hoạt động 5 phút trước`
- hoặc absolute time nếu không có relative formatter

---

## 12. Known backend behaviors FE phải biết

- `PATCH /profile/me` không merge list item-level; backend replace toàn bộ `skillsToTeach` hoặc `skillsToLearn` nếu field đó được gửi lên
- `avatarUrl` phải là URL public thuộc R2 base URL đã cấu hình; FE không được submit arbitrary external image URL
- `totalSessions` hiện đang là field read-only; chưa có endpoint profile nào cho FE sửa
- `lastActiveAt` hiện được update qua auth flows như login / refresh token

---

## 13. Integration checklist cho AI FE

AI phía FE nên làm theo checklist này:

1. Tạo `profileApi` service với 4 API ở trên.
2. Tạo `ProfileDto`, `UpdateMyProfilePayload`, `AvatarUploadUrlDto`.
3. Tạo page hoặc section `My Profile`.
4. Khi mount page, call `GET /profile/me`.
5. Bind dữ liệu vào form edit.
6. Validate client-side theo rules ở mục 5.
7. Khi user đổi avatar:
   - validate file
   - call `POST /profile/me/avatar-upload-url`
   - `PUT` file lên `uploadUrl`
   - set `avatarUrl = publicUrl` vào form state
8. Khi save:
   - build partial PATCH payload
   - call `PATCH /profile/me`
   - replace cache/form initial state bằng response mới
9. Tạo public profile page dùng `GET /profile/{userId}`.
10. Handle `403 PROFILE_PRIVATE` như private-state UI.

---

## 14. Source of truth trong repo

FE team hoặc AI FE nên bám trực tiếp các file này nếu cần verify:

- [ProfileController.cs](D:\EdSkill\EdSkill\src\EdSkill.API\Controllers\ProfileController.cs)
- [ProfileDtos.cs](D:\EdSkill\EdSkill\src\EdSkill.Application\Features\Profile\DTOs\ProfileDtos.cs)
- [UpdateMyProfileCommandValidator.cs](D:\EdSkill\EdSkill\src\EdSkill.Application\Features\Profile\Commands\UpdateMyProfile\UpdateMyProfileCommandValidator.cs)
- [GenerateAvatarUploadUrlCommandValidator.cs](D:\EdSkill\EdSkill\src\EdSkill.Application\Features\Profile\Commands\GenerateAvatarUploadUrl\GenerateAvatarUploadUrlCommandValidator.cs)
- [EdSkill_Profile_FE_Integration.md](D:\EdSkill\EdSkill\docs\EdSkill_Profile_FE_Integration.md)

---

## 15. Update 2026-05-16 - Owned skill objects for session create

Doc delta create-session chi tiet:

- `docs/intergartion/EdSkill_FE_Update_Create_Session_Online_Only_Owned_Skills.md`

Backend da them 2 field additive vao `ProfileDto`:

- `teachingSkills`
- `learningSkills`

Moi item co shape:

```ts
export interface ProfileSkillDto {
  skillId: string;
  name: string;
  iconKey: string | null;
}
```

FE note:

- Van giu `skillsToTeach` va `skillsToLearn` la `string[]` de backward compatibility.
- Man `Create Session` phai dung `teachingSkills` lam source of truth cho dropdown ky nang.
- Khi submit `POST /api/sessions`, FE phai gui `skillId` tu `teachingSkills[i].skillId`, khong duoc tu map nguoc tu text name.

Vi du response moi:

```json
{
  "skillsToTeach": ["Speaking"],
  "skillsToLearn": ["React"],
  "teachingSkills": [
    {
      "skillId": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0001",
      "name": "Speaking",
      "iconKey": "languages"
    }
  ],
  "learningSkills": [
    {
      "skillId": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0002",
      "name": "React",
      "iconKey": "code"
    }
  ]
}
```
