using System;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Metadata;

public class CustomMetadataHandler
    : BaseMetadataHandler
{
    private new static readonly ILog Log = LogManager.GetLogger(typeof(CustomMetadataHandler));

    public CustomMetadataHandler(string contentType, string format)
    {
        base.ContentType = contentType;
        base.ContentFormat = format;
    }

    public override Format Format => base.ContentFormat.ToFormat();

    protected override string CreateMessage(Type dtoType)
    {
        try
        {
            var requestObj = HostContext.GetPlugin<MetadataFeature>()?.CreateExampleObjectFn(dtoType) 
                ?? AutoMappingUtils.PopulateWith(dtoType.CreateInstance());

            using var ms = MemoryStreamFactory.GetStream();
            HostContext.ContentTypes.SerializeToStreamAsync(
                new BasicRequest { ContentType = this.ContentType }, requestObj, ms).Wait();

            return ms.ReadToEnd();
        }
        catch (Exception ex)
        {
            var error = $"Error serializing type '{dtoType.GetOperationName()}' with custom format '{this.ContentFormat}'";
            Log.Error(error, ex);

            return $"{{Unable to show example output for type '{dtoType.GetOperationName()}' using the custom '{this.ContentFormat}' filter}}{ex.Message}";
        }
    }
}