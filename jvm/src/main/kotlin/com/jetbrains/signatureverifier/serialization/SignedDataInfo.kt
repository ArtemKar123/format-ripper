package com.jetbrains.signatureverifier.serialization

import com.jetbrains.signatureverifier.bouncycastle.cms.CMSSignedData
import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.ASN1Integer
import org.bouncycastle.asn1.ASN1Primitive
import org.bouncycastle.util.CollectionStore
import java.math.BigInteger

@Serializable
data class SignedDataInfo(
  @Serializable(BigIntegerSerializer::class)
  val version: BigInteger,
  val digestAlgorithmsInfo: List<AlgorithmInfo>,
  val encapContentInfo: EncapContentInfo,
  val certificates: List<CertificateInfo>,
  val crls: List<EncodableInfo>?,
  val signerInfos: List<SignerInfo>
) : EncodableInfo {

  constructor(signedData: CMSSignedData) : this(
    signedData.signedData.version.value,
    signedData.digestAlgorithmIDs.map { AlgorithmInfo(it) },
    EncapContentInfo.getInstance(signedData.signedData.encapContentInfo),
    signedData.certificates.getMatches(null).toList().map { certificateHolder ->
      CertificateInfo.getInstance(certificateHolder)
    },
    signedData.signedData.crLs?.map { it.toASN1Primitive().toEncodableInfo() },
    signedData.signerInfos.signers.map { SignerInfo(it) }
  )

  override fun toPrimitive(): ASN1Primitive =
    listOf(
      ASN1Integer(version),
      digestAlgorithmsInfo.toPrimitiveList().toDLSet(),
      encapContentInfo.toPrimitive(),
      TaggedObjectInfo.getTaggedObjectWithMetaInfo(
        TaggedObjectMetaInfo(0, 2),
        recreateCertificatesFromStore(
          CollectionStore(certificates.map {
            it.toX509CertificateHolder()
          })
        ).toASN1Primitive()
      ),
      crls?.toPrimitiveList()?.toDLSet()?.toASN1Primitive(),
      signerInfos.toPrimitiveList().toDLSet().toASN1Primitive()
    ).toDLSequence()
}