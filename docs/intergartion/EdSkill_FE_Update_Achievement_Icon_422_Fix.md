# EdSkill FE Update - Fix 422 Khi Upload Icon Achievement

Tai lieu nay la delta nho cho FE sau khi phat hien case upload icon achievement bi `422`.

Muc tieu:

- noi ro FE co phai update gi khong
- noi ro update nao la bat buoc
- dua ra sample flow dung de AI FE lam ngay

Tai lieu nay chi cover:

- `POST /api/admin/achievements/icon-upload-url`
- upload icon tu may local len storage

---

## 1. FE co phai update khong

Co.

Neu FE hien tai dang upload icon achievement ma bi `422`, thi FE phai update it nhat 3 diem:

1. Khong gui `multipart/form-data` vao endpoint `icon-upload-url`
2. Phai gui `application/json` chi chua metadata file
3. Phai upload file binary bang request `PUT` rieng len `uploadUrl` sau khi backend tra ve

---

## 2. Contract dung

### 2.1. Endpoint xin upload URL

```http
POST /api/admin/achievements/icon-upload-url
Authorization: Bearer <admin-token>
Content-Type: application/json
```

Request body:

```json
{
  "fileName": "first-session.jpg",
  "contentType": "image/jpeg",
  "fileSize": 512000
}
```

Response:

```json
{
  "uploadUrl": "https://...",
  "publicUrl": "https://cdn.edskill.test/achievement/....jpg",
  "objectKey": "achievement/...jpg",
  "expiresAt": "2026-05-17T10:15:00Z"
}
```

### 2.2. Request upload file that

Sau khi co `uploadUrl`, FE moi upload file:

```http
PUT <uploadUrl>
Content-Type: image/jpeg
```

Body:

- raw binary cua file

### 2.3. Request tao/sua achievement

Sau khi upload thanh cong, FE lay `publicUrl` va gui vao:

- `POST /api/admin/achievements`
- `PATCH /api/admin/achievements/{achievementId}`

Field can gui:

```json
{
  "iconUrl": "https://cdn.edskill.test/achievement/....jpg"
}
```

---

## 3. FE dang sai o dau

Neu FE dang lam 1 trong cac cach sau, can sua:

### Sai 1: gui `FormData` vao `icon-upload-url`

Sai:

```ts
const formData = new FormData();
formData.append("file", file);

await fetch("/api/admin/achievements/icon-upload-url", {
  method: "POST",
  body: formData,
});
```

Ly do sai:

- backend khong nhan file binary o endpoint nay
- backend chi nhan JSON metadata

### Sai 2: gui sai `contentType`

FE phai gui dung `file.type`.

Gia tri backend ho tro:

- `image/jpeg`
- `image/jpg`
- `image/png`
- `image/webp`

Khong dung:

- `image/svg+xml`
- `application/octet-stream`
- chuoi rong

### Sai 3: file qua lon

Backend dang validate:

- `fileSize > 0`
- `fileSize <= 10MB`

Neu file lon hon `10MB`, FE nen chan o client som.

---

## 4. FE phai update gi cu the

### 4.1. Update service upload icon

FE nen tach thanh 2 ham:

1. `requestAchievementIconUploadUrl(file)`
2. `uploadFileToStorage(uploadUrl, file)`

Khong gop thanh 1 request `multipart/form-data` len backend.

### 4.2. Update validation o client

Truoc khi goi API:

- check `file.name`
- check `file.type`
- check `file.size <= 10 * 1024 * 1024`

Neu fail thi show loi tai client, khong can goi backend.

### 4.3. Update admin create/edit flow

Flow dung:

1. user chon file icon tu may
2. FE validate local
3. FE goi `icon-upload-url`
4. FE `PUT` file len `uploadUrl`
5. FE lay `publicUrl`
6. FE submit create/update achievement voi `iconUrl = publicUrl`

---

## 5. Sample TypeScript implementation

```ts
type AchievementIconUploadUrlDto = {
  uploadUrl: string;
  publicUrl: string;
  objectKey: string;
  expiresAt: string;
};

const ALLOWED_TYPES = new Set([
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/webp",
]);

const MAX_SIZE = 10 * 1024 * 1024;

export function validateAchievementIcon(file: File) {
  if (!file.name) {
    throw new Error("File name is required");
  }

  if (!ALLOWED_TYPES.has(file.type)) {
    throw new Error("Unsupported image type");
  }

  if (file.size <= 0 || file.size > MAX_SIZE) {
    throw new Error("File size must be <= 10MB");
  }
}

export async function requestAchievementIconUploadUrl(
  file: File,
  accessToken: string,
): Promise<AchievementIconUploadUrlDto> {
  validateAchievementIcon(file);

  const response = await fetch("/api/admin/achievements/icon-upload-url", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({
      fileName: file.name,
      contentType: file.type,
      fileSize: file.size,
    }),
  });

  if (!response.ok) {
    throw new Error("Failed to request upload URL");
  }

  return response.json();
}

export async function uploadAchievementIcon(file: File, uploadUrl: string) {
  const response = await fetch(uploadUrl, {
    method: "PUT",
    headers: {
      "Content-Type": file.type,
    },
    body: file,
  });

  if (!response.ok) {
    throw new Error("Failed to upload icon file");
  }
}

export async function prepareAchievementIcon(
  file: File,
  accessToken: string,
): Promise<string> {
  const upload = await requestAchievementIconUploadUrl(file, accessToken);
  await uploadAchievementIcon(file, upload.uploadUrl);
  return upload.publicUrl;
}
```

---

## 6. Sample submit flow

```ts
async function handleCreateAchievement(input: {
  name: string;
  description: string;
  track: "learner" | "companion";
  metric: "completed_sessions" | "completed_hours" | "distinct_completed_learners";
  threshold: number;
  sortOrder: number;
  iconFile?: File | null;
}) {
  let iconUrl: string | null = null;

  if (input.iconFile) {
    iconUrl = await prepareAchievementIcon(input.iconFile, accessToken);
  }

  const response = await fetch("/api/admin/achievements", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({
      name: input.name,
      description: input.description,
      iconUrl,
      track: input.track,
      metric: input.metric,
      threshold: input.threshold,
      sortOrder: input.sortOrder,
    }),
  });

  if (!response.ok) {
    throw new Error("Failed to create achievement");
  }
}
```

---

## 7. Checklist cho AI FE

- Doi request `icon-upload-url` tu `FormData` sang JSON
- Khong gui file binary vao backend app endpoint
- Upload file bang `PUT` len `uploadUrl`
- Luu `publicUrl` vao `iconUrl`
- Validate file type va size o client
- Ho tro `image/jpg` ben canh `image/jpeg`, `image/png`, `image/webp`
- Cho phep file toi da `10MB`

---

## 8. Ket luan ngan

Neu FE dang bi `422`, thi gan nhu chac chan la do FE dang gui sai shape request.

Fix dung la:

- `POST icon-upload-url` bang JSON metadata
- `PUT` file len storage URL
- submit `iconUrl` sau khi upload xong
