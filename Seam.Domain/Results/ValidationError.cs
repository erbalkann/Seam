namespace Seam.Domain.Results;

/// <summary>
/// Tek bir alan (property) için validasyon hatasını temsil eden value object.
/// FluentValidation hataları bu tipe dönüştürülerek Result içinde taşınır.
/// </summary>
/// <param name="PropertyName">Hatanın oluştuğu property adı (ör: "Email").</param>
/// <param name="Message">Kullanıcıya gösterilecek hata mesajı.</param>
public sealed record ValidationError(string PropertyName, string Message);