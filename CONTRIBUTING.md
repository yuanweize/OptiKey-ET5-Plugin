# Contributing to OptiKey-ET5-Plugin

Thank you for contributing to open-source accessibility software. Every improvement helps people with physical disabilities communicate independently.

## Guiding Directives
1. **Never Commit Proprietary Binaries**:
   Under no circumstances will pull requests containing `.dll`, `.lib`, `.exe`, `.sys`, or closed proprietary headers from Tobii or other hardware vendors be accepted.
2. **Never Add Silent Fallbacks in Production**:
   Synthetic gaze generation must remain isolated in `OptiKey.ET5.Plugin.Synthetic` and test suites.
3. **Follow Architectural Decision Records (ADRs)**:
   Review all documents in `docs/adr/` before introducing architectural changes or modifying coordinate transformations.
4. **All Changes Must Build in Windows CI**:
   Run tests locally or verify the Windows GitHub Actions matrix before requesting merge.

## Development Workflow
1. Fork the repository and create a feature branch (`feature/your-feature-name`).
2. Make your changes adhering to C# conventions and existing project layout.
3. Add or update unit tests in `tests/OptiKey.ET5.Plugin.Tests/`.
4. Ensure all unit tests and reflection loader tests pass.
5. Submit a pull request with a descriptive summary of your changes.
