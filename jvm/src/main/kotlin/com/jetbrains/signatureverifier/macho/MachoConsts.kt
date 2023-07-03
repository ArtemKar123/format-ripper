package com.jetbrains.signatureverifier.macho

object MachoConsts {
  val FAT_MAGIC = 0xCAFEBABE
  val FAT_MAGIC_64 = 0xCAFEBABF
  val FAT_CIGAM = 0xBEBAFECA
  val FAT_CIGAM_64 = 0xBFBAFECA
  val MH_MAGIC = 0xFEEDFACE
  val MH_MAGIC_64 = 0xFEEDFACF
  val MH_CIGAM = 0xCEFAEDFE
  val MH_CIGAM_64 = 0xCFFAEDFE
  val CSSLOT_CODEDIRECTORY = 0L // slot index for CodeDirectory
  val CSSLOT_CMS_SIGNATURE = 0x10000L // slot index for CmsSignedData
  val CSSLOT_REQUIREMENTS = 2L
  val CSMAGIC_REQUIREMENTS = 0xfade0c01 // slot index for Requirements
  val CSMAGIC_CMS_SIGNATURE = 0xfade0b01
  val CSMAGIC_BLOBWRAPPER = 0xfade0b01 //used for the cms blob
  val CSMAGIC_CODEDIRECTORY = 0xfade0c02 //used for the CodeDirectory blob
  val LC_SEGMENT = 1
  val LC_SEGMENT_64 = 0x19
  val LC_CODE_SIGNATURE = 0x1D
  val LINKEDIT_SEGMENT_NAME = arrayOf<Byte>(
    0x5f, 0x5f, 0x4c, 0x49, 0x4e, 0x4b, 0x45, 0x44, 0x49, 0x54, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  ).toByteArray()
}

