package com.jetbrains.signatureverifier.serialization

import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.cms.Attribute

@Serializable
data class AppleDeveloperCertificateAttribute(
  override val identifier: TextualInfo,
  val content: List<TextualInfo>
) : AttributeInfo {

  constructor(attribute: Attribute) : this(
    TextualInfo.getInstance(attribute.attrType),
    attribute.attributeValues.map {
      TextualInfo.getInstance(it)
    }
  )

  override fun getPrimitiveContent() = content.toPrimitiveList().toDLSet()
}