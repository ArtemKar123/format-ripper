using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;

namespace JetBrains.Serialization;

[JsonObject(MemberSerialization.OptIn)]
public class IdCtTSTInfo : EncapContentInfo
{
  [JsonProperty("ContentType")] protected override TextualInfo ContentType { get; }

  [JsonProperty("Content")] public TextualInfo Content { get; }

  [JsonConstructor]
  public IdCtTSTInfo(TextualInfo contentType, TextualInfo content)
  {
    ContentType = contentType;
    Content = content;
  }

  public IdCtTSTInfo(ContentInfo contentInfo)
    : this(TextualInfo.GetInstance(contentInfo.ContentType), TextualInfo.GetInstance(contentInfo.Content))
  {
  }

  protected override Asn1Encodable GetContentPrimitive() => Content.ToPrimitive();
}