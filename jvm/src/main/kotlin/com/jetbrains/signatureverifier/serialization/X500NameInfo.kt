package com.jetbrains.signatureverifier.serialization

import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.ASN1Primitive
import org.bouncycastle.asn1.DLSequence
import org.bouncycastle.asn1.x500.X500Name

@Serializable
data class X500NameInfo(
  val name: String,
  val rdNs: List<List<rdNInfo>>
) : EncodableInfo {
  constructor(issuer: X500Name) : this(issuer.toString(),
    issuer.rdNs.map {
      it.typesAndValues.map { tv ->
        rdNInfo(
          TextualInfo.getInstance(tv.type),
          TextualInfo.getInstance(tv.value)
        )
      }
    })

  private fun toDLSequence(): DLSequence =
    rdNs.map {
      it.map { info ->
        info.toPrimitive()
      }.toDLSet()
    }.toDLSequence()

  override fun toPrimitive(): ASN1Primitive = toDLSequence().toASN1Primitive()
}