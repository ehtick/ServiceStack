#nullable enable

namespace ServiceStack;

public interface IHasErrorCode
{
    string ErrorCode { get; }
}