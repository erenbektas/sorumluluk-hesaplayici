# Changelog

## [2.1.0] - 2026-04-13

### Added
- Flexible number parser for PDF-imported exchange rates (`TryParseImportedNumber`), correctly handling both dot-decimal (`1.2345`) and Turkish comma-decimal (`1.000,99`) formats
- PDF guardrails: file size limit (10 MB), page count limit (10 pages), extracted text limit (500K chars)
- Early-exit optimization in PDF line scanning once both EUR/USD and SDR/USD rates are found
- Progress bar on the upload page during PDF loading
- New test file `PdfExchangeRateParserTests` for guardrail validation
- Expanded `TurkishNumberHelperTests` with imported-number parsing coverage

### Changed
- PDF rate extraction now uses `ParseImported()` instead of `Parse()`, fixing incorrect parsing of dot-decimal rates from TCMB PDFs (e.g. `1.2345` was previously treated as `12345`)

## [2.0.0] - 2026-04-13

### Added
- Initial stable release
- SDR liability calculator with CMR, sea, and custom transport types
- TCMB exchange rate PDF import with iText7
- Turkish number formatting and parsing
- RTF/HTML/plain text clipboard export
- Dark theme UI
- Inno Setup installer
