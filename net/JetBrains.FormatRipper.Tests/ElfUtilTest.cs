﻿using System;
using System.IO;
using JetBrains.SignatureVerifier.Elf;
using NUnit.Framework;

namespace JetBrains.SignatureVerifier.Tests.Elf
{
  [TestFixture]
  public class ElfUtilTest
  {
    // Note(ww898): Some architectures don't have the difference in interpreters!!! See https://wiki.debian.org/ArchitectureSpecificsMemo for details.
    // @formatter:off
    [TestCase("busybox-static.nixos-aarch64"  , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_EXEC, ElfMachine.EM_AARCH64    , 0u                                                                                                                                  , null)]
    [TestCase("busybox-static.nixos-x86_64"   , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_EXEC, ElfMachine.EM_X86_64     , 0u                                                                                                                                  , null)]
    [TestCase("busybox.alpine-aarch64"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_AARCH64    , 0u                                                                                                                                  , "/lib/ld-musl-aarch64.so.1")]
    [TestCase("busybox.alpine-armhf"          , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_ARM        , ElfFlags.EF_ARM_EABI_VER5 | ElfFlags.EF_ARM_ABI_FLOAT_HARD                                                                          , "/lib/ld-musl-armhf.so.1")]
    [TestCase("busybox.alpine-ppc64le"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_PPC64      , ElfFlags.EF_PPC64_ABI_VER2                                                                                                          , "/lib/ld-musl-powerpc64le.so.1")]
    [TestCase("busybox.alpine-s390x"          , ElfClass.ELFCLASS64, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_S390       , 0u                                                                                                                                  , "/lib/ld-musl-s390x.so.1")]
    [TestCase("busybox.alpine-i386"           , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_386        , 0u                                                                                                                                  , "/lib/ld-musl-i386.so.1")]
    [TestCase("busybox.alpine-x86_64"         , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_X86_64     , 0u                                                                                                                                  , "/lib/ld-musl-x86_64.so.1")]
    [TestCase("coreutils.nixos-aarch64"       , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_EXEC, ElfMachine.EM_AARCH64    , 0u                                                                                                                                  , "/nix/store/c1nqsqwl9allxbxhqx3iqfxk363qrnzv-glibc-2.32-54/lib/ld-linux-aarch64.so.1")]
    [TestCase("coreutils.nixos-x86_64"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_EXEC, ElfMachine.EM_X86_64     , 0u                                                                                                                                  , "/nix/store/jsp3h3wpzc842j0rz61m5ly71ak6qgdn-glibc-2.32-54/lib/ld-linux-x86-64.so.2")]
    [TestCase("grep.android-i386"             , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_386        , 0u                                                                                                                                  , "/system/bin/linker")]
    [TestCase("grep.android-x86_64"           , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_X86_64     , 0u                                                                                                                                  , "/system/bin/linker64")]
    [TestCase("mktemp.freebsd-aarch64"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_FREEBSD, 0, ElfType.ET_EXEC, ElfMachine.EM_AARCH64    , 0u                                                                                                                                  , "/libexec/ld-elf.so.1")]
    [TestCase("mktemp.freebsd-i386"           , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_FREEBSD, 0, ElfType.ET_EXEC, ElfMachine.EM_386        , 0u                                                                                                                                  , "/libexec/ld-elf.so.1")]
    [TestCase("mktemp.freebsd-powerpc"        , ElfClass.ELFCLASS32, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_FREEBSD, 0, ElfType.ET_EXEC, ElfMachine.EM_PPC        , 0u                                                                                                                                  , "/libexec/ld-elf.so.1")]
    [TestCase("mktemp.freebsd-powerpc64"      , ElfClass.ELFCLASS64, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_FREEBSD, 0, ElfType.ET_EXEC, ElfMachine.EM_PPC64      , ElfFlags.EF_PPC64_ABI_VER2                                                                                                          , "/libexec/ld-elf.so.1")]
    [TestCase("mktemp.freebsd-powerpc64le"    , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_FREEBSD, 0, ElfType.ET_EXEC, ElfMachine.EM_PPC64      , ElfFlags.EF_PPC64_ABI_VER2                                                                                                          , "/libexec/ld-elf.so.1")]
    [TestCase("mktemp.freebsd-riscv64"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_EXEC, ElfMachine.EM_RISCV      , ElfFlags.EF_RISCV_FLOAT_ABI_DOUBLE | ElfFlags.EF_RISCV_RVC                                                                          , "/libexec/ld-elf.so.1")]
    [TestCase("mktemp.freebsd-sparc64"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_FREEBSD, 0, ElfType.ET_EXEC, ElfMachine.EM_SPARCV9    , ElfFlags.EF_SPARCV9_RMO                                                                                                             , "/libexec/ld-elf.so.1")]
    [TestCase("mktemp.freebsd-x86_64"         , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_FREEBSD, 0, ElfType.ET_EXEC, ElfMachine.EM_X86_64     , 0u                                                                                                                                  , "/libexec/ld-elf.so.1")]
    [TestCase("mktemp.gentoo-armv7a_hf-uclibc", ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_ARM        , ElfFlags.EF_ARM_EABI_VER5 | ElfFlags.EF_ARM_ABI_FLOAT_HARD                                                                          , "/lib/ld-uClibc.so.0")]
    [TestCase("mktemp.gentoo-armv4tl"         , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_ARM        , ElfFlags.EF_ARM_EABI_VER5 | ElfFlags.EF_ARM_ABI_FLOAT_SOFT                                                                          , "/lib/ld-linux.so.3")]
    [TestCase("mktemp.gentoo-hppa2.0"         , ElfClass.ELFCLASS32, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_LINUX  , 0, ElfType.ET_DYN , ElfMachine.EM_PARISC     , ElfFlags.EFA_PARISC_1_1                                                                                                             , "/lib/ld.so.1")]
    [TestCase("mktemp.gentoo-ia64"            , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_IA_64      , ElfFlags.EF_IA_64_ABI64                                                                                                             , "/lib/ld-linux-ia64.so.2")]
    [TestCase("mktemp.gentoo-m68k"            , ElfClass.ELFCLASS32, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_68K        , 0u                                                                                                                                  , "/lib/ld.so.1")]
    [TestCase("mktemp.gentoo-sparc"           , ElfClass.ELFCLASS32, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_SPARC32PLUS, ElfFlags.EF_SPARC_SUN_US3 | ElfFlags.EF_SPARC_SUN_US1 | ElfFlags.EF_SPARC_32PLUS                                                    , "/lib/ld-linux.so.2")]
    [TestCase("mktemp.gentoo-mipsel3-uclibc"  , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 1, ElfType.ET_EXEC, ElfMachine.EM_MIPS       , ElfFlags.EF_MIPS_ARCH_3 | ElfFlags.EF_MIPS_ABI_O32 | ElfFlags.EF_MIPS_32BITMODE | ElfFlags.EF_MIPS_CPIC | ElfFlags.EF_MIPS_NOREORDER, "/lib/ld-uClibc.so.0")]
    [TestCase("mktemp.openbsd-alpha"          , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_ALPHA      , 0u                                                                                                                                  , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-armv7"          , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_ARM        , ElfFlags.EF_ARM_EABI_VER5 | ElfFlags.EF_ARM_ABI_FLOAT_SOFT                                                                          , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-hppa"           , ElfClass.ELFCLASS32, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_HPUX   , 0, ElfType.ET_DYN , ElfMachine.EM_PARISC     , ElfFlags.EFA_PARISC_1_1                                                                                                             , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-i386"           , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_386        , 0u                                                                                                                                  , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-powerpc64"      , ElfClass.ELFCLASS64, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_PPC64      , ElfFlags.EF_PPC64_ABI_VER2                                                                                                          , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-landisk"        , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_SH         , ElfFlags.EF_SH2E                                                                                                                    , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-luna88k"        , ElfClass.ELFCLASS32, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_EXEC, ElfMachine.EM_88K        , 0u                                                                                                                                  , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-macppc"         , ElfClass.ELFCLASS32, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_PPC        , 0u                                                                                                                                  , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-octeon"         , ElfClass.ELFCLASS64, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_MIPS       , ElfFlags.EF_MIPS_ARCH_3 | ElfFlags.EF_MIPS_CPIC | ElfFlags.EF_MIPS_PIC | ElfFlags.EF_MIPS_NOREORDER                                 , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-sparc64"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_SPARCV9    , ElfFlags.EF_SPARCV9_RMO                                                                                                             , "/usr/libexec/ld.so")]
    [TestCase("mktemp.openbsd-x86_64"         , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_X86_64     , 0u                                                                                                                                  , "/usr/libexec/ld.so")]
    [TestCase("nologin.opensuse-i586"         , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_386        , 0u                                                                                                                                  , "/lib/ld-linux.so.2")]
    [TestCase("nologin.opensuse-ppc64le"      , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_PPC64      , ElfFlags.EF_PPC64_ABI_VER2                                                                                                          , "/lib64/ld64.so.2")]
    [TestCase("nologin.opensuse-s390x"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2MSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_S390       , 0u                                                                                                                                  , "/lib/ld64.so.1")]
    [TestCase("tempfile.ubuntu-aarch64"       , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_AARCH64    , 0u                                                                                                                                  , "/lib/ld-linux-aarch64.so.1")]
    [TestCase("tempfile.ubuntu-armhf"         , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_ARM        , ElfFlags.EF_ARM_EABI_VER5 | ElfFlags.EF_ARM_ABI_FLOAT_HARD                                                                          , "/lib/ld-linux-armhf.so.3")]
    [TestCase("tempfile.ubuntu-i386"          , ElfClass.ELFCLASS32, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_386        , 0u                                                                                                                                  , "/lib/ld-linux.so.2")]
    [TestCase("tempfile.ubuntu-x86_64"        , ElfClass.ELFCLASS64, ElfData.ELFDATA2LSB, ElfOsAbi.ELFOSABI_NONE   , 0, ElfType.ET_DYN , ElfMachine.EM_X86_64     , 0u                                                                                                                                  , "/lib64/ld-linux-x86-64.so.2")]
    // @formatter:on
    [Test]
    public void ElfInfoTest(string filename, ElfClass expectedClass, ElfData expectedData, ElfOsAbi expectedOsAbi, byte expectedOsAbiVersion, ElfType expectedType, ElfMachine expectedMachine, ElfFlags expectedFlags, string expectedInterpreter)
    {
      var elfInfo = Utils.StreamFromResource(filename, stream => ElfUtil.GetElfInfo(stream));
      Assert.IsNotNull(elfInfo);
      Assert.AreEqual(expectedClass, elfInfo.Class);
      Assert.AreEqual(expectedData, elfInfo.Data);
      Assert.AreEqual(expectedOsAbi, elfInfo.OsAbi);
      Assert.AreEqual(expectedOsAbiVersion, elfInfo.OsAbiVersion);
      Assert.AreEqual(expectedType, elfInfo.Type);
      Assert.AreEqual(expectedMachine, elfInfo.Machine);
      Assert.AreEqual(expectedFlags, elfInfo.Flags, $"Expected 0x{expectedFlags:X}, but was 0x{elfInfo.Flags:X}");
      Assert.AreEqual(expectedInterpreter, elfInfo.Interpreter);
    }
  }
}