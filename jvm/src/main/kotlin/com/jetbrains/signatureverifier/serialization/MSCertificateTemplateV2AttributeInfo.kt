package com.jetbrains.signatureverifier.serialization

import TaggedObjectMetaInfo
import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.DLSequence
import org.bouncycastle.asn1.DLTaggedObject
import org.bouncycastle.asn1.cms.Attribute

// 1.3.6.1.4.1.311.2.1.12
@Serializable
data class MSCertificateTemplateV2AttributeInfo(
  val identifier: StringInfo,
  val content: List<List<TaggedObjectInfo>>
) : AttributeInfo {

  override fun toAttributeDLSequence(): DLSequence =
    listOf(
      identifier.toPrimitive(),
      content.map {
        it.map { it.toPrimitive() }.toDLSequence()
      }.toDLSet()
    ).toDLSequence()

  constructor(attribute: Attribute) : this(
    StringInfo.getInstance(attribute.attrType),
    attribute.attributeValues.map { attributeValue ->
      (attributeValue as DLSequence).map {
        (it as DLTaggedObject).let { outer ->
          TaggedObjectInfo(
            TaggedObjectMetaInfo(outer),
            (outer.baseObject as DLTaggedObject).let { inner ->
              TaggedObjectInfo(
                TaggedObjectMetaInfo(inner),
                StringInfo.getInstance(inner.baseObject)
              )
            }
          )
        }
      }
    }
  )
}