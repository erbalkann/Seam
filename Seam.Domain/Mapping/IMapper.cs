namespace Seam.Domain.Mapping;

using Seam.Domain.Dtos;
using Seam.Domain.Entities;

/// <summary>
/// Nesne dönüşüm (mapping) işlemleri için temel sözleşme.
/// Generic kısıtlar sayesinde yalnızca IEntity ve IDto
/// tiplerinin kullanılması derleme zamanında garanti edilir.
///
/// Desteklenen yönler:
///   1. Entity  → Dto                    (tekil okuma)
///   2. Dto     → Entity                 (tekil yazma)
///   3. IEnumerable&lt;Entity&gt; → IEnumerable&lt;Dto&gt;  (liste okuma)
///   4. IEnumerable&lt;Dto&gt;    → IEnumerable&lt;Entity&gt; (liste yazma)
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Tek bir Entity'yi Dto'ya dönüştürür. (Okuma senaryosu)
    /// </summary>
    TDto ToDto<TEntity, TId, TDto>(TEntity entity)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        where TDto : class, IDto;

    /// <summary>
    /// Tek bir Dto'yu Entity'ye dönüştürür. (Yazma senaryosu)
    /// </summary>
    TEntity ToEntity<TDto, TEntity, TId>(TDto dto)
        where TDto : class, IDto
        where TEntity : class, IEntity<TId>
        where TId : notnull;

    /// <summary>
    /// Entity koleksiyonunu Dto koleksiyonuna dönüştürür. (Liste okuma)
    /// </summary>
    IEnumerable<TDto> ToDtoList<TEntity, TId, TDto>(
        IEnumerable<TEntity> entities)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        where TDto : class, IDto;

    /// <summary>
    /// Dto koleksiyonunu Entity koleksiyonuna dönüştürür. (Liste yazma)
    /// </summary>
    IEnumerable<TEntity> ToEntityList<TDto, TEntity, TId>(
        IEnumerable<TDto> dtos)
        where TDto : class, IDto
        where TEntity : class, IEntity<TId>
        where TId : notnull;
}