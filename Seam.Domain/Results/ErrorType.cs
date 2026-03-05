/// <summary>
/// Hata kategorilerini temsil eden enum.
/// API katmanında HTTP status code'a dönüştürmek için kullanılır.
/// </summary>
public enum ErrorType
{
    /// <summary>400 — İstek geçersiz veya eksik parametre içeriyor.</summary>
    BadRequest,

    /// <summary>401 — Kimlik doğrulama gerekiyor veya başarısız.</summary>
    Unauthorized,

    /// <summary>403 — Kimlik doğrulandı fakat yetki yetersiz.</summary>
    Forbidden,

    /// <summary>404 — İstenen kaynak bulunamadı.</summary>
    NotFound,

    /// <summary>409 — Kaynak zaten mevcut veya çakışma var.</summary>
    Conflict,

    /// <summary>422 — İş kuralı / domain validasyon hatası.</summary>
    Validation,

    /// <summary>500 — Beklenmeyen sistem hatası.</summary>
    InternalError
}