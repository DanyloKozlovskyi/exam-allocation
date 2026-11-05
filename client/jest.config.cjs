// Some MSW dependencies ship ESM only; they are transpiled to CommonJS by ts-jest (allowJs).
const esmOnlyPackages = ['rettime', 'until-async', '@open-draft', '@mswjs'];

/** @type {import('jest').Config} */
module.exports = {
  testEnvironment: 'jest-fixed-jsdom',
  roots: ['<rootDir>/src'],
  setupFilesAfterEnv: ['<rootDir>/src/setupTests.ts'],
  moduleFileExtensions: ['ts', 'tsx', 'js', 'mjs', 'cjs', 'json'],
  transform: {
    '^.+\\.tsx?$': ['ts-jest', { tsconfig: '<rootDir>/tsconfig.jest.json' }],
    '^.+\\.m?js$': ['ts-jest', { tsconfig: '<rootDir>/tsconfig.jest.json' }],
  },
  transformIgnorePatterns: [`/node_modules/(?!(${esmOnlyPackages.join('|')})/)`],
  moduleNameMapper: {
    '\\.(css|less|scss)$': 'identity-obj-proxy',
  },
  testEnvironmentOptions: {
    customExportConditions: [''],
  },
};
