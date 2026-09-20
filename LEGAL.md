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
3. **Zero Proprietary Binary Redistribution**:
   The `OptiKey-ET5-Plugin` repository and its official release assets contain **zero bytes** of Tobii proprietary binaries (`tobii_stream_engine.dll`, `.lib`), header files, or proprietary wrappers.
4. **Independent P/Invoke Declarations**:
   All C# P/Invoke definitions in this repository are written from scratch based on publicly documented C function specifications and Win32 interop best practices. No copyrighted code from Tobii's proprietary SDK examples or historical closed wrappers has been copied.
5. **Interactive Field of Use**:
   Tobii devices distinguish between *Interactive* use (driving the user interface in real time without storing gaze data) and *Analytical* use (recording, aggregating, or streaming gaze data for research/heatmaps). This project operates exclusively in the Interactive domain (`TOBII_FIELD_OF_USE_INTERACTIVE` / `TOBII_FIELD_OF_USE_STORE_OR_TRANSFER_FALSE`).

---

## 2. Engineering Inferences (ENGINEERING INFERENCE)

1. **Dynamic Runtime Binding Under System Installation**:
   When a user installs the official Tobii Experience software on their personal computer, they enter into an end-user agreement with Tobii AB granting them the right to execute Tobii software and drivers on their workstation. A third-party assistive application dynamically linking to standard installed runtime libraries on that same local computer functions equivalently to an assistive screen reader interacting with standard OS accessibility APIs.
2. **Clean Room Interface Isolation**:
   Because `OptiKey.ET5.Plugin.dll` contains only managed C# code that binds dynamically at runtime via `LoadLibrary` / `GetProcAddress`, the distributed plugin artifact is entirely free of third-party proprietary copyrights.
3. **OptiKey Contracts Type Boundary**:
   Compiling against the public contract assembly (`JuliusSweetland.OptiKey.Contracts.dll`) is a technical build dependency. The legal basis for that boundary is not determined by this repository; the plugin's own source is GPL-3.0-only.

---

## 3. Open Legal Questions (OPEN LEGAL QUESTION)

1. **Formal Commercial Endorsement by Tobii**:
   Tobii AB has not reviewed, endorsed, or officially certified `OptiKey-ET5-Plugin`. Users and developers must not represent this project as an official Tobii product.
2. **Tobii SDLA Updates on Consumer Hardware in Assistive Contexts**:
   While Tobii Dynavox explicitly produces assistive medical speech devices (e.g., I-Series, PCEye), the Eye Tracker 5 is marketed as a consumer gaming peripheral. Whether Tobii's consumer EULA permits third-party assistive software to bind to consumer drivers without a dedicated commercial OEM license is an open legal topic in the assistive technology community.
3. **Redistribution Gate**:
   Until formal clarification is obtained from Tobii AB or real-world hardware validation passes all community criteria, initial releases of this plugin are designated as **Community Testing / Pre-release** assets.

---

## 4. Draft Inquiry to Tobii AB Legal / Developer Relations

Prior to promoting this plugin beyond community pre-release testing, the maintainers will submit the following formal clarification request to `developer@tobii.com`:

```
Subject: Open Source Accessibility Plugin for OptiKey (Assistive Technology) using Tobii Eye Tracker 5

Dear Tobii Developer Relations and Legal Team,

We are developing an open-source (GPL-3.0-only) accessibility plugin for OptiKey (https://github.com/OptiKey/OptiKey), a communication and computer-access system used worldwide by individuals with severe physical disabilities, including ALS/MND locked-in users.

Due to the high cost of specialized medical eye trackers, many individuals with limited financial resources purchase the consumer Tobii Eye Tracker 5 as an affordable eye-gaze communication interface.

To adhere strictly to your intellectual property and license terms:
1. We DO NOT distribute any Tobii binaries (e.g., tobii_stream_engine.dll), headers, or libraries.
2. The plugin dynamically loads the tobii_stream_engine.dll already installed on the user's computer via official Tobii Experience software.
3. The plugin operates solely in the Interactive mode (TOBII_FIELD_OF_USE_INTERACTIVE). Gaze coordinates are consumed instantaneously to select on-screen keys and are NEVER recorded, stored, transmitted, or analyzed.
4. The project is completely non-profit and free for all users.

We would be grateful if you could confirm that this non-distributive, dynamic binding model for assistive real-time typing complies with Tobii's developer policies.

Thank you for your dedication to eye-tracking technology and accessibility.

Sincerely,
OptiKey-ET5-Plugin Maintainers
```

---

## 5. Third-Party Trademarks

- "Tobii", "Tobii Eye Tracker 5", "Tobii Dynavox", "PCEye", and "Tobii Experience" are registered trademarks of Tobii AB.
- "OptiKey" is copyright OPTIKEY LTD and Julius Sweetland.
- "Windows" is a registered trademark of Microsoft Corporation.
All references in this project are for identification, compatibility, and descriptive purposes only.
