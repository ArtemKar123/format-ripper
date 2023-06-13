package com.jetbrains.signatureverifier.serialization

import TaggedObjectMetaInfo
import com.google.gson.Gson
import com.jetbrains.signatureverifier.PeFile
import com.jetbrains.signatureverifier.crypt.SignatureVerificationParams
import com.jetbrains.signatureverifier.crypt.SignedMessage
import org.bouncycastle.asn1.*
import org.bouncycastle.asn1.x509.Certificate
import org.bouncycastle.cert.X509CertificateHolder
import org.bouncycastle.util.CollectionStore
import org.bouncycastle.util.Store

data class CertificateInfo(
  val tbsCertificateInfo: TBSCertificateInfo,
  val signatureAlgorithm: SignatureAlgorithmInfo,
  val signatureData: StringInfo
) : EncodableInfo {
  private fun toDlSequence(): DLSequence = listToDLSequence(
    listOf(
      tbsCertificateInfo.toPrimitive(),
      signatureAlgorithm.toPrimitive(),
      signatureData.toPrimitive()
    )
  )

  override fun toPrimitive(): ASN1Primitive =
    toDlSequence().toASN1Primitive()

}

fun recreateCertificatesFromStore(store: Store<X509CertificateHolder>): ASN1Set =
  listToDLSet(store.getMatches(null).toList().map { it.toASN1Structure() })

fun testCertificateRecreation() {
  listOf(
    "dotnet.exe",
    "shell32.dll",
    "ServiceModelRegUI.dll",
    "self_signed_test.exe"
  ).forEach { filename ->
    val result = getTestByteChannel("pe", filename).use {
      val verificationParams = SignatureVerificationParams(null, null, false, false)
      val peFile = PeFile(it)
      val signatureData = peFile.GetSignatureData()
      val signedMessage = SignedMessage.CreateInstance(signatureData)

      val signedData = signedMessage.SignedData
      val ss = signedData.signedData

//    val otherInfo = signedData.getOtherRevocationInfo()

      val serializedDigestAlgorithms = serializeDigestAlgorithms(ss.digestAlgorithms)
      val deserializedDigestAlgorithms = deserializeDigestAlgorithms(serializedDigestAlgorithms)

      val contentInfo = signedData.contentInfo

      val gson = Gson()
//    val json = gson.toJson(ss.encapContentInfo)
//    val recreatedContentInfo = gson.fromJson(json, ContentInfo::class.java)

      val beautifiedCertificates = signedData.certificates.getMatches(null).toList()

      val recreatedList = beautifiedCertificates.map { certificateHolder ->

        val criticalIds = certificateHolder.extensions.criticalExtensionOIDs
        val extensionInfos = certificateHolder.extensions.extensionOIDs.map {
          val extension = certificateHolder.extensions.getExtension(it)
          ExtensionInfo(
            StringInfo.getInstance(extension.extnId),
            criticalIds.contains(extension.extnId),
            StringInfo.getInstance(extension.extnValue)
          )
        }

        val tbsInfo = TBSCertificateInfo(
          certificateHolder.versionNumber,
          certificateHolder.serialNumber.toString(),
          SignatureAlgorithmInfo(certificateHolder.signatureAlgorithm),
          IssuerInfo(certificateHolder.issuer),
          certificateHolder.notBefore,
          certificateHolder.notAfter,
          IssuerInfo(certificateHolder.subject),
          SignatureAlgorithmInfo(certificateHolder.subjectPublicKeyInfo.algorithm),
          StringInfo.getInstance(certificateHolder.subjectPublicKeyInfo.publicKeyData),
          extensionInfos
        )

        val certificateInfo = CertificateInfo(
          tbsInfo,
          SignatureAlgorithmInfo(certificateHolder.signatureAlgorithm),
          StringInfo.getInstance(DERBitString(certificateHolder.signature))
        )

        val json = gson.toJson(certificateInfo)
        val certificateInfoFromJson = gson.fromJson(json, certificateInfo::class.java)

        val recreatedCertificateHolder = X509CertificateHolder(
          Certificate.getInstance(certificateInfoFromJson.toPrimitive())
        )

        println(recreatedCertificateHolder.equals(certificateHolder))
        compareBytes(recreatedCertificateHolder.encoded, certificateHolder.encoded)
        recreatedCertificateHolder
      }

      val recreatedStore = CollectionStore(recreatedList)
      val recreatedCertificates = recreateCertificatesFromStore(recreatedStore)

      compareBytes(recreatedCertificates.getEncoded("DER"), ss.certificates.getEncoded("DER"))
      println()
    }
  }

}