// UI-only enums of the Imports feature (docs/10 §7).

export enum ImportWizardStep {
  Upload = 0,
  Sheet = 1,
  Mapping = 2,
  Validation = 3,
  Mode = 4,
  Commit = 5,
}

export enum ImportLimit {
  /** docs/07: max 20 MB. */
  MaxFileSizeMb = 20,
  BytesPerMb = 1048576,
  FirstHeaderRow = 1,
  MaxHeaderRow = 100,
}

/** Accepted upload extensions (docs/07). */
export enum ImportFileExtension {
  Xlsx = '.xlsx',
  Csv = '.csv',
}

export enum ImportsTestId {
  Wizard = 'import-wizard',
  FileInput = 'import-file-input',
  FileName = 'import-file-name',
  UploadButton = 'import-upload-button',
  TemplateButton = 'import-template-button',
  SheetSelect = 'import-sheet-select',
  HeaderRowField = 'import-header-row-field',
  SamplePreview = 'import-sample-preview',
  MappingField = 'import-mapping-field',
  AttributeKeyField = 'import-attribute-key-field',
  AttributeToggle = 'import-attribute-toggle',
  ValidationSummary = 'import-validation-summary',
  ValidationErrors = 'import-validation-errors',
  ModeGroup = 'import-mode-group',
  DedupeGroup = 'import-dedupe-group',
  SkipInvalid = 'import-skip-invalid',
  CommitSummary = 'import-commit-summary',
  Result = 'import-result',
  FinishButton = 'import-finish-button',
}
