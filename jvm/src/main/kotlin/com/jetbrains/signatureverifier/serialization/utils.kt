package com.jetbrains.signatureverifier.serialization

import com.jetbrains.signatureverifier.bouncycastle.cms.SignerInformationStore
import org.bouncycastle.asn1.*
import org.bouncycastle.asn1.cms.ContentInfo
import org.bouncycastle.asn1.cms.SignedData
import org.bouncycastle.asn1.cms.SignerInfo
import org.bouncycastle.operator.DefaultAlgorithmNameFinder
import java.util.*

fun serializeDigestAlgorithms(algorithmsSet: ASN1Set): List<SignatureAlgorithmInfo> =
  algorithmsSet.map {
    val seq = it as DLSequence
    SignatureAlgorithmInfo(
      DefaultAlgorithmNameFinder().getAlgorithmName(it.first() as ASN1ObjectIdentifier),
      if (seq.last() is ASN1Null) null else seq.last().toString(),
      it.first().toString()
    )
  }

fun deserializeDigestAlgorithms(digestAlgorithms: List<SignatureAlgorithmInfo>): ASN1Set {
  val algorithms = ASN1EncodableVector()
  digestAlgorithms.forEach {
    algorithms.add(
      it.toDLSequence()
    )
  }

  return DLSet(algorithms)
}

fun listToDLSequence(list: List<ASN1Encodable>): DLSequence {
  val vector = ASN1EncodableVector()
  vector.addAll(list.toTypedArray())
  return DLSequence(vector)
}

fun listToDLSet(list: List<ASN1Encodable>): DLSet {
  val vector = ASN1EncodableVector()
  vector.addAll(list.toTypedArray())
  return DLSet(vector)
}

fun recreateContentInfoFromSignedData(signedData: SignedData): ContentInfo {
  val signedDataBytes = signedData.encoded
  val inputStream = ASN1InputStream(signedDataBytes)
  val asn1Object = inputStream.readObject() as ASN1Primitive

  // Wrap the ASN1Primitive object in a ContentInfo structure
  val contentInfo = ContentInfo(
    ContentInfo.signedData,
    asn1Object
  )

  return contentInfo
}

fun recreateSignerInfosFromSignerInformationStore(signerInfoStore: SignerInformationStore): ASN1Set {
  val signerInfos = signerInfoStore.signers

  // Create an ASN1EncodableVector to hold the SignerInfo objects
  val signerInfoVector = ASN1EncodableVector()

  // Iterate over the SignerInformation objects and create SignerInfo objects
  for (signerInfo in signerInfos) {
    val signerInfoASN1 = signerInfo.toASN1Structure() as SignerInfo
    signerInfoVector.add(signerInfoASN1)
  }

  // Create an ASN1Set from the ASN1EncodableVector
  val signerInfoSet = DERSet(signerInfoVector)

  return signerInfoSet
}

public fun compareBytes(
  lhs: ByteArray,
  rhs: ByteArray,
): Boolean {
  println("—————")
  println(
    String.format(
      Locale.ENGLISH,
      "Comparing called from %s",
      Thread.currentThread().stackTrace[2].toString()
    )
  )
  var same = true
  var count = 0
  lhs.forEachIndexed { index, byte ->
    val other = if (index < rhs.size) rhs[index] else 0x0
    if (byte != other) {
      count++
      println(String.format(Locale.ENGLISH, "%d %d %d", index, byte, other))
      same = false
    }
  }
  println(String.format(Locale.ENGLISH, "%d bytes differ", count))
  return same
}