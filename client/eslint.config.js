import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  { ignores: ['dist', 'node_modules', 'coverage', 'public', 'src/api/generated', 'playwright-report', 'test-results'] },
  {
    files: ['**/*.{ts,tsx,mts}'],
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
      'no-unused-vars': 'off',
      '@typescript-eslint/no-unused-vars': 'error',
      // docs/10 §7: enums over types — no string-literal union types.
      'no-restricted-syntax': [
        'error',
        {
          selector: 'TSUnionType > TSLiteralType',
          message: 'Use an enum instead of a string-literal union type (docs/10-conventions.md §7).',
        },
      ],
      '@typescript-eslint/prefer-enum-initializers': 'error',
      // docs/10 §7: no magic numbers — use named constants.
      'no-magic-numbers': 'off',
      '@typescript-eslint/no-magic-numbers': [
        'error',
        {
          ignore: [-1, 0, 1],
          ignoreArrayIndexes: true,
          ignoreEnums: true,
          ignoreNumericLiteralTypes: true,
          ignoreReadonlyClassProperties: true,
          ignoreTypeIndexes: true,
          ignoreDefaultValues: true,
        },
      ],
    },
  },
  {
    files: ['**/*.test.{ts,tsx}', 'src/setupTests.ts', 'e2e/**/*.ts'],
    rules: {
      '@typescript-eslint/no-magic-numbers': 'off',
    },
  },
  {
    files: ['*.config.{js,cjs,mts}', 'jest.config.cjs'],
    languageOptions: { globals: globals.node },
    rules: {
      '@typescript-eslint/no-require-imports': 'off',
    },
  }
);
