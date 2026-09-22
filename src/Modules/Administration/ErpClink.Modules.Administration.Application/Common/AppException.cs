using ErpClink.BuildingBlocks.Application.Common;

namespace ErpClink.Modules.Administration.Application.Common;

/// <summary>Backward-compatible alias; prefer BuildingBlocks AppException.</summary>
public sealed class AppException : ErpClink.BuildingBlocks.Application.Common.AppException
{
    public AppException(string code, string message, int statusCode = 400)
        : base(code, message, statusCode)
    {
    }
}
