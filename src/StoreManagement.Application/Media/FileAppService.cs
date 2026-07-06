using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.Content;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace StoreManagement.Media;

public class FileAppService :
    ApplicationService,
    IFileAppService
{
    private readonly IRepository<Media, Guid> _mediaRepository;

    private readonly IBlobContainer<StoreManagementMediaContainer>
        _blobContainer;

    public FileAppService(
        IRepository<Media, Guid> mediaRepository,
        IBlobContainer<StoreManagementMediaContainer> blobContainer)
    {
        _mediaRepository = mediaRepository;
        _blobContainer = blobContainer;
    }

    [Authorize(StoreManagementPermissions.File.Upload)]
    [Consumes("multipart/form-data")]
    public async Task<MediaDto> UploadAsync(
        [FromForm] UploadMediaDto input)
    {
        if (input.File == null)
        {
            throw new BusinessException(
                "StoreManagement:MediaFileRequired"
            );
        }

        if (input.File.Length <= 0)
        {
            throw new BusinessException(
                "StoreManagement:MediaFileEmpty"
            );
        }

        if (input.File.Length > MediaConsts.MaxFileSizeInBytes)
        {
            throw new BusinessException(
                    "StoreManagement:MediaFileTooLarge"
                )
                .WithData(
                    "MaxFileSizeInBytes",
                    MediaConsts.MaxFileSizeInBytes
                );
        }

        var originalFileName =
            Path.GetFileName(input.File.FileName);

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            originalFileName = "file";
        }

        var extension =
            Path.GetExtension(originalFileName);

        var fileName =
            $"{GuidGenerator.Create():N}{extension}";

        var blobName =
            fileName;

        var contentType =
            string.IsNullOrWhiteSpace(input.File.ContentType)
                ? "application/octet-stream"
                : input.File.ContentType;

        await using (var stream = input.File.OpenReadStream())
        {
            await _blobContainer.SaveAsync(
                blobName,
                stream,
                overrideExisting: false
            );
        }

        var media = new Media(
            GuidGenerator.Create(),
            input.EntityId,
            input.EntityType,
            input.FilePlacement,
            fileName,
            originalFileName,
            blobName,
            contentType,
            input.File.Length
        );

        await _mediaRepository.InsertAsync(
            media,
            autoSave: true
        );

        return MapToDto(media);
    }

    [Authorize(StoreManagementPermissions.File.Download)]
    public async Task<IRemoteStreamContent> GetAsync(
        Guid id)
    {
        var media =
            await _mediaRepository.GetAsync(id);

        var stream =
            await _blobContainer.GetAsync(media.BlobName);

        return new RemoteStreamContent(
            stream,
            media.OriginalFileName,
            media.ContentType
        );
    }

    [Authorize(StoreManagementPermissions.File.Download)]
    public async Task<IRemoteStreamContent> GetByFileNameAsync(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new BusinessException(
                "StoreManagement:MediaFileNameRequired"
            );
        }

        var query =
            await _mediaRepository.GetQueryableAsync();

        var media =
            await AsyncExecuter.FirstOrDefaultAsync(
                query.Where(media =>
                    media.FileName == fileName.Trim())
            );

        if (media == null)
        {
            throw new EntityNotFoundException(
                typeof(Media),
                fileName
            );
        }

        var stream =
            await _blobContainer.GetAsync(media.BlobName);

        return new RemoteStreamContent(
            stream,
            media.OriginalFileName,
            media.ContentType
        );
    }

    [Authorize(StoreManagementPermissions.File.Download)]
    public async Task<IRemoteStreamContent> GetByEntityAsync(
        string entityId,
        MediaEntityType entityType)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new BusinessException(
                "StoreManagement:MediaEntityIdRequired"
            );
        }

        if (entityType == MediaEntityType.Unknown)
        {
            throw new BusinessException(
                "StoreManagement:MediaEntityTypeInvalid"
            );
        }

        var query =
            await _mediaRepository.GetQueryableAsync();

        var media =
            await AsyncExecuter.FirstOrDefaultAsync(
                query.Where(m =>
                    m.EntityId == entityId.Trim() &&
                    m.EntityType == entityType)
                .OrderByDescending(m => m.CreationTime)
            );

        if (media == null)
        {
            throw new EntityNotFoundException(
                typeof(Media),
                $"EntityId: {entityId}, EntityType: {entityType}"
            );
        }

        var stream =
            await _blobContainer.GetAsync(media.BlobName);

        return new RemoteStreamContent(
            stream,
            media.OriginalFileName,
            media.ContentType
        );
    }

    [Authorize(StoreManagementPermissions.File.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var media =
            await _mediaRepository.GetAsync(id);

        await _blobContainer.DeleteAsync(
            media.BlobName
        );

        await _mediaRepository.DeleteAsync(
            media,
            autoSave: true
        );
    }

    private static MediaDto MapToDto(Media media)
    {
        return new MediaDto
        {
            Id = media.Id,
            EntityId = media.EntityId,
            EntityType = media.EntityType,
            FilePlacement = media.FilePlacement,
            FileName = media.FileName,
            OriginalFileName = media.OriginalFileName,
            BlobName = media.BlobName,
            ContentType = media.ContentType,
            Size = media.Size,

            CreationTime = media.CreationTime,
            CreatorId = media.CreatorId,

            LastModificationTime =
                media.LastModificationTime,

            LastModifierId =
                media.LastModifierId,

            IsDeleted = media.IsDeleted,
            DeleterId = media.DeleterId,
            DeletionTime = media.DeletionTime
        };
    }
}