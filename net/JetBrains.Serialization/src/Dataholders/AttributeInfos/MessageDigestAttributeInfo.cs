using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Attribute = Org.BouncyCastle.Asn1.Cms.Attribute;
using JetBrains.Serialization;

[JsonObject(MemberSerialization.OptIn)]
public class MessageDigestAttributeInfo : AttributeInfo
{
  [JsonProperty("Identifier")] protected override TextualInfo Identifier { get; }

  [JsonProperty("Content")] private List<TextualInfo> _content;

  [JsonConstructor]
  public MessageDigestAttributeInfo(TextualInfo identifier, List<TextualInfo> content)
  {
    Identifier = identifier;
    _content = content;
  }

  public MessageDigestAttributeInfo(Attribute attribute)
  {
    Identifier = TextualInfo.GetInstance(attribute.AttrType);
    _content = attribute.AttrValues.ToArray().Select(TextualInfo.GetInstance).ToList();
  }

  public override Asn1Encodable GetPrimitiveContent() => _content.ToPrimitiveDerSet();
}