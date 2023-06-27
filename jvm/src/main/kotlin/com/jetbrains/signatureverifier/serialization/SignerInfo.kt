package com.jetbrains.signatureverifier.serialization

import com.jetbrains.signatureverifier.bouncycastle.cms.SignerInformation
import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.ASN1Integer
import org.bouncycastle.asn1.ASN1ObjectIdentifier
import org.bouncycastle.asn1.ASN1Primitive
import org.bouncycastle.asn1.DLSequence
import org.bouncycastle.asn1.cms.Attribute

@Serializable
data class SignerInfo(
  val version: Int,
  val sid: SignerIdentifierInfo,
  val digestAlgorithm: AlgorithmInfo,
  val authenticatedAttributes: List<AttributeInfo>,
  val digestEncryptionAlgorithm: AlgorithmInfo,
  val encryptedDigest: TextualInfo,
  val unauthenticatedAttributes: List<AttributeInfo>?
) : EncodableInfo {

  constructor(signer: SignerInformation) : this(
    signer.version,
    SignerIdentifierInfo(signer.sID),
    AlgorithmInfo(signer.digestAlgorithmID),
    signer.toASN1Structure().authenticatedAttributes.map {
      AttributeInfo.getInstance(
        signer.signedAttributes?.get(
          (it as DLSequence).first() as ASN1ObjectIdentifier
        ) as Attribute
      )
    },
    AlgorithmInfo(signer.encryptionAlgorithm),
    TextualInfo.getInstance(signer.toASN1Structure().encryptedDigest),
    signer.toASN1Structure().unauthenticatedAttributes?.map {
      AttributeInfo.getInstance(
        signer.unsignedAttributes?.get(
          (it as DLSequence).first() as ASN1ObjectIdentifier
        ) as Attribute
      )
    }
  )

  override fun toPrimitive(): ASN1Primitive =
    listOf(
      ASN1Integer(version.toLong()),
      sid.toPrimitive(),
      digestAlgorithm.toPrimitive(),
      TaggedObjectInfo.getTaggedObjectWithMetaInfo(
        TaggedObjectMetaInfo(0, 2),
        authenticatedAttributes.toPrimitiveList().toDLSet()
      ),
      digestEncryptionAlgorithm.toPrimitive(),
      encryptedDigest.toPrimitive(),
      unauthenticatedAttributes?.let { attributes ->
        TaggedObjectInfo.getTaggedObjectWithMetaInfo(
          TaggedObjectMetaInfo(1, 2),
          attributes.toPrimitiveList().toDLSet()
        )
      }
    ).toDLSequence()
}


