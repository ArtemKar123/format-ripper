package com.jetbrains.signatureverifier.serialization

import TaggedObjectMetaInfo
import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.DLSequence
import org.bouncycastle.asn1.DLTaggedObject
import org.bouncycastle.asn1.cms.Attribute

@Serializable
data class MsCounterSignAttributeInfo(
  val identifier: StringInfo,
  val contentIdentifier: List<StringInfo>,
  val content: List<TaggedObjectInfo>
) : AttributeInfo {

  override fun toAttributeDLSequence(): DLSequence = listOf(
    identifier.toPrimitive(),

    contentIdentifier.zip(content).map {
      listOf(
        it.first.toPrimitive(),
        it.second.toPrimitive()
      ).toDLSequence()
    }.toDLSet()

  ).toDLSequence()

  constructor(attribute: Attribute) : this(
    StringInfo.getInstance(attribute.attrType),
    attribute.attributeValues.map {
      StringInfo.getInstance((it as DLSequence).first())
    },
    attribute.attributeValues.map {
      (it as DLSequence).last().let { sequence ->
        TaggedObjectInfo(
          TaggedObjectMetaInfo(sequence as DLTaggedObject),
          MsCounterSignatureInfo.getInstance(sequence.baseObject as DLSequence)
        )
      }
    }
  )
}