[English](LEGAL.md) | [简体中文](LEGAL.zh-CN.md)

# Legal Notice and Licensing Provenance

> **Document Status**: Active  
> **Applicable Project**: OptiKey-ET5-Plugin  
> **Last Verified**: September 2026

---

## 1. Verified Facts (VERIFIED FACT)

1. **OptiKey Core Licensing**:
   OptiKey is licensed under the GNU General Public License v3.0 (the upstream license terms must be checked independently). All original code developed for `OptiKey-ET5-Plugin` is distributed under GPL-3.0-only.
2. **Historical Context of Tobii Support in OptiKey**:
   Upstream OptiKey previously included built-in Tobii Dynavox support. In git commit `81c88f5a96ac91ac0283357646a4a4bbe3848f84` (July 2023), the OptiKey maintainers removed Tobii support because the repository directly bundled `tobii_stream_engine.dll` and Tobii's Limited Software Development License Agreement (`SoftwareDevelopmentLicenseAgreement_Limited_en.pdf`), which prohibited redistribution within third-party open-source code packages.
3. **Zero Proprietary Binary Redistribution in the Reviewed Baseline**:
   The reviewed `OptiKey-ET5-Plugin` Git tree and downloaded main-CI package contain no Tobii proprietary binaries (`tobii_stream_engine.dll`, `.lib`), header files, SDK archives, or copied proprietary wrappers. No GitHub Release exists.
4. **Independent P/Invoke Declarations**:
   The C# declarations were authored independently; no copyrighted Tobii SDK example or historical OptiKey wrapper code was copied. Their exact compatibility with the current supported Stream Engine ABI is not yet proven.
5. **Intended Data Handling**:
   The implementation selects `TOBII_FIELD_OF_USE_INTERACTIVE` and is designed not to store or transmit gaze data. This technical choice does not itself establish a license right to use Eye Tracker 5 or Tobii software for AAC/accessibility.

---

## 2. Engineering Inferences (ENGINEERING INFERENCE)

1. **Dynamic Runtime Binding Under System Installation**:
   When a user installs the official Tobii Experience software on their personal computer, they enter into an end-user agreement with Tobii AB granting them the right to execute Tobii software and drivers on their workstation. A third-party assistive application dynamically linking to standard installed runtime libraries on that same local computer functions equivalently to an assistive screen reader interacting with standard OS accessibility APIs.
2. **Clean Room Interface Isolation**:
   The reviewed managed plugin and CI package contain no Tobii binary, header, SDK archive, or copied proprietary wrapper. This is a content audit result, not a conclusion about API or license permission.
3. **OptiKey Contracts Type Boundary**:
   Compiling against the public contract assembly (`JuliusSweetland.OptiKey.Contracts.dll`) is a technical build dependency. The legal basis for that boundary is not determined by this repository; the plugin's own source is GPL-3.0-only.

---

## 3. Open Legal Questions (OPEN LEGAL QUESTION)

1. **Formal Commercial Endorsement by Tobii**:
   Tobii AB has not reviewed, endorsed, or officially certified `OptiKey-ET5-Plugin`. Users and developers must not represent this project as an official Tobii product.
2. **Tobii SDLA Updates on Consumer Hardware in Assistive Contexts**:
   While Tobii Dynavox explicitly produces assistive medical speech devices (e.g., I-Series, PCEye), the Eye Tracker 5 is marketed as a consumer gaming peripheral. Whether Tobii's consumer EULA permits third-party assistive software to bind to consumer drivers without a dedicated commercial OEM license is an open legal topic in the assistive technology community.
3. **Redistribution Gate**:
   No hardware-enabled package should be released until Tobii supplies written clarification for this exact use and the engineering validation gates are satisfied.

## 3A. Current Official Conflict (BLOCKER, NOT LEGAL ADVICE)

The current [Tobii Streams SDK page](https://www.tobii.com/products/integration/tobii-streams-sdk) identifies Stream Engine Client 7.2, requires a development-license subscription, and says that Eye Tracker 5 without the `L` is a gaming device that cannot be used for development purposes.

Tobii's current [software-development license overview](https://www.tobii.com/products/integration/tobii-sdk-license) says distribution/commercialization uses an applicable Tobii agreement and identifies Medical Use as an additional license option. A published [Tobii SDLA version 2.0](https://developer.tobii.com/vr/sdla/) expressly lists Assistive and Alternative Communication solutions as Medical Use and excludes Medical Use under that limited license.

These official pages do not establish permission for this project. Engineering research may remain fail-closed, but public distribution or hardware activation requires written clarification from Tobii for the exact non-`L` ET5, open-source, real-time accessibility-input scenario.

---

## 4. Draft Inquiry to Tobii AB Legal / Developer Relations

Before any hardware-enabled distribution, the maintainers should submit the following clarification request through Tobii's current contact channel:

```
Subject: Supported API and license for open-source OptiKey accessibility input with Eye Tracker 5

We are developing a free open-source accessibility input plugin for OptiKey using Tobii Eye Tracker 5. Gaze data is used only in real time as interactive computer input. It is not stored, transmitted, or analyzed. Users install the official Tobii Experience/runtime themselves and the project does not redistribute Tobii binaries.

Your current Streams SDK page says Eye Tracker 5 without the L cannot be used for development, and published licensing material treats AAC as Medical Use. Which currently supported API/SDK, device model, and license terms—if any—permit this exact accessibility use and public open-source distribution? If a separate written agreement or different Tobii hardware is required, please identify it.
```

---

## 5. Third-Party Trademarks

- "Tobii", "Tobii Eye Tracker 5", "Tobii Dynavox", "PCEye", and "Tobii Experience" are registered trademarks of Tobii AB.
- "OptiKey" is copyright OPTIKEY LTD and Julius Sweetland.
- "Windows" is a registered trademark of Microsoft Corporation.
All references in this project are for identification, compatibility, and descriptive purposes only.
