# EdSkill FE Integration - Profile Dashboard + My Space

Tai lieu nay cover man:

- `Thong tin chung`
- `My Space`
- upload avatar / cover / credential

Tai lieu nay bam theo backend hien tai trong repo. Muc tieu la de AI FE co the code truc tiep ma khong can doan them contract.

---

## 1. Scope backend da co

### 1.1. Profile dashboard

- `GET /api/profile/me`
- `PUT /api/profile/me`
- `POST /api/profile/me/avatar-upload-url`
- `POST /api/profile/me/degree-upload-url`
- `POST /api/profile/me/credential-upload-url`
- `GET /api/profile/{userId}`

### 1.2. My Space

- `GET /api/my-space`
- `POST /api/my-space/companion-cards`
- `PATCH /api/my-space/companion-cards/{cardId}`
- `DELETE /api/my-space/companion-cards/{cardId}`
- `POST /api/my-space/learner-cards`
- `PATCH /api/my-space/learner-cards/{cardId}`
- `DELETE /api/my-space/learner-cards/{cardId}`
- `POST /api/my-space/cover-upload-url`
- `POST /api/my-space/credential-upload-url`

### 1.3. Reuse from existing docs

Tai lieu nay khong thay the:

- `EdSkill_FE_Integration_Companion_Public_Profile_Achievements.md`
- `EdSkill_FE_Integration_Wallet_Points_Subscriptions.md`

No chi cover dashboard profile va My Space cua current user.

---

## 2. Auth rules

Can Bearer token:

- tat ca route `profile/me`
- tat ca route `my-space`

Khong can auth:

- `GET /api/profile/{userId}`

JSON tra ve `camelCase`.

---

## 3. Profile DTO cho FE

```ts
export type UserRole = "learner" | "companion" | "admin";
export type UserGender = "Male" | "Female" | "NonBinary" | "PreferNotToSay";

export interface AchievementSummaryDto {
  achievementId: string;
  name: string;
  description: string;
  iconUrl: string | null;
  awardedAt: string;
}

export interface ProfileSkillDto {
  skillId: string;
  name: string;
  iconKey: string | null;
}

export interface ActiveSubscriptionSummaryDto {
  userSubscriptionId: string;
  planId: string;
  code: string;
  name: string;
  targetRole: string;
  status: string;
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

export interface ProfileDto {
  userId: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  dateOfBirth: string | null;
  phone: string | null;
  gender: UserGender | null;
  socialLinkUrl: string | null;
  degreeUrl: string | null;
  credentialUrls: string[];
  credentialCount: number;
  address: string | null;
  skillsToTeach: string[];
  skillsToLearn: string[];
  teachingSkills: ProfileSkillDto[];
  learningSkills: ProfileSkillDto[];
  achievements: AchievementSummaryDto[];
  isPublic: boolean;
  roles: UserRole[];
  totalSessions: number;
  lastActiveAt: string | null;
  isCompanionOnboardingComplete: boolean;
  missingCompanionProfileFields: string[];
  activeSubscriptions: ActiveSubscriptionSummaryDto[];
  subscriptionEntitlements: ResolvedSubscriptionEntitlementsDto | null;
}
```

### Field notes

- `email` la read-only.
- `address` hien thi duoi avatar hoac khu location tren profile owner page.
- `gender` va `socialLinkUrl` la field moi cho man `Thong tin chung`.
- `skillsToTeach` va `skillsToLearn` van ton tai de backward compatibility.
- `teachingSkills` va `learningSkills` la source dung de lam UI co `skillId`.

---

## 4. Profile update payload

Backend route van la:

```http
PUT /api/profile/me
```

Nhung semantics la partial-update:

- omit field: giu nguyen
- gui `null`: clear field cho cac field nullable
- gui `[]`: clear list

TypeScript payload:

```ts
export interface UpdateMyProfilePayload {
  displayName?: string | null;
  bio?: string | null;
  dateOfBirth?: string | null;
  phone?: string | null;
  gender?: UserGender | null;
  socialLinkUrl?: string | null;
  degreeUrl?: string | null;
  credentialUrls?: string[] | null;
  address?: string | null;
  skillsToTeach?: string[] | null;
  skillsToLearn?: string[] | null;
  avatarUrl?: string | null;
  isPublic?: boolean | null;
}
```

Example:

```json
{
  "displayName": "Tran Hoang",
  "phone": "+84912345678",
  "gender": "Male",
  "socialLinkUrl": "https://linkedin.com/in/tran-hoang",
  "address": "Ha Noi",
  "bio": "Toi day Python va React"
}
```

### Validation FE nen bam

- `displayName`: 2..50, chi chu/so/khoang trang
- `bio`: max 500
- `phone`: 8..20, chi so va `+ - ( ) space`
- `socialLinkUrl`: absolute `http/https`
- `address`: max 200
- `skillsToTeach`, `skillsToLearn`: max 20 item, moi item max 50, khong duplicate
- `credentialUrls`: max 10 URL

---

## 5. Upload APIs cho profile

### 5.1. Avatar

```http
POST /api/profile/me/avatar-upload-url
```

Request:

```json
{
  "fileName": "avatar.png",
  "contentType": "image/png",
  "fileSize": 345678
}
```

### 5.2. Degree / credential

```http
POST /api/profile/me/degree-upload-url
POST /api/profile/me/credential-upload-url
```

Response shape chung:

```ts
export interface UploadUrlDto {
  uploadUrl: string;
  publicUrl: string;
  objectKey: string;
  expiresAt: string;
}
```

Flow bat buoc:

1. FE xin `uploadUrl`.
2. FE `PUT` file raw len `uploadUrl`.
3. Sau khi upload thanh cong, FE luu `publicUrl` vao `avatarUrl`, `degreeUrl`, hoac `credentialUrls`.

FE khong upload binary vao API profile.

---

## 6. My Space DTOs cho FE

```ts
export type SessionDeliveryMode = "Online" | "Offline";

export interface MySpaceSkillDto {
  skillId: string;
  name: string;
  iconKey: string | null;
}

export interface CompanionSpaceCardDto {
  companionSpaceCardId: string;
  skill: MySpaceSkillDto;
  title: string;
  description: string | null;
  pricePoints: number;
  durationMinutes: number;
  deliveryModes: SessionDeliveryMode[];
  languages: string[];
  coverImageUrl: string | null;
  credentialUrls: string[];
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface LearnerSpaceCardDto {
  learnerSpaceCardId: string;
  skill: MySpaceSkillDto;
  title: string;
  description: string | null;
  targetPoints: number;
  durationMinutes: number;
  deliveryModes: SessionDeliveryMode[];
  languages: string[];
  coverImageUrl: string | null;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface MySpaceDto {
  companionCards: CompanionSpaceCardDto[];
  learnerCards: LearnerSpaceCardDto[];
}
```

### GET current My Space

```http
GET /api/my-space
```

Response:

```json
{
  "companionCards": [],
  "learnerCards": []
}
```

Rule:

- endpoint nay chi tra card cua current user
- backend da include `skillId`, `skill.name`, `skill.iconKey`
- FE khong can goi them endpoint khac de resolve skill name

---

## 7. Companion card APIs

### 7.1. Create

```http
POST /api/my-space/companion-cards
```

Request:

```ts
export interface CreateCompanionSpaceCardRequest {
  skillId: string;
  title: string;
  description?: string | null;
  pricePoints: number;
  durationMinutes: 30 | 45 | 60 | 90 | 120;
  deliveryModes: SessionDeliveryMode[];
  languages?: string[] | null;
  coverImageUrl?: string | null;
  credentialUrls?: string[] | null;
  isPublished: boolean;
}
```

Business rules:

- `skillId` phai thuoc tap `teachingSkills` cua current user
- chi user co role `companion` moi tao duoc `companion card`
- `credentialUrls` toi da 4
- `languages` toi da 3

### 7.2. Update

```http
PATCH /api/my-space/companion-cards/{cardId}
```

Semantics:

- omit field: giu nguyen
- gui `null` cho `description` hoac `coverImageUrl`: clear
- gui `[]` cho `languages` hoac `credentialUrls`: clear list

### 7.3. Delete

```http
DELETE /api/my-space/companion-cards/{cardId}
```

Success:

- `200 OK`

---

## 8. Learner card APIs

### 8.1. Create

```http
POST /api/my-space/learner-cards
```

Request:

```ts
export interface CreateLearnerSpaceCardRequest {
  skillId: string;
  title: string;
  description?: string | null;
  targetPoints: number;
  durationMinutes: 30 | 45 | 60 | 90 | 120;
  deliveryModes: SessionDeliveryMode[];
  languages?: string[] | null;
  coverImageUrl?: string | null;
  isPublished: boolean;
}
```

Rule:

- `skillId` phai thuoc tap `learningSkills` cua current user

### 8.2. Update

```http
PATCH /api/my-space/learner-cards/{cardId}
```

### 8.3. Delete

```http
DELETE /api/my-space/learner-cards/{cardId}
```

---

## 9. My Space upload APIs

### 9.1. Cover upload URL

```http
POST /api/my-space/cover-upload-url
```

Allowed:

- `image/jpeg`
- `image/png`
- `image/webp`
- max `5 MB`

### 9.2. Credential upload URL

```http
POST /api/my-space/credential-upload-url
```

Allowed:

- `image/jpeg`
- `image/png`
- `image/webp`
- `application/pdf`
- max `10 MB`

Response:

```ts
export interface MySpaceUploadUrlDto {
  uploadUrl: string;
  publicUrl: string;
  objectKey: string;
  expiresAt: string;
}
```

Flow:

1. FE xin upload URL
2. FE `PUT` file len `uploadUrl`
3. FE luu `publicUrl` vao `coverImageUrl` hoac `credentialUrls`

---

## 10. Error codes FE nen support

### Profile

- `PROFILE_NOT_FOUND`
- `PROFILE_PRIVATE`
- `INVALID_DISPLAY_NAME`
- `INVALID_PHONE`
- `INVALID_SOCIAL_LINK_URL`
- `INVALID_ADDRESS`
- `INVALID_SKILLS_TO_TEACH`
- `INVALID_SKILLS_TO_LEARN`
- `INVALID_AVATAR_URL`
- `INVALID_CREDENTIAL_URLS`
- `INVALID_PROFILE_VISIBILITY`

### My Space

- `MY_SPACE_CARD_NOT_FOUND`
- `MY_SPACE_SKILL_NOT_OWNED`
- `FORBIDDEN`
- `SKILL_INACTIVE`
- `INVALID_DURATION_MINUTES`
- `INVALID_DELIVERY_MODES`
- `INVALID_LANGUAGES`
- `INVALID_COVER_IMAGE_URL`
- `INVALID_CREDENTIAL_URLS`
- `INVALID_MY_SPACE_UPLOAD_CONTENT_TYPE`
- `INVALID_MY_SPACE_UPLOAD_FILE_SIZE`

---

## 11. FE implementation notes

### 11.1. Thong tin chung

Render va edit:

- avatar
- displayName
- dateOfBirth
- phone
- email read-only
- gender
- socialLinkUrl
- address
- bio

### 11.2. My Space

Companion tab:

- source skill dropdown: `profile.teachingSkills`
- skill card item render:
  - cover image
  - title
  - skill name
  - pricePoints
  - durationMinutes
  - deliveryModes
  - languages
  - credential count

Learner tab:

- source skill dropdown: `profile.learningSkills`
- item render:
  - cover image
  - title
  - skill name
  - targetPoints
  - durationMinutes
  - deliveryModes
  - languages

### 11.3. Fallback image

Neu `coverImageUrl == null`:

- FE co the fallback sang `profile.avatarUrl`
- backend khong tu map fallback nay vao response

---

## 12. Suggested FE service layer

```ts
export const profileApi = {
  getMyProfile: () => api.get<ProfileDto>("/profile/me"),
  updateMyProfile: (payload: UpdateMyProfilePayload) =>
    api.put<ProfileDto>("/profile/me", payload),
  getUserProfile: (userId: string) => api.get<ProfileDto>(`/profile/${userId}`),
  createAvatarUploadUrl: (payload: GenerateUploadUrlRequest) =>
    api.post<UploadUrlDto>("/profile/me/avatar-upload-url", payload),
  createDegreeUploadUrl: (payload: GenerateUploadUrlRequest) =>
    api.post<UploadUrlDto>("/profile/me/degree-upload-url", payload),
  createCredentialUploadUrl: (payload: GenerateUploadUrlRequest) =>
    api.post<UploadUrlDto>("/profile/me/credential-upload-url", payload),
};

export const mySpaceApi = {
  getMySpace: () => api.get<MySpaceDto>("/my-space"),
  createCompanionCard: (payload: CreateCompanionSpaceCardRequest) =>
    api.post<CompanionSpaceCardDto>("/my-space/companion-cards", payload),
  updateCompanionCard: (cardId: string, payload: UpdateCompanionSpaceCardRequest) =>
    api.patch<CompanionSpaceCardDto>(`/my-space/companion-cards/${cardId}`, payload),
  deleteCompanionCard: (cardId: string) =>
    api.delete(`/my-space/companion-cards/${cardId}`),
  createLearnerCard: (payload: CreateLearnerSpaceCardRequest) =>
    api.post<LearnerSpaceCardDto>("/my-space/learner-cards", payload),
  updateLearnerCard: (cardId: string, payload: UpdateLearnerSpaceCardRequest) =>
    api.patch<LearnerSpaceCardDto>(`/my-space/learner-cards/${cardId}`, payload),
  deleteLearnerCard: (cardId: string) =>
    api.delete(`/my-space/learner-cards/${cardId}`),
  createCoverUploadUrl: (payload: GenerateUploadUrlRequest) =>
    api.post<MySpaceUploadUrlDto>("/my-space/cover-upload-url", payload),
  createCredentialUploadUrl: (payload: GenerateUploadUrlRequest) =>
    api.post<MySpaceUploadUrlDto>("/my-space/credential-upload-url", payload),
};
```

---

## 13. Source of truth trong repo

- `src/EdSkill.API/Controllers/ProfileController.cs`
- `src/EdSkill.API/Controllers/MySpaceController.cs`
- `src/EdSkill.Application/Features/Profile/DTOs/ProfileDtos.cs`
- `src/EdSkill.Application/Features/MySpace/DTOs/MySpaceDtos.cs`
- `src/EdSkill.Application/Features/Profile/Commands/UpdateMyProfile/UpdateMyProfileCommandValidator.cs`
- `src/EdSkill.Application/Features/MySpace/Commands/*`
