using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Attribute = Org.BouncyCastle.Asn1.Cms.Attribute;

namespace JetBrains.Serialization;

[JsonObject(MemberSerialization.OptIn)]
public class SignatureTimeStampAttributeInfo : AttributeInfo
{
  [JsonProperty("Identifier")] protected override TextualInfo Identifier { get; }

  [JsonProperty("Content")] private List<RSASignedDataInfo> _content;

  [JsonConstructor]
  public SignatureTimeStampAttributeInfo(TextualInfo identifier, List<RSASignedDataInfo> content)
  {
    Identifier = identifier;
    _content = content;
  }

  public SignatureTimeStampAttributeInfo(Attribute attribute)
    : this(TextualInfo.GetInstance(attribute.AttrType),
      attribute.AttrValues.ToArray().Select(av => RSASignedDataInfo.GetInstance(av as DerSequence)).ToList())
  {
  }

  public override Asn1Encodable GetPrimitiveContent() =>
    _content.ToPrimitiveDerSet();
}