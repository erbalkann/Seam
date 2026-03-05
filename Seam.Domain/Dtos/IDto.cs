namespace Seam.Domain.Dtos;

/// <summary>
/// Tüm DTO (Data Transfer Object) tiplerinin marker sözleşmesi.
/// Bu interface hiçbir member tanımlamaz; yalnızca semantik
/// bir etiket görevi görür.
///
/// Kullanım amaçları:
///   1. Generic kısıtlarda DTO olduğunu derleme zamanında garantilemek
///      (ör: where TDto : IDto)
///   2. Reflection / kayıt mekanizmalarında DTO tiplerini otomatik
///      taramak (ör: AutoMapper profilleri, MediatR handler kayıtları)
///   3. Result&lt;TData&gt; ile birlikte kullanımda tip güvenliği sağlamak
///      (ör: Result&lt;TDto&gt; where TDto : IDto)
/// </summary>
public interface IDto;