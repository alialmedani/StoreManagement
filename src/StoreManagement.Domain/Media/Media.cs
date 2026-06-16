using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Media;

public class Media : FullAuditedAggregateRoot<Guid>
{
    public string EntityId { get; private set; } = string.Empty;

    public MediaEntityType EntityType { get; private set; }

    public string FilePlacement { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string BlobName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long Size { get; private set; }

    protected Media()
    {
    }

    public Media(
        Guid id,
        string entityId,
        MediaEntityType entityType,
        string filePlacement,
        string fileName,
        string originalFileName,
        string blobName,
        string contentType,
        long size)
        : base(id)
    {
        SetEntity(
            entityId,
            entityType
        );

        SetFilePlacement(filePlacement);

        SetFileInfo(
            fileName,
            originalFileName,
            blobName,
            contentType,
            size
        );
    }

    private void SetEntity(
        string entityId,
        MediaEntityType entityType)
    {
        EntityId = Check.NotNullOrWhiteSpace(
            entityId,
            nameof(entityId),
            MediaConsts.MaxEntityIdLength
        );

        if (!Enum.IsDefined(typeof(MediaEntityType), entityType) ||
            entityType == MediaEntityType.Unknown)
        {
            throw new BusinessException(
                "StoreManagement:MediaEntityTypeInvalid"
            );
        }

        EntityType = entityType;
    }

    private void SetFilePlacement(string filePlacement)
    {
        FilePlacement = Check.NotNullOrWhiteSpace(
            filePlacement,
            nameof(filePlacement),
            MediaConsts.MaxFilePlacementLength
        );
    }

    private void SetFileInfo(
        string fileName,
        string originalFileName,
        string blobName,
        string contentType,
        long size)
    {
        FileName = Check.NotNullOrWhiteSpace(
            fileName,
            nameof(fileName),
            MediaConsts.MaxFileNameLength
        );

        OriginalFileName = Check.NotNullOrWhiteSpace(
            originalFileName,
            nameof(originalFileName),
            MediaConsts.MaxFileNameLength
        );

        BlobName = Check.NotNullOrWhiteSpace(
            blobName,
            nameof(blobName),
            MediaConsts.MaxFileNameLength
        );

        ContentType = Check.NotNullOrWhiteSpace(
            contentType,
            nameof(contentType),
            MediaConsts.MaxContentTypeLength
        );

        if (size <= 0)
        {
            throw new BusinessException(
                "StoreManagement:MediaFileEmpty"
            );
        }

        if (size > MediaConsts.MaxFileSizeInBytes)
        {
            throw new BusinessException(
                    "StoreManagement:MediaFileTooLarge"
                )
                .WithData(
                    "MaxFileSizeInBytes",
                    MediaConsts.MaxFileSizeInBytes
                );
        }

        Size = size;
    }
}