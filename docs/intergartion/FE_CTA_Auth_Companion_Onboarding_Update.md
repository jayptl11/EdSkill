# FE Update - CTA Auth + Companion Onboarding Flow

Tai lieu nay viet rieng cho AI/engineer ben FE de tich hop dung flow moi cua backend, khong can suy doan them.

Scope cua tai lieu nay:

- CTA truoc dang nhap: `Hoc ngay` va `Day hoc`
- Dang ky / login / verify OTP theo `signupIntent`
- Learner cu chuyen sang day
- Companion onboarding wizard sau auth
- Gate tao session offer neu profile day chua day du

Tai lieu nay bo sung cho:

- `docs/intergartion/FE_Auth_Role_Onboarding_Integration.md`
- `docs/intergartion/EdSkill_Profile_FE_Integration.md`

Neu co mau thuan, uu tien tai lieu nay cho flow FE moi.

---

## 1. Muc tieu san pham can FE implement

Khong con role picker thu cong trong form dang ky.

Role duoc xac dinh boi CTA truoc dang nhap:

- `Hoc ngay` -> tao/vao flow voi `signupIntent = "learn"`
- `Day hoc` -> tao/vao flow voi `signupIntent = "teach"`

Quy tac bat buoc:

- User di tu CTA `Day hoc` thi sau verify/login thanh cong phai vao companion onboarding wizard ngay.
- Learner cu bam `Day hoc` cung phai vao companion onboarding wizard.
- Wizard day hoc bat buoc:
  - buoc 1: gioi thieu skill muon day qua `skillsToTeach`
  - buoc 2: setup day du profile hien co cua backend, tru `skillsToLearn`
- Co role `companion` chua dong nghia da duoc day.
- Backend chi cho tao session offer khi onboarding day hoc da complete.

---

## 2. Contract FE can dung

### 2.1. Signup intent

```ts
type SignupIntent = "learn" | "teach";
```

### 2.2. Role noi bo backend

Backend map nhu sau:

- `learn` -> `["learner"]`
- `teach` -> `["learner", "companion"]`

FE khong gui `roles` o public auth flow.

### 2.3. Profile onboarding state

`GET /api/profile/me` tra them:

```ts
interface ProfileDto {
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
  roles: ("learner" | "companion" | "admin")[];
  totalSessions: number;
  lastActiveAt: string | null;
  isCompanionOnboardingComplete: boolean;
  missingCompanionProfileFields: string[];
}
```

`missingCompanionProfileFields` chi co the chua cac key sau:

- `displayName`
- `avatarUrl`
- `bio`
- `university`
- `faculty`
- `yearOfStudy`
- `skillsToTeach`
- `isPublic`

`skillsToLearn` khong nam trong onboarding gate.

---

## 3. API FE phai goi

### 3.1. Auth APIs

#### POST `/api/auth/register`

Request:

```json
{
  "email": "user@example.com",
  "username": "user01",
  "firstName": "An",
  "lastName": "Nguyen",
  "password": "Password123",
  "signupIntent": "teach",
  "acceptedPolicies": [
    { "policyType": "terms", "policyVersion": "2026-05-10.v1" },
    { "policyType": "privacy", "policyVersion": "2026-05-10.v1" },
    { "policyType": "points_tokens", "policyVersion": "2026-05-10.v1" }
  ]
}
```

#### POST `/api/auth/verify-otp`

Request:

```json
{
  "email": "user@example.com",
  "otp": "123456"
}
```

#### POST `/api/auth/login`

Request:

```json
{
  "identifier": "user@example.com",
  "password": "Password123"
}
```

Luu y:

- Login email/password hien tai khong nhan `signupIntent`.
- Vi vay FE phai nho user dang den tu CTA nao de redirect sau login.

#### POST `/api/auth/login-google`

Request:

```json
{
  "idToken": "GOOGLE_ID_TOKEN",
  "signupIntent": "teach"
}
```

### 3.2. Profile APIs

#### GET `/api/profile/me`

Dung de:

- lay thong tin profile hien tai
- xac dinh user co role `companion` hay chua
- xac dinh onboarding day hoc da complete hay chua
- lay `missingCompanionProfileFields` de render wizard/chot redirect

#### PATCH `/api/profile/me`

Dung de cap nhat tung buoc trong wizard.

FE co the:

- save moi buoc rieng
- hoac gom roi save cuoi wizard

Backend dang dung PATCH semantics:

- omit field -> giu nguyen
- gui `null` -> clear mot so field
- gui `[]` -> clear danh sach

#### POST `/api/profile/me/enable-companion`

Chi dung cho learner cu khi bam `Day hoc`.

Behavior:

- neu chua co role `companion` -> backend append role
- neu da co role `companion` -> success idempotent
- response tra `ProfileDto`

#### POST `/api/sessions`

Neu profile day hoc chua complete:

- backend tra `422`
- `errorCode = "COMPANION_PROFILE_INCOMPLETE"`

FE khong nen cho user bam CTA tao session neu `isCompanionOnboardingComplete = false`.

---

## 4. Flow 1 - Guest bam `Hoc ngay`

Day la flow cho nguoi chua dang nhap va muon vao vai tro learner.

### 4.1. Entry

- User bam CTA `Hoc ngay`
- FE mo auth page voi state noi bo:

```ts
signupIntent = "learn"
```

Co the luu state nay o:

- query string
- router state
- local state tam thoi

### 4.2. Register moi

- FE submit `POST /api/auth/register` voi `signupIntent = "learn"`
- FE mo man OTP
- FE submit `POST /api/auth/verify-otp`
- verify success xong -> redirect vao learner flow mac dinh

### 4.3. Login account cu

- FE submit `POST /api/auth/login`
- login success xong -> redirect vao learner flow mac dinh

### 4.4. Google login

- FE submit `POST /api/auth/login-google` voi `signupIntent = "learn"`
- success xong -> redirect vao learner flow mac dinh

### 4.5. Redirect rule

Sau auth thanh cong trong nhanh `Hoc ngay`:

- khong ep wizard companion
- vao homepage learner / discovery / dashboard learner tuy routing FE

---

## 5. Flow 2 - Guest bam `Day hoc`

Day la flow quan trong nhat cua update nay.

### 5.1. Entry

- User bam CTA `Day hoc`
- FE mo auth page voi state:

```ts
signupIntent = "teach"
```

### 5.2. Register moi

- FE submit `POST /api/auth/register` voi `signupIntent = "teach"`
- FE mo man OTP
- FE submit `POST /api/auth/verify-otp`

Sau verify success:

- account da co `roles = ["learner", "companion"]`
- FE khong dua user vao dashboard chung
- FE phai redirect vao companion onboarding wizard ngay

### 5.3. Login bang account da ton tai

- FE submit `POST /api/auth/login`
- success xong -> FE van xem day la `teach entry flow`
- FE phai goi `GET /api/profile/me`
- sau do redirect vao wizard neu chua complete

Luu y:

- login khong nhan `signupIntent`
- redirect duoc quyet dinh boi CTA ma user vua bam ben FE

### 5.4. Google login

- FE submit `POST /api/auth/login-google` voi `signupIntent = "teach"`
- neu account moi -> backend tao role `learner + companion`
- success xong -> FE redirect vao wizard

### 5.5. Redirect rule sau auth o nhanh `Day hoc`

Sau khi auth thanh cong:

1. FE lay `ProfileDto` bang `GET /api/profile/me`
2. Neu `isCompanionOnboardingComplete = false`:
   - redirect vao wizard
3. Neu `isCompanionOnboardingComplete = true`:
   - co the vao dashboard companion / page tao session / teaching area

Khuyen nghi:

- du `roles` da co `companion`, FE van nen uu tien `isCompanionOnboardingComplete` de route

---

## 6. Flow 3 - Learner cu bam `Day hoc`

Flow nay danh cho user da dang nhap va hien tai chi dang la learner.

### 6.1. Entry

- User dang login
- User bam CTA `Day hoc`

### 6.2. FE actions bat buoc

1. Goi `POST /api/profile/me/enable-companion`
2. Nhan `ProfileDto` moi
3. Redirect vao companion onboarding wizard

Luu y:

- endpoint nay khong co nghia onboarding da xong
- no chi dam bao account co role `companion`

### 6.3. Redirect rule

Sau `enable-companion`:

- neu `isCompanionOnboardingComplete = false` -> vao wizard
- neu complete san roi -> co the vao teaching area

---

## 7. Companion onboarding wizard FE phai build

Wizard co 2 phan chinh, theo dung yeu cau san pham hien tai.

## 7.1. Buoc 1 - Gioi thieu skill muon day

FE can bat user khai bao:

- `skillsToTeach`

UI text nen theo huong:

- "Ban muon day ky nang gi?"
- "Gioi thieu ky nang ban muon day"

Backend hien tai chi co field:

- `skillsToTeach: string[]`

Nen FE khong can tim field companion-description rieng.

Neu user chua co `skillsToTeach`:

- wizard chua complete
- `missingCompanionProfileFields` se chua `skillsToTeach`

## 7.2. Buoc 2 - Hoan tat profile day hoc

FE phai yeu cau day du cac field sau:

- `displayName`
- `avatarUrl`
- `bio`
- `university`
- `faculty`
- `yearOfStudy`
- `skillsToTeach`
- `isPublic = true`

Khong bat buoc:

- `skillsToLearn`

Dieu nay co nghia:

- user day hoc phai setup profile day du
- nhung co the bo trong phan "muon hoc gi"

## 7.3. Completion rule

Wizard duoc coi la xong khi:

- `isCompanionOnboardingComplete = true`
- va `missingCompanionProfileFields.length === 0`

FE khong nen tu du doan completion bang logic rieng neu da co response nay tu backend.

---

## 8. FE state machine de AI co the code truc tiep

Khuyen nghi mo hinh state:

```ts
type EntryMode = "learn" | "teach" | null;

type TeachingAccessState =
  | "learner_only"
  | "companion_incomplete"
  | "companion_ready";
```

Ham suy ra state:

```ts
function getTeachingAccessState(profile: ProfileDto): TeachingAccessState {
  const hasCompanionRole = profile.roles.includes("companion");

  if (!hasCompanionRole) {
    return "learner_only";
  }

  if (!profile.isCompanionOnboardingComplete) {
    return "companion_incomplete";
  }

  return "companion_ready";
}
```

Router behavior:

- `entryMode = "learn"`:
  - auth success -> learner pages
- `entryMode = "teach"`:
  - auth success -> check profile -> wizard neu incomplete
- logged-in learner bam `Day hoc`:
  - goi `enable-companion`
  - check profile
  - wizard neu incomplete

---

## 9. Man hinh FE can co

Toi thieu FE nen co:

1. CTA entry handling
- nut `Hoc ngay`
- nut `Day hoc`

2. Auth page
- nhan `signupIntent` tu entry state
- register submit kem `signupIntent`
- google login submit kem `signupIntent`

3. OTP verify page
- verify xong route theo entry mode

4. Companion onboarding wizard
- step skills to teach
- step full profile
- save qua `PATCH /api/profile/me`

5. Teaching area guard
- truoc khi vao create session page, doc `ProfileDto`
- neu incomplete -> redirect wizard

---

## 10. Error handling FE nen lam

### 10.1. Auth

- `INVALID_SIGNUP_INTENT`
  - xem nhu bug FE hoac state hu query
  - fallback ve `learn` hoac show error

### 10.2. Companion onboarding

- `COMPANION_PROFILE_INCOMPLETE`
  - xay ra khi user co role `companion` nhung profile chua xong
  - FE redirect user ve wizard
  - goi lai `GET /api/profile/me` de hien field con thieu

### 10.3. Profile

- `PROFILE_NOT_FOUND`
  - coi la blocker
  - show error page hoac logout + retry flow tuy app

---

## 11. Trinh tu tich hop khuyen nghi cho AI FE

1. Them `entryMode/signupIntent` vao state/router auth
2. Sua register flow gui `signupIntent`
3. Sua google login flow gui `signupIntent`
4. Sau auth thanh cong, centralize redirect rule theo `entryMode`
5. Them fetch `GET /api/profile/me` sau auth trong nhanh `Day hoc`
6. Build companion onboarding wizard
7. Them learner-upgrade action `POST /api/profile/me/enable-companion`
8. Guard trang tao session bang `isCompanionOnboardingComplete`
9. Neu API `POST /api/sessions` tra `COMPANION_PROFILE_INCOMPLETE`, redirect nguoc ve wizard

---

## 12. Checklist acceptance cho FE

FE tich hop dung khi cac case sau pass:

- Guest bam `Hoc ngay` -> register/login xong vao learner flow
- Guest bam `Day hoc` -> register/login xong vao wizard ngay
- Learner cu bam `Day hoc` -> duoc enable companion, sau do vao wizard
- Wizard xong -> `GET /api/profile/me` tra `isCompanionOnboardingComplete = true`
- User chua xong wizard khong vao duoc create session flow
- User chua xong wizard ma van goi create session -> FE xu ly `COMPANION_PROFILE_INCOMPLETE` dung
- `skillsToLearn` bo trong van duoc xem la complete neu cac field bat buoc khac da du

