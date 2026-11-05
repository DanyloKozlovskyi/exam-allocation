# 07 — Import & Export

Interfaces live in `Application/Common/Interfaces/`, implementations in
`Infrastructure/ImportExport/` (ClosedXML, CsvHelper, QuestPDF are infrastructure concerns,
like blob/mail in pal-suite). Import flow is orchestrated by MediatR commands in
`Application/Imports/Commands`. Headers, synonyms, sheet names and XML names are constants
in `Infrastructure/ImportExport/Constants/`; error codes are the `ImportErrorCode` enum;
fields are the `ParticipantField` enum. Services:

```csharp
IParticipantFileReader   // Read(Stream, fileName) → WorkbookPreview (sheets, headers, rows)
IColumnMappingSuggester  // Suggest(headers) → ColumnMapping
IParticipantRowValidator // Validate(rows, mapping, existing) → ImportValidation
IAllocationExporter      // Xlsx / Xml / Csv / Pdf (Stream output) from AllocationExportModel
```

`AllocationExportModel` is a flat read model built by `Application/Allocations/Services/AllocationExportModelBuilder` (session, centers, rooms,
seats, participants, items) so exporters never touch the DB.

## Import

### Accepted files
- `.xlsx` (ClosedXML), `.csv` (CsvHelper; delimiter auto-detected `;`/`,`/tab; encoding
  UTF-8 with/without BOM, fallback Windows-1251 if invalid UTF-8 detected).
- Max 20 MB, max 100 000 rows.

### Template (`GET /import-template`, also `samples/participants-template.xlsx`)

| Column (UA header) | Field | Required | Notes |
|---|---|---|---|
| ID | externalId | no | unique per session if provided |
| Прізвище | lastName | yes | |
| Ім'я | firstName | yes | |
| По батькові | middleName | no | |
| Група | group | no* | *required if a rule uses `group` |
| Навчальний заклад | school | no | |
| Особливі потреби | needsAccessibleSeat | no | `так/ні`, `yes/no`, `1/0`, `+/-`, empty = no |
| Примітки | notes | no | |
| *any other column* | attributes.<key> | no | key = slugified header (`Профіль` → `profil`), user can rename in mapping step |

### Mapping suggestion
Case-insensitive, trimmed, apostrophe-normalized (`'`, `’`, `ʼ`) match against synonyms:

| Field | Synonyms |
|---|---|
| lastName | прізвище, фамилия, last name, surname |
| firstName | ім'я, имя, first name, name |
| middleName | по батькові, отчество, middle name, patronymic |
| fullName | піб, фіо, full name, учасник — split into last/first/middle by spaces |
| group | група, клас, group, class |
| school | навчальний заклад, заклад, школа, університет, school |
| needsAccessibleSeat | особливі потреби, інклюзія, доступність, accessible |
| externalId | id, код, номер, № |

### Validation codes (row-level)
`RequiredMissing`, `ValueTooLong`, `BooleanUnparseable`, `DuplicateInFile`,
`DuplicateExisting` (vs DB, per `dedupeBy`), `EmptyRow` (skipped silently).
Whitespace is trimmed; multiple spaces collapsed; names kept as typed (no case change).

Commit is **transactional**: all valid rows or nothing; rows with errors are skipped only if
`skipInvalid=true` in the request.

## Export

### XLSX
- Sheet `Зведення`: session name, date, run time, objective, per-center totals.
- Sheet `Усі учасники`: № | Прізвище | Ім'я | По батькові | Група | Навч. заклад | Центр |
  Адреса | Аудиторія | Ряд | Парта | Місце — autofilter, frozen header, sorted by name.
- One sheet per room (`<Center short>–<Room>`, ≤ 31 chars, unique): list sorted by seat, and
  below it a seating grid (cells = desks, text = surnames), aisles as narrow empty columns.

### XML
Schema `samples/allocation.xsd` (owned by the import/export workstream). Shape:

```xml
<allocation xmlns="urn:exam-allocation:v1" session="…" generatedAt="…" index="0">
  <objective roomsUsed="12" cost="12.00" softPenalty="3"/>
  <center id="…" name="…" address="…" lat="50.45" lng="30.52">
    <room id="…" name="Ауд. 214" capacity="30" filled="28">
      <seat key="r0c0s0" row="1" desk="1" place="1">
        <participant id="…" externalId="…" lastName="…" firstName="…" middleName="…" group="…"/>
      </seat>
    </room>
  </center>
</allocation>
```

Generated with `XmlWriter` (streaming), validated against the XSD in tests.

### CSV
Same columns as `Усі учасники`, UTF-8 with BOM (Excel-friendly), delimiter `;` default.

### PDF (QuestPDF)
Per room: page 1 — header (center, room, date), seating grid with surnames; page 2 —
alphabetical list with seat labels and a signature column. A4 landscape for grids.
`?roomId=` limits to one room.
