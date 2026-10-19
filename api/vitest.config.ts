import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    include: ['pruebas/**/*.test.ts'],
    // Las pruebas comparten una base MySQL: una a la vez.
    fileParallelism: false,
    testTimeout: 20_000,
    hookTimeout: 60_000,
  },
})
