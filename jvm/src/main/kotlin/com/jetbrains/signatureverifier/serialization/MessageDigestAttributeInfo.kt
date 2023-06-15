package com.jetbrains.signatureverifier.serialization

import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.ASN1EncodableVector
import org.bouncycastle.asn1.ASN1ObjectIdentifier
import org.bouncycastle.asn1.DLSequence
import org.bouncycastle.asn1.DLSet
import org.bouncycastle.asn1.cms.Attribute

// 1.2.840.113549.1.9.4
@Serializable
data class MessageDigestAttributeInfo(
  val identifier: StringInfo,
  val value: StringInfo,
) : AttributeValueInfo() {
  override fun toAttributeDLSequence(): DLSequence = listToDLSequence(
    listOf(
      identifier.toPrimitive(),
      listToDLSet(
        listOf(value.toPrimitive())
      )
    )
  )

  constructor(attribute: Attribute) : this(
    StringInfo.getInstance(attribute.attrType),
    StringInfo.getInstance(attribute.attributeValues.first())
  )
}