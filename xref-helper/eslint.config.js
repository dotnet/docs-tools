const typescriptEslint = require('@typescript-eslint/eslint-plugin');
const typescriptParser = require('@typescript-eslint/parser');

module.exports = [
    {
        files: ['src/**/*.ts'],
        languageOptions: {
            parser: typescriptParser,
            parserOptions: {
                ecmaVersion: 6,
                sourceType: 'module',
                project: './tsconfig.json'
            }
        },
        plugins: {
            '@typescript-eslint': typescriptEslint
        },
        rules: {
            '@typescript-eslint/naming-convention': [
                'warn',
                {
                    'selector': 'enumMember',
                    'format': ['camelCase', 'PascalCase']
                },
                {
                    'selector': 'default',
                    'format': ['camelCase']
                },
                {
                    'selector': 'variable',
                    'format': ['camelCase', 'UPPER_CASE', 'PascalCase'],
                    'leadingUnderscore': 'allow'
                },
                {
                    'selector': 'typeLike',
                    'format': ['PascalCase']
                },
                {
                    'selector': 'import',
                    'format': ['camelCase', 'PascalCase']
                },
                {
                    'selector': 'classProperty',
                    'format': ['camelCase'],
                    'leadingUnderscore': 'allow'
                }
            ],
            'curly': 'warn',
            'eqeqeq': 'warn',
            'no-throw-literal': 'warn',
            'semi': 'off'
        }
    },
    {
        ignores: [
            'out/**',
            'dist/**',
            '**/*.d.ts',
            'node_modules/**'
        ]
    }
];
