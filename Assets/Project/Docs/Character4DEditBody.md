Dựa trên code đã đọc, đây là cách thay đổi từng phần:

## Cách thay đổi ngoại hình nhân vật

### Qua `Character4D` (API chính)

```csharp
var character4D = GetComponent<Character4D>();
var sprites = character4D.SpriteCollection;
```

### 1. Màu da (Body Color)

```csharp
// Đổi màu da = đổi color trên BodyRenderers + EarsRenderers + HeadRenderer
character4D.SetBody(itemSprite, BodyPart.Body, new Color(0.9f, 0.7f, 0.5f));
```

Hoặc trực tiếp trên từng `Character`:
```csharp
character4D.Parts.ForEach(part => {
    part.BodyRenderers.ForEach(r => r.color = skinColor);
    part.EarsRenderers.ForEach(r => r.color = skinColor);
    part.HeadRenderer.color = skinColor;
});
```

### 2. Tóc (Hair)

```csharp
// Chọn kiểu tóc từ SpriteCollection
var hairSprite = sprites.Hair[index]; // chọn kiểu tóc
character4D.SetBody(hairSprite, BodyPart.Hair, hairColor); // + đổi màu tóc
```

### 3. Khuôn mặt

```csharp
// Mắt
character4D.SetBody(sprites.Eyes[index], BodyPart.Eyes, eyeColor);

// Lông mày
character4D.SetBody(sprites.Eyebrows[index], BodyPart.Eyebrows);

// Miệng
character4D.SetBody(sprites.Mouth[index], BodyPart.Mouth);

// Râu
character4D.SetBody(sprites.Beard[index], BodyPart.Beard, beardColor);

// Tai
character4D.SetBody(sprites.Ears[index], BodyPart.Ears, skinColor);

// Makeup
character4D.SetBody(sprites.Makeup[index], BodyPart.Makeup);
```

### 4. Xóa (set null)

```csharp
// Bỏ râu
character4D.SetBody(null, BodyPart.Beard);

// Bỏ tóc
character4D.SetBody(null, BodyPart.Hair);
```

---

### Tóm tắt API

| Phần | API | Có đổi màu? |
|---|---|---|
| Thân/Da | `SetBody(item, BodyPart.Body, color)` | ✅ color = màu da |
| Đầu | `SetBody(item, BodyPart.Head, color)` | ✅ |
| Tóc | `SetBody(item, BodyPart.Hair, color)` | ✅ color = màu tóc |
| Tai | `SetBody(item, BodyPart.Ears, color)` | ✅ nên = màu da |
| Mắt | `SetBody(item, BodyPart.Eyes, color)` | ✅ color = màu mắt |
| Lông mày | `SetBody(item, BodyPart.Eyebrows)` | ❌ |
| Miệng | `SetBody(item, BodyPart.Mouth)` | ❌ |
| Râu | `SetBody(item, BodyPart.Beard, color)` | ✅ |
| Makeup | `SetBody(item, BodyPart.Makeup, color)` | ✅ |

Tất cả đều đi qua `Character.SetBody()` → cuối cùng gọi `Initialize()` để cập nhật tất cả `SpriteRenderer`. Bạn chỉ cần truyền đúng `ItemSprite` từ `SpriteCollection` + `BodyPart` enum + color (optional).