using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace StoreManagement.Media;

public interface IFileAppService : IApplicationService
{
    Task<MediaDto> UploadAsync(
        UploadMediaDto input
    );

    Task<IRemoteStreamContent> GetAsync(
        Guid id
    );

    Task<IRemoteStreamContent> GetByFileNameAsync(
        string fileName
    );

    Task DeleteAsync(
        Guid id
    );
}