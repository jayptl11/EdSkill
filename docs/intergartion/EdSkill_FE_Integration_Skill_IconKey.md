# EdSkill FE Integration - Skill IconKey

Tai lieu nay danh cho FE tich hop phan `iconKey` cua skill catalog sau khi backend da ho tro luu va tra ve ma icon.

Muc tieu:

- FE lay `iconKey` tu backend va map sang icon component trong local icon library.
- FE khong upload file icon.
- FE co the cache danh sach skill public de dung cho skill suggestions duoi search bar.
- FE admin co the tao/sua skill kem `iconKey`.

Quan trong:

- Tai lieu nay KHONG bo dropdown skill/mon o search.
- Dropdown van giu nguyen vai tro de user chon skill.
- Thay doi moi chi la: moi item trong dropdown co them `iconKey` de render icon, va FE co the cache list skill de xu ly `next` o client.

Tai lieu nay cover:

- `GET /api/skills`
- `GET /api/admin/skills`
- `POST /api/admin/skills`
- `PATCH /api/admin/skills/{skillId}`

Neu can business flow companion discovery thi doc them:

- `docs/intergartion/EdSkill_FE_Update_Companion_Search_Online_Only.md`

Neu can delta BE thi doc:

- `docs/api-changes/2026-05-14/skill-icon-key.md`

---

## 1. Tong quan contract

Backend dung field:

- `iconKey`

Y nghia:

- day la ma dinh danh icon do FE quy uoc truoc
- FE dung gia tri nay de map sang icon component
- backend khong tra URL file, khong tra SVG, khong quan ly upload

Vi du gia tri hop le:

- `book-open`
- `calculator`
- `code`
- `languages`
- `paintbrush`
- `music`
- `camera`

Rule backend:

- `iconKey` la optional
- co the la `null`
- neu co gia tri thi backend validate:
  - max `50` ky tu
  - regex `^[a-z0-9]+(?:-[a-z0-9]+)*$`
- backend khong validate allow-list icon

He qua cho FE:

- FE duoc tu do mo rong icon mapping ma khong can backend deploy lai
- FE phai tu bao dam local mapping cho nhung key ma FE muon hien thi

---

## 2. Public skill API cho suggestions

### 2.1. Endpoint

```http
GET /api/skills?q=&category=&limit=
```

### 2.2. Query params

- `q`: optional
- `category`: optional
- `limit`: optional

Rule:

- default `limit` hien tai la `100`
- min `1`
- max `100`

### 2.3. Response 200

```json
[
  {
    "id": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0001",
    "name": "Speaking",
    "slug": "speaking",
    "category": "Communication",
    "iconKey": "languages"
  },
  {
    "id": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0008",
    "name": "Presentation",
    "slug": "presentation",
    "category": "Communication",
    "iconKey": null
  }
]
```

### 2.4. FE use case khuyen nghi

Dung endpoint nay cho:

- skill suggestions duoi search bar
- skill picker/autocomplete
- preload skill list de cache local
- dropdown chon skill/mon trong man hinh search companion

Flow khuyen nghi:

1. FE goi `GET /api/skills?limit=100` khi vao man hinh search companion.
2. Cache list skill tai client.
3. Van render dropdown/combo box chon skill nhu hien tai.
4. Trong dropdown:
   - ban dau co the hien khoang 5 skill dau
   - moi item gom `icon + name`
   - khi user bam `next`, FE tu mo rong them 1 item o client
5. Khi user chon 1 skill:
   - dua `name` len UI search input
   - giu `id` de goi API companion search bang `skillId`
   - dung `iconKey` de render icon

Luu y:

- FE khong can goi API moi moi lan bam `next`
- FE khong nen doi sang search companion bang `skillName`
- FE van phai dung `skillId`

---

## 3. Admin skill APIs

## 3.1. `GET /api/admin/skills`

### Response 200

```json
[
  {
    "id": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0001",
    "name": "Speaking",
    "slug": "speaking",
    "category": "Communication",
    "iconKey": "languages",
    "basePointCost": 100,
    "aliases": ["English", "Tieng Anh"],
    "isActive": true
  }
]
```

### FE dung gi

- render cot icon hoac preview icon trong admin table
- fill default value cho form edit skill

## 3.2. `POST /api/admin/skills`

### Request

```json
{
  "name": "Mock Interview",
  "slug": "mock-interview",
  "category": "Career",
  "iconKey": "camera",
  "basePointCost": 120,
  "aliases": ["Phong van thu"]
}
```

### Response 201

```json
{
  "id": "f50a0bb8-0f76-4c88-a06f-8de03d4f2c6d",
  "name": "Mock Interview",
  "slug": "mock-interview",
  "category": "Career",
  "iconKey": "camera",
  "basePointCost": 120,
  "aliases": ["Phong van thu"],
  "isActive": true
}
```

Behavior:

- omit `iconKey` -> backend luu `null`
- gui `iconKey: null` -> backend luu `null`
- gui `iconKey: "   "` -> backend normalize thanh `null`
- gui `iconKey` hop le -> backend trim va luu

## 3.3. `PATCH /api/admin/skills/{skillId}`

### Request update icon

```json
{
  "iconKey": "paintbrush"
}
```

### Request clear icon

```json
{
  "iconKey": null
}
```

Hoac:

```json
{
  "iconKey": ""
}
```

### Response 200

```json
{
  "id": "8e5a3f2c-8d35-4d83-8e85-a3156a5e0008",
  "name": "Presentation",
  "slug": "presentation",
  "category": "Communication",
  "iconKey": "paintbrush",
  "basePointCost": 140,
  "aliases": ["Presentation", "Thuyet trinh"],
  "isActive": true
}
```

Behavior:

- omit `iconKey` -> khong doi gia tri hien tai
- `iconKey: null` -> clear icon
- `iconKey: ""` hoac whitespace -> clear icon
- `iconKey` hop le -> update icon moi

---

## 4. TypeScript types FE nen dung

```ts
export type SkillIconKey = string | null;

export type SkillSuggestionItem = {
  id: string;
  name: string;
  slug: string;
  category: string | null;
  iconKey: SkillIconKey;
};

export type AdminSkillItem = {
  id: string;
  name: string;
  slug: string;
  category: string | null;
  iconKey: SkillIconKey;
  basePointCost: number;
  aliases: string[];
  isActive: boolean;
};

export type CreateSkillPayload = {
  name: string;
  slug?: string | null;
  category?: string | null;
  iconKey?: string | null;
  basePointCost: number;
  aliases?: string[] | null;
};

export type UpdateSkillPayload = {
  name?: string;
  slug?: string | null;
  category?: string | null;
  iconKey?: string | null;
  basePointCost?: number;
  aliases?: string[] | null;
  isActive?: boolean;
};

export type ValidationErrorResponse = {
  statusCode: 422;
  errorCode: "VALIDATION_ERROR";
  message: string;
  errors: Array<{
    property: string;
    message: string;
    errorCode: string;
  }>;
};
```

---

## 5. Icon mapping o FE

Backend chi tra key, nen FE phai co map local.

Vi du:

```ts
import {
  BookOpen,
  Calculator,
  Code,
  Languages,
  Paintbrush,
  Music,
  Camera,
} from "lucide-react";

export const skillIconMap = {
  "book-open": BookOpen,
  calculator: Calculator,
  code: Code,
  languages: Languages,
  paintbrush: Paintbrush,
  music: Music,
  camera: Camera,
} as const;

export function resolveSkillIcon(iconKey: string | null) {
  if (!iconKey) return null;
  return skillIconMap[iconKey as keyof typeof skillIconMap] ?? null;
}
```

Render rule:

- neu `iconKey` co map -> render icon tu library
- neu `iconKey` la `null` -> render fallback icon hoac khong render
- neu `iconKey` khong nam trong local map -> fallback icon, khong crash UI

---

## 6. FE validation va form behavior

FE nen validate som truoc khi submit admin form.

Rule khuyen nghi:

- trim `iconKey` truoc khi submit
- cho phep empty value
- neu value sau trim la rong:
  - create: co the bo field hoac gui `null`
  - update: co the gui `null` neu user muon xoa icon
- neu co value:
  - max `50`
  - regex `^[a-z0-9]+(?:-[a-z0-9]+)*$`

UI text goi y:

- field label: `Icon key`
- help text: `Nhap ma icon do FE quy uoc, vi du: languages, code, book-open`

Khuyen nghi UX:

- dung text input don gian
- neu FE co danh sach icon co san thi co the dung dropdown hoac combobox
- show preview icon ngay trong form neu local map tim thay component

---

## 7. Error handling FE can map

### 7.1. Validation 422

Vi du:

```json
{
  "statusCode": 422,
  "errorCode": "VALIDATION_ERROR",
  "message": "Validation failed",
  "errors": [
    {
      "property": "IconKey",
      "message": "Skill icon key is invalid",
      "errorCode": "INVALID_SKILL_ICON_KEY"
    }
  ]
}
```

Code FE can gap:

- `INVALID_SKILL_ICON_KEY`
- `INVALID_SKILL_NAME`
- `INVALID_SKILL_SLUG`
- `INVALID_SKILL_CATEGORY`
- `INVALID_SKILL_ALIASES`
- `INVALID_SKILL_BASE_POINTS`
- `INVALID_LIMIT`

### 7.2. Conflict 409

- `SKILL_NAME_EXISTS`
- `SKILL_SLUG_EXISTS`
- `SKILL_ALIAS_CONFLICT`

### 7.3. Not found 404

- `SKILL_NOT_FOUND`

---

## 8. Search page implementation flow

## 8.1. Data loading

FE nen lam:

1. `GET /api/skills?limit=100`
2. luu vao state hoac cache layer
3. tao list suggestion/dropdown tu response do

## 8.2. Render item suggestion

Moi item suggestion nen dung:

- `id`
- `name`
- `iconKey`

Khuyen nghi:

- van dung dropdown/combo box de user chon skill, khong doi sang text input free-form
- hien icon ben trai
- hien `name`
- co the an `slug` va `category` neu UI khong can

## 8.3. Khi user chon skill

FE nen:

1. dua `name` len search input
2. luu `id` vao search state
3. call companion search bang `skillId`

Khong nen:

- chi luu text name ma bo `id`
- goi companion search bang `slug`
- goi companion search bang `iconKey`

---

## 9. Admin page implementation flow

## 9.1. Create form

Field de xuat:

- `name`
- `slug`
- `category`
- `iconKey`
- `basePointCost`
- `aliases`

Flow:

1. Admin nhap `iconKey`
2. FE validate local
3. FE co the preview icon neu tim thay trong local map
4. submit `POST /api/admin/skills`

## 9.2. Edit form

Field giu nguyen nhu create form.

Them behavior:

- nut `Clear icon` -> set `iconKey = null` khi submit
- neu user xoa text trong input va submit:
  - FE co the gui `null`
  - hoac gui `""`
  - backend deu xu ly thanh clear

---

## 10. Checklist FE handoff

1. Them `iconKey` vao types cua public skill va admin skill.
2. Them local `skillIconMap`.
3. Update UI skill suggestions de render icon neu co.
4. Cache `GET /api/skills?limit=100` cho man hinh companion search.
5. Khi user chon skill, giu `skillId` de goi discovery APIs.
6. Them field `iconKey` vao admin create/edit skill form.
7. Them FE validation cho `iconKey`.
8. Them fallback icon/null-safe render de UI khong vo neu key chua duoc map.

---

## 11. Mau implementation nhanh

### 11.1. Fetch public skills

```ts
export async function getSkillSuggestions() {
  const response = await fetch("/api/skills?limit=100");

  if (!response.ok) {
    throw new Error("Failed to load skills");
  }

  return (await response.json()) as SkillSuggestionItem[];
}
```

### 11.2. Build selected skill state

```ts
export type SelectedSkill = {
  skillId: string;
  name: string;
  iconKey: string | null;
};
```

### 11.3. Map sang companion search filter

```ts
const params = new URLSearchParams();
params.set("skillId", selectedSkill.skillId);
```

---

## 12. Tom tat nhanh cho AI FE

AI FE can lam it nhat:

1. Goi `GET /api/skills?limit=100` khi vao man hinh search companion.
2. Render suggestion item voi `name + iconKey`.
3. Cache skill list va xu ly `next` o client.
4. Giu `skillId` khi user chon item.
5. Them `iconKey` vao form tao/sua skill cua admin.
6. Validate `iconKey` bang regex backend dang dung.
7. Fallback an toan khi `iconKey` la `null` hoac chua co trong local map.
