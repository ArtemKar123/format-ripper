package com.jetbrains.signatureverifier.serialization

import kotlinx.serialization.Serializable
import org.bouncycastle.asn1.ASN1Primitive
import org.bouncycastle.asn1.ASN1Set
import org.bouncycastle.asn1.DERBitString
import org.bouncycastle.asn1.DLSequence
import org.bouncycastle.asn1.x509.Certificate
import org.bouncycastle.cert.X509CertificateHolder
import org.bouncycastle.util.Store

@Serializable
data class CertificateInfo(
  val tbsCertificateInfo: TBSCertificateInfo,
  val signatureAlgorithm: AlgorithmInfo,
  val signatureData: StringInfo
) : EncodableInfo {
  companion object {
    fun getInstance(certificateHolder: X509CertificateHolder) = CertificateInfo(
      TBSCertificateInfo.getInstance(certificateHolder),
      AlgorithmInfo(certificateHolder.signatureAlgorithm),
      StringInfo.getInstance(DERBitString(certificateHolder.signature))
    )

  }

  private fun toDlSequence(): DLSequence =
    listOf(
      tbsCertificateInfo.toPrimitive(),
      signatureAlgorithm.toPrimitive(),
      signatureData.toPrimitive()
    ).toDLSequence()

  fun toX509CertificateHolder() = X509CertificateHolder(
    Certificate.getInstance(toPrimitive())
  )

  override fun toPrimitive(): ASN1Primitive =
    toDlSequence().toASN1Primitive()
}

fun recreateCertificatesFromStore(store: Store<X509CertificateHolder>): ASN1Set =
  store.getMatches(null).toList().map { it.toASN1Structure() }.toDLSet()