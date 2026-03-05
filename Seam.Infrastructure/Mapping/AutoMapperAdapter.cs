namespace Seam.Infrastructure.Mapping;

using Seam.Domain.Dtos;
using Seam.Domain.Entities;

/// <summary>
/// IMapper sözleşmesinin AutoMapper implementasyonu.
/// AutoMapper'ı doğrudan uygulama katmanına sızdırmaz —
/// IMapper arayüzü üzerinden soyutlanır.
///
/// Convention-based mapping varsayılan olarak çalışır:
/// aynı isimli property'ler otomatik eşleşir.
/// Özel mapping ihtiyacı varsa tüketen proje kendi
/// MapperConfiguration'ını yazar — Seam'e dokunmaz.
/// </summary>
public sealed class AutoMapperAdapter(AutoMapper.IMapper autoMapper)
    : Domain.Mapping.IMapper
{
    /// <inheritdoc />
    public TDto ToDto<TEntity, TId, TDto>(TEntity entity)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        where TDto : class, IDto
        => autoMapper.Map<TDto>(entity);

    /// <inheritdoc />
    public TEntity ToEntity<TDto, TEntity, TId>(TDto dto)
        where TDto : class, IDto
        where TEntity : class, IEntity<TId>
        where TId : notnull
        => autoMapper.Map<TEntity>(dto);

    /// <inheritdoc />
    public IEnumerable<TDto> ToDtoList<TEntity, TId, TDto>(
        IEnumerable<TEntity> entities)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        where TDto : class, IDto
        => autoMapper.Map<IEnumerable<TDto>>(entities);

    /// <inheritdoc />
    public IEnumerable<TEntity> ToEntityList<TDto, TEntity, TId>(
        IEnumerable<TDto> dtos)
        where TDto : class, IDto
        where TEntity : class, IEntity<TId>
        where TId : notnull
        => autoMapper.Map<IEnumerable<TEntity>>(dtos);
}