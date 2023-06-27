package com.jetbrains.signatureverifier.serialization

import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.ASN1Integer
import org.bouncycastle.asn1.ASN1Primitive
import org.bouncycastle.asn1.x509.IssuerSerial
import java.math.BigInteger

@Serializable
data class IssuerSerialInfo(
  val generalNames: List<GeneralNameInfo>,
  @Serializable(BigIntegerSerializer::class)
  val serial: BigInteger,
  val issuerUID: TextualInfo?
) : EncodableInfo {

  constructor(issuer: IssuerSerial) : this(
    issuer.issuer.names.map { GeneralNameInfo(it) },
    issuer.serial.value,
    issuer.issuerUID?.let { TextualInfo.getInstance(it) }
  )

  override fun toPrimitive(): ASN1Primitive =
    listOf(
      generalNames.toPrimitiveList().toDLSequence(),
      ASN1Integer(serial),
      issuerUID?.toPrimitive()
    ).toDLSequence()
}