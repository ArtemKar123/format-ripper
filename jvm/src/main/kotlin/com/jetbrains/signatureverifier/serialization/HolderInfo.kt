package com.jetbrains.signatureverifier.serialization

import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.ASN1Primitive
import org.bouncycastle.asn1.x509.Holder

@Serializable
data class HolderInfo(
  val baseCertificateId: IssuerSerialInfo?,
  val entityName: List<GeneralNameInfo>?,
  val objectDigestInfo: ObjectDigestInfo?,
  val version: Int
) : EncodableInfo {

  constructor(holder: Holder) : this(
    holder.baseCertificateID?.let { IssuerSerialInfo(it) },
    holder.entityName?.let { it.names.map { name -> GeneralNameInfo(name) } },
    holder.objectDigestInfo?.let { ObjectDigestInfo(it) },
    holder.version
  )

  override fun toPrimitive(): ASN1Primitive = when (version) {
    0 -> when (entityName) {
      null ->
        TaggedObjectInfo.getTaggedObjectWithMetaInfo(
          TaggedObjectMetaInfo(0, 1),
          baseCertificateId!!.toPrimitive()
        ).toASN1Primitive()

      else ->
        TaggedObjectInfo.getTaggedObjectWithMetaInfo(
          TaggedObjectMetaInfo(1, 1),
          entityName.toPrimitiveList().toDLSequence()
        ).toASN1Primitive()
    }

    1 -> listOf(
      baseCertificateId?.let {
        TaggedObjectInfo.getTaggedObjectWithMetaInfo(
          TaggedObjectMetaInfo(0, 2),
          it.toPrimitive()
        )
      },
      entityName?.let {
        TaggedObjectInfo.getTaggedObjectWithMetaInfo(
          TaggedObjectMetaInfo(1, 2),
          it.toPrimitiveList().toDLSequence()
        )
      },
      objectDigestInfo?.let {
        TaggedObjectInfo.getTaggedObjectWithMetaInfo(
          TaggedObjectMetaInfo(2, 2),
          it.toPrimitive()
        )
      },
    ).toDLSequence()

    else -> throw IllegalArgumentException("Unexpected version $version")
  }
}