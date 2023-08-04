using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509.Store;
using Attribute = Org.BouncyCastle.Asn1.Cms.Attribute;

namespace JetBrains.Serialization;

[JsonObject(MemberSerialization.Fields)]
public class CounterSignatureInfo : IEncodableInfo
{
  public int Version { get; }
  public SignerIdentifierInfo Sid { get; }
  public AlgorithmInfo DigestAlgorithm { get; }
  public List<AttributeInfo> AuthenticatedAttributes { get; }
  public AlgorithmInfo DigestEncryptionAlgorithm { get; }
  public TextualInfo EncryptedDigest { get; }
  public TaggedObjectInfo CounterSignature { get; }

  public CounterSignatureInfo(
    int version,
    SignerIdentifierInfo sid,
    AlgorithmInfo digestAlgorithm,
    List<AttributeInfo> authenticatedAttributes,
    AlgorithmInfo digestEncryptionAlgorithm,
    TextualInfo encryptedDigest,
    TaggedObjectInfo counterSignature = null)
  {
    Version = version;
    Sid = sid;
    DigestAlgorithm = digestAlgorithm;
    AuthenticatedAttributes = authenticatedAttributes;
    DigestEncryptionAlgorithm = digestEncryptionAlgorithm;
    EncryptedDigest = encryptedDigest;
    CounterSignature = counterSignature;
  }

  public static CounterSignatureInfo GetInstance(DerSequence sequence)
  {
    var iterator = sequence.GetEnumerator();
    iterator.MoveNext();

    var version = ((DerInteger)iterator.Current).Value.IntValue;
    iterator.MoveNext();

    var signerSequence = ((DerSequence)iterator.Current).ToArray();

    var signerIdentifierInfo = new SignerIdentifierInfo(
      X509Name.GetInstance(signerSequence[0]),
      (DerInteger)signerSequence[1]
    );
    iterator.MoveNext();

    var digestAlgorithm = new AlgorithmInfo(AlgorithmIdentifier.GetInstance(iterator.Current));
    iterator.MoveNext();

    var attributes = ((DerSequence)((DerTaggedObject)iterator.Current).GetObject())
      .ToArray()
      .Select(it => AttributeInfo.GetInstance(Attribute.GetInstance(it)))
      .ToList();
    iterator.MoveNext();

    var encryptionAlgorithm = new AlgorithmInfo(AlgorithmIdentifier.GetInstance(iterator.Current));
    iterator.MoveNext();

    var encryptedDigest = TextualInfo.GetInstance((Asn1Encodable)iterator.Current);

    TaggedObjectInfo? counterSignature = null;
    if (iterator.MoveNext())
    {
      var obj = (DerTaggedObject)iterator.Current;
      counterSignature = new TaggedObjectInfo(
        obj.IsExplicit(),
        obj.TagNo,
        AttributeInfo.GetInstance(Attribute.GetInstance(obj.GetObject())));
    }

    return new CounterSignatureInfo(
      version,
      signerIdentifierInfo,
      digestAlgorithm,
      attributes,
      encryptionAlgorithm,
      encryptedDigest,
      counterSignature);
  }

  public Asn1Encodable ToPrimitive()
  {
    var primitiveValues = new List<Asn1Encodable>
    {
      new DerInteger(Version),
      Sid.ToPrimitive(),
      DigestAlgorithm.ToPrimitive(),
      TaggedObjectInfo.GetTaggedObject(false, 0, AuthenticatedAttributes.ToPrimitiveDerSet()),
      DigestEncryptionAlgorithm.ToPrimitive(),
      EncryptedDigest.ToPrimitive()
    };

    if (CounterSignature != null)
    {
      primitiveValues.Add(CounterSignature.ToPrimitive());
    }

    return primitiveValues.ToDerSequence();
  }
}