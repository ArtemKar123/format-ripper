using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Attribute = Org.BouncyCastle.Asn1.Cms.Attribute;
using JetBrains.Serialization;
using Org.BouncyCastle.Asn1.X509;

[JsonObject(MemberSerialization.Fields)]
public class MsCounterSignatureInfo : IEncodableInfo
{
  public int Version { get; }
  public List<AlgorithmInfo> Algorithms { get; }
  public TSTInfo TstInfo { get; }
  public TaggedObjectInfo TaggedCertificateInfo { get; }
  public List<CounterSignatureInfo> CounterSignatures { get; }

  public MsCounterSignatureInfo(
    int version,
    List<AlgorithmInfo> algorithms,
    TSTInfo tstInfo,
    TaggedObjectInfo taggedCertificateInfo,
    List<CounterSignatureInfo> counterSignatures)
  {
    Version = version;
    Algorithms = algorithms;
    TstInfo = tstInfo;
    TaggedCertificateInfo = taggedCertificateInfo;
    CounterSignatures = counterSignatures;
  }

  public static MsCounterSignatureInfo GetInstance(DerSequence sequence)
  {
    var enumerator = sequence.GetEnumerator();

    enumerator.MoveNext();
    var version = ((DerInteger)enumerator.Current).Value.IntValue;

    enumerator.MoveNext();
    var algorithms = ((DerSet)enumerator.Current)
      .OfType<DerSequence>()
      .ToList()
      .Select(sequence => new AlgorithmInfo(AlgorithmIdentifier.GetInstance(sequence)))
      .ToList();

    enumerator.MoveNext();
    var tstInfo = new TSTInfo((DerSequence)enumerator.Current);

    enumerator.MoveNext();
    var certificateSequence = (DerSequence)((DerTaggedObject)enumerator.Current).GetObject();
    var certificateInfos = certificateSequence
      .OfType<DerSequence>()
      .Select(CertificateInfo.GetInstance)
      .ToList();

    TaggedObjectInfo certificateInfoTagged = new TaggedObjectInfo(
      ((DerTaggedObject)enumerator.Current).IsExplicit(),
      ((DerTaggedObject)enumerator.Current).TagNo,
      new SequenceInfo(
        ((DerSequence)((DerTaggedObject)enumerator.Current).GetObject()).ToArray()
        .Select(it => CertificateInfo.GetInstance(it.ToAsn1Object())).ToList()
      )
    );

    enumerator.MoveNext();
    var counterSignatures = ((DerSet)enumerator.Current)
      .OfType<DerSequence>()
      .Select(sequence => CounterSignatureInfo.GetInstance(sequence))
      .ToList();

    return new MsCounterSignatureInfo(
      version,
      algorithms,
      tstInfo,
      certificateInfoTagged,
      counterSignatures);
  }

  public virtual Asn1Encodable ToPrimitive()
  {
    return new List<Asn1Encodable>
    {
      new DerInteger(Version),
      Algorithms.Cast<IEncodableInfo>().ToList().ToPrimitiveList().ToDerSet(),
      TstInfo.ToPrimitive(),
      TaggedCertificateInfo.ToPrimitive(),
      CounterSignatures.Cast<IEncodableInfo>().ToList().ToPrimitiveList().ToDerSet()
    }.ToDerSequence();
  }
}