package com.jetbrains.signatureverifier.serialization

import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.ASN1Primitive
@Serializable
sealed interface EncodableInfo {
  fun toPrimitive(): ASN1Primitive
}