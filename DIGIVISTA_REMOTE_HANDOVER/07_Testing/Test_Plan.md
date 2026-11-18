# Digivista Remote — Master Test Plan & Verification Matrix

## 1. Scope & Strategy
The Digivista Remote test suite employs a multi-tiered verification strategy spanning automated unit tests, cryptographic verification, subsystem integration, and end-to-end loopback session simulations.

---

## 2. Test Execution Commands

```powershell
# Execute complete test suite across all projects
dotnet test AetherDesk.slnx --logger "console;verbosity=detailed"
```

---

## 3. Test Suites Overview

### 3.1 AetherDesk.Core.Tests (Unit & Cryptographic Verification)
- **`SecurityTests.cs`:**
  - `EcdhKeyExchange_DerivesMatchingSharedSecretBetweenPeers`
  - `AesGcmCipher_EncryptsAndDecryptsAccurately`
  - `AesGcmCipher_DetectsTampering`
  - `PasswordHasher_HashesAndVerifiesAccurately`
  - `SecurityAuditLogger_LogsEventsAndPersistsToDatabase`
  - `SessionPermissions_BitmaskOperationsAndSerialization_WorkAccurately`
- **`IdentityAndCompressionTests.cs`:**
  - `DeviceIdGenerator_ProducesFormattedNineDigitId`
  - `TileDiffEngine_DetectsDirtyBlocksAndComputesAccurateHashes`
- **`ProtocolTests.cs`:**
  - `AdbpPacket_SerializesAndDeserializesWithoutCorruption`
  - `ScreenTileBatchMsg_CompressesAndDecompressesAccurately`
- **`DatabaseTests.cs`:**
  - `DatabaseService_StoresAndRetrievesSettings`
  - `DatabaseService_RecordsRecentSessionsAndTrustedDevices`
- **`DistributionTests.cs`:**
  - `CleanSourceArchive_ExistsAndMatchesSha256Checksum` (Verifies exclusion of `.git/`, `bin/`, `obj/`, `*.user`, `*.env`)

### 3.2 AetherDesk.Integration.Tests (Subsystem Communication)
- `SignalingClient_ConnectsAndRegistersDeviceId`
- `SignalingClient_RoutesConnectionRequestsBetweenPeers`

### 3.3 AetherDesk.E2E.Tests (End-to-End Session Simulation)
- `E2ESession_EstablishesE2EEConnectionAndExchangesKeyFrames`
- `E2ESession_InjectsMouseAndKeyboardEventsOverEncryptedStream`
- `E2ESession_SynchronizesClipboardAcrossEncryptedChannel`
- `E2ESession_TransfersFileChunksAndVerifiesSha256Checksum`
- `E2ESession_HostRevokesMouseControlAndDropsUnauthorizedPackets`
- `E2ESession_HostRevokesFileTransferAndRejectsInitRequest`
- `E2ESession_UnattendedAuthenticationFlowSucceedsWithValidCredentials`
- `E2ESession_UnattendedAuthenticationFailsWithInvalidPassword`
- `E2ESession_InSessionChatExchangesMessagesReliably`
- `E2ESession_HostTerminatesSessionAndViewerReceivesDisconnectNotice`

---

*Prepared for Digivista Infotech.*
