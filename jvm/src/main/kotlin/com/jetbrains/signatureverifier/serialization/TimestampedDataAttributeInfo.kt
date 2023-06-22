package com.jetbrains.signatureverifier.serialization

import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.DLSequence
import org.bouncycastle.asn1.cms.Attribute

// 1.3.6.1.4.1.311.10.3.28
@Serializable
data class TimestampedDataAttributeInfo(
  val identifier: TextualInfo,
  val content: List<TextualInfo>
) : AttributeInfo {

  constructor(attribute: Attribute) : this(
    TextualInfo.getInstance(attribute.attrType),
    attribute.attributeValues.map { TextualInfo.getInstance(it) }
  )

  override fun toAttributeDLSequence(): DLSequence =
    listOf(
      identifier.toPrimitive(),
      content.toPrimitiveList().toDLSet()
    ).toDLSequence()
}