package com.jetbrains.signatureverifier.tests.serialization

import com.jetbrains.signatureverifier.PeFile
import com.jetbrains.signatureverifier.crypt.SignatureVerificationParams
import com.jetbrains.signatureverifier.crypt.SignedMessage
import com.jetbrains.signatureverifier.crypt.SignedMessageVerifier
import com.jetbrains.signatureverifier.crypt.VerifySignatureStatus
import com.jetbrains.signatureverifier.serialization.*
import kotlinx.coroutines.runBlocking
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import org.bouncycastle.asn1.cms.SignerInfo
import org.junit.jupiter.api.Assertions
import org.junit.jupiter.params.ParameterizedTest
import org.junit.jupiter.params.provider.Arguments
import org.junit.jupiter.params.provider.MethodSource
import java.util.*
import java.util.stream.Stream
import com.jetbrains.signatureverifier.serialization.SignerInfo as SerializableSignerInfo

class SignerInformationSerializationTests {

  @ParameterizedTest
  @MethodSource("SignedPEProvider")
  fun SignersSerializaionTest(signedPeResourceName: String) {
    getTestByteChannel("pe", signedPeResourceName).use {
      val verificationParams = SignatureVerificationParams(null, null, false, false)
      val peFile = PeFile(it)
      val signatureData = peFile.GetSignatureData()
      val signedMessage = SignedMessage.CreateInstance(signatureData)
      val signedMessageVerifier = SignedMessageVerifier(ConsoleLogger.Instance)
      runBlocking { signedMessageVerifier.VerifySignatureAsync(signedMessage, verificationParams) }

      val signedData = signedMessage.SignedData
      val signers = signedData.signerInfos.signers
      signers.forEach { signer ->
        val primitive = signer.toASN1Structure()
        val signerInfo = SerializableSignerInfo(signer)

        val json = Json.encodeToString(signerInfo)

        val recreatedSignerInfo =
          SignerInfo.getInstance(Json.decodeFromString<SerializableSignerInfo>(json).toPrimitive())

        Assertions.assertEquals(
          true,
          compareBytes(
            primitive.getEncoded("DER"),
            recreatedSignerInfo.getEncoded("DER"),
            verbose = false
          )
        )
      }

      val signerInfos = signers.map { SerializableSignerInfo(it) }
      val json = Json.encodeToString(signerInfos)


      val recreatedSignerInfos =
        Json.decodeFromString<List<SerializableSignerInfo>>(json).map { it.toPrimitive() }.toDLSet()

      Assertions.assertEquals(
        true,
        compareBytes(
          signedData.signedData.signerInfos.getEncoded("DER"),
          recreatedSignerInfos.getEncoded("DER"),
          verbose = false
        )
      )

    }
  }


  companion object {
    private const val pe_01_signed = "ServiceModelRegUI.dll"

    private const val pe_02_signed = "self_signed_test.exe"

    private const val pe_03_signed = "shell32.dll"

    private const val pe_04_signed = "IntelAudioService.exe"

    private const val pe_05_signed = "libcrypto-1_1-x64.dll"

    private const val pe_06_signed = "libssl-1_1-x64.dll"

    private const val pe_07_signed = "JetBrains.dotUltimate.2021.3.EAP1D.Checked.web.exe"

    private const val pe_08_signed = "dotnet.exe"
    private const val pe_08_not_signed = "dotnet_no_sign.exe"

    @JvmStatic
    fun SignedMsiProvider(): Stream<Arguments> {
      return Stream.of(
        Arguments.of("2dac4b.msi", VerifySignatureStatus.Valid),
      )
    }

    @JvmStatic
    fun SignedMachoProvider(): Stream<Arguments> {
      return Stream.of(
        Arguments.of("env-wrapper.x64"),
        Arguments.of("libMonoSupportW.x64.dylib"),
        Arguments.of("cat"),
        Arguments.of("JetBrains.Profiler.PdbServer"),
        Arguments.of("fat.dylib_signed"),
        Arguments.of("libhostfxr.dylib")
      )
    }

    @JvmStatic
    fun SignedPEProvider(): Stream<Arguments> {
      return Stream.of(
        Arguments.of(
          pe_01_signed
        ),
        Arguments.of(
          pe_02_signed
        ),
        Arguments.of(
          pe_03_signed
        ),
        Arguments.of(
          pe_04_signed
        ),
        Arguments.of(
          pe_05_signed
        ),
        Arguments.of(
          pe_06_signed
        ),
        Arguments.of(
          pe_07_signed
        ),
        Arguments.of(
          pe_08_signed, pe_08_not_signed
        ),
      )
    }
  }
}